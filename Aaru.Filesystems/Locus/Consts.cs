// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Locus filesystem plugin
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
//     License aint with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// Commit count

// Disk address

// Fstore

// Global File System number

// Inode number

// Filesystem pack number

// Timestamp

// ReSharper disable UnusedMember.Local
// ReSharper disable UnusedType.Local

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Locus filesystem</summary>
public sealed partial class Locus
{
    const int NICINOD    = 325;
    const int NICFREE    = 600;
    const int OLDNICINOD = 700;
    const int OLDNICFREE = 500;

    const uint LOCUS_MAGIC     = 0xFFEEDDCD;
    const uint LOCUS_CIGAM     = 0xCDDDEEFF;
    const uint LOCUS_MAGIC_OLD = 0xFFEEDDCC;
    const uint LOCUS_CIGAM_OLD = 0xCCDDEEFF;

    const string FS_TYPE = "locus";

    /// <summary>Number of direct block pointers in inode</summary>
    const int NADDR = 13;

    /// <summary>Number of direct block pointers (NADDR-3)</summary>
    const int NDADDR = 10;

    /// <summary>Maximum length of directory entry name</summary>
    const int NAME_MAX = 255;

    /// <summary>Old directory entry name size</summary>
    const int DIRSIZ = 14;

    /// <summary>Number of commit list entries for old format</summary>
    const int OLDNCMTLST = 50;

    /// <summary>Number of commit list entries</summary>
    const int NCMTLST = 200;

    /// <summary>Number of filesystem generation entries</summary>
    const int NGENLST = 25;

    /// <summary>Small block buffer size in bytes</summary>
    const int SMBLKSZ = 384;
}