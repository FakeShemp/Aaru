// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

public sealed partial class EFS
{
#region Nested type: FileType

    /// <summary>EFS file type flags (from di_mode).</summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FileType : ushort
    {
        /// <summary>Type mask.</summary>
        IFMT = 0xF000,
        /// <summary>Named pipe (FIFO).</summary>
        IFIFO = 0x1000,
        /// <summary>Character special device.</summary>
        IFCHR = 0x2000,
        /// <summary>Character special link.</summary>
        IFCHRLNK = 0x3000,
        /// <summary>Directory.</summary>
        IFDIR = 0x4000,
        /// <summary>Block special device.</summary>
        IFBLK = 0x6000,
        /// <summary>Block special link.</summary>
        IFBLKLNK = 0x7000,
        /// <summary>Regular file.</summary>
        IFREG = 0x8000,
        /// <summary>Symbolic link.</summary>
        IFLNK = 0xA000,
        /// <summary>Socket.</summary>
        IFSOCK = 0xC000
    }

#endregion

#region Nested type: FilePermissions

    /// <summary>EFS file permission flags (from di_mode).</summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FilePermissions : ushort
    {
        /// <summary>Set user ID on execution.</summary>
        ISUID = 0x0800,
        /// <summary>Set group ID on execution.</summary>
        ISGID = 0x0400,
        /// <summary>Sticky bit (save text / restricted deletion).</summary>
        ISVTX = 0x0200,
        /// <summary>Owner read permission.</summary>
        IRUSR = 0x0100,
        /// <summary>Owner write permission.</summary>
        IWUSR = 0x0080,
        /// <summary>Owner execute permission.</summary>
        IXUSR = 0x0040,
        /// <summary>Group read permission.</summary>
        IRGRP = 0x0020,
        /// <summary>Group write permission.</summary>
        IWGRP = 0x0010,
        /// <summary>Group execute permission.</summary>
        IXGRP = 0x0008,
        /// <summary>Others read permission.</summary>
        IROTH = 0x0004,
        /// <summary>Others write permission.</summary>
        IWOTH = 0x0002,
        /// <summary>Others execute permission.</summary>
        IXOTH = 0x0001
    }

#endregion

#region Nested type: InodeVersion

    /// <summary>EFS inode version values.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum InodeVersion : byte
    {
        /// <summary>Standard EFS inode.</summary>
        EFS_IVER_EFS = 0,
        /// <summary>AFS special inode.</summary>
        EFS_IVER_AFSSPEC = 1,
        /// <summary>AFS normal inode.</summary>
        EFS_IVER_AFSINO = 2
    }

#endregion

#region Nested type: DirtyFlags

    /// <summary>Values for sb_dirty field indicating filesystem state.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum DirtyFlags : short
    {
        /// <summary>Filesystem is clean and unmounted.</summary>
        EFS_CLEAN = 0x0000,
        /// <summary>Mounted a dirty filesystem (root only).</summary>
        EFS_ACTIVEDIRT = 0x0BAD,
        /// <summary>Filesystem is mounted and clean.</summary>
        EFS_ACTIVE = 0x7777,
        /// <summary>Filesystem is dirty.</summary>
        EFS_DIRTY = 0x1234
    }

#endregion
}