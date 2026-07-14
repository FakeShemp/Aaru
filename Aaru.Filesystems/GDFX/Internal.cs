// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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

public sealed partial class GDFX
{
    sealed class DecodedEntry
    {
        internal byte   Attributes  { get; init; }
        internal uint   DataSector  { get; init; }
        internal uint   DataSize    { get; init; }
        internal bool   IsDirectory => (Attributes & ATTR_DIRECTORY) != 0;
        internal string Name        { get; init; }
    }

    sealed class GdfxDirNode : IDirNode
    {
        internal string[] Contents { get; init; }
        internal int      Position { get; set; }

        /// <inheritdoc />
        public string Path { get; init; }
    }

    sealed class GdfxFileNode : IFileNode
    {
        internal uint StartSector { get; init; }

        /// <inheritdoc />
        public string Path   { get; init; }
        /// <inheritdoc />
        public long   Length { get; init; }
        /// <inheritdoc />
        public long   Offset { get; set; }
    }
}