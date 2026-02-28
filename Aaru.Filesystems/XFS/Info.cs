// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class XFS
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(imagePlugin.Info.SectorSize < 512) return false;

        // Misaligned
        if(imagePlugin.Info.MetadataMediaType == MetadataMediaType.OpticalDisc)
        {
            var sbSize = (uint)((Marshal.SizeOf<Superblock>() + 0x400) / imagePlugin.Info.SectorSize);

            if((Marshal.SizeOf<Superblock>() + 0x400) % imagePlugin.Info.SectorSize != 0) sbSize++;

            ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) return false;

            if(sector.Length < Marshal.SizeOf<Superblock>()) return false;

            var sbpiece = new byte[Marshal.SizeOf<Superblock>()];

            foreach(int location in new[]
                    {
                        0, 0x200, 0x400
                    })
            {
                Array.Copy(sector, location, sbpiece, 0, Marshal.SizeOf<Superblock>());

                Superblock xfsSb = Marshal.ByteArrayToStructureBigEndian<Superblock>(sbpiece);

                AaruLogging.Debug(MODULE_NAME,
                                  Localization.magic_at_0_X3_equals_1_expected_2,
                                  location,
                                  xfsSb.magicnum,
                                  XFS_MAGIC);

                if(xfsSb.magicnum == XFS_MAGIC) return true;
            }
        }
        else
        {
            foreach(int i in new[]
                    {
                        0, 1, 2
                    })
            {
                var location = (ulong)i;

                var sbSize = (uint)(Marshal.SizeOf<Superblock>() / imagePlugin.Info.SectorSize);

                if(Marshal.SizeOf<Superblock>() % imagePlugin.Info.SectorSize != 0) sbSize++;

                ErrorNumber errno =
                    imagePlugin.ReadSectors(partition.Start + location, false, sbSize, out byte[] sector, out _);

                if(errno != ErrorNumber.NoError) continue;

                if(sector.Length < Marshal.SizeOf<Superblock>()) return false;

                Superblock xfsSb = Marshal.ByteArrayToStructureBigEndian<Superblock>(sector);

                AaruLogging.Debug(MODULE_NAME,
                                  Localization.magic_at_0_equals_1_expected_2,
                                  location,
                                  xfsSb.magicnum,
                                  XFS_MAGIC);

                if(xfsSb.magicnum == XFS_MAGIC) return true;
            }
        }

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= Encoding.GetEncoding("iso-8859-15");
        information =   "";
        metadata    =   new FileSystem();

        if(imagePlugin.Info.SectorSize < 512) return;

        var xfsSb = new Superblock();

        // Misaligned
        if(imagePlugin.Info.MetadataMediaType == MetadataMediaType.OpticalDisc)
        {
            var sbSize = (uint)((Marshal.SizeOf<Superblock>() + 0x400) / imagePlugin.Info.SectorSize);

            if((Marshal.SizeOf<Superblock>() + 0x400) % imagePlugin.Info.SectorSize != 0) sbSize++;

            ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sbSize, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError || sector.Length < Marshal.SizeOf<Superblock>()) return;

            var sbpiece = new byte[Marshal.SizeOf<Superblock>()];

            foreach(int location in new[]
                    {
                        0, 0x200, 0x400
                    })
            {
                Array.Copy(sector, location, sbpiece, 0, Marshal.SizeOf<Superblock>());

                xfsSb = Marshal.ByteArrayToStructureBigEndian<Superblock>(sbpiece);

                AaruLogging.Debug(MODULE_NAME,
                                  Localization.magic_at_0_X3_equals_1_expected_2,
                                  location,
                                  xfsSb.magicnum,
                                  XFS_MAGIC);

                if(xfsSb.magicnum == XFS_MAGIC) break;
            }
        }
        else
        {
            foreach(int i in new[]
                    {
                        0, 1, 2
                    })
            {
                var location = (ulong)i;
                var sbSize   = (uint)(Marshal.SizeOf<Superblock>() / imagePlugin.Info.SectorSize);

                if(Marshal.SizeOf<Superblock>() % imagePlugin.Info.SectorSize != 0) sbSize++;

                ErrorNumber errno =
                    imagePlugin.ReadSectors(partition.Start + location, false, sbSize, out byte[] sector, out _);

                if(errno != ErrorNumber.NoError || sector.Length < Marshal.SizeOf<Superblock>()) return;

                xfsSb = Marshal.ByteArrayToStructureBigEndian<Superblock>(sector);

                AaruLogging.Debug(MODULE_NAME,
                                  Localization.magic_at_0_equals_1_expected_2,
                                  location,
                                  xfsSb.magicnum,
                                  XFS_MAGIC);

                if(xfsSb.magicnum == XFS_MAGIC) break;
            }
        }

        if(xfsSb.magicnum != XFS_MAGIC) return;

        var sb         = new StringBuilder();
        var versionNum = (ushort)(xfsSb.version & XFS_SB_VERSION_NUMBITS);

        // Construct volume name: V1-V3 use only sb_fname (6 bytes), V4+ may use sb_fname + sb_fpack (12 bytes)
        byte[] volumeNameBytes;

        if(versionNum >= XFS_SB_VERSION_4)
        {
            volumeNameBytes = new byte[12];
            Array.Copy(xfsSb.fname, 0, volumeNameBytes, 0, 6);
            Array.Copy(xfsSb.fpack, 0, volumeNameBytes, 6, 6);
        }
        else
            volumeNameBytes = xfsSb.fname;

        string volumeName = StringHandlers.CToString(volumeNameBytes, encoding);

        sb.AppendLine(Localization.XFS_filesystem);
        sb.AppendFormat(Localization.Filesystem_version_0, versionNum).AppendLine();

        // Report version-specific features
        switch(versionNum)
        {
            case XFS_SB_VERSION_2:
                sb.AppendLine(Localization.XFS_version_has_extended_attributes);

                break;
            case XFS_SB_VERSION_3:
                sb.AppendLine(Localization.XFS_version_has_extended_attributes);
                sb.AppendLine(Localization.XFS_version_has_32_bit_link_counts);

                break;
            case XFS_SB_VERSION_4:
            {
                ushort versionFlags = xfsSb.version;

                if((versionFlags & XFS_SB_VERSION_ATTRBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_extended_attributes);

                if((versionFlags & XFS_SB_VERSION_NLINKBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_32_bit_link_counts);

                if((versionFlags & XFS_SB_VERSION_QUOTABIT) != 0) sb.AppendLine(Localization.XFS_version_has_quotas);

                if((versionFlags & XFS_SB_VERSION_ALIGNBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_inode_alignment);

                if((versionFlags & XFS_SB_VERSION_DALIGNBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_data_stripe_alignment);

                if((versionFlags & XFS_SB_VERSION_SHAREDBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_shared_filesystem_support);

                if((versionFlags & XFS_SB_VERSION_DIRV2BIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_directory_v2);
                else
                    sb.AppendLine(Localization.XFS_version_has_directory_v1);

                if((versionFlags & XFS_SB_VERSION_EXTFLGBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_unwritten_extents);

                if((versionFlags & XFS_SB_VERSION_LOGV2BIT) != 0) sb.AppendLine(Localization.XFS_version_has_log_v2);

                if((versionFlags & XFS_SB_VERSION_SECTORBIT) != 0)
                    sb.AppendLine(Localization.XFS_version_has_sector_size_override);

                break;
            }
        }

        sb.AppendFormat(Localization._0_bytes_per_sector,             xfsSb.sectsize).AppendLine();
        sb.AppendFormat(Localization._0_bytes_per_block,              xfsSb.blocksize).AppendLine();
        sb.AppendFormat(Localization._0_bytes_per_inode,              xfsSb.inodesize).AppendLine();
        sb.AppendFormat(Localization._0_data_blocks_in_volume_1_free, xfsSb.dblocks, xfsSb.fdblocks).AppendLine();
        sb.AppendFormat(Localization._0_blocks_per_allocation_group,  xfsSb.agblocks).AppendLine();
        sb.AppendFormat(Localization._0_allocation_groups_in_volume,  xfsSb.agcount).AppendLine();
        sb.AppendFormat(Localization._0_inodes_in_volume_1_free,      xfsSb.icount, xfsSb.ifree).AppendLine();

        if(xfsSb.inprogress > 0) sb.AppendLine(Localization.fsck_in_progress);

        sb.AppendFormat(Localization.Volume_name_0, volumeName).AppendLine();

        // Show pack name for pre-V4 (IRIX) if non-empty
        if(versionNum < XFS_SB_VERSION_4)
        {
            string packName = StringHandlers.CToString(xfsSb.fpack, encoding);

            if(!string.IsNullOrEmpty(packName)) sb.AppendFormat(Localization.Pack_name_0, packName).AppendLine();
        }

        sb.AppendFormat(Localization.Volume_UUID_0, xfsSb.uuid).AppendLine();

        information = sb.ToString();

        metadata = new FileSystem
        {
            Type         = FS_TYPE,
            ClusterSize  = xfsSb.blocksize,
            Clusters     = xfsSb.dblocks,
            FreeClusters = xfsSb.fdblocks,
            Files        = xfsSb.icount - xfsSb.ifree,
            Dirty        = xfsSb.inprogress > 0,
            VolumeName   = volumeName,
            VolumeSerial = xfsSb.uuid.ToString()
        };
    }

#endregion
}