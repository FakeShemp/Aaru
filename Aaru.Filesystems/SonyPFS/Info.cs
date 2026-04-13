// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        uint sectorSize = imagePlugin.Info.SectorSize;

        if(sectorSize < 512) return false;

        // Superblock is at sector 0 relative to partition data start
        int sbSize = Marshal.SizeOf<SuperBlock>();

        var sectorsToRead = (uint)((sbSize + sectorSize - 1) / sectorSize);

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectorsToRead, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        if(sector.Length < sbSize) return false;

        SuperBlock sb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

        if(sb.magic != PFS_SUPER_MAGIC) return false;

        if(sb.version > PFS_FORMAT_VERSION) return false;

        return true;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        throw new NotImplementedException();
    }
}