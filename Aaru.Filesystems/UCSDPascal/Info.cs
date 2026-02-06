// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the U.C.S.D. Pascal filesystem and shows information.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
public sealed partial class PascalPlugin
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Length < 3) return false;

        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = imagePlugin.ReadSectors(_multiplier * 2 + partition.Start,
                                                    false,
                                                    _multiplier,
                                                    out byte[] volBlock,
                                                    out _);

        if(errno != ErrorNumber.NoError) return false;

        // Try little endian first (Apple II)
        if(ValidateVolumeEntry(volBlock, imagePlugin, false)) return true;

        // Try big endian (other platforms)
        return ValidateVolumeEntry(volBlock, imagePlugin, true);
    }

    /// <summary>Validates a UCSD Pascal volume entry with the specified endianness</summary>
    /// <param name="volBlock">Raw volume block data</param>
    /// <param name="imagePlugin">The media image</param>
    /// <param name="bigEndian">True if data is big endian, false for little endian</param>
    /// <returns>True if the volume entry is valid</returns>
    bool ValidateVolumeEntry(byte[] volBlock, IMediaImage imagePlugin, bool bigEndian)
    {
        short firstBlock;
        short lastBlock;
        short entryType;
        short blocks;
        short files;

        if(bigEndian)
        {
            firstBlock = BigEndianBitConverter.ToInt16(volBlock, 0x00);
            lastBlock  = BigEndianBitConverter.ToInt16(volBlock, 0x02);
            entryType  = BigEndianBitConverter.ToInt16(volBlock, 0x04);
            blocks     = BigEndianBitConverter.ToInt16(volBlock, 0x0E);
            files      = BigEndianBitConverter.ToInt16(volBlock, 0x10);
        }
        else
        {
            firstBlock = BitConverter.ToInt16(volBlock, 0x00);
            lastBlock  = BitConverter.ToInt16(volBlock, 0x02);
            entryType  = BitConverter.ToInt16(volBlock, 0x04);
            blocks     = BitConverter.ToInt16(volBlock, 0x0E);
            files      = BitConverter.ToInt16(volBlock, 0x10);
        }

        var volumeName = new byte[8];
        Array.Copy(volBlock, 0x06, volumeName, 0, 8);

        AaruLogging.Debug(MODULE_NAME, "volEntry.firstBlock = {0} (bigEndian={1})", firstBlock, bigEndian);
        AaruLogging.Debug(MODULE_NAME, "volEntry.lastBlock = {0}",                  lastBlock);
        AaruLogging.Debug(MODULE_NAME, "volEntry.entryType = {0}",                  (PascalFileKind)entryType);
        AaruLogging.Debug(MODULE_NAME, "volEntry.blocks = {0}",                     blocks);
        AaruLogging.Debug(MODULE_NAME, "volEntry.files = {0}",                      files);

        // First block is always 0 (even is it's sector 2)
        if(firstBlock != 0) return false;

        // Last volume record block must be after first block, and before end of device
        if(lastBlock <= firstBlock || (ulong)lastBlock > imagePlugin.Info.Sectors / _multiplier - 2) return false;

        // Volume record entry type must be volume or secure
        if((PascalFileKind)entryType != PascalFileKind.Volume && (PascalFileKind)entryType != PascalFileKind.Secure)
            return false;

        // Volume name is max 7 characters
        if(volumeName[0] > 7) return false;

        // Volume blocks is equal to volume sectors
        if(blocks < 0 || (ulong)blocks != imagePlugin.Info.Sectors / _multiplier) return false;

        // There can be not less than zero files
        return files >= 0;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding ??= new Apple2();
        var sbInformation = new StringBuilder();
        metadata    = new FileSystem();
        information = "";
        _multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        if(imagePlugin.Info.Sectors < 3) return;

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = imagePlugin.ReadSectors(_multiplier * 2 + partition.Start,
                                                    false,
                                                    _multiplier,
                                                    out byte[] volBlock,
                                                    out _);

        if(errno != ErrorNumber.NoError) return;

        // Detect endianness - try little endian first (Apple II), then big endian
        bool bigEndian = !ValidateVolumeEntry(volBlock, imagePlugin, false);

        PascalVolumeEntry volEntry;

        if(bigEndian)
        {
            volEntry = new PascalVolumeEntry
            {
                FirstBlock = BigEndianBitConverter.ToInt16(volBlock, 0x00),
                LastBlock  = BigEndianBitConverter.ToInt16(volBlock, 0x02),
                EntryType  = (PascalFileKind)BigEndianBitConverter.ToInt16(volBlock, 0x04),
                VolumeName = new byte[8],
                Blocks     = BigEndianBitConverter.ToInt16(volBlock, 0x0E),
                Files      = BigEndianBitConverter.ToInt16(volBlock, 0x10),
                Dummy      = BigEndianBitConverter.ToInt16(volBlock, 0x12),
                LastBoot   = BigEndianBitConverter.ToInt16(volBlock, 0x14),
                Tail       = BigEndianBitConverter.ToInt32(volBlock, 0x16)
            };
        }
        else
        {
            volEntry = new PascalVolumeEntry
            {
                FirstBlock = BitConverter.ToInt16(volBlock, 0x00),
                LastBlock  = BitConverter.ToInt16(volBlock, 0x02),
                EntryType  = (PascalFileKind)BitConverter.ToInt16(volBlock, 0x04),
                VolumeName = new byte[8],
                Blocks     = BitConverter.ToInt16(volBlock, 0x0E),
                Files      = BitConverter.ToInt16(volBlock, 0x10),
                Dummy      = BitConverter.ToInt16(volBlock, 0x12),
                LastBoot   = BitConverter.ToInt16(volBlock, 0x14),
                Tail       = BitConverter.ToInt32(volBlock, 0x16)
            };
        }

        Array.Copy(volBlock, 0x06, volEntry.VolumeName, 0, 8);

        // First block is always 0 (even is it's sector 2)
        if(volEntry.FirstBlock != 0) return;

        // Last volume record block must be after first block, and before end of device
        if(volEntry.LastBlock        <= volEntry.FirstBlock ||
           (ulong)volEntry.LastBlock > imagePlugin.Info.Sectors / _multiplier - 2)
            return;

        // Volume record entry type must be volume or secure
        if(volEntry.EntryType != PascalFileKind.Volume && volEntry.EntryType != PascalFileKind.Secure) return;

        // Volume name is max 7 characters
        if(volEntry.VolumeName[0] > 7) return;

        // Volume blocks is equal to volume sectors
        if(volEntry.Blocks < 0 || (ulong)volEntry.Blocks != imagePlugin.Info.Sectors / _multiplier) return;

        // There can be not less than zero files
        if(volEntry.Files < 0) return;

        sbInformation.AppendFormat(Localization.Volume_record_spans_from_block_0_to_block_1,
                                   volEntry.FirstBlock,
                                   volEntry.LastBlock)
                     .AppendLine();

        sbInformation
           .AppendFormat(Localization.Volume_name_0, StringHandlers.PascalToString(volEntry.VolumeName, encoding))
           .AppendLine();

        sbInformation.AppendFormat(Localization.Volume_has_0_blocks, volEntry.Blocks).AppendLine();
        sbInformation.AppendFormat(Localization.Volume_has_0_files,  volEntry.Files).AppendLine();

        sbInformation
           .AppendFormat(Localization.Volume_last_booted_on_0, DateHandlers.UcsdPascalToDateTime(volEntry.LastBoot))
           .AppendLine();

        if(bigEndian)
            sbInformation.AppendLine("Volume is big endian");
        else
            sbInformation.AppendLine("Volume is little endian");

        information = sbInformation.ToString();

        imagePlugin.ReadSectors(partition.Start, false, _multiplier * 2, out byte[] boot, out _);

        metadata = new FileSystem
        {
            Bootable    = !ArrayHelpers.ArrayIsNullOrEmpty(boot),
            Clusters    = (ulong)volEntry.Blocks,
            ClusterSize = imagePlugin.Info.SectorSize,
            Files       = (ulong)volEntry.Files,
            Type        = FS_TYPE,
            VolumeName  = StringHandlers.PascalToString(volEntry.VolumeName, encoding)
        };
    }

#endregion
}