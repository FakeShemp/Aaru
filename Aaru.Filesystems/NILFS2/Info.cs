// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

// ReSharper disable UnusedMember.Local

using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class NILFS2
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(imagePlugin.Info.SectorSize < 512) return false;

        uint sbAddr = NILFS2_SUPER_OFFSET / imagePlugin.Info.SectorSize;

        if(sbAddr == 0) sbAddr = 1;

        var sbSize = (uint)(Marshal.SizeOf<Superblock>() / imagePlugin.Info.SectorSize);

        if(Marshal.SizeOf<Superblock>() % imagePlugin.Info.SectorSize != 0) sbSize++;

        if(partition.Start + sbAddr + sbSize >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start + sbAddr, false, sbSize, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < Marshal.SizeOf<Superblock>()) return false;

        Superblock nilfsSb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        if(nilfsSb.magic == NILFS2_MAGIC) return true;

        // Try secondary superblock at NILFS_SB2_OFFSET_BYTES(devsize) = (((devsize >> 12) - 1) << 12)
        ulong partitionSize = (partition.End - partition.Start + 1) * imagePlugin.Info.SectorSize;

        if(partitionSize < NILFS2_SEG_MIN_BLOCKS * NILFS2_MIN_BLOCK_SIZE + 4096) return false;

        ulong sb2Offset = ((partitionSize >> 12) - 1) << 12;
        uint  sb2Addr   = (uint)(sb2Offset / imagePlugin.Info.SectorSize);

        if(partition.Start + sb2Addr + sbSize >= partition.End) return false;

        errno = imagePlugin.ReadSectors(partition.Start + sb2Addr, false, sbSize, out byte[] sector2, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector2.Length < Marshal.SizeOf<Superblock>()) return false;

        Superblock nilfsSb2 = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector2);

        return nilfsSb2.magic == NILFS2_MAGIC;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= Encoding.UTF8;
        information =   "";
        metadata    =   new FileSystem();

        if(imagePlugin.Info.SectorSize < 512) return;

        uint sbAddr = NILFS2_SUPER_OFFSET / imagePlugin.Info.SectorSize;

        if(sbAddr == 0) sbAddr = 1;

        var sbSize = (uint)(Marshal.SizeOf<Superblock>() / imagePlugin.Info.SectorSize);

        if(Marshal.SizeOf<Superblock>() % imagePlugin.Info.SectorSize != 0) sbSize++;

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start + sbAddr, false, sbSize, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return;

        if(sector.Length < Marshal.SizeOf<Superblock>()) return;

        Superblock nilfsSb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        bool primaryValid = nilfsSb.magic == NILFS2_MAGIC;

        // Try the secondary superblock at NILFS_SB2_OFFSET_BYTES(devsize) = (((devsize >> 12) - 1) << 12)
        ulong partitionSize = (partition.End - partition.Start + 1) * imagePlugin.Info.SectorSize;

        if(partitionSize >= NILFS2_SEG_MIN_BLOCKS * NILFS2_MIN_BLOCK_SIZE + 4096)
        {
            ulong sb2Offset = ((partitionSize >> 12) - 1) << 12;
            uint  sb2Addr   = (uint)(sb2Offset / imagePlugin.Info.SectorSize);

            if(partition.Start + sb2Addr + sbSize < partition.End)
            {
                ErrorNumber sb2Errno =
                    imagePlugin.ReadSectors(partition.Start + sb2Addr, false, sbSize, out byte[] sector2, out _);

                if(sb2Errno == ErrorNumber.NoError && sector2.Length >= Marshal.SizeOf<Superblock>())
                {
                    Superblock sb2 = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector2);

                    if(sb2.magic == NILFS2_MAGIC && (!primaryValid || sb2.last_cno > nilfsSb.last_cno))
                    {
                        nilfsSb      = sb2;
                        primaryValid = true;
                    }
                }
            }
        }

        if(!primaryValid) return;

        var sb = new StringBuilder();

        sb.AppendLine(Localization.NILFS2_filesystem);
        sb.AppendFormat(Localization.Version_0_1,           nilfsSb.rev_level, nilfsSb.minor_rev_level).AppendLine();
        sb.AppendFormat(Localization._0_bytes_per_block,    1 << (int)(nilfsSb.log_block_size + 10)).AppendLine();
        sb.AppendFormat(Localization._0_bytes_in_volume,    nilfsSb.dev_size).AppendLine();
        sb.AppendFormat(Localization._0_blocks_per_segment, nilfsSb.blocks_per_segment).AppendLine();
        sb.AppendFormat(Localization._0_segments,           nilfsSb.nsegments).AppendLine();

        if(nilfsSb.creator_os == 0)
            sb.AppendLine(Localization.Filesystem_created_on_Linux);
        else
            sb.AppendFormat(Localization.Creator_OS_code_0, nilfsSb.creator_os).AppendLine();

        sb.AppendFormat(Localization._0_bytes_per_inode, nilfsSb.inode_size).AppendLine();
        sb.AppendFormat(Localization.Volume_UUID_0,      nilfsSb.uuid).AppendLine();

        sb.AppendFormat(Localization.Volume_name_0, StringHandlers.CToString(nilfsSb.volume_name, encoding))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_created_on_0, DateHandlers.UnixUnsignedToDateTime(nilfsSb.ctime))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_last_mounted_on_0, DateHandlers.UnixUnsignedToDateTime(nilfsSb.mtime))
          .AppendLine();

        sb.AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixUnsignedToDateTime(nilfsSb.wtime))
          .AppendLine();

        information = sb.ToString();

        metadata = new FileSystem
        {
            Type             = FS_TYPE,
            ClusterSize      = (uint)(1 << (int)(nilfsSb.log_block_size + 10)),
            VolumeName       = StringHandlers.CToString(nilfsSb.volume_name, encoding),
            VolumeSerial     = nilfsSb.uuid.ToString(),
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(nilfsSb.ctime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(nilfsSb.wtime)
        };

        if(nilfsSb.creator_os == 0) metadata.SystemIdentifier = "Linux";

        metadata.Clusters = nilfsSb.dev_size / metadata.ClusterSize;
    }

#endregion
}