// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HP Logical Interchange Format plugin
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

/// <inheritdoc />
public sealed partial class LIF
{
    static ErrorNumber ReadLogicalBytes(IMediaImage imagePlugin, Partition partition, ulong byteOffset, uint length,
                                        out byte[]  data)
    {
        data = null;

        if(imagePlugin is null || length == 0) return ErrorNumber.InvalidArgument;

        uint imageSectorSize = imagePlugin.Info.SectorSize;

        if(imageSectorSize == 0) return ErrorNumber.InvalidArgument;

        ulong sectorOffset   = byteOffset / imageSectorSize;
        ulong offsetInSector = byteOffset % imageSectorSize;
        ulong bytesToRead    = offsetInSector + length;
        ulong sectorsNeeded  = bytesToRead / imageSectorSize;

        if(bytesToRead % imageSectorSize > 0) sectorsNeeded++;

        if(sectorsNeeded == 0 || sectorsNeeded > uint.MaxValue) return ErrorNumber.InvalidArgument;

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start + sectorOffset,
                                                    false,
                                                    (uint)sectorsNeeded,
                                                    out byte[] sectorData,
                                                    out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sectorData.LongLength < (long)(offsetInSector + length)) return ErrorNumber.InvalidArgument;

        data = new byte[length];
        Array.Copy(sectorData, (long)offsetInSector, data, 0L, length);

        return ErrorNumber.NoError;
    }

    static ErrorNumber ReadLogicalRecords(IMediaImage imagePlugin, Partition partition, uint record, uint recordCount,
                                          out byte[]  data)
    {
        if(recordCount > uint.MaxValue / LIF_RECORD_SIZE)
        {
            data = null;

            return ErrorNumber.InvalidArgument;
        }

        return ReadLogicalBytes(imagePlugin,
                                partition,
                                (ulong)record * LIF_RECORD_SIZE,
                                recordCount   * LIF_RECORD_SIZE,
                                out data);
    }

    sealed class LifDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

    sealed class LifFileNode : IFileNode
    {
        /// <summary>Starting logical record of the file data on the medium.</summary>
        internal uint StartRecord;

        /// <inheritdoc />
        public string Path   { get; init; }
        /// <inheritdoc />
        public long   Length { get; init; }
        /// <inheritdoc />
        public long   Offset { get; set; }
    }
}