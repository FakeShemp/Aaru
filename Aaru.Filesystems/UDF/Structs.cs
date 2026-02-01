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
        public readonly ushort                   lengthOfImplementationUse;

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

    /// <summary>Extended File Entry for UDF 2.00+ per ECMA-167 4/14.17</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedFileEntry
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
        public readonly ulong                    objectSize;
        public readonly ulong                    logicalBlocksRecorded;
        public readonly Timestamp                accessTime;
        public readonly Timestamp                modificationTime;
        public readonly Timestamp                creationTime;
        public readonly Timestamp                attributeTime;
        public readonly uint                     checkpoint;
        public readonly uint                     reserved;
        public readonly LongAllocationDescriptor extendedAttributeICB;
        public readonly LongAllocationDescriptor streamDirectoryICB;
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

    /// <summary>Generic Extended Attribute header per ECMA-167 4/14.10.1</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct GenericExtendedAttributeHeader
    {
        public readonly uint attributeType;
        public readonly byte attributeSubtype;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint attributeLength;

        // Followed by attribute data
    }

    /// <summary>Implementation Use Extended Attribute per ECMA-167 4/14.10.8</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ImplementationUseExtendedAttribute
    {
        public readonly uint attributeType;    // 2048
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint             attributeLength;
        public readonly uint             implementationUseLength;
        public readonly EntityIdentifier implementationIdentifier;

        // Followed by implementation use data
    }

    /// <summary>Application Use Extended Attribute per ECMA-167 4/14.10.9</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ApplicationUseExtendedAttribute
    {
        public readonly uint attributeType;    // 65536
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint             attributeLength;
        public readonly uint             applicationUseLength;
        public readonly EntityIdentifier applicationIdentifier;

        // Followed by application use data
    }

    /// <summary>Character Set Information Extended Attribute per ECMA-167 4/14.10.2</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct CharacterSetInformationExtendedAttribute
    {
        public readonly uint attributeType;    // 1
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint attributeLength;
        public readonly uint escapeSequencesLength;
        public readonly byte characterSetType;

        // Followed by escape sequences
    }

    /// <summary>Alternate Permissions Extended Attribute per ECMA-167 4/14.10.3</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AlternatePermissionsExtendedAttribute
    {
        public readonly uint attributeType;    // 3
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint   attributeLength;
        public readonly ushort ownerIdentification;
        public readonly ushort groupIdentification;
        public readonly ushort permission;
    }

    /// <summary>File Times Extended Attribute per ECMA-167 4/14.10.5</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileTimesExtendedAttribute
    {
        public readonly uint attributeType;    // 5
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint attributeLength;
        public readonly uint dataLength;
        public readonly uint fileTimeExistence;

        // Followed by timestamps
    }

    /// <summary>Information Times Extended Attribute per ECMA-167 4/14.10.6</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct InformationTimesExtendedAttribute
    {
        public readonly uint attributeType;    // 6
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint attributeLength;
        public readonly uint dataLength;
        public readonly uint infoTimeExistence;

        // Followed by timestamps
    }

    /// <summary>Device Specification Extended Attribute per ECMA-167 4/14.10.7</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DeviceSpecificationExtendedAttribute
    {
        public readonly uint attributeType;    // 12
        public readonly byte attributeSubtype; // 1
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] reserved;
        public readonly uint attributeLength;
        public readonly uint implementationUseLength;
        public readonly uint majorDeviceIdentification;
        public readonly uint minorDeviceIdentification;

        // Followed by implementation use
    }

    /// <summary>Free EA Space per UDF 2.01 3.3.4.5.1.1</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FreeEaSpace
    {
        public readonly ushort headerChecksum;

        // Followed by Free EA Space bytes (IU_L - 2 bytes)
    }

    /// <summary>DVD CGMS Info per UDF 2.01 3.3.4.5.1.2</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DvdCgmsInfo
    {
        public readonly ushort headerChecksum;
        public readonly byte   cgmsInformation;
        public readonly byte   dataStructureType;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] protectionSystemInformation;
    }

    /// <summary>OS/400 Directory Info per UDF 2.01</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Os400DirInfo
    {
        public readonly ushort headerChecksum;
        public readonly ushort reserved; // Must be 0
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 44)]
        public readonly byte[] directoryInfo;
    }

    /// <summary>OS/2 EA header per UDF 1.02 3.3.4.5.3.1</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Os2EaHeader
    {
        public readonly ushort headerChecksum;

        // Followed by FEA entries (IU_L - 2 bytes)
    }

    /// <summary>OS/2 Full EA (FEA) per UDF 1.02 3.3.4.5.3.1</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Fea
    {
        public readonly byte   flags;
        public readonly byte   lengthOfName;
        public readonly ushort lengthOfValue;

        // Followed by Name (L_N bytes) and Value (L_V bytes)
    }

    /// <summary>Mac Volume Info per UDF 1.02 3.3.4.5.4</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MacVolumeInfo
    {
        public readonly ushort    headerChecksum;
        public readonly Timestamp lastModificationDate;
        public readonly Timestamp lastBackupDate;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] volumeFinderInformation;
    }

    /// <summary>Mac Finder Info header per UDF 1.02 3.3.4.5.5</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MacFinderInfoHeader
    {
        public readonly ushort headerChecksum;
        public readonly ushort padding;

        // Followed by FinderInfo data (typically 16 or 32 bytes)
    }

    /// <summary>Mac Unique ID Table per UDF 1.02 3.3.4.5.6</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MacUniqueIdTable
    {
        public readonly ushort headerChecksum;
        public readonly ushort reserved;
        public readonly uint   numberOfUniqueIdMaps;

        // Followed by UniqueIdMap entries (N_DID × 8 bytes)
    }

    /// <summary>Unique ID Map entry per UDF 1.02 3.3.4.5.6</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct UniqueIdMap
    {
        public readonly uint uniqueId;
        public readonly uint parentUniqueId;
    }

#region UDF 1.50 Structures

    /// <summary>Type 1 Partition Map per ECMA-167 3/10.7.2</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Type1PartitionMap
    {
        public readonly byte   partitionMapType;   // 1
        public readonly byte   partitionMapLength; // 6
        public readonly ushort volumeSequenceNumber;
        public readonly ushort partitionNumber;
    }

    /// <summary>Type 2 Partition Map header per ECMA-167 3/10.7.3</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Type2PartitionMapHeader
    {
        public readonly byte partitionMapType;   // 2
        public readonly byte partitionMapLength; // 64
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved1;
        public readonly EntityIdentifier partitionTypeIdentifier;
        public readonly ushort           volumeSequenceNumber;
        public readonly ushort           partitionNumber;

        // Followed by partition-type-specific data
    }

    /// <summary>Virtual Partition Map per UDF 1.50 2.2.8</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VirtualPartitionMap
    {
        public readonly byte partitionMapType;   // 2
        public readonly byte partitionMapLength; // 64
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved1;
        public readonly EntityIdentifier partitionTypeIdentifier; // "*UDF Virtual Partition"
        public readonly ushort           volumeSequenceNumber;
        public readonly ushort           partitionNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 24)]
        public readonly byte[] reserved2;
    }

    /// <summary>Sparable Partition Map per UDF 1.50 2.2.9</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SparablePartitionMap
    {
        public readonly byte partitionMapType;   // 2
        public readonly byte partitionMapLength; // 64
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved1;
        public readonly EntityIdentifier partitionTypeIdentifier; // "*UDF Sparable Partition"
        public readonly ushort           volumeSequenceNumber;
        public readonly ushort           partitionNumber;
        public readonly ushort           packetLength;          // In sectors, typically 32
        public readonly byte             numberOfSparingTables; // Usually 2
        public readonly byte             reserved2;
        public readonly uint             sizeOfEachSparingTable;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly uint[] locationsOfSparingTables; // Up to 4 locations, typically 2 used
    }

    /// <summary>Sparing Table per UDF 1.50 2.2.11</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SparingTable
    {
        public readonly DescriptorTag    tag;
        public readonly EntityIdentifier sparingIdentifier; // "*UDF Sparing Table"
        public readonly ushort           reallocationTableLength;
        public readonly ushort           reserved;
        public readonly uint             sequenceNumber;

        // Followed by SparingTableEntry[reallocationTableLength]
    }

    /// <summary>Sparing Table Entry per UDF 1.50 2.2.11</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct SparingTableEntry
    {
        public readonly uint originalLocation;
        public readonly uint mappedLocation;
    }

    /// <summary>Virtual Allocation Table (VAT) ICB Entry for UDF 1.50 2.2.10</summary>
    /// <remarks>
    ///     In UDF 1.50, the VAT is stored as a file. The VAT file contains:
    ///     - An array of uint32 entries mapping virtual to physical block numbers
    ///     - The last entry points to the previous VAT ICB (for multi-session)
    ///     The VAT file entry is located at the last recorded block of the track/session
    /// </remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VirtualAllocationTable150
    {
        // The VAT is simply an array of uint32 values
        // vatEntry[n] contains the logical block number of virtual block n
        // Special value 0xFFFFFFFF means the block is not allocated
        // The length of the VAT is determined by the file size / 4
    }

    /// <summary>Virtual Allocation Table header for UDF 2.00+ (2.2.10)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VirtualAllocationTable200Header
    {
        public readonly ushort lengthOfHeader;
        public readonly ushort lengthOfImplementationUse;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly byte[] logicalVolumeIdentifier;
        public readonly uint   previousVatIcbLocation;
        public readonly uint   numberOfFiles;
        public readonly uint   numberOfDirectories;
        public readonly ushort minimumReadUdf;
        public readonly ushort minimumWriteUdf;
        public readonly ushort maximumWriteUdf;
        public readonly ushort reserved;

        // Followed by implementationUse[lengthOfImplementationUse]
        // Followed by VAT entries (uint32 array)
    }

    /// <summary>Metadata Partition Map per UDF 2.50 2.2.10</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct MetadataPartitionMap
    {
        public readonly byte partitionMapType;   // 2
        public readonly byte partitionMapLength; // 64
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] reserved1;
        public readonly EntityIdentifier partitionTypeIdentifier; // "*UDF Metadata Partition"
        public readonly ushort           volumeSequenceNumber;
        public readonly ushort           partitionNumber;
        public readonly uint             metadataFileLocation;
        public readonly uint             metadataMirrorFileLocation;
        public readonly uint             metadataBitmapFileLocation;
        public readonly uint             allocationUnitSize;
        public readonly ushort           alignmentUnitSize;
        public readonly byte             flags;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public readonly byte[] reserved2;
    }

#endregion
}