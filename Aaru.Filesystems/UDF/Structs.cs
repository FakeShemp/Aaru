// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

// TODO: Detect bootable
/// <inheritdoc />
/// <summary>Implements detection of the Universal Disk Format filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class UDF
{
#region Nested type: AnchorVolumeDescriptorPointer

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AnchorVolumeDescriptorPointer
    {
        public readonly DescriptorTag    tag;
        public readonly ExtentDescriptor mainVolumeDescriptorSequenceExtent;
        public readonly ExtentDescriptor reserveVolumeDescriptorSequenceExtent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 480)]
        public readonly byte[] reserved;
    }

#endregion

#region Nested type: CharacterSpecification

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CharacterSpecification
    {
        public readonly byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 63)]
        public readonly byte[] information;
    }

#endregion

#region Nested type: DescriptorTag

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DescriptorTag
    {
        public readonly TagIdentifier tagIdentifier;
        public readonly ushort        descriptorVersion;
        public readonly byte          tagChecksum;
        public readonly byte          reserved;
        public readonly ushort        tagSerialNumber;
        public readonly ushort        descriptorCrc;
        public readonly ushort        descriptorCrcLength;
        public readonly uint          tagLocation;
    }

#endregion

#region Nested type: EntityIdentifier

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct EntityIdentifier
    {
        /// <summary>Entity flags</summary>
        public readonly EntityFlags flags;
        /// <summary>Structure identifier</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 23)]
        public readonly byte[] identifier;
        /// <summary>Structure data</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] identifierSuffix;
    }

#endregion

#region Nested type: ExtentDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtentDescriptor
    {
        public readonly uint length;
        public readonly uint location;
    }

#endregion

#region Nested type: LogicalVolumeDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogicalVolumeDescriptor
    {
        public readonly DescriptorTag          tag;
        public readonly uint                   volumeDescriptorSequenceNumber;
        public readonly CharacterSpecification descriptorCharacterSet;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] logicalVolumeIdentifier;
        public readonly uint             logicalBlockSize;
        public readonly EntityIdentifier domainIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public readonly byte[] logicalVolumeContentsUse;
        public readonly uint             mapTableLength;
        public readonly uint             numberOfPartitionMaps;
        public readonly EntityIdentifier implementationIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] implementationUse;
        public readonly ExtentDescriptor integritySequenceExtent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 440)]
        public readonly byte[] partitionMaps;
    }

#endregion

    /* ISO 13346 4/14.15 */
    readonly struct LogicalVolumeHeaderDesc
    {
        readonly ulong uniqueID;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        readonly byte[] reserved;
    }

#region Nested type: LogicalVolumeIntegrityDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogicalVolumeIntegrityDescriptor
    {
        public readonly DescriptorTag           tag;
        public readonly Timestamp               recordingDateTime;
        public readonly IntegrityType           integrityType;
        public readonly ExtentDescriptor        nextIntegrityExtent;
        public readonly LogicalVolumeHeaderDesc logicalVolumeContentsUse;
        public readonly uint                    numberOfPartitions;
        public readonly uint                    lengthOfImplementationUse;

        // Follows uint[numberOfPartitions] freeSpaceTable;
        // Follows uint[numberOfPartitions] sizeTable;
        // Follows byte[lengthOfImplementationUse] implementationUse;
    }

#endregion

#region Nested type: LogicalVolumeIntegrityDescriptorImplementationUse

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogicalVolumeIntegrityDescriptorImplementationUse
    {
        public readonly EntityIdentifier implementationId;
        public readonly uint             files;
        public readonly uint             directories;
        public readonly ushort           minimumReadUDF;
        public readonly ushort           minimumWriteUDF;
        public readonly ushort           maximumWriteUDF;
    }

#endregion

#region Nested type: PrimaryVolumeDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PrimaryVolumeDescriptor
    {
        public readonly DescriptorTag tag;
        public readonly uint          volumeDescriptorSequenceNumber;
        public readonly uint          primaryVolumeDescriptorNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] volumeIdentifier;
        public readonly ushort volumeSequenceNumber;
        public readonly ushort maximumVolumeSequenceNumber;
        public readonly ushort interchangeLevel;
        public readonly ushort maximumInterchangeLevel;
        public readonly uint   characterSetList;
        public readonly uint   maximumCharacterSetList;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] volumeSetIdentifier;
        public readonly CharacterSpecification descriptorCharacterSet;
        public readonly CharacterSpecification explanatoryCharacterSet;
        public readonly ExtentDescriptor       volumeAbstract;
        public readonly ExtentDescriptor       volumeCopyright;
        public readonly EntityIdentifier       applicationIdentifier;
        public readonly Timestamp              recordingDateTime;
        public readonly EntityIdentifier       implementationIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public readonly byte[] implementationUse;
        public readonly uint                  predecessorVolumeDescriptorSequenceLocation;
        public readonly VolumeDescriptorFlags flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public readonly byte[] reserved;
    }

#endregion


#region Nested type: Timestamp

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Timestamp
    {
        public readonly ushort typeAndZone;
        public readonly short  year;
        public readonly byte   month;
        public readonly byte   day;
        public readonly byte   hour;
        public readonly byte   minute;
        public readonly byte   second;
        public readonly byte   centiseconds;
        public readonly byte   hundredsMicroseconds;
        public readonly byte   microseconds;
    }

#endregion

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BeginningExtendedAreaDescriptor
    {
        public readonly byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] identifier;
        public readonly byte version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2041)]
        public readonly byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeStructureDescriptor
    {
        public readonly byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] identifier;
        public readonly byte version;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2041)]
        public readonly byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct BootDescriptor
    {
        public readonly byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] identifier;
        public readonly byte             version;
        public readonly byte             reserved;
        public readonly EntityIdentifier architectureType;
        public readonly EntityIdentifier bootIdentifier;
        public readonly uint             bootExtentLocation;
        public readonly uint             bootExtentLength;
        public readonly ulong            loadAddress;
        public readonly ulong            startAddress;
        public readonly Timestamp        creationDateAndTime;
        public readonly BootFlags        flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1906)]
        public readonly byte[] bootUse;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct NsrDescriptor
    {
        public readonly byte type;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] identifier;
        public readonly byte version;
        public readonly byte reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2040)]
        public readonly byte[] data;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeDescriptorPointer
    {
        public readonly DescriptorTag    tag;
        public readonly uint             volumeDescriptorSequenceNumber;
        public readonly ExtentDescriptor volumeDescriptorPointer;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 484)]
        public readonly byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ImplementationUseVolumeDescriptor
    {
        public readonly DescriptorTag    tag;
        public readonly uint             volumeDescriptorSequenceNumber;
        public readonly EntityIdentifier implementationIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 460)]
        public readonly byte[] implementationUse;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PartitionDescriptor
    {
        public readonly DescriptorTag    tag;
        public readonly uint             volumeDescriptorSequenceNumber;
        public readonly PartitionFlags   flags;
        public readonly ushort           partitionNumber;
        public readonly EntityIdentifier partitionContents;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] partitionContentsUse;
        public readonly PartitionAccess  accessType;
        public readonly uint             partitionStartingLocation;
        public readonly uint             partitionLength;
        public readonly EntityIdentifier implementationIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] implementationUse;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 156)]
        public readonly byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct UnallocatedSpaceDescriptor
    {
        public readonly DescriptorTag tag;
        public readonly uint          volumeDescriptorSequenceNumber;
        public readonly uint          numberOfAllocationDescriptors;

        // Followed by Allocation Descriptors
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TerminatingDescriptor
    {
        public readonly DescriptorTag tag;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 496)]
        public readonly byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LogicalBlockAddress
    {
        public readonly uint   logicalBlockNumber;
        public readonly ushort partitionReferenceNumber;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ShortAllocationDescriptor
    {
        public readonly uint extentLength;
        public readonly uint extentLocation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct LongAllocationDescriptor
    {
        public readonly uint                extentLength;
        public readonly LogicalBlockAddress extentLocation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        public readonly byte[] implementationUse;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAllocationDescriptor
    {
        public readonly uint                extentLength;
        public readonly uint                recordedLength;
        public readonly uint                informationLength;
        public readonly LogicalBlockAddress extentLocation;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] implementationUse;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileSetDescriptor
    {
        public readonly DescriptorTag          tag;
        public readonly Timestamp              recordingDateAndTime;
        public readonly ushort                 interchangeLevel;
        public readonly ushort                 maximumInterchangeLevel;
        public readonly uint                   characterSetList;
        public readonly uint                   maximumCharacterSetList;
        public readonly uint                   fileSetNumber;
        public readonly uint                   fileSetDescriptorNumber;
        public readonly CharacterSpecification logicalVolumeIdentifierCharacterSet;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] logicalVolumeIdentifier;
        public readonly CharacterSpecification fileSetIdentifierCharacterSet;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] fileSetIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] copyrightFileIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] abstractFileIdentifier;
        public readonly LongAllocationDescriptor rootDirectoryICB;
        public readonly EntityIdentifier         domainIdentifier;
        public readonly LongAllocationDescriptor nextExtent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 48)]
        public readonly byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PartitionHeaderDescriptor
    {
        public readonly ShortAllocationDescriptor unallocatedSpaceTable;
        public readonly ShortAllocationDescriptor unallocatedSpaceBitmap;
        public readonly ShortAllocationDescriptor partitionIntegrityTable;
        public readonly ShortAllocationDescriptor freedSpaceTable;
        public readonly ShortAllocationDescriptor freedSpaceBitmap;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 88)]
        public readonly byte[] reserved;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileIdentifierDescriptor
    {
        public readonly DescriptorTag            tag;
        public readonly ushort                   fileVersionNumber;
        public readonly FileCharacteristics      fileCharacteristics;
        public readonly byte                     lengthOfFileIdentifier;
        public readonly LongAllocationDescriptor icb;
        public readonly uint                     lengthOfImplementationUse;

        // Followed by Implementation Use, and File Identifier
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IcbTag
    {
        public readonly uint   priorRecordedNumberOfDirectEntries;
        public readonly ushort strategyType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] strategyParameter;
        public readonly ushort              maximumNumberOfEntries;
        public readonly byte                reserved;
        public readonly FileType            fileType;
        public readonly LogicalBlockAddress parentIcbLocation;
        public readonly FileFlags           flags;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct IndirectEntry
    {
        public readonly DescriptorTag            tag;
        public readonly IcbTag                   icbTag;
        public readonly LongAllocationDescriptor indirectIcb;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct TerminalEntry
    {
        public readonly DescriptorTag tag;
        public readonly IcbTag        icbTag;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileEntry
    {
        public readonly DescriptorTag            tag;
        public readonly IcbTag                   icbTag;
        public readonly uint                     uid;
        public readonly uint                     gid;
        public readonly Permissions              permissions;
        public readonly ushort                   fileLinkCount;
        public readonly byte                     recordFormat;
        public readonly byte                     recordDisplayAttributes;
        public readonly uint                     recordLength;
        public readonly ulong                    informationLength;
        public readonly ulong                    logicalBlocksRecorded;
        public readonly Timestamp                accessTime;
        public readonly Timestamp                modificationTime;
        public readonly Timestamp                attributeTime;
        public readonly uint                     checkpoint;
        public readonly LongAllocationDescriptor extendedAttributeICB;
        public readonly EntityIdentifier         implementationIdentifier;
        public readonly ulong                    uniqueId;
        public readonly uint                     lengthOfExtendedAttributes;
        public readonly uint                     lengthOfAllocationDescriptors;

        // Followed by Extended Attributes and Allocation Descriptors
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedAttributeHeaderDescriptor
    {
        public readonly DescriptorTag tag;
        public readonly uint          implementationAttributesLocation;
        public readonly uint          applicationAttributesLocation;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct UnallocatedSpaceEntry
    {
        public readonly DescriptorTag tag;
        public readonly IcbTag        icbTag;
        public readonly uint          lengthOfUnallocatedSpaceDescriptors;

        // Followed by Unallocated Space Descriptors
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SpaceBitmapEntry
    {
        public readonly DescriptorTag tag;
        public readonly uint          numberOfBits;
        public readonly uint          numberOfBytes;

        // Followed by Bitmap Data (bytes)
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct PartitionIntegrityEntry
    {
        public readonly DescriptorTag tag;
        public readonly IcbTag        icbTag;
        public readonly Timestamp     recordingTime;
        public readonly byte          integrityType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 175)]
        public readonly byte[] reserved;
        public readonly EntityIdentifier ImplementationIdentifier;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public readonly byte[] implementationUse;
    }
}