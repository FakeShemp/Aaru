// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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

public partial class SonyPFS
{
#region Nested type: FileType

    /// <summary>PFS file type flags (from inode mode, IOP FIO).</summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FileType : ushort
    {
        /// <summary>File type mask.</summary>
        IFMT = 0xF000,
        /// <summary>Symbolic link.</summary>
        IFLNK = 0x4000,
        /// <summary>Regular file.</summary>
        IFREG = 0x2000,
        /// <summary>Directory.</summary>
        IFDIR = 0x1000
    }

#endregion

#region Nested type: FileAttributes

    /// <summary>PFS file attribute flags.</summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FileAttributes : ushort
    {
        /// <summary>File is readable.</summary>
        READABLE = 0x0001,
        /// <summary>File is writeable.</summary>
        WRITEABLE = 0x0002,
        /// <summary>File is executable.</summary>
        EXECUTABLE = 0x0004,
        /// <summary>File is copy-protected.</summary>
        COPYPROTECT = 0x0008,
        /// <summary>Directory entry.</summary>
        SUBDIR = 0x0020,
        /// <summary>File is closed.</summary>
        CLOSED = 0x0080,
        /// <summary>PDA attribute.</summary>
        PDA = 0x0800,
        /// <summary>PSX attribute.</summary>
        PSX = 0x1000,
        /// <summary>Hidden file.</summary>
        HIDDEN = 0x4000
    }

#endregion

#region Nested type: FsckStatus

    /// <summary>PFS filesystem check status flags.</summary>
    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    enum FsckStatus : uint
    {
        /// <summary>No errors.</summary>
        OK = 0x00,
        /// <summary>Write error occurred.</summary>
        WRITE_ERROR = 0x01,
        /// <summary>Errors were fixed.</summary>
        ERRORS_FIXED = 0x02
    }

#endregion
}