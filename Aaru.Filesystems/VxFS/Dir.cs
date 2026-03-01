// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath is "/")
        {
            if(_rootDirectoryCache.Count == 0) return ErrorNumber.NoSuchFile;

            node = new VxFsDirNode
            {
                Path     = "/",
                Position = 0,
                Entries  = _rootDirectoryCache.Keys.ToArray()
            };

            return ErrorNumber.NoError;
        }

        string stripped = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                              ? normalizedPath[1..]
                              : normalizedPath;

        string[] components = stripped.Split('/', StringSplitOptions.RemoveEmptyEntries);

        if(components.Length == 0) return ErrorNumber.InvalidArgument;

        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < components.Length; i++)
        {
            string component = components[i];

            if(component is "." or "..") continue;

            if(!currentEntries.TryGetValue(component, out uint inodeNumber)) return ErrorNumber.NoSuchFile;

            // Read inode from primary ilist and verify it's a directory
            ErrorNumber errno = ReadInodeFromInode(_ilistInode, inodeNumber, out DiskInode inode);

            if(errno != ErrorNumber.NoError) return errno;

            if((VxfsFileType)(inode.vdi_mode & VXFS_TYPE_MASK) != VxfsFileType.Dir) return ErrorNumber.NotDirectory;

            // Read subdirectory entries from disk
            errno = ReadDirectoryEntries(inode, out Dictionary<string, uint> subEntries);

            if(errno != ErrorNumber.NoError) return errno;

            // Last component — create the node
            if(i == components.Length - 1)
            {
                node = new VxFsDirNode
                {
                    Path     = normalizedPath,
                    Position = 0,
                    Entries  = subEntries.Keys.ToArray()
                };

                return ErrorNumber.NoError;
            }

            // Intermediate component — descend
            currentEntries = subEntries;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node)
    {
        if(node is not VxFsDirNode dirNode) return ErrorNumber.InvalidArgument;

        dirNode.Position = -1;
        dirNode.Entries  = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename)
    {
        filename = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not VxFsDirNode dirNode) return ErrorNumber.InvalidArgument;

        if(dirNode.Position < 0) return ErrorNumber.InvalidArgument;

        if(dirNode.Position >= dirNode.Entries.Length) return ErrorNumber.NoError;

        filename = dirNode.Entries[dirNode.Position++];

        return ErrorNumber.NoError;
    }

    /// <summary>Reads directory entries from an inode's data blocks</summary>
    /// <param name="inode">Directory inode</param>
    /// <param name="entries">Output dictionary mapping filename to inode number</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadDirectoryEntries(DiskInode inode, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>(StringComparer.Ordinal);

        byte[] dirData = ReadInodeData(inode);

        if(dirData == null || dirData.Length == 0) return ErrorNumber.InvalidArgument;

        int blockSize = _superblock.vs_bsize;
        var pos       = 0;

        while(pos < dirData.Length)
        {
            int blockEnd = pos + blockSize;

            if(blockEnd > dirData.Length) blockEnd = dirData.Length;

            // Read directory block header
            if(pos + 4 > dirData.Length) break;

            var dNhash = BitConverter.ToUInt16(dirData, pos + 2);

            if(_bigEndian) dNhash = (ushort)(dNhash >> 8 | dNhash << 8);

            // Skip overhead: DirectoryBlock header (4 bytes) + hash table (2 * d_nhash bytes)
            int overhead   = 4   + 2 * dNhash;
            int entryStart = pos + overhead;

            while(entryStart + 10 <= blockEnd)
            {
                var dIno     = BitConverter.ToUInt32(dirData, entryStart);
                var dReclen  = BitConverter.ToUInt16(dirData, entryStart + 4);
                var dNamelen = BitConverter.ToUInt16(dirData, entryStart + 6);

                if(_bigEndian)
                {
                    dIno = dIno >> 24 & 0xFF | dIno >> 8 & 0xFF00 | dIno << 8 & 0xFF0000 | dIno << 24 & 0xFF000000;

                    dReclen  = (ushort)(dReclen  >> 8 | dReclen  << 8);
                    dNamelen = (ushort)(dNamelen >> 8 | dNamelen << 8);
                }

                if(dReclen == 0) break;

                if(dIno != 0 && dNamelen > 0 && entryStart + 10 + dNamelen <= dirData.Length)
                {
                    string name = _encoding.GetString(dirData, entryStart + 10, dNamelen);

                    // Trim null terminators
                    int nullIdx = name.IndexOf('\0');

                    if(nullIdx >= 0) name = name[..nullIdx];

                    if(name.Length > 0) entries[name] = dIno;
                }

                entryStart += dReclen;
            }

            pos = blockEnd;
        }

        return ErrorNumber.NoError;
    }
}