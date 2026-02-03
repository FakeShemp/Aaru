// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
/// <summary>Implements detection of the exFAT filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class exFAT
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(12 + partition.Start >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSector(0 + partition.Start, false, out byte[] vbrSector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(vbrSector.Length < 512) return false;

        VolumeBootRecord vbr = Marshal.ByteArrayToStructureLittleEndian<VolumeBootRecord>(vbrSector);

        return _signature.SequenceEqual(vbr.FileSystemName);
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";

        var sb = new StringBuilder();
        metadata = new FileSystem();

        ErrorNumber errno = imagePlugin.ReadSector(0 + partition.Start, false, out byte[] vbrSector, out _);

        if(errno != ErrorNumber.NoError) return;

        VolumeBootRecord vbr = Marshal.ByteArrayToStructureLittleEndian<VolumeBootRecord>(vbrSector);

        errno = imagePlugin.ReadSector(9 + partition.Start, false, out byte[] parametersSector, out _);

        if(errno != ErrorNumber.NoError) return;

        OemParameterTable parametersTable =
            Marshal.ByteArrayToStructureLittleEndian<OemParameterTable>(parametersSector);

        errno = imagePlugin.ReadSector(11 + partition.Start, false, out byte[] chkSector, out _);

        if(errno != ErrorNumber.NoError) return;

        ChecksumSector chksector = Marshal.ByteArrayToStructureLittleEndian<ChecksumSector>(chkSector);

        sb.AppendLine(Localization.Microsoft_exFAT);
        sb.AppendFormat(Localization.Partition_offset_0, vbr.PartitionOffset).AppendLine();

        sb.AppendFormat(Localization.Volume_has_0_sectors_of_1_bytes_each_for_a_total_of_2_bytes,
                        vbr.VolumeLength,
                        1 << vbr.BytesPerSectorShift,
                        vbr.VolumeLength * (ulong)(1 << vbr.BytesPerSectorShift))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_uses_clusters_of_0_sectors_1_bytes_each,
                        1 << vbr.SectorsPerClusterShift,
                        (1 << vbr.BytesPerSectorShift) * (1 << vbr.SectorsPerClusterShift))
          .AppendLine();

        sb.AppendFormat(Localization.First_FAT_starts_at_sector_0_and_runs_for_1_sectors, vbr.FatOffset, vbr.FatLength)
          .AppendLine();

        sb.AppendFormat(Localization.Volume_uses_0_FATs, vbr.NumberOfFats).AppendLine();

        sb.AppendFormat(Localization.Cluster_heap_starts_at_sector_0_contains_1_clusters_and_is_2_used,
                        vbr.ClusterHeapOffset,
                        vbr.ClusterCount,
                        vbr.PercentInUse)
          .AppendLine();

        sb.AppendFormat(Localization.Root_directory_starts_at_cluster_0, vbr.FirstClusterOfRootDirectory).AppendLine();

        sb.AppendFormat(Localization.Filesystem_revision_is_0_1,
                        (vbr.FileSystemRevision & 0xFF00) >> 8,
                        vbr.FileSystemRevision & 0xFF)
          .AppendLine();

        sb.AppendFormat(Localization.Volume_serial_number_0_X8, vbr.VolumeSerialNumber).AppendLine();
        sb.AppendFormat(Localization.BIOS_drive_is_0,           vbr.DriveSelect).AppendLine();

        if(vbr.VolumeFlags.HasFlag(VolumeFlags.ActiveFat)) sb.AppendLine(Localization.Second_FAT_is_in_use);

        if(vbr.VolumeFlags.HasFlag(VolumeFlags.VolumeDirty)) sb.AppendLine(Localization.Volume_is_dirty);

        if(vbr.VolumeFlags.HasFlag(VolumeFlags.MediaFailure))
            sb.AppendLine(Localization.Underlying_media_presented_errors);

        var count = 1;

        for(var i = 0; i < 10; i++)
        {
            // Read individual 48-byte parameter from the sector
            var parameterBytes = new byte[48];
            Array.Copy(parametersSector, i * 48, parameterBytes, 0, 48);

            var parameterGuid = new Guid(BitConverter.ToUInt32(parameterBytes, 0),
                                         BitConverter.ToUInt16(parameterBytes, 4),
                                         BitConverter.ToUInt16(parameterBytes, 6),
                                         parameterBytes[8],
                                         parameterBytes[9],
                                         parameterBytes[10],
                                         parameterBytes[11],
                                         parameterBytes[12],
                                         parameterBytes[13],
                                         parameterBytes[14],
                                         parameterBytes[15]);

            if(parameterGuid == _oemFlashParameterGuid)
            {
                FlashParameters flashParams = Marshal.ByteArrayToStructureLittleEndian<FlashParameters>(parameterBytes);

                sb.AppendFormat(Localization.OEM_Parameters_0, count).AppendLine();

                if(flashParams.EraseBlockSize > 0)
                    sb.AppendFormat("\t" + Localization._0_bytes_in_erase_block, flashParams.EraseBlockSize)
                      .AppendLine();

                if(flashParams.PageSize > 0)
                    sb.AppendFormat("\t" + Localization._0_bytes_per_page, flashParams.PageSize).AppendLine();

                if(flashParams.SpareSectors > 0)
                    sb.AppendFormat("\t" + Localization._0_spare_blocks, flashParams.SpareSectors).AppendLine();

                if(flashParams.RandomAccessTime > 0)
                    sb.AppendFormat("\t" + Localization._0_nanoseconds_random_access_time, flashParams.RandomAccessTime)
                      .AppendLine();

                if(flashParams.ProgrammingTime > 0)
                    sb.AppendFormat("\t" + Localization._0_nanoseconds_program_time, flashParams.ProgrammingTime)
                      .AppendLine();

                if(flashParams.ReadCycle > 0)
                    sb.AppendFormat("\t" + Localization._0_nanoseconds_read_cycle_time, flashParams.ReadCycle)
                      .AppendLine();

                if(flashParams.WriteCycle > 0)
                    sb.AppendFormat("\t" + Localization._0_nanoseconds_write_cycle_time, flashParams.WriteCycle)
                      .AppendLine();
            }
            else if(parameterGuid != Guid.Empty)
                sb.AppendFormat(Localization.Found_unknown_parameter_type_0, parameterGuid).AppendLine();

            count++;
        }

        sb.AppendFormat(Localization.Checksum_0_X8, chksector.Checksum[0]).AppendLine();

        metadata.ClusterSize  = (uint)((1 << vbr.BytesPerSectorShift) * (1 << vbr.SectorsPerClusterShift));
        metadata.Clusters     = vbr.ClusterCount;
        metadata.Dirty        = vbr.VolumeFlags.HasFlag(VolumeFlags.VolumeDirty);
        metadata.Type         = FS_TYPE;
        metadata.VolumeSerial = $"{vbr.VolumeSerialNumber:X8}";

        information = sb.ToString();
    }

#endregion
}