// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
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
using Aaru.Decoders;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class LisaFS
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(imagePlugin.Info.ReadableSectorTags?.Contains(SectorTagType.AppleSonyTag) != true) return false;

        // Minimal LisaOS disk is 3.5" single sided double density, 800 sectors
        if(imagePlugin.Info.Sectors < 800) return false;

        int beforeMddf = -1;

        // LisaOS searches sectors until tag tells MDDF resides there, so we'll search 100 sectors
        for(var i = 0; i < 100; i++)
        {
            ErrorNumber errno = imagePlugin.ReadSectorTag((ulong)i, false, SectorTagType.AppleSonyTag, out byte[] tag);

            if(errno != ErrorNumber.NoError) continue;

            DecodeTag(tag, out LisaTag.PriamTag searchTag);

            AaruLogging.Debug(MODULE_NAME, Localization.Sector_0_file_ID_1, i, searchTag.FileId);

            if(beforeMddf == -1 && searchTag.FileId == FILEID_LOADER_SIGNED) beforeMddf = i - 1;

            if(searchTag.FileId != FILEID_MDDF) continue;

            errno = imagePlugin.ReadSector((ulong)i, false, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            var infoMddf = new MDDF
            {
                mddf_block                   = BigEndianBitConverter.ToUInt32(sector, 0x6C),
                volsize_minus_one            = BigEndianBitConverter.ToUInt32(sector, 0x70),
                volsize_minus_mddf_minus_one = BigEndianBitConverter.ToUInt32(sector, 0x74),
                vol_size                     = BigEndianBitConverter.ToUInt32(sector, 0x78),
                blocksize                    = BigEndianBitConverter.ToUInt16(sector, 0x7C),
                datasize                     = BigEndianBitConverter.ToUInt16(sector, 0x7E)
            };

            AaruLogging.Debug(MODULE_NAME, Localization.Current_sector_0, i);
            AaruLogging.Debug(MODULE_NAME, "mddf.mddf_block = {0}",       infoMddf.mddf_block);
            AaruLogging.Debug(MODULE_NAME, "Disk size = {0} sectors",     imagePlugin.Info.Sectors);
            AaruLogging.Debug(MODULE_NAME, "mddf.vol_size = {0} sectors", infoMddf.vol_size);
            AaruLogging.Debug(MODULE_NAME, "mddf.vol_size - 1 = {0}",     infoMddf.volsize_minus_one);

            AaruLogging.Debug(MODULE_NAME,
                              "mddf.vol_size - mddf.mddf_block -1 = {0}",
                              infoMddf.volsize_minus_mddf_minus_one);

            AaruLogging.Debug(MODULE_NAME, "Disk sector = {0} bytes",    imagePlugin.Info.SectorSize);
            AaruLogging.Debug(MODULE_NAME, "mddf.blocksize = {0} bytes", infoMddf.blocksize);
            AaruLogging.Debug(MODULE_NAME, "mddf.datasize = {0} bytes",  infoMddf.datasize);

            if(infoMddf.mddf_block != i - beforeMddf) return false;

            if(infoMddf.vol_size > imagePlugin.Info.Sectors) return false;

            if(infoMddf.vol_size - 1 != infoMddf.volsize_minus_one) return false;

            if(infoMddf.vol_size - i - 1 != infoMddf.volsize_minus_mddf_minus_one - beforeMddf) return false;

            if(infoMddf.datasize > infoMddf.blocksize) return false;

            if(infoMddf.blocksize < imagePlugin.Info.SectorSize) return false;

            return infoMddf.datasize == imagePlugin.Info.SectorSize;
        }

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    = new LisaRoman();
        information = "";
        metadata    = new FileSystem();
        var sb = new StringBuilder();

        if(imagePlugin.Info.ReadableSectorTags?.Contains(SectorTagType.AppleSonyTag) != true) return;

        // Minimal LisaOS disk is 3.5" single sided double density, 800 sectors
        if(imagePlugin.Info.Sectors < 800) return;

        int beforeMddf = -1;

        // LisaOS searches sectors until tag tells MDDF resides there, so we'll search 100 sectors
        for(var i = 0; i < 100; i++)
        {
            ErrorNumber errno = imagePlugin.ReadSectorTag((ulong)i, false, SectorTagType.AppleSonyTag, out byte[] tag);

            if(errno != ErrorNumber.NoError) continue;

            DecodeTag(tag, out LisaTag.PriamTag searchTag);

            AaruLogging.Debug(MODULE_NAME, Localization.Sector_0_file_ID_1, i, searchTag.FileId);

            if(beforeMddf == -1 && searchTag.FileId == FILEID_LOADER_SIGNED) beforeMddf = i - 1;

            if(searchTag.FileId != FILEID_MDDF) continue;

            errno = imagePlugin.ReadSector((ulong)i, false, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            var infoMddf = new MDDF();
            var pString  = new byte[33];

            infoMddf.fsversion = BigEndianBitConverter.ToUInt16(sector, 0x00);
            infoMddf.volid     = BigEndianBitConverter.ToUInt64(sector, 0x02);
            infoMddf.volnum    = BigEndianBitConverter.ToUInt16(sector, 0x0A);
            Array.Copy(sector, 0x0C, pString, 0, 33);
            infoMddf.volname     = StringHandlers.PascalToString(pString, encoding);
            infoMddf.volname_pad = sector[0x2D];
            Array.Copy(sector, 0x2E, pString, 0, 33);

            // Prevent garbage
            infoMddf.password          = pString[0] <= 32 ? StringHandlers.PascalToString(pString, encoding) : "";
            infoMddf.password_pad      = sector[0x4F];
            infoMddf.machine_id        = BigEndianBitConverter.ToUInt32(sector, 0x50);
            infoMddf.master_machine_id = BigEndianBitConverter.ToUInt32(sector, 0x54);
            var lisaTime = BigEndianBitConverter.ToUInt32(sector, 0x58);
            infoMddf.dtvc                         = DateHandlers.LisaToDateTime(lisaTime);
            lisaTime                              = BigEndianBitConverter.ToUInt32(sector, 0x5C);
            infoMddf.dtcc                         = DateHandlers.LisaToDateTime(lisaTime);
            lisaTime                              = BigEndianBitConverter.ToUInt32(sector, 0x60);
            infoMddf.dtvb                         = DateHandlers.LisaToDateTime(lisaTime);
            lisaTime                              = BigEndianBitConverter.ToUInt32(sector, 0x64);
            infoMddf.dtvs                         = DateHandlers.LisaToDateTime(lisaTime);
            infoMddf.copy_thread                  = BigEndianBitConverter.ToUInt32(sector, 0x68);
            infoMddf.mddf_block                   = BigEndianBitConverter.ToUInt32(sector, 0x6C);
            infoMddf.volsize_minus_one            = BigEndianBitConverter.ToUInt32(sector, 0x70);
            infoMddf.volsize_minus_mddf_minus_one = BigEndianBitConverter.ToUInt32(sector, 0x74);
            infoMddf.vol_size                     = BigEndianBitConverter.ToUInt32(sector, 0x78);
            infoMddf.blocksize                    = BigEndianBitConverter.ToUInt16(sector, 0x7C);
            infoMddf.datasize                     = BigEndianBitConverter.ToUInt16(sector, 0x7E);
            infoMddf.unknown4                     = BigEndianBitConverter.ToUInt16(sector, 0x80);
            infoMddf.unknown5                     = BigEndianBitConverter.ToUInt32(sector, 0x82);
            infoMddf.unknown6                     = BigEndianBitConverter.ToUInt32(sector, 0x86);
            infoMddf.clustersize                  = BigEndianBitConverter.ToUInt16(sector, 0x8A);
            infoMddf.fs_size                      = BigEndianBitConverter.ToUInt32(sector, 0x8C);
            infoMddf.unknown7                     = BigEndianBitConverter.ToUInt32(sector, 0x90);
            infoMddf.srec_ptr                     = BigEndianBitConverter.ToUInt32(sector, 0x94);
            infoMddf.slist_packing                = BigEndianBitConverter.ToUInt16(sector, 0x98);
            infoMddf.srec_len                     = BigEndianBitConverter.ToUInt16(sector, 0x9A);
            infoMddf.first_file                   = BigEndianBitConverter.ToUInt16(sector, 0x9C);
            infoMddf.empty_file                   = BigEndianBitConverter.ToUInt16(sector, 0x9E);
            infoMddf.maxfiles                     = BigEndianBitConverter.ToUInt16(sector, 0xA0);
            infoMddf.hintsize                     = BigEndianBitConverter.ToUInt16(sector, 0xA2);
            infoMddf.leader_offset                = BigEndianBitConverter.ToUInt16(sector, 0xA4);
            infoMddf.leader_pages                 = BigEndianBitConverter.ToUInt16(sector, 0xA6);
            infoMddf.flabel_offset                = BigEndianBitConverter.ToUInt16(sector, 0xA8);
            infoMddf.unusedi1                     = BigEndianBitConverter.ToUInt16(sector, 0xAA);
            infoMddf.map_offset                   = BigEndianBitConverter.ToUInt16(sector, 0xAC);
            infoMddf.map_size                     = BigEndianBitConverter.ToUInt16(sector, 0xAE);
            infoMddf.filecount                    = BigEndianBitConverter.ToUInt16(sector, 0xB0);
            infoMddf.unusedl1                     = BigEndianBitConverter.ToUInt32(sector, 0xB2);
            infoMddf.freestart                    = BigEndianBitConverter.ToUInt32(sector, 0xB6);
            infoMddf.freecount                    = BigEndianBitConverter.ToUInt32(sector, 0xBA);
            infoMddf.rootmaxentries               = BigEndianBitConverter.ToUInt16(sector, 0xBE);
            infoMddf.mountinfo                    = BigEndianBitConverter.ToUInt32(sector, 0xC0);
            infoMddf.overmount_stamp              = BigEndianBitConverter.ToUInt64(sector, 0xC4);
            infoMddf.pmem_id                      = BigEndianBitConverter.ToUInt32(sector, 0xCC);
            infoMddf.pmem_alarm_ref               = BigEndianBitConverter.ToUInt16(sector, 0xD0);
            infoMddf.pmem_parm_mem                = new ushort[32];

            for(var j = 0; j < infoMddf.pmem_parm_mem.Length; j++)
                infoMddf.pmem_parm_mem[j] = BigEndianBitConverter.ToUInt16(sector, 0xD2 + j * 2);

            infoMddf.vol_scavenged   = sector[0x112];
            infoMddf.tbt_copied      = sector[0x113];
            infoMddf.backup_volid    = BigEndianBitConverter.ToUInt64(sector, 0x114);
            infoMddf.result_scavenge = BigEndianBitConverter.ToUInt16(sector, 0x11C);
            infoMddf.smallmap_offset = BigEndianBitConverter.ToUInt16(sector, 0x11E);
            infoMddf.hentry_offset   = BigEndianBitConverter.ToUInt16(sector, 0x120);
            infoMddf.boot_code       = BigEndianBitConverter.ToUInt16(sector, 0x122);
            infoMddf.boot_environ    = BigEndianBitConverter.ToUInt16(sector, 0x124);
            infoMddf.flabel_size     = BigEndianBitConverter.ToUInt16(sector, 0x126);
            infoMddf.fs_overhead     = BigEndianBitConverter.ToUInt16(sector, 0x128);
            infoMddf.oem_id          = BigEndianBitConverter.ToUInt32(sector, 0x12A);
            infoMddf.root_page       = BigEndianBitConverter.ToUInt32(sector, 0x12E);
            infoMddf.tree_depth      = BigEndianBitConverter.ToUInt16(sector, 0x132);
            infoMddf.node_id         = BigEndianBitConverter.ToUInt16(sector, 0x134);
            infoMddf.vol_seq_no      = BigEndianBitConverter.ToUInt16(sector, 0x136);
            infoMddf.vol_mounted     = sector[0x138];

            AaruLogging.Debug(MODULE_NAME, "mddf.volname_pad = 0x{0:X2} ({0})",    infoMddf.volname_pad);
            AaruLogging.Debug(MODULE_NAME, "mddf.password_pad = 0x{0:X2} ({0})",   infoMddf.password_pad);
            AaruLogging.Debug(MODULE_NAME, "mddf.copy_thread = 0x{0:X8} ({0})",    infoMddf.copy_thread);
            AaruLogging.Debug(MODULE_NAME, "mddf.unknown4 = 0x{0:X4} ({0})",       infoMddf.unknown4);
            AaruLogging.Debug(MODULE_NAME, "mddf.unknown5 = 0x{0:X8} ({0})",       infoMddf.unknown5);
            AaruLogging.Debug(MODULE_NAME, "mddf.unknown6 = 0x{0:X8} ({0})",       infoMddf.unknown6);
            AaruLogging.Debug(MODULE_NAME, "mddf.unknown7 = 0x{0:X8} ({0})",       infoMddf.unknown7);
            AaruLogging.Debug(MODULE_NAME, "mddf.slist_packing = 0x{0:X4} ({0})",  infoMddf.slist_packing);
            AaruLogging.Debug(MODULE_NAME, "mddf.first_file = 0x{0:X4} ({0})",     infoMddf.first_file);
            AaruLogging.Debug(MODULE_NAME, "mddf.empty_file = 0x{0:X4} ({0})",     infoMddf.empty_file);
            AaruLogging.Debug(MODULE_NAME, "mddf.maxfiles = 0x{0:X4} ({0})",       infoMddf.maxfiles);
            AaruLogging.Debug(MODULE_NAME, "mddf.hintsize = 0x{0:X4} ({0})",       infoMddf.hintsize);
            AaruLogging.Debug(MODULE_NAME, "mddf.leader_offset = 0x{0:X4} ({0})",  infoMddf.leader_offset);
            AaruLogging.Debug(MODULE_NAME, "mddf.leader_pages = 0x{0:X4} ({0})",   infoMddf.leader_pages);
            AaruLogging.Debug(MODULE_NAME, "mddf.flabel_offset = 0x{0:X4} ({0})",  infoMddf.flabel_offset);
            AaruLogging.Debug(MODULE_NAME, "mddf.unusedi1 = 0x{0:X4} ({0})",       infoMddf.unusedi1);
            AaruLogging.Debug(MODULE_NAME, "mddf.map_offset = 0x{0:X4} ({0})",     infoMddf.map_offset);
            AaruLogging.Debug(MODULE_NAME, "mddf.map_size = 0x{0:X4} ({0})",       infoMddf.map_size);
            AaruLogging.Debug(MODULE_NAME, "mddf.unusedl1 = 0x{0:X8} ({0})",       infoMddf.unusedl1);
            AaruLogging.Debug(MODULE_NAME, "mddf.freestart = 0x{0:X8} ({0})",      infoMddf.freestart);
            AaruLogging.Debug(MODULE_NAME, "mddf.rootmaxentries = 0x{0:X4} ({0})", infoMddf.rootmaxentries);
            AaruLogging.Debug(MODULE_NAME, "mddf.mountinfo = 0x{0:X8} ({0})",      infoMddf.mountinfo);
            AaruLogging.Debug(MODULE_NAME, "mddf.pmem_id = 0x{0:X8} ({0})",        infoMddf.pmem_id);
            AaruLogging.Debug(MODULE_NAME, "mddf.pmem_alarm_ref = 0x{0:X4} ({0})", infoMddf.pmem_alarm_ref);
            AaruLogging.Debug(MODULE_NAME, "mddf.vol_scavenged = 0x{0:X2} ({0})",  infoMddf.vol_scavenged);
            AaruLogging.Debug(MODULE_NAME, "mddf.tbt_copied = 0x{0:X2} ({0})",     infoMddf.tbt_copied);
            AaruLogging.Debug(MODULE_NAME, "mddf.oem_id = 0x{0:X8} ({0})",         infoMddf.oem_id);
            AaruLogging.Debug(MODULE_NAME, "mddf.root_page = 0x{0:X8} ({0})",      infoMddf.root_page);
            AaruLogging.Debug(MODULE_NAME, "mddf.tree_depth = 0x{0:X4} ({0})",     infoMddf.tree_depth);
            AaruLogging.Debug(MODULE_NAME, "mddf.node_id = 0x{0:X4} ({0})",        infoMddf.node_id);

            if(infoMddf.mddf_block != i - beforeMddf) return;

            if(infoMddf.vol_size > imagePlugin.Info.Sectors) return;

            if(infoMddf.vol_size - 1 != infoMddf.volsize_minus_one) return;

            if(infoMddf.vol_size - i - 1 != infoMddf.volsize_minus_mddf_minus_one - beforeMddf) return;

            if(infoMddf.datasize > infoMddf.blocksize) return;

            if(infoMddf.blocksize < imagePlugin.Info.SectorSize) return;

            if(infoMddf.datasize != imagePlugin.Info.SectorSize) return;

            switch(infoMddf.fsversion)
            {
                case LISA_V1:
                    sb.AppendLine("LisaFS v1");

                    break;
                case LISA_V2:
                    sb.AppendLine("LisaFS v2");

                    break;
                case LISA_V3:
                    sb.AppendLine("LisaFS v3");

                    break;
                default:
                    sb.AppendFormat(Localization.Unknown_LisaFS_version_0, infoMddf.fsversion).AppendLine();

                    break;
            }

            sb.AppendFormat(Localization.Volume_name_0,      infoMddf.volname).AppendLine();
            sb.AppendFormat(Localization.Volume_password_0,  infoMddf.password).AppendLine();
            sb.AppendFormat(Localization.Volume_ID_0_X16,    infoMddf.volid).AppendLine();
            sb.AppendFormat(Localization.Backup_volume_ID_0, infoMddf.backup_volid).AppendLine();

            sb.AppendFormat(Localization.Master_copy_ID_0, infoMddf.master_machine_id).AppendLine();

            sb.AppendFormat(Localization.Volume_is_number_0_of_1, infoMddf.volnum, infoMddf.vol_seq_no).AppendLine();

            sb.AppendFormat(Localization.Serial_number_of_Lisa_computer_that_created_this_volume_0, infoMddf.machine_id)
              .AppendLine();

            sb.AppendFormat(Localization.Serial_number_of_Lisa_computer_that_can_use_this_volume_software_0,
                            infoMddf.pmem_id)
              .AppendLine();

            sb.AppendFormat(Localization.Volume_created_on_0, infoMddf.dtvc).AppendLine();
            sb.AppendFormat(Localization.Volume_catalog_created_on_0, infoMddf.dtcc).AppendLine();
            sb.AppendFormat(Localization.Volume_backed_up_on_0, infoMddf.dtvb).AppendLine();
            sb.AppendFormat(Localization.Volume_scavenged_on_0, infoMddf.dtvs).AppendLine();
            sb.AppendFormat(Localization.MDDF_is_in_block_0, infoMddf.mddf_block + beforeMddf).AppendLine();
            sb.AppendFormat(Localization.There_are_0_reserved_blocks_before_volume, beforeMddf).AppendLine();
            sb.AppendFormat(Localization._0_blocks_minus_one, infoMddf.volsize_minus_one).AppendLine();

            sb.AppendFormat(Localization._0_blocks_minus_one_minus_MDDF_offset, infoMddf.volsize_minus_mddf_minus_one)
              .AppendLine();

            sb.AppendFormat(Localization._0_blocks_in_volume,          infoMddf.vol_size).AppendLine();
            sb.AppendFormat(Localization._0_bytes_per_sector_uncooked, infoMddf.blocksize).AppendLine();
            sb.AppendFormat(Localization._0_bytes_per_sector,          infoMddf.datasize).AppendLine();
            sb.AppendFormat(Localization._0_blocks_per_cluster,        infoMddf.clustersize).AppendLine();
            sb.AppendFormat(Localization._0_blocks_in_filesystem,      infoMddf.fs_size).AppendLine();
            sb.AppendFormat(Localization._0_files_in_volume,           infoMddf.filecount).AppendLine();
            sb.AppendFormat(Localization._0_blocks_free,               infoMddf.freecount).AppendLine();
            sb.AppendFormat(Localization._0_bytes_in_LisaInfo,         infoMddf.flabel_size).AppendLine();
            sb.AppendFormat(Localization.Filesystem_overhead_0,        infoMddf.fs_overhead).AppendLine();
            sb.AppendFormat(Localization.Scavenger_result_code_0,      infoMddf.result_scavenge).AppendLine();
            sb.AppendFormat(Localization.Boot_code_0,                  infoMddf.boot_code).AppendLine();
            sb.AppendFormat(Localization.Boot_environment_0,           infoMddf.boot_environ).AppendLine();
            sb.AppendFormat(Localization.Overmount_stamp_0,            infoMddf.overmount_stamp).AppendLine();

            sb.AppendFormat(Localization.S_Records_start_at_0_and_spans_for_1_blocks,
                            infoMddf.srec_ptr + infoMddf.mddf_block + beforeMddf,
                            infoMddf.srec_len)
              .AppendLine();

            sb.AppendLine(infoMddf.vol_mounted == 0 ? Localization.Volume_is_clean : Localization.Volume_is_dirty);

            information = sb.ToString();

            metadata = new FileSystem();

            if(DateTime.Compare(infoMddf.dtvb, DateHandlers.LisaToDateTime(0)) > 0) metadata.BackupDate = infoMddf.dtvb;

            metadata.Clusters    = infoMddf.vol_size;
            metadata.ClusterSize = (uint)(infoMddf.clustersize * infoMddf.datasize);

            if(DateTime.Compare(infoMddf.dtvc, DateHandlers.LisaToDateTime(0)) > 0)
                metadata.CreationDate = infoMddf.dtvc;

            metadata.Dirty        = infoMddf.vol_mounted != 0;
            metadata.Files        = infoMddf.filecount;
            metadata.FreeClusters = infoMddf.freecount;
            metadata.Type         = FS_TYPE;
            metadata.VolumeName   = infoMddf.volname;
            metadata.VolumeSerial = $"{infoMddf.volid:X16}";

            return;
        }
    }

#endregion
}