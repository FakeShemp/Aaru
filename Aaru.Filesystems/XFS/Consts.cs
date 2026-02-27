// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    const uint XFS_MAGIC = 0x58465342;

    const string FS_TYPE = "xfs";

    // Superblock version numbers
    const ushort XFS_SB_VERSION_1 = 1;
    const ushort XFS_SB_VERSION_2 = 2;
    const ushort XFS_SB_VERSION_3 = 3;
    const ushort XFS_SB_VERSION_4 = 4;
    const ushort XFS_SB_VERSION_5 = 5;

    // Superblock version feature bits
    const ushort XFS_SB_VERSION_NUMBITS     = 0x000F;
    const ushort XFS_SB_VERSION_ATTRBIT     = 0x0010;
    const ushort XFS_SB_VERSION_NLINKBIT    = 0x0020;
    const ushort XFS_SB_VERSION_QUOTABIT    = 0x0040;
    const ushort XFS_SB_VERSION_ALIGNBIT    = 0x0080;
    const ushort XFS_SB_VERSION_DALIGNBIT   = 0x0100;
    const ushort XFS_SB_VERSION_SHAREDBIT   = 0x0200;
    const ushort XFS_SB_VERSION_LOGV2BIT    = 0x0400;
    const ushort XFS_SB_VERSION_SECTORBIT   = 0x0800;
    const ushort XFS_SB_VERSION_EXTFLGBIT   = 0x1000;
    const ushort XFS_SB_VERSION_DIRV2BIT    = 0x2000;
    const ushort XFS_SB_VERSION_BORGBIT     = 0x4000;
    const ushort XFS_SB_VERSION_MOREBITSBIT = unchecked(0x8000);

    // sb_features2 bits
    const uint XFS_SB_VERSION2_LAZYSBCOUNTBIT = 0x00000002;
    const uint XFS_SB_VERSION2_ATTR2BIT       = 0x00000008;
    const uint XFS_SB_VERSION2_PARENTBIT      = 0x00000010;
    const uint XFS_SB_VERSION2_PROJID32BIT    = 0x00000080;
    const uint XFS_SB_VERSION2_CRCBIT         = 0x00000100;
    const uint XFS_SB_VERSION2_FTYPE          = 0x00000200;

    // V5 superblock read-only compatible feature bits
    const uint XFS_SB_FEAT_RO_COMPAT_FINOBT   = 1 << 0;
    const uint XFS_SB_FEAT_RO_COMPAT_RMAPBT   = 1 << 1;
    const uint XFS_SB_FEAT_RO_COMPAT_REFLINK  = 1 << 2;
    const uint XFS_SB_FEAT_RO_COMPAT_INOBTCNT = 1 << 3;

    // V5 superblock incompatible feature bits
    const uint XFS_SB_FEAT_INCOMPAT_FTYPE       = 1 << 0;
    const uint XFS_SB_FEAT_INCOMPAT_SPINODES    = 1 << 1;
    const uint XFS_SB_FEAT_INCOMPAT_META_UUID   = 1 << 2;
    const uint XFS_SB_FEAT_INCOMPAT_BIGTIME     = 1 << 3;
    const uint XFS_SB_FEAT_INCOMPAT_NEEDSREPAIR = 1 << 4;
    const uint XFS_SB_FEAT_INCOMPAT_NREXT64     = 1 << 5;
    const uint XFS_SB_FEAT_INCOMPAT_EXCHRANGE   = 1 << 6;
    const uint XFS_SB_FEAT_INCOMPAT_PARENT      = 1 << 7;
    const uint XFS_SB_FEAT_INCOMPAT_METADIR     = 1 << 8;
    const uint XFS_SB_FEAT_INCOMPAT_ZONED       = 1 << 9;
    const uint XFS_SB_FEAT_INCOMPAT_ZONE_GAPS   = 1 << 10;

    // V5 superblock log incompatible feature bits
    const uint XFS_SB_FEAT_INCOMPAT_LOG_XATTRS = 1 << 0;

    // AG header magic numbers
    const uint XFS_AGF_MAGIC  = 0x58414746; // 'XAGF'
    const uint XFS_AGI_MAGIC  = 0x58414749; // 'XAGI'
    const uint XFS_AGFL_MAGIC = 0x5841464C; // 'XAFL'

    // Inode magic number
    const ushort XFS_DINODE_MAGIC = 0x494E; // 'IN'

    // Realtime superblock magic
    const uint XFS_RTSB_MAGIC = 0x46726F67; // 'Frog'

    // Realtime bitmap and summary magic
    const uint XFS_RTBITMAP_MAGIC  = 0x424D505A; // 'BMPZ'
    const uint XFS_RTSUMMARY_MAGIC = 0x53554D59; // 'SUMY'

    // Quota magic
    const ushort XFS_DQUOT_MAGIC = 0x4451; // 'DQ'

    // Symlink magic
    const uint XFS_SYMLINK_MAGIC = 0x58534C4D; // 'XSLM'

    // Allocation btree magic numbers
    const uint XFS_ABTB_MAGIC     = 0x41425442; // 'ABTB'
    const uint XFS_ABTB_CRC_MAGIC = 0x41423342; // 'AB3B'
    const uint XFS_ABTC_MAGIC     = 0x41425443; // 'ABTC'
    const uint XFS_ABTC_CRC_MAGIC = 0x41423343; // 'AB3C'

    // Inode btree magic numbers
    const uint XFS_IBT_MAGIC      = 0x49414254; // 'IABT'
    const uint XFS_IBT_CRC_MAGIC  = 0x49414233; // 'IAB3'
    const uint XFS_FIBT_MAGIC     = 0x46494254; // 'FIBT'
    const uint XFS_FIBT_CRC_MAGIC = 0x46494233; // 'FIB3'

    // Reverse mapping btree magic
    const uint XFS_RMAP_CRC_MAGIC   = 0x524D4233; // 'RMB3'
    const uint XFS_RTRMAP_CRC_MAGIC = 0x4D415052; // 'MAPR'

    // Reference count btree magic
    const uint XFS_REFC_CRC_MAGIC   = 0x52334643; // 'R3FC'
    const uint XFS_RTREFC_CRC_MAGIC = 0x52434E54; // 'RCNT'

    // BMAP btree magic numbers
    const uint XFS_BMAP_MAGIC     = 0x424D4150; // 'BMAP'
    const uint XFS_BMAP_CRC_MAGIC = 0x424D4133; // 'BMA3'

    // DA btree magic numbers
    const ushort XFS_DA_NODE_MAGIC    = 0xFEBE;
    const ushort XFS_DA3_NODE_MAGIC   = 0x3EBE;
    const ushort XFS_ATTR_LEAF_MAGIC  = 0xFBEE;
    const ushort XFS_ATTR3_LEAF_MAGIC = 0x3BEE;

    // Directory magic numbers
    const uint   XFS_DIR2_BLOCK_MAGIC = 0x58443242; // 'XD2B'
    const uint   XFS_DIR2_DATA_MAGIC  = 0x58443244; // 'XD2D'
    const uint   XFS_DIR2_FREE_MAGIC  = 0x58443246; // 'XD2F'
    const uint   XFS_DIR3_BLOCK_MAGIC = 0x58444233; // 'XDB3'
    const uint   XFS_DIR3_DATA_MAGIC  = 0x58444433; // 'XDD3'
    const uint   XFS_DIR3_FREE_MAGIC  = 0x58444633; // 'XDF3'
    const ushort XFS_DIR2_LEAF1_MAGIC = 0xD2F1;
    const ushort XFS_DIR2_LEAFN_MAGIC = 0xD2FF;
    const ushort XFS_DIR3_LEAF1_MAGIC = 0x3DF1;
    const ushort XFS_DIR3_LEAFN_MAGIC = 0x3DFF;

    // Remote attribute magic
    const uint XFS_ATTR3_RMT_MAGIC = 0x5841524D; // 'XARM'

    // Inode format types (di_format)
    const byte XFS_DINODE_FMT_DEV        = 0;
    const byte XFS_DINODE_FMT_LOCAL      = 1;
    const byte XFS_DINODE_FMT_EXTENTS    = 2;
    const byte XFS_DINODE_FMT_BTREE      = 3;
    const byte XFS_DINODE_FMT_UUID       = 4;
    const byte XFS_DINODE_FMT_META_BTREE = 5;

    // Inode di_flags
    const ushort XFS_DIFLAG_REALTIME     = 1 << 0;
    const ushort XFS_DIFLAG_PREALLOC     = 1 << 1;
    const ushort XFS_DIFLAG_NEWRTBM      = 1 << 2;
    const ushort XFS_DIFLAG_IMMUTABLE    = 1 << 3;
    const ushort XFS_DIFLAG_APPEND       = 1 << 4;
    const ushort XFS_DIFLAG_SYNC         = 1 << 5;
    const ushort XFS_DIFLAG_NOATIME      = 1 << 6;
    const ushort XFS_DIFLAG_NODUMP       = 1 << 7;
    const ushort XFS_DIFLAG_RTINHERIT    = 1 << 8;
    const ushort XFS_DIFLAG_PROJINHERIT  = 1 << 9;
    const ushort XFS_DIFLAG_NOSYMLINKS   = 1 << 10;
    const ushort XFS_DIFLAG_EXTSIZE      = 1 << 11;
    const ushort XFS_DIFLAG_EXTSZINHERIT = 1 << 12;
    const ushort XFS_DIFLAG_NODEFRAG     = 1 << 13;
    const ushort XFS_DIFLAG_FILESTREAM   = 1 << 14;

    // Inode di_flags2
    const ulong XFS_DIFLAG2_DAX        = 1UL << 0;
    const ulong XFS_DIFLAG2_REFLINK    = 1UL << 1;
    const ulong XFS_DIFLAG2_COWEXTSIZE = 1UL << 2;
    const ulong XFS_DIFLAG2_BIGTIME    = 1UL << 3;
    const ulong XFS_DIFLAG2_NREXT64    = 1UL << 4;
    const ulong XFS_DIFLAG2_METADATA   = 1UL << 5;

    // Directory file type values
    const byte XFS_DIR3_FT_UNKNOWN  = 0;
    const byte XFS_DIR3_FT_REG_FILE = 1;
    const byte XFS_DIR3_FT_DIR      = 2;
    const byte XFS_DIR3_FT_CHRDEV   = 3;
    const byte XFS_DIR3_FT_BLKDEV   = 4;
    const byte XFS_DIR3_FT_FIFO     = 5;
    const byte XFS_DIR3_FT_SOCK     = 6;
    const byte XFS_DIR3_FT_SYMLINK  = 7;
    const byte XFS_DIR3_FT_WHT      = 8;

    // Attribute flags
    const byte XFS_ATTR_LOCAL      = 1 << 0;
    const byte XFS_ATTR_ROOT       = 1 << 1;
    const byte XFS_ATTR_SECURE     = 1 << 2;
    const byte XFS_ATTR_PARENT     = 1 << 3;
    const byte XFS_ATTR_INCOMPLETE = 1 << 7;

    // Quota type flags
    const byte XFS_DQTYPE_USER    = 1 << 0;
    const byte XFS_DQTYPE_PROJ    = 1 << 1;
    const byte XFS_DQTYPE_GROUP   = 1 << 2;
    const byte XFS_DQTYPE_BIGTIME = 1 << 7;

    // Metafile types
    const byte XFS_METAFILE_UNKNOWN    = 0;
    const byte XFS_METAFILE_DIR        = 1;
    const byte XFS_METAFILE_USRQUOTA   = 2;
    const byte XFS_METAFILE_GRPQUOTA   = 3;
    const byte XFS_METAFILE_PRJQUOTA   = 4;
    const byte XFS_METAFILE_RTBITMAP   = 5;
    const byte XFS_METAFILE_RTSUMMARY  = 6;
    const byte XFS_METAFILE_RTRMAP     = 7;
    const byte XFS_METAFILE_RTREFCOUNT = 8;

    // Unlinked inode hash table size
    const int XFS_AGI_UNLINKED_BUCKETS = 64;

    // Attribute leaf map size
    const int XFS_ATTR_LEAF_MAPSIZE = 3;

    // Directory data free count
    const int XFS_DIR2_DATA_FD_COUNT = 3;

    // Maximum inode number (56 bits)
    const ulong XFS_MAXINUMBER = (1UL << 56) - 1;
}