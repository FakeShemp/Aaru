// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
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
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class UDF
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        // At position 0x8000, there should be a Volume Recognition Sequence
        uint sectorSize = imagePlugin.Info.SectorSize;

        if(sectorSize == 2352) sectorSize = 2048;

        uint beaLocation = 0x8000 / sectorSize;

        if(imagePlugin.ReadSector(beaLocation, false, out byte[] buffer, out _) != ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        BeginningExtendedAreaDescriptor bea =
            Marshal.ByteArrayToStructureLittleEndian<BeginningExtendedAreaDescriptor>(buffer);

        // Not the beginning of an Extended Area
        if(!bea.identifier.SequenceEqual(_bea)) return ErrorNumber.InvalidArgument;

        // Search for 16 sectors for a correct volume structure descriptor
        var foundVsd = false;

        for(var i = 1; i < 16; i++)
        {
            if(imagePlugin.ReadSector((ulong)(beaLocation + i), false, out buffer, out _) != ErrorNumber.NoError)
                return ErrorNumber.InvalidArgument;

            VolumeStructureDescriptor vsd = Marshal.ByteArrayToStructureLittleEndian<VolumeStructureDescriptor>(buffer);

            // This media is recorded according to ECMA-167 version 2
            if(vsd.type == 0 && vsd.identifier.SequenceEqual(_nsr))
            {
                foundVsd = true;

                continue;
            }

            // Terminating Extended Area Descriptor
            if(vsd.type == 0 && vsd.identifier.SequenceEqual(_tea)) break;
        }

        if(!foundVsd) return ErrorNumber.InvalidArgument;

        // Next we search for the anchor volume descriptor pointer
        // It should be at sector 256, N-256 or N, with N being the last sector of the volume
        // UDF does not play well with partitioning schemes
        AnchorVolumeDescriptorPointer avdp      = default;
        var                           avdpFound = false;

        foreach(ulong location in new[]
                {
                    256UL, imagePlugin.Info.Sectors - 256, imagePlugin.Info.Sectors - 1
                })
        {
            if(imagePlugin.ReadSector(location, false, out buffer, out _) != ErrorNumber.NoError)
                return ErrorNumber.InvalidArgument;

            avdp = Marshal.ByteArrayToStructureLittleEndian<AnchorVolumeDescriptorPointer>(buffer);

            if(avdp.tag.tagIdentifier != TagIdentifier.AnchorVolumeDescriptorPointer ||
               avdp.tag.tagLocation   != location)
                continue;

            avdpFound = true;

            break;
        }

        if(!avdpFound) return ErrorNumber.InvalidArgument;

        // Parse Volume Descriptor Sequence to find PVD, LVD, and Partition Descriptor(s)
        var                     partitionDescriptors = new Dictionary<ushort, PartitionDescriptor>();
        PrimaryVolumeDescriptor pvd;
        LogicalVolumeDescriptor lvd         = default;
        var                     foundPvd    = false;
        var                     foundLvd    = false;
        uint                    vdsLength   = avdp.mainVolumeDescriptorSequenceExtent.length / sectorSize;
        uint                    vdsLocation = avdp.mainVolumeDescriptorSequenceExtent.location;

        for(uint i = 0; i < vdsLength; i++)
        {
            if(imagePlugin.ReadSector(vdsLocation + i, false, out buffer, out _) != ErrorNumber.NoError) continue;

            var tagId = (TagIdentifier)BitConverter.ToUInt16(buffer, 0);

            switch(tagId)
            {
                case TagIdentifier.TerminatingDescriptor:
                    i = vdsLength; // Exit loop

                    break;

                case TagIdentifier.PrimaryVolumeDescriptor:
                    pvd      = Marshal.ByteArrayToStructureLittleEndian<PrimaryVolumeDescriptor>(buffer);
                    foundPvd = pvd.tag.tagLocation == vdsLocation + i;

                    break;

                case TagIdentifier.LogicalVolumeDescriptor:
                    lvd      = Marshal.ByteArrayToStructureLittleEndian<LogicalVolumeDescriptor>(buffer);
                    foundLvd = lvd.tag.tagLocation == vdsLocation + i;

                    break;

                case TagIdentifier.PartitionDescriptor:
                    PartitionDescriptor pd = Marshal.ByteArrayToStructureLittleEndian<PartitionDescriptor>(buffer);

                    if(pd.tag.tagLocation == vdsLocation + i) partitionDescriptors[pd.partitionNumber] = pd;

                    break;
            }
        }

        if(!foundPvd || !foundLvd || partitionDescriptors.Count == 0) return ErrorNumber.InvalidArgument;

        // Not UDF
        if(!lvd.domainIdentifier.identifier.SequenceEqual(_magic)) return ErrorNumber.InvalidArgument;

        // Read the Logical Volume Integrity Descriptor to check UDF revision
        if(imagePlugin.ReadSector(lvd.integritySequenceExtent.location, false, out byte[] lvidBuffer, out _) !=
           ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        LogicalVolumeIntegrityDescriptor lvid =
            Marshal.ByteArrayToStructureLittleEndian<LogicalVolumeIntegrityDescriptor>(lvidBuffer);

        if(lvid.tag.tagIdentifier != TagIdentifier.LogicalVolumeIntegrityDescriptor ||
           lvid.tag.tagLocation   != lvd.integritySequenceExtent.location)
            return ErrorNumber.InvalidArgument;

        // The Implementation Use area follows the free space and size tables
        // Offset = 80 (fixed LVID header) + numberOfPartitions * 4 (free space) + numberOfPartitions * 4 (size)
        LogicalVolumeIntegrityDescriptorImplementationUse lvidiu =
            Marshal.ByteArrayToStructureLittleEndian<LogicalVolumeIntegrityDescriptorImplementationUse>(lvidBuffer,
                (int)(80 + lvid.numberOfPartitions * 8),
                System.Runtime.InteropServices.Marshal
                      .SizeOf<LogicalVolumeIntegrityDescriptorImplementationUse>());

        // UDF 1.02 = 0x0102 = 258
        // Reject media that requires a UDF revision higher than 1.02 to read
        if(lvidiu.minimumReadUDF > 0x0102) return ErrorNumber.InvalidArgument;

        // Get the first partition for FSD location
        if(!partitionDescriptors.TryGetValue(0, out PartitionDescriptor firstPartition))
        {
            // Use the first available partition if partition 0 doesn't exist
            using Dictionary<ushort, PartitionDescriptor>.ValueCollection.Enumerator enumerator =
                partitionDescriptors.Values.GetEnumerator();

            if(!enumerator.MoveNext()) return ErrorNumber.InvalidArgument;

            firstPartition = enumerator.Current;
        }

        // FSD is at logical block 0 of the partition (UDF 1.02)
        ulong fsdAbsoluteSector = firstPartition.partitionStartingLocation;

        if(imagePlugin.ReadSector(fsdAbsoluteSector, false, out buffer, out _) != ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        FileSetDescriptor fsd = Marshal.ByteArrayToStructureLittleEndian<FileSetDescriptor>(buffer);

        if(fsd.tag.tagIdentifier != TagIdentifier.FileSetDescriptor) return ErrorNumber.InvalidArgument;

        // The rootDirectoryICB contains the ICB of the root directory
        _rootDirectoryIcb = fsd.rootDirectoryICB;

        // Get the partition where the root directory ICB resides
        if(!partitionDescriptors.TryGetValue(_rootDirectoryIcb.extentLocation.partitionReferenceNumber,
                                             out PartitionDescriptor rootPartition))
            return ErrorNumber.InvalidArgument;

        // Save partition starting location for offset calculations
        _partitionStartingLocation = rootPartition.partitionStartingLocation;

        // Calculate the absolute sector of the root directory ICB
        ulong rootIcbAbsoluteSector = _partitionStartingLocation + _rootDirectoryIcb.extentLocation.logicalBlockNumber;

        if(imagePlugin.ReadSector(rootIcbAbsoluteSector, false, out buffer, out _) != ErrorNumber.NoError)
            return ErrorNumber.InvalidArgument;

        FileEntry rootEntry = Marshal.ByteArrayToStructureLittleEndian<FileEntry>(buffer);

        if(rootEntry.tag.tagIdentifier != TagIdentifier.FileEntry) return ErrorNumber.InvalidArgument;

        // Fill filesystem info from the descriptors
        // Free space table is at offset 80 in LVID, each entry is 4 bytes per partition
        uint freeBlocks = 0;

        for(uint i = 0; i < lvid.numberOfPartitions; i++)
            freeBlocks += BitConverter.ToUInt32(lvidBuffer, (int)(80 + i * 4));

        _statfs = new FileSystemInfo
        {
            Blocks         = firstPartition.partitionLength,
            FilenameLength = 254, // UDF 1.02 max filename length
            Files          = lvidiu.files + lvidiu.directories,
            FreeBlocks     = freeBlocks,
            FreeFiles      = 0, // UDF doesn't have a fixed inode table
            PluginId       = Id,
            Type           = $"UDF {lvidiu.minimumReadUDF >> 8}.{lvidiu.minimumReadUDF & 0xFF:D2}"
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }
}