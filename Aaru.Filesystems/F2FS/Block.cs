// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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

public sealed partial class F2FS
{
    /// <summary>Reads a single filesystem block by its block number</summary>
    ErrorNumber ReadBlock(uint blockNumber, out byte[] blockData)
    {
        blockData = null;

        // Convert block number to sector address
        uint  sectorsPerBlock = _blockSize / _imagePlugin.Info.SectorSize;
        ulong sectorAddr      = _partition.Start + (ulong)blockNumber * sectorsPerBlock;

        ErrorNumber errno = _imagePlugin.ReadSectors(sectorAddr, false, sectorsPerBlock, out blockData, out _);

        return errno;
    }
}