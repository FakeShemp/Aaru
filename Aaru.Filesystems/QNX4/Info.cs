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

        // Too much useless information
        /*
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_fname = {0}", CurrentEncoding.GetString(qnxSb.RootDir.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_size = {0}", qnxSb.RootDir.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_first_xtnt.xtnt_blk = {0}", qnxSb.RootDir.di_first_xtnt.xtnt_blk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_first_xtnt.xtnt_size = {0}", qnxSb.RootDir.di_first_xtnt.xtnt_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_xblk = {0}", qnxSb.RootDir.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.RootDir.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.RootDir.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.RootDir.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.RootDir.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_num_xtnts = {0}", qnxSb.RootDir.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_mode = {0}", Convert.ToString(qnxSb.RootDir.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_uid = {0}", qnxSb.RootDir.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_gid = {0}", qnxSb.RootDir.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_nlink = {0}", qnxSb.RootDir.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_zero = {0}", qnxSb.RootDir.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_type = {0}", qnxSb.RootDir.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.RootDir.di_status = {0}", qnxSb.RootDir.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_fname = {0}", CurrentEncoding.GetString(qnxSb.Inode.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_size = {0}", qnxSb.Inode.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_first_xtnt.xtnt_blk = {0}", qnxSb.Inode.di_first_xtnt.xtnt_blk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_first_xtnt.xtnt_size = {0}", qnxSb.Inode.di_first_xtnt.xtnt_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_xblk = {0}", qnxSb.Inode.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Inode.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Inode.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Inode.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Inode.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_num_xtnts = {0}", qnxSb.Inode.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_mode = {0}", Convert.ToString(qnxSb.Inode.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_uid = {0}", qnxSb.Inode.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_gid = {0}", qnxSb.Inode.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_nlink = {0}", qnxSb.Inode.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_zero = {0}", qnxSb.Inode.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_type = {0}", qnxSb.Inode.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Inode.di_status = {0}", qnxSb.Inode.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_fname = {0}", CurrentEncoding.GetString(qnxSb.Boot.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_size = {0}", qnxSb.Boot.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_first_xtnt.xtnt_blk = {0}", qnxSb.Boot.di_first_xtnt.xtnt_blk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_first_xtnt.xtnt_size = {0}", qnxSb.Boot.di_first_xtnt.xtnt_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_xblk = {0}", qnxSb.Boot.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Boot.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Boot.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Boot.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.Boot.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_num_xtnts = {0}", qnxSb.Boot.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_mode = {0}", Convert.ToString(qnxSb.Boot.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_uid = {0}", qnxSb.Boot.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_gid = {0}", qnxSb.Boot.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_nlink = {0}", qnxSb.Boot.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_zero = {0}", qnxSb.Boot.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_type = {0}", qnxSb.Boot.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.Boot.di_status = {0}", qnxSb.Boot.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_fname = {0}", CurrentEncoding.GetString(qnxSb.AltBoot.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_size = {0}", qnxSb.AltBoot.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_first_xtnt.xtnt_blk = {0}", qnxSb.AltBoot.di_first_xtnt.xtnt_blk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_first_xtnt.xtnt_size = {0}", qnxSb.AltBoot.di_first_xtnt.xtnt_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_xblk = {0}", qnxSb.AltBoot.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.AltBoot.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.AltBoot.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.AltBoot.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.AltBoot.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_num_xtnts = {0}", qnxSb.AltBoot.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_mode = {0}", Convert.ToString(qnxSb.AltBoot.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_uid = {0}", qnxSb.AltBoot.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_gid = {0}", qnxSb.AltBoot.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_nlink = {0}", qnxSb.AltBoot.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_zero = {0}", qnxSb.AltBoot.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_type = {0}", qnxSb.AltBoot.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.AltBoot.di_status = {0}", qnxSb.AltBoot.di_status);
        */

        information = Localization.QNX4_filesystem +
                      "\n"                         +
                      string.Format(Localization.Created_on_0,
                                    DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_ftime)) +
                      "\n";

        metadata = new FileSystem
        {
            Type             = FS_TYPE,
            Clusters         = partition.Length,
            ClusterSize      = 512,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_ftime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(qnxSb.RootDir.di_mtime)
        };

        metadata.Bootable |= qnxSb.Boot.di_size != 0 || qnxSb.AltBoot.di_size != 0;
    }

#endregion
}