// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Linux extended filesystem 2, 3 and 4 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the Linux extended filesystem 2, 3 and 4 and shows information.
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

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class ext2FS
{
    const int SB_POS = 0x400;

    /// <summary>Same magic for ext2, ext3 and ext4</summary>
    const ushort EXT2_MAGIC = 0xEF53;

    const ushort EXT2_MAGIC_OLD = 0xEF51;

    // ext? filesystem states
    /// <summary>Cleanly-unmounted volume</summary>
    const ushort EXT2_VALID_FS = 0x0001;
    /// <summary>Dirty volume</summary>
    const ushort EXT2_ERROR_FS = 0x0002;
    /// <summary>Recovering orphan files</summary>
    const ushort EXT3_ORPHAN_FS = 0x0004;

    // ext? default mount flags
    /// <summary>Enable debugging messages</summary>
    const uint EXT2_DEFM_DEBUG = 0x000001;
    /// <summary>Emulates BSD behaviour on new file creation</summary>
    const uint EXT2_DEFM_BSDGROUPS = 0x000002;
    /// <summary>Enable user xattrs</summary>
    const uint EXT2_DEFM_XATTR_USER = 0x000004;
    /// <summary>Enable POSIX ACLs</summary>
    const uint EXT2_DEFM_ACL = 0x000008;
    /// <summary>Use 16bit UIDs</summary>
    const uint EXT2_DEFM_UID16 = 0x000010;
    /// <summary>Journal data mode</summary>
    const uint EXT3_DEFM_JMODE_DATA = 0x000040;
    /// <summary>Journal ordered mode</summary>
    const uint EXT3_DEFM_JMODE_ORDERED = 0x000080;
    /// <summary>Journal writeback mode</summary>
    const uint EXT3_DEFM_JMODE_WBACK = 0x000100;

    // Behaviour on errors
    /// <summary>Continue execution</summary>
    const ushort EXT2_ERRORS_CONTINUE = 1;
    /// <summary>Remount fs read-only</summary>
    const ushort EXT2_ERRORS_RO = 2;
    /// <summary>Panic</summary>
    const ushort EXT2_ERRORS_PANIC = 3;

    // OS codes
    const uint EXT2_OS_LINUX   = 0;
    const uint EXT2_OS_HURD    = 1;
    const uint EXT2_OS_MASIX   = 2;
    const uint EXT2_OS_FREEBSD = 3;
    const uint EXT2_OS_LITES   = 4;

    // Revision levels
    /// <summary>The good old (original) format</summary>
    const uint EXT2_GOOD_OLD_REV = 0;
    /// <summary>V2 format w/ dynamic inode sizes</summary>
    const uint EXT2_DYNAMIC_REV = 1;

    // Compatible features
    /// <summary>Pre-allocate directories</summary>
    const uint EXT2_FEATURE_COMPAT_DIR_PREALLOC = 0x00000001;
    /// <summary>imagic inodes ?</summary>
    const uint EXT2_FEATURE_COMPAT_IMAGIC_INODES = 0x00000002;
    /// <summary>Has journal (it's ext3)</summary>
    const uint EXT3_FEATURE_COMPAT_HAS_JOURNAL = 0x00000004;
    /// <summary>EA blocks</summary>
    const uint EXT2_FEATURE_COMPAT_EXT_ATTR = 0x00000008;
    /// <summary>Online filesystem resize reservations</summary>
    const uint EXT2_FEATURE_COMPAT_RESIZE_INO = 0x00000010;
    /// <summary>Can use hashed indexes on directories</summary>
    const uint EXT2_FEATURE_COMPAT_DIR_INDEX = 0x00000020;
    /// <summary>Sparse super block v2</summary>
    const uint EXT4_FEATURE_COMPAT_SPARSE_SUPER2 = 0x00000200;
    /// <summary>Fast commit journal</summary>
    const uint EXT4_FEATURE_COMPAT_FAST_COMMIT = 0x00000400;
    /// <summary>Stable inodes (for encryption)</summary>
    const uint EXT4_FEATURE_COMPAT_STABLE_INODES = 0x00000800;
    /// <summary>Orphan file allocated</summary>
    const uint EXT4_FEATURE_COMPAT_ORPHAN_FILE = 0x00001000;

    // Read-only compatible features
    /// <summary>Reduced number of superblocks</summary>
    const uint EXT2_FEATURE_RO_COMPAT_SPARSE_SUPER = 0x00000001;
    /// <summary>Can have files bigger than 2GiB</summary>
    const uint EXT2_FEATURE_RO_COMPAT_LARGE_FILE = 0x00000002;
    /// <summary>Use B-Tree for directories</summary>
    const uint EXT2_FEATURE_RO_COMPAT_BTREE_DIR = 0x00000004;
    /// <summary>Can have files bigger than 2TiB *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_HUGE_FILE = 0x00000008;
    /// <summary>Group descriptor checksums and sparse inode table *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_GDT_CSUM = 0x00000010;
    /// <summary>More than 32000 directory entries *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_DIR_NLINK = 0x00000020;
    /// <summary>Nanosecond timestamps and creation time *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_EXTRA_ISIZE = 0x00000040;
    /// <summary>Has snapshot *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_HAS_SNAPSHOT = 0x00000080;
    /// <summary>Quota support *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_QUOTA = 0x00000100;
    /// <summary>Big allocation clusters instead of blocks *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_BIGALLOC = 0x00000200;
    /// <summary>Metadata checksum using crc32c *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_METADATA_CSUM = 0x00000400;
    /// <summary>Filesystem replica support *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_REPLICA = 0x00000800;
    /// <summary>Read-only filesystem image *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_READONLY = 0x00001000;
    /// <summary>Project quotas *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_PROJECT = 0x00002000;
    /// <summary>Verity inodes support *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_VERITY = 0x00008000;
    /// <summary>Orphan inodes present in orphan file *ext4*</summary>
    const uint EXT4_FEATURE_RO_COMPAT_ORPHAN_PRESENT = 0x00010000;

    // Incompatible features
    /// <summary>Uses compression</summary>
    const uint EXT2_FEATURE_INCOMPAT_COMPRESSION = 0x00000001;
    /// <summary>Filetype in directory entries</summary>
    const uint EXT2_FEATURE_INCOMPAT_FILETYPE = 0x00000002;
    /// <summary>Journal needs recovery *ext3*</summary>
    const uint EXT3_FEATURE_INCOMPAT_RECOVER = 0x00000004;
    /// <summary>Has journal on another device *ext3*</summary>
    const uint EXT3_FEATURE_INCOMPAT_JOURNAL_DEV = 0x00000008;
    /// <summary>Reduced block group backups</summary>
    const uint EXT2_FEATURE_INCOMPAT_META_BG = 0x00000010;
    /// <summary>Volume use extents *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_EXTENTS = 0x00000040;
    /// <summary>Supports volumes bigger than 2^32 blocks *ext4*</summary>

    // ReSharper disable once InconsistentNaming
    const uint EXT4_FEATURE_INCOMPAT_64BIT = 0x00000080;
    /// <summary>Multi-mount protection *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_MMP = 0x00000100;
    /// <summary>Flexible block group metadata location *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_FLEX_BG = 0x00000200;
    /// <summary>EA in inode *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_EA_INODE = 0x00000400;
    /// <summary>Data can reside in directory entry *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_DIRDATA = 0x00001000;
    /// <summary>Checksum seed in superblock *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_CSUM_SEED = 0x00002000;
    /// <summary>Large directory with 3-level hash tree *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_LARGEDIR = 0x00004000;
    /// <summary>Data stored in inode body *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_INLINE_DATA = 0x00008000;
    /// <summary>Encrypted inodes *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_ENCRYPT = 0x00010000;
    /// <summary>Casefolded directory entries *ext4*</summary>
    const uint EXT4_FEATURE_INCOMPAT_CASEFOLD = 0x00020000;

    // Miscellaneous filesystem flags
    /// <summary>Signed dirhash in use</summary>
    const uint EXT2_FLAGS_SIGNED_HASH = 0x00000001;
    /// <summary>Unsigned dirhash in use</summary>
    const uint EXT2_FLAGS_UNSIGNED_HASH = 0x00000002;
    /// <summary>Testing development code</summary>
    const uint EXT2_FLAGS_TEST_FILESYS = 0x00000004;

    const string FS_TYPE_EXT2 = "ext2";
    const string FS_TYPE_EXT3 = "ext3";
    const string FS_TYPE_EXT4 = "ext4";

    // Incompatible features supported by this read-only implementation
    const uint EXT2_SUPPORTED_INCOMPAT = EXT2_FEATURE_INCOMPAT_COMPRESSION |
                                         EXT2_FEATURE_INCOMPAT_FILETYPE    |
                                         EXT3_FEATURE_INCOMPAT_RECOVER     |
                                         EXT3_FEATURE_INCOMPAT_JOURNAL_DEV |
                                         EXT2_FEATURE_INCOMPAT_META_BG     |
                                         EXT4_FEATURE_INCOMPAT_EXTENTS     |
                                         EXT4_FEATURE_INCOMPAT_64BIT       |
                                         EXT4_FEATURE_INCOMPAT_FLEX_BG     |
                                         EXT4_FEATURE_INCOMPAT_EA_INODE    |
                                         EXT4_FEATURE_INCOMPAT_DIRDATA     |
                                         EXT4_FEATURE_INCOMPAT_CSUM_SEED   |
                                         EXT4_FEATURE_INCOMPAT_LARGEDIR    |
                                         EXT4_FEATURE_INCOMPAT_CASEFOLD;

    const string MODULE_NAME = "ext2FS plugin";

    // Root inode number
    const uint EXT2_ROOT_INO = 2;

    // Good old inode size for original revision
    const ushort EXT2_GOOD_OLD_INODE_SIZE = 128;

    // First non-reserved inode for original revision
    const uint EXT2_GOOD_OLD_FIRST_INO = 11;

    // Block size limits
    const uint EXT4_MIN_BLOCK_SIZE = 1024;
    const uint EXT4_MAX_BLOCK_SIZE = 65536;

    // Block group descriptor size limits
    const ushort EXT4_MIN_DESC_SIZE = 32;

    // Extent tree header magic
    const ushort EXT4_EXTENT_MAGIC = 0xF30A;

    // Inode flags (i_flags field)
    /// <summary>Secure deletion</summary>
    const uint EXT2_SECRM_FL = 0x00000001;
    /// <summary>Undelete</summary>
    const uint EXT2_UNRM_FL = 0x00000002;
    /// <summary>Compress file</summary>
    const uint EXT2_COMPR_FL = 0x00000004;
    /// <summary>Synchronous updates</summary>
    const uint EXT2_SYNC_FL = 0x00000008;
    /// <summary>Immutable file</summary>
    const uint EXT2_IMMUTABLE_FL = 0x00000010;
    /// <summary>Append only</summary>
    const uint EXT2_APPEND_FL = 0x00000020;
    /// <summary>Do not dump file</summary>
    const uint EXT2_NODUMP_FL = 0x00000040;
    /// <summary>Do not update atime</summary>
    const uint EXT2_NOATIME_FL = 0x00000080;
    /// <summary>Dirty (modified) compressed file</summary>
    const uint EXT2_DIRTY_FL = 0x00000100;
    /// <summary>One or more compressed clusters</summary>
    const uint EXT2_COMPRBLK_FL = 0x00000200;
    /// <summary>Don't compress file</summary>
    const uint EXT2_NOCOMPR_FL = 0x00000400;
    /// <summary>Encrypted inode (also historically compression error)</summary>
    const uint EXT4_ENCRYPT_FL = 0x00000800;
    /// <summary>Compression error (e2compr, same bit as EXT4_ENCRYPT_FL)</summary>
    const uint EXT2_ECOMPR_FL = 0x00000800;
    /// <summary>B-tree/hash-indexed directory</summary>
    const uint EXT2_INDEX_FL = 0x00001000;
    /// <summary>AFS directory</summary>
    const uint EXT2_IMAGIC_FL = 0x00002000;
    /// <summary>File data should be journaled (ext3/4)</summary>
    const uint EXT3_JOURNAL_DATA_FL = 0x00004000;
    /// <summary>File tail should not be merged</summary>
    const uint EXT2_NOTAIL_FL = 0x00008000;
    /// <summary>Synchronous directory modifications</summary>
    const uint EXT2_DIRSYNC_FL = 0x00010000;
    /// <summary>Top of directory hierarchies</summary>
    const uint EXT2_TOPDIR_FL = 0x00020000;
    /// <summary>Huge file</summary>
    const uint EXT4_HUGE_FILE_FL = 0x00040000;
    /// <summary>Inode uses extents</summary>
    const uint EXT4_EXTENTS_FL = 0x00080000;
    /// <summary>Verity protected inode</summary>
    const uint EXT4_VERITY_FL = 0x00100000;
    /// <summary>Inode used for large EA</summary>
    const uint EXT4_EA_INODE_FL = 0x00200000;
    /// <summary>Blocks past EOF for this inode</summary>
    const uint EXT4_EOFBLOCKS_FL = 0x00400000;
    /// <summary>Do not copy-on-write this file</summary>
    const uint EXT4_NOCOW_FL = 0x00800000;
    /// <summary>Inode is a snapshot</summary>
    const uint EXT4_SNAPFILE_FL = 0x01000000;
    /// <summary>Inode is DAX (direct access)</summary>
    const uint EXT4_DAX_FL = 0x02000000;
    /// <summary>Snapshot is being deleted</summary>
    const uint EXT4_SNAPFILE_DELETED_FL = 0x04000000;
    /// <summary>Snapshot shrink has completed</summary>
    const uint EXT4_SNAPFILE_SHRUNK_FL = 0x08000000;
    /// <summary>Inode has inline data</summary>
    const uint EXT4_INLINE_DATA_FL = 0x10000000;
    /// <summary>Create with parents projid</summary>
    const uint EXT4_PROJINHERIT_FL = 0x20000000;
    /// <summary>Casefolded directory</summary>
    const uint EXT4_CASEFOLD_FL = 0x40000000;

    // POSIX file mode masks
    const ushort S_IFMT   = 0xF000;
    const ushort S_IFSOCK = 0xC000;
    const ushort S_IFLNK  = 0xA000;
    const ushort S_IFREG  = 0x8000;
    const ushort S_IFBLK  = 0x6000;
    const ushort S_IFDIR  = 0x4000;
    const ushort S_IFCHR  = 0x2000;
    const ushort S_IFIFO  = 0x1000;

    // Extended attribute constants
    /// <summary>Extended attribute block magic number</summary>
    const uint EXT4_XATTR_MAGIC = 0xEA020000;
    /// <summary>Minimum xattr entry header size (without name)</summary>
    const int EXT4_XATTR_ENTRY_HDR_SIZE = 16;
    /// <summary>Alignment mask for xattr entries</summary>
    const int EXT4_XATTR_ROUND = 3;
    /// <summary>Size of the external xattr block header</summary>
    const int EXT4_XATTR_HEADER_SIZE = 32;
    /// <summary>Size of the ibody xattr header (just the magic)</summary>
    const int EXT4_XATTR_IBODY_HEADER_SIZE = 4;

    // Extended attribute name indexes
    const byte EXT4_XATTR_INDEX_USER              = 1;
    const byte EXT4_XATTR_INDEX_POSIX_ACL_ACCESS  = 2;
    const byte EXT4_XATTR_INDEX_POSIX_ACL_DEFAULT = 3;
    const byte EXT4_XATTR_INDEX_TRUSTED           = 4;
    const byte EXT4_XATTR_INDEX_LUSTRE            = 5;
    const byte EXT4_XATTR_INDEX_SECURITY          = 6;
    const byte EXT4_XATTR_INDEX_SYSTEM            = 7;
    const byte EXT4_XATTR_INDEX_RICHACL           = 8;
    const byte EXT4_XATTR_INDEX_ENCRYPTION        = 9;
    const byte EXT4_XATTR_INDEX_HURD              = 10;

    // e2compr compressed cluster head magic
    /// <summary>Magic number for e2compr v0.4.x compressed cluster heads</summary>
    const ushort EXT2_COMPRESS_MAGIC_04X = 0x5EF2;

    // e2compr compression algorithm IDs
    /// <summary>No compression</summary>
    const byte EXT2_NONE_ALG = 0;
    /// <summary>gzip / zlib deflate</summary>
    const byte EXT2_GZIP_ALG = 1;
    /// <summary>bzip2</summary>
    const byte EXT2_BZIP2_ALG = 2;
    /// <summary>LZO</summary>
    const byte EXT2_LZO_ALG = 3;
    /// <summary>LZRW3-A</summary>
    const byte EXT2_LZRW3A_ALG = 4;
    /// <summary>LZV1</summary>
    const byte EXT2_LZV1_ALG = 5;
    /// <summary>Number of supported e2compr algorithms</summary>
    const byte EXT2_N_ALGORITHMS = 6;
}