// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Internal types for the AO-DOS file system plugin.
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

using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class AODOS
{
    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class AoDosDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Array of filenames in this directory</summary>
        internal string[] Entries { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents with streaming support</summary>
    sealed class AoDosFileNode : IFileNode
    {
        /// <summary>The file's directory entry</summary>
        internal DirectoryEntry Entry { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }
}