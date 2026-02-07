// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Block reading operations for the Files-11 On-Disk Structure.
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
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    /// <summary>Reads a single ODS block (512 bytes) from the image, handling different sector sizes.</summary>
    /// <param name="imagePlugin">The media image.</param>
    /// <param name="partition">The partition.</param>
    /// <param name="lbn">Logical Block Number (in ODS 512-byte blocks).</param>
    /// <param name="block">Output block data (512 bytes).</param>
    /// <returns>Error number indicating success or failure.</returns>
    ErrorNumber ReadOdsBlock(IMediaImage imagePlugin, Partition partition, uint lbn, out byte[] block)
    {
        block = null;

        // Calculate which device sector contains this ODS block
        ulong deviceSector = partition.Start + lbn / _blocksPerSector;

        // Calculate offset within the device sector
        uint offsetInSector = lbn % _blocksPerSector * ODS_BLOCK_SIZE;

        ErrorNumber errno = imagePlugin.ReadSector(deviceSector, false, out byte[] sectorData, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sectorData == null || sectorData.Length < offsetInSector + ODS_BLOCK_SIZE)
            return ErrorNumber.InvalidArgument;

        // Extract the ODS block from the sector
        block = new byte[ODS_BLOCK_SIZE];
        Array.Copy(sectorData, (int)offsetInSector, block, 0, ODS_BLOCK_SIZE);

        return ErrorNumber.NoError;
    }
}