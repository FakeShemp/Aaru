// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : QNX4 filesystem plugin.
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

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of QNX 4 filesystem</summary>
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class QNX4
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Start + 1 >= imagePlugin.Info.Sectors) return false;

        ErrorNumber errno = imagePlugin.ReadSector(partition.Start + 1, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < 512) return false;

        qnx4_super_block qnxSb = Marshal.ByteArrayToStructureLittleEndian<qnx4_super_block>(sector);

        // Check root directory name
        if(!_rootDirFname.SequenceEqual(qnxSb.RootDir.di_fname)) return false;

        // Check sizes are multiple of blocks
        if(qnxSb.RootDir.di_size % 512 != 0 ||
           qnxSb.Inode.di_size   % 512 != 0 ||
           qnxSb.Boot.di_size    % 512 != 0 ||
           qnxSb.AltBoot.di_size % 512 != 0)
            return false;

        // Check extents are not past device
        if(qnxSb.RootDir.di_first_xtnt.xtnt_blk + partition.Start >= partition.End ||
           qnxSb.Inode.di_first_xtnt.xtnt_blk   + partition.Start >= partition.End ||
           qnxSb.Boot.di_first_xtnt.xtnt_blk    + partition.Start >= partition.End ||
           qnxSb.AltBoot.di_first_xtnt.xtnt_blk + partition.Start >= partition.End)
            return false;

        // Check inodes are in use
        return (qnxSb.RootDir.di_status & 0x01) == 0x01 &&
               (qnxSb.Inode.di_status   & 0x01) == 0x01 &&
               (qnxSb.Boot.di_status    & 0x01) == 0x01;

        // All hail filesystems without identification marks
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();
        ErrorNumber errno = imagePlugin.ReadSector(partition.Start + 1, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return;

        if(sector.Length < 512) return;

        qnx4_super_block qnxSb = Marshal.ByteArrayToStructureLittleEndian<qnx4_super_block>(sector);

        var sb = new StringBuilder();

        sb.AppendLine(Localization.QNX4_filesystem);
        sb.AppendFormat(Localization._0_bytes_per_block, QNX4_BLOCK_SIZE).AppendLine();

        sb.AppendFormat(Localization._0_blocks_in_volume_1_bytes, partition.Length, partition.Length * QNX4_BLOCK_SIZE)
          .AppendLine();

        sb.AppendFormat(Localization.Root_directory_size_0_bytes, qnxSb.RootDir.di_size).AppendLine();

        sb.AppendFormat(Localization.Root_directory_extents_0, qnxSb.RootDir.di_num_xtnts).AppendLine();

        sb.AppendFormat(Localization.Root_directory_starts_at_block_0, qnxSb.RootDir.di_first_xtnt.xtnt_blk)
          .AppendLine();

        sb.AppendFormat(Localization.Inode_bitmap_size_0_bytes, qnxSb.Inode.di_size).AppendLine();

        sb.AppendFormat(Localization.Inode_bitmap_starts_at_block_0, qnxSb.Inode.di_first_xtnt.xtnt_blk).AppendLine();

        if(qnxSb.Boot.di_size > 0)
        {
            sb.AppendFormat(Localization.Boot_image_size_0_bytes, qnxSb.Boot.di_size).AppendLine();

            sb.AppendFormat(Localization.Boot_image_starts_at_block_0, qnxSb.Boot.di_first_xtnt.xtnt_blk).AppendLine();
        }

        if(qnxSb.AltBoot.di_size > 0)
        {
            sb.AppendFormat(Localization.Alternate_boot_image_size_0_bytes, qnxSb.AltBoot.di_size).AppendLine();

            sb.AppendFormat(Localization.Alternate_boot_image_starts_at_block_0, qnxSb.AltBoot.di_first_xtnt.xtnt_blk)
              .AppendLine();
        }

        sb.AppendFormat(Localization.Created_on_0, DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_ftime))
          .AppendLine();

        sb.AppendFormat(Localization.Last_modified_on_0, DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_mtime))
          .AppendLine();

        sb.AppendFormat(Localization.Last_accessed_on_0, DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_atime))
          .AppendLine();

        metadata = new FileSystem
        {
            Type             = FS_TYPE,
            Clusters         = partition.Length,
            ClusterSize      = QNX4_BLOCK_SIZE,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_ftime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_mtime),
            Bootable         = qnxSb.Boot.di_size != 0 || qnxSb.AltBoot.di_size != 0
        };

        information = sb.ToString();
    }

#endregion
}