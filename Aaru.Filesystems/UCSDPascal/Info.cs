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

using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Marshal = Aaru.Helpers.Marshal;
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
        PascalVolumeEntry volEntry = Marshal.ByteArrayToStructureLittleEndian<PascalVolumeEntry>(volBlock);

        if(IsValidVolumeEntry(volEntry, imagePlugin)) return true;

        // Try big endian (other platforms)
        volEntry = Marshal.ByteArrayToStructureBigEndian<PascalVolumeEntry>(volBlock);

        return IsValidVolumeEntry(volEntry, imagePlugin);
    }

    /// <summary>Validates a UCSD Pascal volume entry</summary>
    /// <param name="volEntry">Volume entry to validate</param>
    /// <param name="imagePlugin">The media image</param>
    /// <returns>True if the volume entry is valid</returns>
    bool IsValidVolumeEntry(PascalVolumeEntry volEntry, IMediaImage imagePlugin)
    {
        AaruLogging.Debug(MODULE_NAME, "volEntry.firstBlock = {0}", volEntry.FirstBlock);
        AaruLogging.Debug(MODULE_NAME, "volEntry.lastBlock = {0}",  volEntry.LastBlock);
        AaruLogging.Debug(MODULE_NAME, "volEntry.entryType = {0}",  volEntry.EntryType);
        AaruLogging.Debug(MODULE_NAME, "volEntry.blocks = {0}",     volEntry.Blocks);
        AaruLogging.Debug(MODULE_NAME, "volEntry.files = {0}",      volEntry.Files);

        // First block is always 0 (even is it's sector 2)
        if(volEntry.FirstBlock != 0) return false;

        // Last volume record block must be after first block, and before end of device
        if(volEntry.LastBlock        <= volEntry.FirstBlock ||
           (ulong)volEntry.LastBlock > imagePlugin.Info.Sectors / _multiplier - 2)
            return false;

        // Volume record entry type must be volume or secure
        if(volEntry.EntryType != PascalFileKind.Volume && volEntry.EntryType != PascalFileKind.Secure) return false;

        // Volume name is max 7 characters
        if(volEntry.VolumeName?[0] > 7) return false;

        // Volume blocks is equal to volume sectors
        if(volEntry.Blocks < 0 || (ulong)volEntry.Blocks != imagePlugin.Info.Sectors / _multiplier) return false;

        // There can be not less than zero files
        return volEntry.Files >= 0;
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

        // Try little endian first (Apple II), then big endian (other platforms)
        var bigEndian = false;

        PascalVolumeEntry volEntry = Marshal.ByteArrayToStructureLittleEndian<PascalVolumeEntry>(volBlock);

        if(!IsValidVolumeEntry(volEntry, imagePlugin))
        {
            bigEndian = true;
            volEntry  = Marshal.ByteArrayToStructureBigEndian<PascalVolumeEntry>(volBlock);

            if(!IsValidVolumeEntry(volEntry, imagePlugin)) return;
        }

        // Detect machine type based on endianness and sector size
        MachineType machineType = DetectMachineType(bigEndian, imagePlugin.Info.SectorSize);

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

        // Report endianness
        sbInformation.AppendLine(bigEndian ? Localization.Volume_is_big_endian : Localization.Volume_is_little_endian);

        // Report machine type
        sbInformation.AppendFormat(Localization.Probable_machine_type_0, GetMachineTypeName(machineType)).AppendLine();

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

    /// <summary>
    ///     Detects the probable machine type based on endianness and sector size.
    ///     Apple II (6502) uses 256-byte sectors, most others use 512-byte sectors.
    /// </summary>
    /// <param name="bigEndian">True if the volume is big endian</param>
    /// <param name="sectorSize">Sector size in bytes</param>
    /// <returns>The detected machine type</returns>
    static MachineType DetectMachineType(bool bigEndian, uint sectorSize)
    {
        if(bigEndian)
        {
            // Big endian machines: Motorola 6800/6809, TI TMS9900, GA-16/440
            // Without more information, we can only say it's a big-endian P-Code machine
            return MachineType.PCodeBigEndian;
        }

        // Little endian machines
        if(sectorSize == 256)
        {
            // 256-byte sectors strongly suggest Apple II (6502)
            return MachineType.Mos6502;
        }

        // 512-byte sectors: could be PDP-11, Z80, 8080, or generic P-Code LE
        // Without examining boot code, we can only say it's a little-endian P-Code machine
        return MachineType.PCodeLittleEndian;
    }

    /// <summary>Gets a human-readable name for the machine type</summary>
    /// <param name="machineType">The machine type</param>
    /// <returns>Human-readable machine type name</returns>
    static string GetMachineTypeName(MachineType machineType) => machineType switch
                                                                 {
                                                                     MachineType.Unknown => "Unknown",
                                                                     MachineType.PCodeBigEndian =>
                                                                         "P-Code (Big Endian) - possibly Motorola 6800/6809, TI TMS9900, or GA-16/440",
                                                                     MachineType.PCodeLittleEndian =>
                                                                         "P-Code (Little Endian) - possibly PDP-11, Z80, Intel 8080, or compatible",
                                                                     MachineType.Pdp11 => "PDP-11 / LSI-11 / Terak",
                                                                     MachineType.Intel8080 => "Intel 8080/8085",
                                                                     MachineType.Z80 => "Zilog Z80",
                                                                     MachineType.Mos6502 => "MOS 6502/65C02 (Apple II)",
                                                                     MachineType.Motorola6800 => "Motorola 6800/6809",
                                                                     MachineType.Ti9900 => "TI TMS9900 / TI-99/4",
                                                                     MachineType.Ga440 => "GA-16/440",
                                                                     _ => "Unknown"
                                                                 };

#endregion
}