// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class SysVfs
{
    /// <summary>Magic number for XENIX</summary>
    const uint XENIX_MAGIC = 0x002B5544;
    /// <summary>Byte swapped magic number for XENIX</summary>
    const uint XENIX_CIGAM = 0x44552B00;
    /// <summary>Magic number for System V</summary>
    const uint SYSV_MAGIC = 0xFD187E20;
    /// <summary>Byte swapped magic number for System V</summary>
    const uint SYSV_CIGAM = 0x207E18FD;

    // Rest have no magic.
    // Per a Linux kernel, Coherent fs has following:
    const string COH_FNAME = "noname";
    const string COH_FPACK = "nopack";
    const string COH_XXXXX = "xxxxx";
    const string COH_XXXXS = "xxxxx ";
    const string COH_XXXXN = "xxxxx\n";

    // SCO AFS
    const ushort SCO_NFREE = 0xFFFF;

    // UNIX 7th Edition has nothing to detect it, so check for a valid filesystem is a must :(
    const ushort V7_NICINOD = 100;
    const ushort V7_NICFREE = 100;
    const uint   V7_MAXSIZE = 0x00FFFFFF;

    const string FS_TYPE_XENIX    = "xenixfs";
    const string FS_TYPE_SVR4     = "sysv_r4";
    const string FS_TYPE_SVR2     = "sysv_r2";
    const string FS_TYPE_AFS      = "sco_afs";
    const string FS_TYPE_COHERENT = "coherent";
    const string FS_TYPE_UNIX7    = "unix7fs";

    /// <summary>January 1st, 1980 in UNIX time. Used to discriminate SysV R2 from R4.</summary>
    const int JAN_1_1980 = (10 * 365 + 2) * 24 * 60 * 60;

    /// <summary>Number of superblock inodes</summary>
    const int NICINOD = 100;
    /// <summary>Number of superblock free inodes (Coherent)</summary>
    const int COH_NICFREE = 64;
    /// <summary>Number of superblock free inodes (XENIX 3)</summary>
    const int XNX_NICFREE = 100;
    /// <summary>Number of superblock free inodes</summary>
    const int NICFREE = 50;
    /// <summary>Number of superblock free inodes in archaic filesystems when block size is 1024 bytes</summary>
    const int NICFREE_CL2 = 178;
    /// <summary>Number of superblock free inodes in archaic filesystems when block size is 2048 bytes</summary>
    const int NICFREE_CL4 = 434;
    /// <summary>Filler for XENIX superblock</summary>
    const int NSBFILL = 371;
    /// <summary>Filler for XENIX 3 superblock</summary>
    const int XNX3_NSBFILL = 51;

    /// <summary>Clean filesystem</summary>
    const uint FS_OKAY = 0x7c269d38;
    /// <summary>Active filesystem</summary>
    const uint FS_ACTIVE = 0x5e72d81a;
    /// <summary>Bad root</summary>
    const uint FS_BAD = 0xcb096f43;
    /// <summary>Filesystem corrupted by a bad block</summary>
    const uint FS_BADBLK = 0xbadbc14b;

    /// <summary>Maximum size of a filename</summary>
    const int DIRSIZE = 14;
}