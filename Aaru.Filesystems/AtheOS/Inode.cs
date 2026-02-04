// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Atheos filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AtheOS
{
    /// <summary>Reads an inode from disk given its block address</summary>
    /// <param name="blockAddress">The absolute block number of the inode</param>
    /// <param name="inode">Output parameter for the read inode</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(long blockAddress, out Inode inode)
    {
        inode = default(Inode);

        uint sectorSize          = _imagePlugin.Info.SectorSize;
        long byteAddressInFS     = blockAddress           * _superblock.block_size;
        long partitionByteOffset = (long)_partition.Start * sectorSize;
        long absoluteByteAddress = byteAddressInFS + partitionByteOffset;

        long sectorAddress  = absoluteByteAddress / sectorSize;
        var  offsetInSector = (int)(absoluteByteAddress % sectorSize);
        int  bytesToRead    = _superblock.inode_size;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode at block {0}: byte_offset=0x{1:X8}, sector={2}, offset_in_sector={3}",
                          blockAddress,
                          absoluteByteAddress,
                          sectorAddress,
                          offsetInSector);

        // Read enough sectors to cover the inode
        int sectorsNeeded = (offsetInSector + bytesToRead + (int)sectorSize - 1) / (int)sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors((ulong)sectorAddress,
                                                     false,
                                                     (uint)sectorsNeeded,
                                                     out byte[] sectorData,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode sectors: {0}", errno);

            return errno;
        }

        // Extract the inode from the correct offset
        var inodeData = new byte[bytesToRead];
        Array.Copy(sectorData, offsetInSector, inodeData, 0, bytesToRead);

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

        AaruLogging.Debug(MODULE_NAME, "Inode read successfully. Magic: 0x{0:X8}", inode.magic1);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an inode and returns both the parsed structure and raw data</summary>
    /// <param name="blockAddress">Block address of the inode</param>
    /// <param name="inode">Output parsed inode</param>
    /// <param name="inodeData">Output raw inode data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInodeWithData(long blockAddress, out Inode inode, out byte[] inodeData)
    {
        inode     = default(Inode);
        inodeData = null;

        uint sectorSize          = _imagePlugin.Info.SectorSize;
        long byteAddressInFS     = blockAddress           * _superblock.block_size;
        long partitionByteOffset = (long)_partition.Start * sectorSize;
        long absoluteByteAddress = byteAddressInFS + partitionByteOffset;

        long sectorAddress  = absoluteByteAddress / sectorSize;
        var  offsetInSector = (int)(absoluteByteAddress % sectorSize);
        int  bytesToRead    = _superblock.inode_size;

        int sectorsNeeded = (offsetInSector + bytesToRead + (int)sectorSize - 1) / (int)sectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors((ulong)sectorAddress,
                                                     false,
                                                     (uint)sectorsNeeded,
                                                     out byte[] sectorData,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError) return errno;

        inodeData = new byte[bytesToRead];
        Array.Copy(sectorData, offsetInSector, inodeData, 0, bytesToRead);

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets the inode for a given path, returning both the parsed inode and raw data</summary>
    /// <param name="path">Path to the file or directory</param>
    /// <param name="inode">Output inode structure</param>
    /// <param name="inodeData">Output raw inode data (for reading SmallData)</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber GetInodeForPath(string path, out Inode inode, out byte[] inodeData)
    {
        inode     = default(Inode);
        inodeData = null;

        // Normalize path
        string normalizedPath = path ?? "/";

        if(normalizedPath == "" || normalizedPath == ".") normalizedPath = "/";

        // Root directory
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
        {
            long rootBlockAddress = (long)_superblock.root_dir_ag * _superblock.blocks_per_ag +
                                    _superblock.root_dir_start;

            return ReadInodeWithData(rootBlockAddress, out inode, out inodeData);
        }

        // Parse path components
        string pathWithoutLeadingSlash = normalizedPath.StartsWith("/", StringComparison.Ordinal)
                                             ? normalizedPath[1..]
                                             : normalizedPath;

        string[] pathComponents = pathWithoutLeadingSlash.Split('/', StringSplitOptions.RemoveEmptyEntries)
                                                         .Where(static c => c != "." && c != "..")
                                                         .ToArray();

        if(pathComponents.Length == 0) return ErrorNumber.InvalidArgument;

        // Traverse path
        Dictionary<string, long> currentEntries = _rootDirectoryCache;

        for(var i = 0; i < pathComponents.Length; i++)
        {
            string component = pathComponents[i];

            if(!currentEntries.TryGetValue(component, out long childInodeAddr)) return ErrorNumber.NoSuchFile;

            // Last component - return its inode
            if(i == pathComponents.Length - 1) return ReadInodeWithData(childInodeAddr, out inode, out inodeData);

            // Not last component - must be a directory
            ErrorNumber errno = ReadInode(childInodeAddr, out Inode childInode);

            if(errno != ErrorNumber.NoError) return errno;

            if(!IsDirectory(childInode)) return ErrorNumber.NotDirectory;

            errno = ParseDirectoryBTree(childInode.data, out currentEntries);

            if(errno != ErrorNumber.NoError) return errno;
        }

        return ErrorNumber.NoSuchFile;
    }
}