// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class SysVfs
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or "." or "./") normalizedPath = "/";

        if(!normalizedPath.StartsWith('/')) normalizedPath = "/" + normalizedPath;

        // Remove trailing slash (except for root)
        if(normalizedPath.Length > 1 && normalizedPath.EndsWith('/')) normalizedPath = normalizedPath[..^1];

        Dictionary<string, ushort> dirContents;
        ushort                     inodeNumber;

        if(normalizedPath == "/")
        {
            dirContents = _rootDirectoryCache;
            inodeNumber = SYSV_ROOT_INO;
        }
        else
        {
            // Walk the path from root
            ErrorNumber errno = ResolvePath(normalizedPath, out inodeNumber);

            if(errno != ErrorNumber.NoError) return errno;

            // Check if it's a directory
            errno = ReadInode(inodeNumber, out Inode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((inode.di_mode & S_IFMT) != S_IFDIR) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryContents(inodeNumber, out dirContents);

            if(errno != ErrorNumber.NoError) return errno;
        }

        var contents = dirContents.Keys.ToList();
        contents.Sort(StringComparer.Ordinal);

        node = new SysVDirNode
        {
            Path        = normalizedPath,
            Position    = 0,
            InodeNumber = inodeNumber,
            Contents    = contents.ToArray()
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not SysVDirNode myNode) return ErrorNumber.InvalidArgument;

        myNode.Position = -1;
        myNode.Contents = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not SysVDirNode myNode) return ErrorNumber.InvalidArgument;

        if(myNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(myNode.Position >= myNode.Contents.Length) return ErrorNumber.NoError;

        filename = myNode.Contents[myNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Resolves a path to its inode number by walking from the root directory</summary>
    /// <param name="path">Absolute path starting with /</param>
    /// <param name="inodeNumber">The resolved inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ResolvePath(string path, out ushort inodeNumber)
    {
        inodeNumber = SYSV_ROOT_INO;

        if(path is "/" or "") return ErrorNumber.NoError;

        string[] segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

        Dictionary<string, ushort> currentDir = _rootDirectoryCache;

        for(var i = 0; i < segments.Length; i++)
        {
            if(!currentDir.TryGetValue(segments[i], out ushort childIno))
            {
                AaruLogging.Debug(MODULE_NAME, "Path segment '{0}' not found", segments[i]);

                return ErrorNumber.NoSuchFile;
            }

            inodeNumber = childIno;

            // If this is the last segment, we're done
            if(i == segments.Length - 1) return ErrorNumber.NoError;

            // Otherwise, this must be a directory — read its contents for the next iteration
            ErrorNumber errno = ReadInode(childIno, out Inode childInode);

            if(errno != ErrorNumber.NoError) return errno;

            if((childInode.di_mode & S_IFMT) != S_IFDIR) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryContents(childIno, out currentDir);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the contents of a directory from its inode</summary>
    /// <param name="inodeNumber">The inode number of the directory</param>
    /// <param name="entries">Dictionary of filename to inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryContents(ushort inodeNumber, out Dictionary<string, ushort> entries)
    {
        entries = new Dictionary<string, ushort>();

        ErrorNumber errno = ReadInode(inodeNumber, out Inode inode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode {0}: {1}", inodeNumber, errno);

            return errno;
        }

        if((inode.di_mode & S_IFMT) != S_IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Inode {0} is not a directory", inodeNumber);

            return ErrorNumber.NotDirectory;
        }

        int dirSize   = inode.di_size;
        int entrySize = 2 + DIRSIZE; // ushort d_ino + byte[14] d_name = 16
        var bytesRead = 0;

        // Read direct blocks (0-9)
        for(var i = 0; i < 10 && bytesRead < dirSize; i++)
        {
            uint blockNumber = Read3ByteAddress(inode.di_addr, i);

            if(blockNumber == 0) continue;

            errno = ReadBlock(blockNumber, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", blockNumber, errno);

                continue;
            }

            var blockOffset = 0;

            while(blockOffset + entrySize <= blockData.Length && bytesRead < dirSize)
            {
                ushort dIno;

                switch(_bytesex)
                {
                    case Bytesex.BigEndian:
                        dIno = (ushort)(blockData[blockOffset] << 8 | blockData[blockOffset + 1]);

                        break;
                    default:
                        dIno = (ushort)(blockData[blockOffset] | blockData[blockOffset + 1] << 8);

                        break;
                }

                if(dIno != 0)
                {
                    var nameBytes = new byte[DIRSIZE];
                    Array.Copy(blockData, blockOffset + 2, nameBytes, 0, DIRSIZE);
                    string name = StringHandlers.CToString(nameBytes, _encoding);

                    if(!string.IsNullOrEmpty(name) && name is not ("." or "..")) entries[name] = dIno;
                }

                blockOffset += entrySize;
                bytesRead   += entrySize;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Read {0} entries from directory inode {1}", entries.Count, inodeNumber);

        return ErrorNumber.NoError;
    }
}