// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Block.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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

namespace Aaru.Filesystems;

public sealed partial class XFS
{
    /// <summary>Reads a full filesystem block at the given filesystem block number</summary>
    /// <param name="fsBlock">Filesystem block number</param>
    /// <param name="blockData">Output block data</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBlock(ulong fsBlock, out byte[] blockData)
    {
        // Linux: XFS_FSB_TO_AGNO(mp, fsbno) = fsbno >> sb_agblklog
        //        XFS_FSB_TO_AGBNO(mp, fsbno) = fsbno & mask(sb_agblklog)
        //        XFS_AGB_TO_DADDR(mp, agno, agbno) = (agno * sb_agblocks + agbno) * bb_per_block
        ulong agNo     = fsBlock >> _superblock.agblklog;
        ulong agBno    = fsBlock & (1UL << _superblock.agblklog) - 1;
        ulong absBlock = agNo * _superblock.agblocks + agBno;

        ulong byteOffset = absBlock * _superblock.blocksize;

        return ReadBytes(byteOffset, _superblock.blocksize, out blockData);
    }

    /// <summary>Reads bytes from the filesystem at the given byte offset from the partition start</summary>
    /// <param name="byteOffset">Byte offset from the partition start</param>
    /// <param name="length">Number of bytes to read</param>
    /// <param name="data">Output byte array</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadBytes(ulong byteOffset, uint length, out byte[] data)
    {
        data = null;

        uint sectorSize = _imagePlugin.Info.SectorSize;

        ulong startSector    = byteOffset / sectorSize + _partition.Start;
        var   offsetInSector = (int)(byteOffset % sectorSize);

        var sectorsToRead = (uint)((offsetInSector + length + sectorSize - 1) / sectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(startSector, false, sectorsToRead, out byte[] raw, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(offsetInSector + (int)length > raw.Length) return ErrorNumber.InvalidArgument;

        data = new byte[length];
        Array.Copy(raw, offsetInSector, data, 0, length);

        return ErrorNumber.NoError;
    }
}