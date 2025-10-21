// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the High Performance Optical File System and shows
//     information.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(16 + partition.Start >= partition.End) return false;

        ErrorNumber errno =
            imagePlugin.ReadSector(0 + partition.Start,
                                   out byte[] hpofsBpbSector); // Seek to BIOS parameter block, on logical sector 0

        if(errno != ErrorNumber.NoError) return false;

        if(hpofsBpbSector.Length < 512) return false;

        BiosParameterBlock bpb = Marshal.ByteArrayToStructureLittleEndian<BiosParameterBlock>(hpofsBpbSector);

        return bpb.fs_type.SequenceEqual(_type);
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= Encoding.GetEncoding("ibm850");
        information =   "";
        metadata    =   new FileSystem();

        var sb = new StringBuilder();

        ErrorNumber errno =
            imagePlugin.ReadSector(0 + partition.Start,
                                   out byte[] hpofsBpbSector); // Seek to BIOS parameter block, on logical sector 0

        if(errno != ErrorNumber.NoError) return;

        errno = imagePlugin.ReadSector(13 + partition.Start,
                                       out byte[] medInfoSector); // Seek to media information block, on logical sector 13

        if(errno != ErrorNumber.NoError) return;

        errno = imagePlugin.ReadSector(14 + partition.Start,
                                       out byte[] volInfoSector); // Seek to volume information block, on logical sector 14

        if(errno != ErrorNumber.NoError) return;

        BiosParameterBlock bpb = Marshal.ByteArrayToStructureLittleEndian<BiosParameterBlock>(hpofsBpbSector);

        MediaInformationBlock mib =
            Marshal.ByteArrayToStructureBigEndian<MediaInformationBlock>(medInfoSector);

        VolumeInformationBlock vib =
            Marshal.ByteArrayToStructureBigEndian<VolumeInformationBlock>(volInfoSector);

        AaruLogging.Debug(MODULE_NAME, "bpb.oem_name = \"{0}\"", StringHandlers.CToString(bpb.oem_name));

        AaruLogging.Debug(MODULE_NAME, "bpb.bps = {0}",            bpb.bps);
        AaruLogging.Debug(MODULE_NAME, "bpb.spc = {0}",            bpb.spc);
        AaruLogging.Debug(MODULE_NAME, "bpb.rsectors = {0}",       bpb.rsectors);
        AaruLogging.Debug(MODULE_NAME, "bpb.fats_no = {0}",        bpb.fats_no);
        AaruLogging.Debug(MODULE_NAME, "bpb.root_ent = {0}",       bpb.root_ent);
        AaruLogging.Debug(MODULE_NAME, "bpb.sectors = {0}",        bpb.sectors);
        AaruLogging.Debug(MODULE_NAME, "bpb.media = 0x{0:X2}",     bpb.media);
        AaruLogging.Debug(MODULE_NAME, "bpb.spfat = {0}",          bpb.spfat);
        AaruLogging.Debug(MODULE_NAME, "bpb.sptrk = {0}",          bpb.sptrk);
        AaruLogging.Debug(MODULE_NAME, "bpb.heads = {0}",          bpb.heads);
        AaruLogging.Debug(MODULE_NAME, "bpb.hsectors = {0}",       bpb.hsectors);
        AaruLogging.Debug(MODULE_NAME, "bpb.big_sectors = {0}",    bpb.big_sectors);
        AaruLogging.Debug(MODULE_NAME, "bpb.drive_no = 0x{0:X2}",  bpb.drive_no);
        AaruLogging.Debug(MODULE_NAME, "bpb.nt_flags = {0}",       bpb.nt_flags);
        AaruLogging.Debug(MODULE_NAME, "bpb.signature = 0x{0:X2}", bpb.signature);
        AaruLogging.Debug(MODULE_NAME, "bpb.serial_no = 0x{0:X8}", bpb.serial_no);

        AaruLogging.Debug(MODULE_NAME,
                          "bpb.volume_label = \"{0}\"",
                          StringHandlers.SpacePaddedToString(bpb.volume_label));

        AaruLogging.Debug(MODULE_NAME, "bpb.fs_type = \"{0}\"", StringHandlers.CToString(bpb.fs_type));

        AaruLogging.Debug(MODULE_NAME, "bpb.boot_code is empty? = {0}", ArrayHelpers.ArrayIsNullOrEmpty(bpb.boot_code));

        AaruLogging.Debug(MODULE_NAME, "bpb.unknown = {0}",     bpb.unknown);
        AaruLogging.Debug(MODULE_NAME, "bpb.unknown2 = {0}",    bpb.unknown2);
        AaruLogging.Debug(MODULE_NAME, "bpb.signature2 = {0}",  bpb.signature2);
        AaruLogging.Debug(MODULE_NAME, "mib.blockId = \"{0}\"", StringHandlers.CToString(mib.blockId));

        AaruLogging.Debug(MODULE_NAME,
                          "mib.volumeLabel = \"{0}\"",
                          StringHandlers.SpacePaddedToString(mib.volumeLabel));

        AaruLogging.Debug(MODULE_NAME, "mib.comment = \"{0}\"", StringHandlers.SpacePaddedToString(mib.comment));

        AaruLogging.Debug(MODULE_NAME, "mib.serial = 0x{0:X8}", mib.serial);

        AaruLogging.Debug(MODULE_NAME,
                          "mib.creationTimestamp = {0}",
                          DateHandlers.DosToDateTime(mib.creationDate, mib.creationTime));

        AaruLogging.Debug(MODULE_NAME, "mib.codepageType = {0}", mib.codepageType);
        AaruLogging.Debug(MODULE_NAME, "mib.codepage = {0}",     mib.codepage);
        AaruLogging.Debug(MODULE_NAME, "mib.rps = {0}",          mib.rps);
        AaruLogging.Debug(MODULE_NAME, "mib.bps = {0}",          mib.bps);
        AaruLogging.Debug(MODULE_NAME, "mib.bpc = {0}",          mib.bpc);
        AaruLogging.Debug(MODULE_NAME, "mib.unknown2 = {0}",     mib.unknown2);
        AaruLogging.Debug(MODULE_NAME, "mib.sectors = {0}",      mib.sectors);
        AaruLogging.Debug(MODULE_NAME, "mib.unknown3 = {0}",     mib.unknown3);
        AaruLogging.Debug(MODULE_NAME, "mib.unknown4 = {0}",     mib.unknown4);
        AaruLogging.Debug(MODULE_NAME, "mib.major = {0}",        mib.major);
        AaruLogging.Debug(MODULE_NAME, "mib.minor = {0}",        mib.minor);
        AaruLogging.Debug(MODULE_NAME, "mib.unknown5 = {0}",     mib.unknown5);
        AaruLogging.Debug(MODULE_NAME, "mib.unknown6 = {0}",     mib.unknown6);

        AaruLogging.Debug(MODULE_NAME, "mib.filler is empty? = {0}", ArrayHelpers.ArrayIsNullOrEmpty(mib.filler));

        AaruLogging.Debug(MODULE_NAME, "vib.blockId = \"{0}\"", StringHandlers.CToString(vib.blockId));
        AaruLogging.Debug(MODULE_NAME, "vib.unknown = {0}",     vib.unknown);
        AaruLogging.Debug(MODULE_NAME, "vib.unknown2 = {0}",    vib.unknown2);

        AaruLogging.Debug(MODULE_NAME, "vib.unknown3 is empty? = {0}", ArrayHelpers.ArrayIsNullOrEmpty(vib.unknown3));

        AaruLogging.Debug(MODULE_NAME, "vib.unknown4 = \"{0}\"", StringHandlers.SpacePaddedToString(vib.unknown4));

        AaruLogging.Debug(MODULE_NAME, "vib.owner = \"{0}\"", StringHandlers.SpacePaddedToString(vib.owner));

        AaruLogging.Debug(MODULE_NAME, "vib.unknown5 = \"{0}\"", StringHandlers.SpacePaddedToString(vib.unknown5));

        AaruLogging.Debug(MODULE_NAME, "vib.unknown6 = {0}",    vib.unknown6);
        AaruLogging.Debug(MODULE_NAME, "vib.percentFull = {0}", vib.percentFull);
        AaruLogging.Debug(MODULE_NAME, "vib.unknown7 = {0}",    vib.unknown7);

        AaruLogging.Debug(MODULE_NAME, "vib.filler is empty? = {0}", ArrayHelpers.ArrayIsNullOrEmpty(vib.filler));

        sb.AppendLine(Localization.HPOFS_Name);
        sb.AppendFormat(Localization.OEM_name_0, StringHandlers.SpacePaddedToString(bpb.oem_name)).AppendLine();
        sb.AppendFormat(Localization._0_bytes_per_sector, bpb.bps).AppendLine();
        sb.AppendFormat(Localization._0_sectors_per_cluster, bpb.spc).AppendLine();
        sb.AppendFormat(Localization.Media_descriptor_0, bpb.media).AppendLine();
        sb.AppendFormat(Localization._0_sectors_per_track, bpb.sptrk).AppendLine();
        sb.AppendFormat(Localization._0_heads, bpb.heads).AppendLine();
        sb.AppendFormat(Localization._0_sectors_hidden_before_BPB, bpb.hsectors).AppendLine();
        sb.AppendFormat(Localization._0_sectors_on_volume_1_bytes, mib.sectors, mib.sectors * bpb.bps).AppendLine();
        sb.AppendFormat(Localization.BIOS_drive_number_0, bpb.drive_no).AppendLine();
        sb.AppendFormat(Localization.Serial_number_0, mib.serial).AppendLine();

        sb.AppendFormat(Localization.Volume_label_0, StringHandlers.SpacePaddedToString(mib.volumeLabel, encoding))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_comment_0, StringHandlers.SpacePaddedToString(mib.comment, encoding))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_owner_0, StringHandlers.SpacePaddedToString(vib.owner, encoding))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_created_on_0,
                        DateHandlers.DosToDateTime(mib.creationDate, mib.creationTime))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_uses_0_codepage_1,
                        mib.codepageType is > 0 and < 3
                            ? mib.codepageType == 2 ? Localization.EBCDIC : Localization.ASCII
                            : Localization.Unknown_codepage,
                        mib.codepage)
          .AppendLine();

        sb.AppendFormat(Localization.RPS_level_0,                  mib.rps).AppendLine();
        sb.AppendFormat(Localization.Filesystem_version_0_1,       mib.major, mib.minor).AppendLine();
        sb.AppendFormat(Localization.Volume_can_be_filled_up_to_0, vib.percentFull).AppendLine();

        metadata = new FileSystem
        {
            Clusters               = mib.sectors / bpb.spc,
            ClusterSize            = (uint)(bpb.bps * bpb.spc),
            CreationDate           = DateHandlers.DosToDateTime(mib.creationDate, mib.creationTime),
            DataPreparerIdentifier = StringHandlers.SpacePaddedToString(vib.owner, encoding),
            Type                   = FS_TYPE,
            VolumeName             = StringHandlers.SpacePaddedToString(mib.volumeLabel, encoding),
            VolumeSerial           = $"{mib.serial:X8}",
            SystemIdentifier       = StringHandlers.SpacePaddedToString(bpb.oem_name)
        };

        information = sb.ToString();
    }

#endregion
}