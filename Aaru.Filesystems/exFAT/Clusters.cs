// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Clusters.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft exFAT filesystem plugin.
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
using System.IO;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
public sealed partial class exFAT
{
    /// <summary>Reads a cluster chain from the image.</summary>
    /// <param name="firstCluster">First cluster of the chain.</param>
    /// <param name="isContiguous">If true, the chain is contiguous and FAT is not used.</param>
    /// <param name="expectedLength">Expected length in bytes (used for contiguous allocations).</param>
    /// <returns>Byte array containing the cluster chain data, or null on error.</returns>
    byte[] ReadClusterChain(uint firstCluster, bool isContiguous = false, ulong expectedLength = 0)
    {
        if(firstCluster < 2 || firstCluster > _clusterCount + 1) return null;

        var clusters = new List<uint>();

        if(isContiguous && expectedLength > 0)
        {
            // For contiguous allocations, calculate the number of clusters needed
            var clusterCountNeeded = (uint)((expectedLength + _bytesPerCluster - 1) / _bytesPerCluster);

            for(uint i = 0; i < clusterCountNeeded; i++) clusters.Add(firstCluster + i);
        }
        else
        {
            // Follow the FAT chain
            uint currentCluster = firstCluster;

            while(currentCluster >= 2 && currentCluster <= _clusterCount + 1)
            {
                clusters.Add(currentCluster);

                // Prevent infinite loops
                if(clusters.Count > _clusterCount) break;

                uint nextCluster = _fatEntries[currentCluster];

                // End of chain markers
                if(nextCluster >= 0xFFFFFFF8) break;

                // Bad cluster marker
                if(nextCluster == 0xFFFFFFF7) break;

                currentCluster = nextCluster;
            }
        }

        if(clusters.Count == 0) return null;

        var ms = new MemoryStream();

        foreach(uint cluster in clusters)
        {
            ulong sector = _clusterHeapOffset + (ulong)(cluster - 2) * _sectorsPerCluster;

            ErrorNumber errno = _image.ReadSectors(sector, false, _sectorsPerCluster, out byte[] buffer, out _);

            if(errno != ErrorNumber.NoError) return null;

            ms.Write(buffer, 0, buffer.Length);
        }

        return ms.ToArray();
    }

    /// <summary>Gets the cluster number at a specific position in the FAT chain.</summary>
    /// <param name="firstCluster">First cluster of the chain.</param>
    /// <param name="position">Position in the chain (0-based).</param>
    /// <returns>Cluster number at the position, or 0 if invalid.</returns>
    uint GetClusterAtPosition(uint firstCluster, uint position)
    {
        if(firstCluster < 2 || firstCluster > _clusterCount + 1) return 0;

        uint currentCluster = firstCluster;

        for(uint i = 0; i < position; i++)
        {
            if(currentCluster < 2 || currentCluster > _clusterCount + 1) return 0;

            uint nextCluster = _fatEntries[currentCluster];

            // End of chain markers
            if(nextCluster >= 0xFFFFFFF8) return 0;

            // Bad cluster marker
            if(nextCluster == 0xFFFFFFF7) return 0;

            currentCluster = nextCluster;
        }

        return currentCluster;
    }
}