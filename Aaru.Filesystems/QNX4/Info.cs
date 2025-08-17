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
// Copyright © 2011-2025 Natalia Portillo
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

        ErrorNumber errno = imagePlugin.ReadSector(partition.Start + 1, out byte[] sector);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < 512) return false;

        Superblock qnxSb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        // Check root directory name
        if(!_rootDirFname.SequenceEqual(qnxSb.rootDir.di_fname)) return false;

        // Check sizes are multiple of blocks
        if(qnxSb.rootDir.di_size % 512 != 0 ||
           qnxSb.inode.di_size   % 512 != 0 ||
           qnxSb.boot.di_size    % 512 != 0 ||
           qnxSb.altBoot.di_size % 512 != 0)
            return false;

        // Check extents are not past device
        if(qnxSb.rootDir.di_first_xtnt.Block + partition.Start >= partition.End ||
           qnxSb.inode.di_first_xtnt.Block   + partition.Start >= partition.End ||
           qnxSb.boot.di_first_xtnt.Block    + partition.Start >= partition.End ||
           qnxSb.altBoot.di_first_xtnt.Block + partition.Start >= partition.End)
            return false;

        // Check inodes are in use
        return (qnxSb.rootDir.di_status & 0x01) == 0x01 &&
               (qnxSb.inode.di_status   & 0x01) == 0x01 &&
               (qnxSb.boot.di_status    & 0x01) == 0x01;

        // All hail filesystems without identification marks
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();
        ErrorNumber errno = imagePlugin.ReadSector(partition.Start + 1, out byte[] sector);

        if(errno != ErrorNumber.NoError) return;

        if(sector.Length < 512) return;

        Superblock qnxSb = Marshal.ByteArrayToStructureLittleEndian<Superblock>(sector);

        // Too much useless information
        /*
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_fname = {0}", CurrentEncoding.GetString(qnxSb.rootDir.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_size = {0}", qnxSb.rootDir.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_first_xtnt.block = {0}", qnxSb.rootDir.di_first_xtnt.block);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_first_xtnt.length = {0}", qnxSb.rootDir.di_first_xtnt.length);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_xblk = {0}", qnxSb.rootDir.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.rootDir.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.rootDir.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.rootDir.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.rootDir.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_num_xtnts = {0}", qnxSb.rootDir.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_mode = {0}", Convert.ToString(qnxSb.rootDir.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_uid = {0}", qnxSb.rootDir.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_gid = {0}", qnxSb.rootDir.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_nlink = {0}", qnxSb.rootDir.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_zero = {0}", qnxSb.rootDir.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_type = {0}", qnxSb.rootDir.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.rootDir.di_status = {0}", qnxSb.rootDir.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_fname = {0}", CurrentEncoding.GetString(qnxSb.inode.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_size = {0}", qnxSb.inode.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_first_xtnt.block = {0}", qnxSb.inode.di_first_xtnt.block);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_first_xtnt.length = {0}", qnxSb.inode.di_first_xtnt.length);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_xblk = {0}", qnxSb.inode.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.inode.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.inode.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.inode.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.inode.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_num_xtnts = {0}", qnxSb.inode.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_mode = {0}", Convert.ToString(qnxSb.inode.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_uid = {0}", qnxSb.inode.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_gid = {0}", qnxSb.inode.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_nlink = {0}", qnxSb.inode.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_zero = {0}", qnxSb.inode.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_type = {0}", qnxSb.inode.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.inode.di_status = {0}", qnxSb.inode.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_fname = {0}", CurrentEncoding.GetString(qnxSb.boot.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_size = {0}", qnxSb.boot.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_first_xtnt.block = {0}", qnxSb.boot.di_first_xtnt.block);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_first_xtnt.length = {0}", qnxSb.boot.di_first_xtnt.length);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_xblk = {0}", qnxSb.boot.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.boot.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.boot.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.boot.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.boot.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_num_xtnts = {0}", qnxSb.boot.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_mode = {0}", Convert.ToString(qnxSb.boot.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_uid = {0}", qnxSb.boot.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_gid = {0}", qnxSb.boot.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_nlink = {0}", qnxSb.boot.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_zero = {0}", qnxSb.boot.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_type = {0}", qnxSb.boot.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.boot.di_status = {0}", qnxSb.boot.di_status);

        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_fname = {0}", CurrentEncoding.GetString(qnxSb.altBoot.di_fname));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_size = {0}", qnxSb.altBoot.di_size);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_first_xtnt.block = {0}", qnxSb.altBoot.di_first_xtnt.block);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_first_xtnt.length = {0}", qnxSb.altBoot.di_first_xtnt.length);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_xblk = {0}", qnxSb.altBoot.di_xblk);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_ftime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.altBoot.di_ftime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_mtime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.altBoot.di_mtime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_atime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.altBoot.di_atime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_ctime = {0}", DateHandlers.UNIXUnsignedToDateTime(qnxSb.altBoot.di_ctime));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_num_xtnts = {0}", qnxSb.altBoot.di_num_xtnts);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_mode = {0}", Convert.ToString(qnxSb.altBoot.di_mode, 8));
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_uid = {0}", qnxSb.altBoot.di_uid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_gid = {0}", qnxSb.altBoot.di_gid);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_nlink = {0}", qnxSb.altBoot.di_nlink);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_zero = {0}", qnxSb.altBoot.di_zero);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_type = {0}", qnxSb.altBoot.di_type);
        AaruLogging.DebugWriteLine(MODULE_NAME, "qnxSb.altBoot.di_status = {0}", qnxSb.altBoot.di_status);
        */

        information = Localization.QNX4_filesystem +
                      "\n"                         +
                      string.Format(Localization.Created_on_0,
                                    DateHandlers.UnixUnsignedToDateTime(qnxSb.rootDir.di_ftime)) +
                      "\n";

        metadata = new FileSystem
        {
            Type             = FS_TYPE,
            Clusters         = partition.Length,
            ClusterSize      = 512,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(qnxSb.rootDir.di_ftime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(qnxSb.rootDir.di_mtime)
        };

        metadata.Bootable |= qnxSb.boot.di_size != 0 || qnxSb.altBoot.di_size != 0;
    }

#endregion
}