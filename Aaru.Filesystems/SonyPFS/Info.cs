// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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

public partial class SonyPFS
{
    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        uint sectorSize = imagePlugin.Info.SectorSize;

        if(sectorSize < 512) return false;

        // Superblock is at sector 0 relative to partition data start
        int sbSize = Marshal.SizeOf<SuperBlock>();

        var sectorsToRead = (uint)((sbSize + sectorSize - 1) / sectorSize);

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectorsToRead, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < sbSize) return false;

        SuperBlock sb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

        if(sb.magic != PFS_SUPER_MAGIC) return false;

        if(sb.version > PFS_FORMAT_VERSION) return false;

        return true;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();

        uint sectorSize = imagePlugin.Info.SectorSize;

        if(sectorSize < 512)
            return;

        int sbSize        = Marshal.SizeOf<SuperBlock>();
        var sectorsToRead = (uint)((sbSize + sectorSize - 1) / sectorSize);

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectorsToRead, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
            return;

        if(sector.Length < sbSize)
            return;

        SuperBlock sb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

        if(sb.magic != PFS_SUPER_MAGIC)
            return;

        var sbInfo = new StringBuilder();

        sbInfo.AppendLine(Localization.PFS_filesystem);
        sbInfo.AppendFormat(Localization.Format_version_0, sb.version).AppendLine();

        sbInfo.AppendFormat(Localization.Last_modified_by_module_version_0_1, sb.modver >> 8, sb.modver & 0xFF)
              .AppendLine();

        sbInfo.AppendFormat(Localization._0_bytes_per_zone, sb.zone_size).AppendLine();
        sbInfo.AppendFormat(Localization._0_sub_partitions, sb.num_subs).AppendLine();

        sbInfo.AppendFormat(Localization.Root_directory_is_at_block_0_sub_partition_1,
                            sb.root.number, sb.root.subpart)
              .AppendLine();

        sbInfo.AppendFormat(Localization.Journal_is_at_block_0_sub_partition_1_size_2_blocks,
                            sb.log.number, sb.log.subpart, sb.log.count)
              .AppendLine();

        if((sb.pfsFsckStat & (uint)FsckStatus.WRITE_ERROR) != 0)
            sbInfo.AppendLine(Localization.Filesystem_has_write_errors);

        if((sb.pfsFsckStat & (uint)FsckStatus.ERRORS_FIXED) != 0)
            sbInfo.AppendLine(Localization.Filesystem_errors_were_fixed);

        if(sb.pfsFsckStat != 0)
            sbInfo.AppendLine(Localization.Volume_is_dirty);

        information = sbInfo.ToString();

        metadata = new FileSystem
        {
            Type        = FS_TYPE,
            ClusterSize = sb.zone_size,
            Clusters    = (partition.End - partition.Start + 1) * sectorSize / sb.zone_size,
            Dirty       = sb.pfsFsckStat != 0
        };
    }
}