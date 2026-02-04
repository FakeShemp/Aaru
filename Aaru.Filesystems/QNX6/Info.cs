// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX6 filesystem plugin.
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
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of QNX 6 filesystem</summary>
public sealed partial class QNX6
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        uint sectors     = QNX6_SUPER_BLOCK_SIZE / imagePlugin.Info.SectorSize;
        uint bootSectors = QNX6_BOOT_BLOCKS_SIZE / imagePlugin.Info.SectorSize;

        if(partition.Start + bootSectors + sectors >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectors, out byte[] audiSector, out _);

        if(errno != ErrorNumber.NoError) return false;

        errno = imagePlugin.ReadSectors(partition.Start + bootSectors, false, sectors, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < QNX6_SUPER_BLOCK_SIZE) return false;

        qnx6_mmi_super_block audiSb = Marshal.ByteArrayToStructureLittleEndian<qnx6_mmi_super_block>(audiSector);

        qnx6_super_block qnxSb = Marshal.ByteArrayToStructureLittleEndian<qnx6_super_block>(sector);

        return qnxSb.sb_magic == QNX6_MAGIC || audiSb.sb_magic == QNX6_MAGIC;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();
        var  sb          = new StringBuilder();
        uint sectors     = QNX6_SUPER_BLOCK_SIZE / imagePlugin.Info.SectorSize;
        uint bootSectors = QNX6_BOOT_BLOCKS_SIZE / imagePlugin.Info.SectorSize;

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectors, out byte[] audiSector, out _);

        if(errno != ErrorNumber.NoError) return;

        errno = imagePlugin.ReadSectors(partition.Start + bootSectors, false, sectors, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return;

        if(sector.Length < QNX6_SUPER_BLOCK_SIZE) return;

        qnx6_mmi_super_block audiSb = Marshal.ByteArrayToStructureLittleEndian<qnx6_mmi_super_block>(audiSector);

        qnx6_super_block qnxSb = Marshal.ByteArrayToStructureLittleEndian<qnx6_super_block>(sector);

        bool audi = audiSb.sb_magic == QNX6_MAGIC;

        if(audi)
        {
            sb.AppendLine(Localization.QNX6_Audi_filesystem);
            sb.AppendFormat(Localization.Checksum_0_X8,       audiSb.sb_checksum).AppendLine();
            sb.AppendFormat(Localization.Serial_0_X16,        audiSb.sb_serial).AppendLine();
            sb.AppendFormat(Localization._0_bytes_per_block,  audiSb.sb_blocksize).AppendLine();
            sb.AppendFormat(Localization._0_inodes_free_of_1, audiSb.sb_free_inodes, audiSb.sb_num_inodes).AppendLine();

            sb.AppendFormat(Localization._0_blocks_1_bytes_free_of_2_3_bytes,
                            audiSb.sb_free_blocks,
                            audiSb.sb_free_blocks * audiSb.sb_blocksize,
                            audiSb.sb_num_blocks,
                            audiSb.sb_num_blocks * audiSb.sb_blocksize)
              .AppendLine();

            metadata = new FileSystem
            {
                Type         = FS_TYPE,
                Clusters     = audiSb.sb_num_blocks,
                ClusterSize  = audiSb.sb_blocksize,
                Bootable     = true,
                Files        = audiSb.sb_num_inodes - audiSb.sb_free_inodes,
                FreeClusters = audiSb.sb_free_blocks,
                VolumeSerial = $"{audiSb.sb_serial:X16}"
            };

            //xmlFSType.VolumeName = CurrentEncoding.GetString(audiSb.sb_id);

            information = sb.ToString();

            return;
        }

        sb.AppendLine(Localization.QNX6_filesystem);
        sb.AppendFormat(Localization.Checksum_0_X8, qnxSb.sb_checksum).AppendLine();
        sb.AppendFormat(Localization.Serial_0_X16,  qnxSb.sb_serial).AppendLine();
        sb.AppendFormat(Localization.Created_on_0,  DateHandlers.UnixUnsignedToDateTime(qnxSb.sb_ctime)).AppendLine();

        sb.AppendFormat(Localization.Last_mounted_on_0, DateHandlers.UnixUnsignedToDateTime(qnxSb.sb_atime))
          .AppendLine();

        sb.AppendFormat(Localization.Flags_0_X8,    qnxSb.sb_flags).AppendLine();
        sb.AppendFormat(Localization.Version1_0_X4, qnxSb.sb_version1).AppendLine();
        sb.AppendFormat(Localization.Version2_0_X4, qnxSb.sb_version2).AppendLine();

        //sb.AppendFormat("Volume ID: \"{0}\"", CurrentEncoding.GetString(qnxSb.sb_volumeid)).AppendLine();
        sb.AppendFormat(Localization._0_bytes_per_block,  qnxSb.sb_blocksize).AppendLine();
        sb.AppendFormat(Localization._0_inodes_free_of_1, qnxSb.sb_free_inodes, qnxSb.sb_num_inodes).AppendLine();

        sb.AppendFormat(Localization._0_blocks_1_bytes_free_of_2_3_bytes,
                        qnxSb.sb_free_blocks,
                        qnxSb.sb_free_blocks * qnxSb.sb_blocksize,
                        qnxSb.sb_num_blocks,
                        qnxSb.sb_num_blocks * qnxSb.sb_blocksize)
          .AppendLine();

        metadata = new FileSystem
        {
            Type             = FS_TYPE,
            Clusters         = qnxSb.sb_num_blocks,
            ClusterSize      = qnxSb.sb_blocksize,
            Bootable         = true,
            Files            = qnxSb.sb_num_inodes - qnxSb.sb_free_inodes,
            FreeClusters     = qnxSb.sb_free_blocks,
            VolumeSerial     = $"{qnxSb.sb_serial:X16}",
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(qnxSb.sb_ctime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(qnxSb.sb_atime)
        };

        //xmlFSType.VolumeName = CurrentEncoding.GetString(qnxSb.sb_volumeid);

        information = sb.ToString();
    }

#endregion
}