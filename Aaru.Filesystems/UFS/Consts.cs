// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX FIle System plugin.
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

// ReSharper disable UnusedMember.Local

namespace Aaru.Filesystems;

// Using information from Linux kernel headers
/// <inheritdoc />
/// <summary>Implements detection of BSD Fast File System (FFS, aka UNIX File System)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
    // Superblock size in most UNIX
    const uint SBSIZE = 8192;

    // Superblock size in A/UX
    const uint SBSIZE_AUX = 4096;

    // FreeBSD specifies starts at byte offsets 0, 8192, 65536 and 262144, but in other cases it's following sectors
    // Without bootcode
    const ulong SBLOCK_FLOPPY = 0;

    // With bootcode
    const ulong SBLOCK = 1;

    // Dunno, longer boot code
    const ulong SBLOCK_LONG_BOOT = 8;

    // Found on AT&T for MD-2D floppies
    const ulong SBLOCK_ATT_DSDD = 14;

    // Found on hard disks (Atari UNIX e.g.)
    const ulong SBLOCK_PIGGY = 32;

    // MAGICs
    // UFS magic
    const uint UFS_MAGIC = 0x00011954;

    // Big-endian UFS magic
    const uint UFS_CIGAM = 0x54190100;

    // BorderWare UFS
    const uint UFS_MAGIC_BW = 0x0F242697;

    // Big-endian BorderWare UFS
    const uint UFS_CIGAM_BW = 0x9726240F;

    // UFS2 magic
    const uint UFS2_MAGIC = 0x19540119;

    // Big-endian UFS2 magic
    const uint UFS2_CIGAM = 0x19015419;

    // Incomplete newfs
    const uint UFS_BAD_MAGIC = 0x19960408;

    // Big-endian incomplete newfs
    const uint UFS_BAD_CIGAM = 0x08049619;

    const uint FSOKAY   = 0x7c269d38;
    const uint FSACTIVE = 0x5e72d81a;
    const uint FSBAD    = 0xcb096f43;


    // Consts for HPUX
    /// <summary>Magic number for file system allowing long file names.</summary>
    const uint FS_MAGIC_LFN = 0x095014;
    /// <summary>Magic number for file systems which have their fs_featurebits field set up.</summary>
    const uint FD_FSMAGIC = 0x195612;
    /// <summary>Long file names</summary>
    const int FSF_LFN = 1;

    const int HPUX_FS_CLEAN = 0x17;
    const int HPUX_FS_OK    = 0x53;
    const int HPUX_FS_NOTOK = 0x31;

    // Consts for OSF/1
    const uint FS_SEC_MAGIC  = 0x80011954;
    const int  OSF1_FS_CLEAN = 3;

    // Bitmask for RISCos
    const int RISCOS_UFS_CLEAN = 0x01;
    const int RISCOS_UFS_MOUNT = 0x02;

    // Solaris
    const uint MTB_UFS_MAGIC   = 0xdecade;
    const int  SUN_FSACTIVE    = 0;
    const int  SUN_FSCLEAN     = 1;
    const int  SUN_FSSTABLE    = 2;
    const int  SUN_FSBAD       = 0xff;
    const int  SUN_FSSUSPEND   = 0xfe;
    const int  SUN_FSLOG       = 0xfd;
    const int  SUN_FSFIX       = 0xfc;
    const int  FSLARGEFILES    = 1;
    const int  FS_RECLAIM      = 1;
    const int  FS_RECLAIMING   = 2;
    const int  FS_CHECKCLEAN   = 4;
    const int  FS_CHECKRECLAIM = 8;
    const int  FS_PRE_FLAG     = 0;
    const int  FS_ALL_ROLLED   = 1;
    const int  FS_NEED_ROLL    = 2;

    const int NRPOS           = 8;
    const int MAXCPG          = 32;
    const int MAXCSBUFS       = 32;
    const int MAXMNTLEN       = 512;
    const int MAXMNTLEN_SHORT = 512 - 12;

    const string FS_TYPE_UFS  = "ufs";
    const string FS_TYPE_UFS2 = "ufs2";
}