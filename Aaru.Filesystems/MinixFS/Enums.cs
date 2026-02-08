// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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

namespace Aaru.Filesystems;

// Information from the Minix source code
/// <inheritdoc />
/// <summary>Implements detection of the MINIX filesystem</summary>
public sealed partial class MinixFS
{
#region Nested type: FilesystemStateFlags

    /// <summary>Superblock s_flags/s_state field flags</summary>
    [Flags]
    enum FilesystemStateFlags : ushort
    {
        /// <summary>Filesystem was cleanly unmounted</summary>
        Clean = 1 << 0
    }

#endregion

#region Nested type: MandatoryFlags

    /// <summary>
    ///     Mandatory feature flags - if any of these bits are set and the implementation doesn't understand them, do not
    ///     mount the filesystem
    /// </summary>
    [Flags]
    enum MandatoryFlags : ushort
    {
        /// <summary>Mask for mandatory flags that must be understood</summary>
        Mask = 0xFF00
    }

#endregion

#region Nested type: InodeUpdateFlags

    /// <summary>Flags for inode time updates</summary>
    [Flags]
    enum InodeUpdateFlags : byte
    {
        /// <summary>Access time needs updating</summary>
        AccessTime = 0x02,

        /// <summary>Change time needs updating</summary>
        ChangeTime = 0x04,

        /// <summary>Modification time needs updating</summary>
        ModificationTime = 0x08
    }

#endregion

#region Nested type: InodeMode

    /// <summary>Inode mode field - file type and permissions</summary>
    [Flags]
    enum InodeMode : ushort
    {
        /// <summary>File type mask</summary>
        TypeMask = 0xF000,

        /// <summary>FIFO (named pipe)</summary>
        Fifo = 0x1000,

        /// <summary>Character device</summary>
        CharDevice = 0x2000,

        /// <summary>Directory</summary>
        Directory = 0x4000,

        /// <summary>Block device</summary>
        BlockDevice = 0x6000,

        /// <summary>Regular file</summary>
        Regular = 0x8000,

        /// <summary>Symbolic link</summary>
        SymbolicLink = 0xA000,

        /// <summary>Socket</summary>
        Socket = 0xC000,

        /// <summary>Set-user-ID on execution</summary>
        SetUid = 0x0800,

        /// <summary>Set-group-ID on execution</summary>
        SetGid = 0x0400,

        /// <summary>Sticky bit</summary>
        Sticky = 0x0200,

        /// <summary>Owner read permission</summary>
        OwnerRead = 0x0100,

        /// <summary>Owner write permission</summary>
        OwnerWrite = 0x0080,

        /// <summary>Owner execute permission</summary>
        OwnerExecute = 0x0040,

        /// <summary>Group read permission</summary>
        GroupRead = 0x0020,

        /// <summary>Group write permission</summary>
        GroupWrite = 0x0010,

        /// <summary>Group execute permission</summary>
        GroupExecute = 0x0008,

        /// <summary>Others read permission</summary>
        OthersRead = 0x0004,

        /// <summary>Others write permission</summary>
        OthersWrite = 0x0002,

        /// <summary>Others execute permission</summary>
        OthersExecute = 0x0001,

        /// <summary>Permission mask (lower 9 bits)</summary>
        PermissionMask = 0x01FF,

        /// <summary>Inode is not allocated</summary>
        NotAllocated = 0x0000
    }

#endregion

#region Nested type: FilesystemVersion

    /// <summary>Filesystem version identifiers</summary>
    enum FilesystemVersion
    {
        /// <summary>Invalid/unknown version</summary>
        Unknown = 0,

        /// <summary>Minix version 1</summary>
        V1 = 1,

        /// <summary>Minix version 2</summary>
        V2 = 2,

        /// <summary>Minix version 3</summary>
        V3 = 3
    }

#endregion
}