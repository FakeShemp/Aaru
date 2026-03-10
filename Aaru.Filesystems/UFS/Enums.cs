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
// ReSharper disable UnusedType.Local

namespace Aaru.Filesystems;

// Using information from Linux kernel headers
/// <inheritdoc />
/// <summary>Implements detection of BSD Fast File System (FFS, aka UNIX File System)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
    enum FsOptim
    {
        /// <summary>minimize allocation time</summary>
        FS_OPTTIME = 0,
        /// <summary>minimize disk fragmentation</summary>
        FS_OPTSPACE = 1
    }

    enum RotationalFormat
    {
        /// <summary>4.2BSD rotational table format</summary>
        FS_42POSTBLFMT = -1,
        /// <summary>dynamic rotational table format</summary>
        FS_DYNAMICPOSTBLFMT = 1
    }

    enum InodeFormat
    {
        /// <summary>4.2BSD inode format</summary>
        FS_42INODEFMT = -1,
        /// <summary>4.4BSD inode format</summary>
        FS_44INODEFMT = 2
    }
}