// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Zone.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : MINIX filesystem plugin.
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
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class MinixFS
{
    /// <summary>Reads an indirect zone and its data</summary>
    /// <param name="zoneNum">Indirect zone number</param>
    /// <param name="data">Data buffer to fill</param>
    /// <param name="bytesRead">Current bytes read</param>
    /// <param name="totalSize">Total size to read</param>
    /// <param name="level">Indirection level (1=single, 2=double, 3=triple)</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadIndirectZone(uint zoneNum, ref byte[] data, ref int bytesRead, int totalSize, int level)
    {
        if(zoneNum == 0 || bytesRead >= totalSize) return ErrorNumber.NoError;

        ErrorNumber errno = ReadBlock((int)zoneNum, out byte[] indirectData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading indirect zone {0}: {1}", zoneNum, errno);

            return errno;
        }

        // Number of zone pointers per block
        int pointerSize      = _version == FilesystemVersion.V1 ? 2 : 4;
        int pointersPerBlock = _blockSize / pointerSize;

        for(var i = 0; i < pointersPerBlock && bytesRead < totalSize; i++)
        {
            uint pointer;

            if(_version == FilesystemVersion.V1)
            {
                int off = i * 2;

                pointer = _littleEndian
                              ? BitConverter.ToUInt16(indirectData, off)
                              : (ushort)(indirectData[off] << 8 | indirectData[off + 1]);
            }
            else
            {
                int off = i * 4;

                pointer = _littleEndian
                              ? BitConverter.ToUInt32(indirectData, off)
                              : (uint)(indirectData[off]     << 24 |
                                       indirectData[off + 1] << 16 |
                                       indirectData[off + 2] << 8  |
                                       indirectData[off + 3]);
            }

            if(pointer == 0)
            {
                // Sparse
                int toFill = Math.Min(_blockSize, totalSize - bytesRead);
                bytesRead += toFill;

                continue;
            }

            if(level == 1)
            {
                // Direct data block
                errno = ReadBlock((int)pointer, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                int toCopy = Math.Min(blockData.Length, totalSize - bytesRead);
                Array.Copy(blockData, 0, data, bytesRead, toCopy);
                bytesRead += toCopy;
            }
            else
            {
                // Another level of indirection
                errno = ReadIndirectZone(pointer, ref data, ref bytesRead, totalSize, level - 1);

                if(errno != ErrorNumber.NoError) return errno;
            }
        }

        return ErrorNumber.NoError;
    }
}