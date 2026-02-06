// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Super.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handles mounting and umounting the U.C.S.D. Pascal filesystem.
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
public sealed partial class PascalPlugin
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _device   = imagePlugin;
        _encoding = encoding ?? new Apple2();

        options ??= GetDefaultOptions();

        if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out _debug);

        if(_device.Info.Sectors < 3) return ErrorNumber.InvalidArgument;

        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = _device.ReadSectors(_multiplier * 2, false, _multiplier, out _catalogBlocks, out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Try little endian first (Apple II), then big endian (other platforms)
        _bigEndian = false;

        if(!TryParseVolumeEntry(_catalogBlocks, false))
        {
            _bigEndian = true;

            if(!TryParseVolumeEntry(_catalogBlocks, true)) return ErrorNumber.InvalidArgument;
        }

        // Parse the volume entry with the detected endianness
        ParseVolumeEntry(_catalogBlocks, _bigEndian);

        if(_mountedVolEntry.FirstBlock       != 0                                                                     ||
           _mountedVolEntry.LastBlock        <= _mountedVolEntry.FirstBlock                                           ||
           (ulong)_mountedVolEntry.LastBlock > _device.Info.Sectors / _multiplier - 2                                 ||
           _mountedVolEntry.EntryType != PascalFileKind.Volume && _mountedVolEntry.EntryType != PascalFileKind.Secure ||
           _mountedVolEntry.VolumeName[0] > 7                                                                         ||
           _mountedVolEntry.Blocks        < 0                                                                         ||
           (ulong)_mountedVolEntry.Blocks != _device.Info.Sectors / _multiplier                                       ||
           _mountedVolEntry.Files         < 0)
            return ErrorNumber.InvalidArgument;

        errno = _device.ReadSectors(_multiplier * 2,
                                    false,
                                    (uint)(_mountedVolEntry.LastBlock - _mountedVolEntry.FirstBlock - 2) * _multiplier,
                                    out _catalogBlocks,
                                    out _);

        if(errno != ErrorNumber.NoError) return errno;

        var offset = 26;

        _fileEntries = [];

        while(offset + 26 < _catalogBlocks.Length)
        {
            var entry = new PascalFileEntry
            {
                Filename = new byte[16]
            };

            if(_bigEndian)
            {
                entry.FirstBlock       = BigEndianBitConverter.ToInt16(_catalogBlocks, offset + 0x00);
                entry.LastBlock        = BigEndianBitConverter.ToInt16(_catalogBlocks, offset + 0x02);
                entry.EntryType        = (PascalFileKind)BigEndianBitConverter.ToInt16(_catalogBlocks, offset + 0x04);
                entry.LastBytes        = BigEndianBitConverter.ToInt16(_catalogBlocks, offset + 0x16);
                entry.ModificationTime = BigEndianBitConverter.ToInt16(_catalogBlocks, offset + 0x18);
            }
            else
            {
                entry.FirstBlock       = BitConverter.ToInt16(_catalogBlocks, offset + 0x00);
                entry.LastBlock        = BitConverter.ToInt16(_catalogBlocks, offset + 0x02);
                entry.EntryType        = (PascalFileKind)BitConverter.ToInt16(_catalogBlocks, offset + 0x04);
                entry.LastBytes        = BitConverter.ToInt16(_catalogBlocks, offset + 0x16);
                entry.ModificationTime = BitConverter.ToInt16(_catalogBlocks, offset + 0x18);
            }

            Array.Copy(_catalogBlocks, offset + 0x06, entry.Filename, 0, 16);

            if(entry.Filename[0] <= 15 && entry.Filename[0] > 0) _fileEntries.Add(entry);

            offset += 26;
        }

        errno = _device.ReadSectors(0, false, 2 * _multiplier, out _bootBlocks, out _);

        if(errno != ErrorNumber.NoError) return errno;

        Metadata = new FileSystem
        {
            Bootable    = !ArrayHelpers.ArrayIsNullOrEmpty(_bootBlocks),
            Clusters    = (ulong)_mountedVolEntry.Blocks,
            ClusterSize = _device.Info.SectorSize,
            Files       = (ulong)_mountedVolEntry.Files,
            Type        = FS_TYPE,
            VolumeName  = StringHandlers.PascalToString(_mountedVolEntry.VolumeName, _encoding)
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <summary>Tries to parse a volume entry with the specified endianness to check validity</summary>
    /// <param name="data">Raw catalog data</param>
    /// <param name="bigEndian">True for big endian, false for little endian</param>
    /// <returns>True if the volume entry appears valid</returns>
    bool TryParseVolumeEntry(byte[] data, bool bigEndian)
    {
        short firstBlock;
        short lastBlock;
        short entryType;
        short blocks;
        short files;

        if(bigEndian)
        {
            firstBlock = BigEndianBitConverter.ToInt16(data, 0x00);
            lastBlock  = BigEndianBitConverter.ToInt16(data, 0x02);
            entryType  = BigEndianBitConverter.ToInt16(data, 0x04);
            blocks     = BigEndianBitConverter.ToInt16(data, 0x0E);
            files      = BigEndianBitConverter.ToInt16(data, 0x10);
        }
        else
        {
            firstBlock = BitConverter.ToInt16(data, 0x00);
            lastBlock  = BitConverter.ToInt16(data, 0x02);
            entryType  = BitConverter.ToInt16(data, 0x04);
            blocks     = BitConverter.ToInt16(data, 0x0E);
            files      = BitConverter.ToInt16(data, 0x10);
        }

        // First block is always 0
        if(firstBlock != 0) return false;

        // Last volume record block must be after first block, and before end of device
        if(lastBlock <= firstBlock || (ulong)lastBlock > _device.Info.Sectors / _multiplier - 2) return false;

        // Volume record entry type must be volume or secure
        if((PascalFileKind)entryType != PascalFileKind.Volume && (PascalFileKind)entryType != PascalFileKind.Secure)
            return false;

        // Volume name is max 7 characters
        if(data[0x06] > 7) return false;

        // Volume blocks is equal to volume sectors
        if(blocks < 0 || (ulong)blocks != _device.Info.Sectors / _multiplier) return false;

        // There can be not less than zero files
        return files >= 0;
    }

    /// <summary>Parses the volume entry with the specified endianness</summary>
    /// <param name="data">Raw catalog data</param>
    /// <param name="bigEndian">True for big endian, false for little endian</param>
    void ParseVolumeEntry(byte[] data, bool bigEndian)
    {
        if(bigEndian)
        {
            _mountedVolEntry.FirstBlock = BigEndianBitConverter.ToInt16(data, 0x00);
            _mountedVolEntry.LastBlock  = BigEndianBitConverter.ToInt16(data, 0x02);
            _mountedVolEntry.EntryType  = (PascalFileKind)BigEndianBitConverter.ToInt16(data, 0x04);
            _mountedVolEntry.Blocks     = BigEndianBitConverter.ToInt16(data, 0x0E);
            _mountedVolEntry.Files      = BigEndianBitConverter.ToInt16(data, 0x10);
            _mountedVolEntry.Dummy      = BigEndianBitConverter.ToInt16(data, 0x12);
            _mountedVolEntry.LastBoot   = BigEndianBitConverter.ToInt16(data, 0x14);
            _mountedVolEntry.Tail       = BigEndianBitConverter.ToInt32(data, 0x16);
        }
        else
        {
            _mountedVolEntry.FirstBlock = BitConverter.ToInt16(data, 0x00);
            _mountedVolEntry.LastBlock  = BitConverter.ToInt16(data, 0x02);
            _mountedVolEntry.EntryType  = (PascalFileKind)BitConverter.ToInt16(data, 0x04);
            _mountedVolEntry.Blocks     = BitConverter.ToInt16(data, 0x0E);
            _mountedVolEntry.Files      = BitConverter.ToInt16(data, 0x10);
            _mountedVolEntry.Dummy      = BitConverter.ToInt16(data, 0x12);
            _mountedVolEntry.LastBoot   = BitConverter.ToInt16(data, 0x14);
            _mountedVolEntry.Tail       = BitConverter.ToInt32(data, 0x16);
        }

        _mountedVolEntry.VolumeName = new byte[8];
        Array.Copy(data, 0x06, _mountedVolEntry.VolumeName, 0, 8);
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        _mounted     = false;
        _fileEntries = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = new FileSystemInfo
        {
            Blocks         = (ulong)_mountedVolEntry.Blocks,
            FilenameLength = 16,
            Files          = (ulong)_mountedVolEntry.Files,
            FreeBlocks     = 0,
            PluginId       = Id,
            Type           = FS_TYPE
        };

        stat.FreeBlocks = (ulong)(_mountedVolEntry.Blocks - (_mountedVolEntry.LastBlock - _mountedVolEntry.FirstBlock));

        foreach(PascalFileEntry entry in _fileEntries) stat.FreeBlocks -= (ulong)(entry.LastBlock - entry.FirstBlock);

        return ErrorNumber.NotImplemented;
    }

#endregion
}