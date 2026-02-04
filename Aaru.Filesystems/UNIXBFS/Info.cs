// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UnixWare boot filesystem plugin.
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
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class BFS
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(2 + partition.Start >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSector(0 + partition.Start, false, out byte[] tmp, out _);

        if(errno != ErrorNumber.NoError) return false;

        var magic = BitConverter.ToUInt32(tmp, 0);

        return magic is BFS_MAGIC or BFS_MAGIC_BE;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= Encoding.GetEncoding("iso-8859-15");
        information =   "";
        metadata    =   new FileSystem();

        var         sb    = new StringBuilder();
        ErrorNumber errno = imagePlugin.ReadSector(0 + partition.Start, false, out byte[] bfsSbSector, out _);

        if(errno != ErrorNumber.NoError) return;

        SuperBlock bfsSb;

        // Check endianness
        var magic = BitConverter.ToUInt32(bfsSbSector, 0);

        if(magic == BFS_MAGIC)
            bfsSb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(bfsSbSector);
        else if(magic == BFS_MAGIC_BE)
            bfsSb = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(bfsSbSector);
        else
            return;

        string fsName  = StringHandlers.CToString(bfsSb.s_fsname, encoding);
        string volName = StringHandlers.CToString(bfsSb.s_volume, encoding);

        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_magic: 0x{0:X8}", bfsSb.s_magic);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_start: 0x{0:X8}", bfsSb.s_start);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_end: 0x{0:X8}",   bfsSb.s_end);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_from: 0x{0:X8}",  bfsSb.s_from);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_to: 0x{0:X8}",    bfsSb.s_to);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_bfrom: 0x{0:X8}", bfsSb.s_bfrom);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_bto: 0x{0:X8}",   bfsSb.s_bto);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_fsname: 0x{0}",   fsName);
        AaruLogging.Debug(MODULE_NAME, "bfs_sb.s_volume: 0x{0}",   volName);

        sb.AppendLine(Localization.UNIX_Boot_Filesystem);

        sb.AppendFormat(Localization.Volume_goes_from_byte_0_to_byte_1_for_2_bytes,
                        bfsSb.s_start,
                        bfsSb.s_end,
                        bfsSb.s_end - bfsSb.s_start)
          .AppendLine();

        sb.AppendFormat(Localization.Filesystem_name_0, fsName).AppendLine();
        sb.AppendFormat(Localization.Volume_name_0,     volName).AppendLine();

        metadata = new FileSystem
        {
            Type        = FS_TYPE,
            VolumeName  = volName,
            ClusterSize = imagePlugin.Info.SectorSize,
            Clusters    = partition.End - partition.Start + 1
        };

        information = sb.ToString();
    }

#endregion
}