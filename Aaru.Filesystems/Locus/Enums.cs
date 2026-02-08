// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
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

using System;
using System.Diagnostics.CodeAnalysis;

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
#region Nested type: Flags

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [Flags]
    enum Flags : ushort
    {
        SB_RDONLY   = 0x1,   /* no writes on filesystem */
        SB_CLEAN    = 0x2,   /* fs unmounted cleanly (or checks run) */
        SB_DIRTY    = 0x4,   /* fs mounted without CLEAN bit set */
        SB_RMV      = 0x8,   /* fs is a removable file system */
        SB_PRIMPACK = 0x10,  /* This is the primary pack of the filesystem */
        SB_REPLTYPE = 0x20,  /* This is a replicated type filesystem. */
        SB_USER     = 0x40,  /* This is a "user" replicated filesystem. */
        SB_BACKBONE = 0x80,  /* backbone pack ; complete copy of primary pack but not modifiable */
        SB_NFS      = 0x100, /* This is a NFS type filesystem */
        SB_BYHAND   = 0x200, /* Inhibits automatic fscks on a mangled file system */
        SB_NOSUID   = 0x400, /* Set-uid/Set-gid is disabled */
        SB_SYNCW    = 0x800  /* Synchronous Write */
    }

#endregion

#region Nested type: Version

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [Flags]
    enum Version : byte
    {
        SB_SB4096  = 1, /* smallblock filesys with 4096 byte blocks */
        SB_B1024   = 2, /* 1024 byte block filesystem */
        NUMSCANDEV = 5  /* Used by scangfs(), refed in space.h */
    }

#endregion

#region Nested type: DiskFlags

    /// <summary>Disk inode flags (di_dflag)</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [Flags]
    enum DiskFlags : short
    {
        /// <summary>File has been deleted</summary>
        DIDEL = 0x1,
        /// <summary>File is stored locally</summary>
        DISTORE = 0x2,
        /// <summary>Inode has been allocated to a file</summary>
        DIALLOC = 0x10,
        /// <summary>This is a hidden directory</summary>
        DIHIDDEN = 0x20,
        /// <summary>This is a BSD 4.3 format, long directory</summary>
        DILONGDIR = 0x40,
        /// <summary>This file is a symbolic link</summary>
        DILINK = 0x80,
        /// <summary>DISTORE is forced to retain current value</summary>
        DIFORCE = 0x100,
        /// <summary>File is used for x286 IPC support</summary>
        DIXIPC = 0x200,
        /// <summary>File is mounted on</summary>
        DIMOUNTEDON = 0x400,
        /// <summary>File is a socket</summary>
        DISOCKET = 0x800
    }

#endregion

#region Nested type: FileMode

    /// <summary>File mode flags (di_mode)</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [Flags]
    enum FileMode : ushort
    {
        /// <summary>Type of file mask</summary>
        IFMT = 0xF000,
        /// <summary>FIFO special</summary>
        IFIFO = 0x1000,
        /// <summary>Character special</summary>
        IFCHR = 0x2000,
        /// <summary>Character multiplex file</summary>
        IFMPC = 0x3000,
        /// <summary>Directory</summary>
        IFDIR = 0x4000,
        /// <summary>Block special</summary>
        IFBLK = 0x6000,
        /// <summary>Block multiplex file</summary>
        IFMPB = 0x7000,
        /// <summary>Regular file</summary>
        IFREG = 0x8000,
        /// <summary>Set user id on execution</summary>
        ISUID = 0x800,
        /// <summary>Set group id on execution</summary>
        ISGID = 0x400,
        /// <summary>Save swapped text even after use</summary>
        ISVTX = 0x200,
        /// <summary>Read permission</summary>
        IREAD = 0x100,
        /// <summary>Write permission</summary>
        IWRITE = 0x80,
        /// <summary>Execute permission</summary>
        IEXEC = 0x40
    }

#endregion

#region Nested type: SmallBlockFlags

    /// <summary>Small block flags (di_sbflag)</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    [SuppressMessage("ReSharper", "BuiltInTypeReferenceStyle")]
    [Flags]
    enum SmallBlockFlags : byte
    {
        /// <summary>The small block is in use in the disk inode</summary>
        SBINUSE = 0x1
    }

#endregion
}