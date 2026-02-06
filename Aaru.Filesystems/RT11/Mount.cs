// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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
using System.Runtime.InteropServices;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding    = encoding ?? Encoding.ASCII;
        _imagePlugin = imagePlugin;
        _partition   = partition;

        AaruLogging.Debug(MODULE_NAME, "Mount: Starting RT-11 mount");

        // RT-11 uses 512-byte blocks
        if(imagePlugin.Info.SectorSize != 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Mount: Sector size is not 512 bytes");

            return ErrorNumber.InvalidArgument;
        }

        // Read home block (block 1)
        ErrorNumber errno =
            imagePlugin.ReadSector(partition.Start + HOME_BLOCK, false, out byte[] homeBlockData, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Mount: Could not read home block, error {errno}");

            return errno;
        }

        // Parse home block
        _homeBlock = Marshal.PtrToStructure<HomeBlock>(Marshal.UnsafeAddrOfPinnedArrayElement(homeBlockData, 0));

        // Validate home block checksum
        if(!ValidateHomeBlockChecksum(homeBlockData))
        {
            AaruLogging.Debug(MODULE_NAME, "Mount: Home block checksum is invalid");

            return ErrorNumber.InvalidArgument;
        }

        // Check system identification - should be "DECRT11A    " (12 bytes)
        string systemId = _encoding.GetString(_homeBlock.format).TrimEnd();

        if(!systemId.StartsWith("DECRT11", StringComparison.OrdinalIgnoreCase))
        {
            AaruLogging.Debug(MODULE_NAME, $"Mount: Invalid system ID: {systemId}");

            return ErrorNumber.InvalidArgument;
        }

        // Get first directory block
        _firstDirectoryBlock = _homeBlock.rootBlock;

        if(_firstDirectoryBlock < RESERVED_BLOCKS)
        {
            AaruLogging.Debug(MODULE_NAME, $"Mount: First directory block {_firstDirectoryBlock} is in reserved area");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, $"Mount: Home block valid, first directory at block {_firstDirectoryBlock}");

        // Read first directory segment (2 blocks = 1024 bytes)
        errno = imagePlugin.ReadSectors(partition.Start + _firstDirectoryBlock,
                                        false,
                                        2,
                                        out byte[] dirSegmentData,
                                        out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Mount: Could not read directory segment, error {errno}");

            return errno;
        }

        // Parse directory segment header
        DirectorySegmentHeader segmentHeader =
            Marshal.PtrToStructure<DirectorySegmentHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, 0));

        _totalSegments = segmentHeader.totalSegments;

        AaruLogging.Debug(MODULE_NAME,
                          $"Mount: Directory has {_totalSegments} total segments, extra bytes per entry: {segmentHeader.extraBytesPerEntry}");

        // Cache root directory contents
        _rootDirectoryCache = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        ErrorNumber parseError =
            ParseDirectorySegment(dirSegmentData, segmentHeader, out List<(string filename, uint startBlock)> entries);

        if(parseError != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Mount: Error parsing directory segment: {parseError}");

            return parseError;
        }

        AaruLogging.Debug(MODULE_NAME, $"Mount: Cached {entries.Count} directory entries");

        foreach((string name, uint startBlock) in entries) _rootDirectoryCache[name] = startBlock;

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <summary>Validates the home block checksum</summary>
    /// <param name="homeBlockData">Home block data (512 bytes)</param>
    /// <returns>True if checksum is valid</returns>
    static bool ValidateHomeBlockChecksum(byte[] homeBlockData)
    {
        // Checksum is at offset 510-511 (last word)
        var storedChecksum = BitConverter.ToUInt16(homeBlockData, 510);

        // Calculate checksum - simple additive checksum of first 255 words (510 bytes)
        ushort calculatedChecksum = 0;

        for(var i = 0; i < 510; i += 2)
        {
            var word = BitConverter.ToUInt16(homeBlockData, i);
            calculatedChecksum += word;
        }

        return calculatedChecksum == storedChecksum;
    }
}