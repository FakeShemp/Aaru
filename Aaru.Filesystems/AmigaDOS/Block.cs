// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Amiga Fast File System plugin.
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

using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class AmigaDOSPlugin
{
    /// <summary>Reads a block from the filesystem</summary>
    /// <param name="block">Block number (relative to filesystem start)</param>
    /// <param name="data">Output block data</param>
    /// <returns>Error code</returns>
    ErrorNumber ReadBlock(uint block, out byte[] data)
    {
        data = null;

        ulong sectorAddress = _partition.Start + (ulong)block * _sectorsPerBlock;

        if(sectorAddress >= _partition.End) return ErrorNumber.InvalidArgument;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddress, false, _sectorsPerBlock, out data, out _);

        return errno;
    }
}