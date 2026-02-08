// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
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

using System.Collections.Generic;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AcornADFS
{
    /// <summary>Internal representation of a directory entry</summary>
    sealed class DirectoryEntryInfo
    {
        /// <summary>Filename</summary>
        public string Name { get; set; }

        /// <summary>RISC OS load address</summary>
        public uint LoadAddr { get; set; }

        /// <summary>RISC OS exec address</summary>
        public uint ExecAddr { get; set; }

        /// <summary>File length in bytes</summary>
        public uint Length { get; set; }

        /// <summary>Indirect disc address</summary>
        public uint IndAddr { get; set; }

        /// <summary>File attributes</summary>
        public byte Attributes { get; set; }
    }

    /// <summary>Directory node for enumerating directory contents</summary>
    sealed class AcornDirNode : IDirNode
    {
        /// <summary>Current position in the directory enumeration (entry index)</summary>
        internal int Position { get; set; }

        /// <summary>Cached directory entries</summary>
        internal Dictionary<string, DirectoryEntryInfo> Entries { get; set; }

        /// <summary>Array of entry names for enumeration</summary>
        internal string[] EntryNames { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    /// <summary>File node for reading file contents</summary>
    sealed class AcornFileNode : IFileNode
    {
        /// <summary>Current read position within the file</summary>
        public long Offset { get; set; }

        /// <summary>File length in bytes</summary>
        public long Length { get; init; }

        /// <summary>Indirect disc address of the file</summary>
        internal uint IndAddr { get; init; }

        /// <summary>File attributes</summary>
        internal byte Attributes { get; init; }

        /// <inheritdoc />
        public string Path { get; init; }
    }
}