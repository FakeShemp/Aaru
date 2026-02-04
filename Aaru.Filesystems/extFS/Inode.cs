// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class extFS
{
    /// <summary>ext inode size in bytes</summary>
    static readonly int EXT_INODE_SIZE = Marshal.SizeOf<ext_inode>();

    /// <summary>Number of inodes per block</summary>
    static readonly int EXT_INODES_PER_BLOCK = (int)EXT_BLOCK_SIZE / EXT_INODE_SIZE;

    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">The inode number to read</param>
    /// <param name="inode">The read inode structure</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out ext_inode inode)
    {
        inode = default(ext_inode);

        if(inodeNumber == 0 || inodeNumber > _superblock.s_ninodes)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate the block containing this inode
        // Inode table starts at block 2 (after boot block and superblock)
        // From Linux kernel: block = 2 + (ino-1) / EXT_INODES_PER_BLOCK
        uint inodeBlock = (uint)(2 + (inodeNumber - 1) / EXT_INODES_PER_BLOCK);

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0} from block {1} (inodes per block: {2})",
                          inodeNumber,
                          inodeBlock,
                          EXT_INODES_PER_BLOCK);

        // Read the block containing the inode
        ErrorNumber errno = ReadBlock(inodeBlock, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode block: {0}", errno);

            return errno;
        }

        // Calculate offset within the block
        int inodeOffset = (int)((inodeNumber - 1) % (uint)EXT_INODES_PER_BLOCK) * EXT_INODE_SIZE;

        AaruLogging.Debug(MODULE_NAME, "Inode offset within block: {0}", inodeOffset);

        // Parse the inode structure using marshalling
        inode = Marshal.ByteArrayToStructureLittleEndian<ext_inode>(blockData, inodeOffset, EXT_INODE_SIZE);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, nlinks={3}",
                          inodeNumber,
                          inode.i_mode,
                          inode.i_size,
                          inode.i_nlinks);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the inode number for a path</summary>
    /// <param name="path">The file path</param>
    /// <param name="inodeNum">The inode number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber GetInodeNumber(string path, out uint inodeNum)
    {
        inodeNum = 0;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        if(normalizedPath == "/")
        {
            inodeNum = EXT_ROOT_INO;

            return ErrorNumber.NoError;
        }

        // Parse path
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Navigate to target
        Dictionary<string, uint> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < pathComponents.Length - 1; i++)
        {
            string component = pathComponents[i];

            if(!currentEntries.TryGetValue(component, out uint childInodeNum)) return ErrorNumber.NoSuchFile;

            ErrorNumber errno = ReadInode(childInodeNum, out ext_inode childInode);

            if(errno != ErrorNumber.NoError) return errno;

            if((childInode.i_mode & 0xF000) != 0x4000) return ErrorNumber.NotDirectory;

            errno = ReadDirectoryEntries(childInode, out Dictionary<string, uint> childEntries);

            if(errno != ErrorNumber.NoError) return errno;

            currentEntries = childEntries;
        }

        // Get the target inode number
        string targetName = pathComponents[^1];

        if(!currentEntries.TryGetValue(targetName, out inodeNum)) return ErrorNumber.NoSuchFile;

        return ErrorNumber.NoError;
    }
}