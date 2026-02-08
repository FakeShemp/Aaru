// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Acorn filesystem plugin.
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

/// <inheritdoc />
/// <summary>Implements detection of Acorn's Advanced Data Filing System (ADFS)</summary>
public sealed partial class AcornADFS
{
#region Nested type: FileAttributes

    /// <summary>ADFS file/directory attributes (NDA = New Directory Attributes)</summary>
    [Flags]
    enum FileAttributes : byte
    {
        /// <summary>Owner has read permission</summary>
        OwnerRead = 1 << 0,

        /// <summary>Owner has write permission</summary>
        OwnerWrite = 1 << 1,

        /// <summary>File is locked against deletion</summary>
        Locked = 1 << 2,

        /// <summary>Entry is a directory</summary>
        Directory = 1 << 3,

        /// <summary>File is executable (RISC OS specific)</summary>
        Execute = 1 << 4,

        /// <summary>Public has read permission</summary>
        PublicRead = 1 << 5,

        /// <summary>Public has write permission</summary>
        PublicWrite = 1 << 6
    }

#endregion

#region Nested type: FragmentId

    /// <summary>Special fragment identifiers used in the allocation map</summary>
    enum FragmentId : uint
    {
        /// <summary>Free space fragment</summary>
        Free = 0,

        /// <summary>Bad block fragment</summary>
        Bad = 1,

        /// <summary>Root directory fragment</summary>
        Root = 2
    }

#endregion

#region Nested type: DiscDensity

    /// <summary>Disc density values from disc record</summary>
    enum DiscDensity : byte
    {
        /// <summary>Hard disc (fixed media)</summary>
        HardDisc = 0,

        /// <summary>Single density floppy</summary>
        Single = 1,

        /// <summary>Double density floppy</summary>
        Double = 2,

        /// <summary>Double density floppy (alternate)</summary>
        DoublePlus = 3,

        /// <summary>Quad density floppy</summary>
        Quad = 4,

        /// <summary>Octal density floppy</summary>
        Octal = 8
    }

#endregion

#region Nested type: BootOption

    /// <summary>Boot option values (*OPT 4,n)</summary>
    enum BootOption : byte
    {
        /// <summary>No boot action</summary>
        None = 0,

        /// <summary>Load the file</summary>
        Load = 1,

        /// <summary>Run the file</summary>
        Run = 2,

        /// <summary>Execute *EXEC on the file</summary>
        Exec = 3
    }

#endregion

#region Nested type: DiscRecordFlags

    /// <summary>Flags in the disc record flags field</summary>
    [Flags]
    enum DiscRecordFlags : byte
    {
        /// <summary>Disc uses big directories (F+ format, disc > 512MB)</summary>
        BigFlag = 1 << 0
    }

#endregion
}