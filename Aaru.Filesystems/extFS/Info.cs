// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem plugin.
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
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
/// <summary>Implements detection of the Linux extended filesystem</summary>

// ReSharper disable once InconsistentNaming
public sealed partial class extFS
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(imagePlugin.Info.SectorSize < 512) return false;

        ulong sbSectorOff = SB_POS / imagePlugin.Info.SectorSize;
        uint  sbOff       = SB_POS % imagePlugin.Info.SectorSize;

        if(sbSectorOff + partition.Start >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSector(sbSectorOff + partition.Start, false, out byte[] sbSector, out _);

        if(errno != ErrorNumber.NoError) return false;

        var sb = new byte[512];

        if(sbOff + 512 > sbSector.Length) return false;

        Array.Copy(sbSector, sbOff, sb, 0, 512);

        var magic = BitConverter.ToUInt16(sb, 0x038);

        return magic == EXT_MAGIC;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();

        var sb = new StringBuilder();

        if(imagePlugin.Info.SectorSize < 512) return;

        ulong sbSectorOff = SB_POS / imagePlugin.Info.SectorSize;
        uint  sbOff       = SB_POS % imagePlugin.Info.SectorSize;

        if(sbSectorOff + partition.Start >= partition.End) return;

        ErrorNumber errno = imagePlugin.ReadSector(sbSectorOff + partition.Start, false, out byte[] sblock, out _);

        if(errno != ErrorNumber.NoError) return;

        var sbSector = new byte[512];
        Array.Copy(sblock, sbOff, sbSector, 0, 512);

        var extSb = new ext_super_block
        {
            s_ninodes         = BitConverter.ToUInt32(sbSector, 0x000),
            s_nzones          = BitConverter.ToUInt32(sbSector, 0x004),
            s_firstfreeblock  = BitConverter.ToUInt32(sbSector, 0x008),
            s_freeblockscount = BitConverter.ToUInt32(sbSector, 0x00C),
            s_firstfreeinode  = BitConverter.ToUInt32(sbSector, 0x010),
            s_freeinodescount = BitConverter.ToUInt32(sbSector, 0x014),
            s_firstdatazone   = BitConverter.ToUInt32(sbSector, 0x018),
            s_log_zone_size   = BitConverter.ToUInt32(sbSector, 0x01C),
            s_max_size        = BitConverter.ToUInt32(sbSector, 0x020)
        };

        sb.AppendLine(Localization.ext_filesystem);
        sb.AppendFormat(Localization._0_zones_in_volume,     extSb.s_nzones);
        sb.AppendFormat(Localization._0_free_blocks_1_bytes, extSb.s_freeblockscount, extSb.s_freeblockscount * 1024);

        sb.AppendFormat(Localization._0_inodes_in_volume_1_free_2,
                        extSb.s_ninodes,
                        extSb.s_freeinodescount,
                        extSb.s_freeinodescount * 100 / extSb.s_ninodes);

        sb.AppendFormat(Localization.First_free_inode_is_0, extSb.s_firstfreeinode);
        sb.AppendFormat(Localization.First_free_block_is_0, extSb.s_firstfreeblock);
        sb.AppendFormat(Localization.First_data_zone_is_0,  extSb.s_firstdatazone);
        sb.AppendFormat(Localization.Log_zone_size_0,       extSb.s_log_zone_size);
        sb.AppendFormat(Localization.Max_zone_size_0,       extSb.s_max_size);

        metadata = new FileSystem
        {
            Type         = FS_TYPE,
            FreeClusters = extSb.s_freeblockscount,
            ClusterSize  = 1024,
            Clusters     = (partition.End - partition.Start + 1) * imagePlugin.Info.SectorSize / 1024
        };

        information = sb.ToString();
    }

#endregion
}