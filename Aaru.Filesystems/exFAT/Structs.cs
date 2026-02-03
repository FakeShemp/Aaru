// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
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

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
/// <summary>Implements detection of the exFAT filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class exFAT
{
#region Nested type: VolumeBootRecord

    /// <summary>Main and Backup Boot Sector structure (Section 3.1).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeBootRecord
    {
        /// <summary>Jump instruction for CPUs (EBh 76h 90h).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] JumpBoot;
        /// <summary>File system name, shall be "EXFAT   " in ASCII.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] FileSystemName;
        /// <summary>Must be zero to prevent FAT12/16/32 from mounting.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 53)]
        public readonly byte[] MustBeZero;
        /// <summary>Media-relative sector offset of the partition.</summary>
        public readonly ulong PartitionOffset;
        /// <summary>Size of the exFAT volume in sectors.</summary>
        public readonly ulong VolumeLength;
        /// <summary>Volume-relative sector offset of the First FAT.</summary>
        public readonly uint FatOffset;
        /// <summary>Length in sectors of each FAT table.</summary>
        public readonly uint FatLength;
        /// <summary>Volume-relative sector offset of the Cluster Heap.</summary>
        public readonly uint ClusterHeapOffset;
        /// <summary>Number of clusters in the Cluster Heap.</summary>
        public readonly uint ClusterCount;
        /// <summary>Cluster index of the first cluster of the root directory.</summary>
        public readonly uint FirstClusterOfRootDirectory;
        /// <summary>Volume serial number.</summary>
        public readonly uint VolumeSerialNumber;
        /// <summary>File system revision (high byte = major, low byte = minor).</summary>
        public readonly ushort FileSystemRevision;
        /// <summary>Volume flags.</summary>
        public readonly VolumeFlags VolumeFlags;
        /// <summary>Bytes per sector as a power of 2 (e.g., 9 = 512 bytes).</summary>
        public readonly byte BytesPerSectorShift;
        /// <summary>Sectors per cluster as a power of 2.</summary>
        public readonly byte SectorsPerClusterShift;
        /// <summary>Number of FATs (1 or 2).</summary>
        public readonly byte NumberOfFats;
        /// <summary>Extended INT 13h drive number.</summary>
        public readonly byte DriveSelect;
        /// <summary>Percentage of clusters allocated (0-100 or 0xFF if unknown).</summary>
        public readonly byte PercentInUse;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public readonly byte[] Reserved;
        /// <summary>Boot-strapping instructions.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 390)]
        public readonly byte[] BootCode;
        /// <summary>Boot signature (AA55h).</summary>
        public readonly ushort BootSignature;
    }

#endregion

#region Nested type: ExtendedBootSector

    /// <summary>Extended Boot Sector structure (Section 3.2).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ExtendedBootSector
    {
        /// <summary>Boot-strapping instructions (size is 2^BytesPerSectorShift - 4).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 508)]
        public readonly byte[] ExtendedBootCode;
        /// <summary>Extended boot signature (AA550000h).</summary>
        public readonly uint ExtendedBootSignature;
    }

#endregion

#region Nested type: OemParameter

    /// <summary>Generic OEM Parameters template (Section 3.3.2).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OemParameter
    {
        /// <summary>GUID identifying the parameter type.</summary>
        public readonly Guid ParametersGuid;
        /// <summary>Custom defined data (32 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] CustomDefined;
    }

#endregion

#region Nested type: FlashParameters

    /// <summary>Flash Parameters structure (Section 3.3.4). GUID: {0A0C7E46-3399-4021-90C8-FA6D389C4BA2}</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FlashParameters
    {
        /// <summary>GUID identifying the parameter type.</summary>
        public readonly Guid ParametersGuid;
        /// <summary>Size in bytes of the flash media's erase block.</summary>
        public readonly uint EraseBlockSize;
        /// <summary>Size in bytes of the flash media's page.</summary>
        public readonly uint PageSize;
        /// <summary>Number of sectors available for internal sparing operations.</summary>
        public readonly uint SpareSectors;
        /// <summary>Average random access time in nanoseconds.</summary>
        public readonly uint RandomAccessTime;
        /// <summary>Average programming time in nanoseconds.</summary>
        public readonly uint ProgrammingTime;
        /// <summary>Average read cycle time in nanoseconds.</summary>
        public readonly uint ReadCycle;
        /// <summary>Average write cycle time in nanoseconds.</summary>
        public readonly uint WriteCycle;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public readonly byte[] Reserved;
    }

#endregion

#region Nested type: OemParameterTable

    /// <summary>OEM Parameters structure (Section 3.3).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct OemParameterTable
    {
        /// <summary>Array of 10 OEM parameters (48 bytes each).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly OemParameter[] Parameters;
        /// <summary>Reserved (size is 2^BytesPerSectorShift - 480, minimum 32 for 512-byte sectors).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public readonly byte[] Reserved;
    }

#endregion

#region Nested type: ChecksumSector

    /// <summary>Boot Checksum structure (Section 3.4). Contains repeating 4-byte checksum.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct ChecksumSector
    {
        /// <summary>Repeating 4-byte checksum (128 entries for 512-byte sector).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public readonly uint[] Checksum;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>Generic DirectoryEntry template (Section 6.2). All directory entries are 32 bytes.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct DirectoryEntry
    {
        /// <summary>Entry type.</summary>
        public readonly byte EntryType;
        /// <summary>Custom defined (19 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 19)]
        public readonly byte[] CustomDefined;
        /// <summary>Index of the first cluster of the allocation.</summary>
        public readonly uint FirstCluster;
        /// <summary>Size in bytes of the data.</summary>
        public readonly ulong DataLength;
    }

#endregion

#region Nested type: AllocationBitmapDirectoryEntry

    /// <summary>Allocation Bitmap Directory Entry (Section 7.1). EntryType = 81h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct AllocationBitmapDirectoryEntry
    {
        /// <summary>Entry type (81h for first bitmap, 81h with BitmapIdentifier=1 for second).</summary>
        public readonly byte EntryType;
        /// <summary>Bitmap flags.</summary>
        public readonly byte BitmapFlags;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public readonly byte[] Reserved;
        /// <summary>Index of the first cluster of the Allocation Bitmap.</summary>
        public readonly uint FirstCluster;
        /// <summary>Size in bytes of the Allocation Bitmap.</summary>
        public readonly ulong DataLength;
    }

#endregion

#region Nested type: UpcaseTableDirectoryEntry

    /// <summary>Up-case Table Directory Entry (Section 7.2). EntryType = 82h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct UpcaseTableDirectoryEntry
    {
        /// <summary>Entry type (82h).</summary>
        public readonly byte EntryType;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] Reserved1;
        /// <summary>Checksum of the Up-case Table.</summary>
        public readonly uint TableChecksum;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public readonly byte[] Reserved2;
        /// <summary>Index of the first cluster of the Up-case Table.</summary>
        public readonly uint FirstCluster;
        /// <summary>Size in bytes of the Up-case Table.</summary>
        public readonly ulong DataLength;
    }

#endregion

#region Nested type: VolumeLabelDirectoryEntry

    /// <summary>Volume Label Directory Entry (Section 7.3). EntryType = 83h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeLabelDirectoryEntry
    {
        /// <summary>Entry type (83h).</summary>
        public readonly byte EntryType;
        /// <summary>Length of the volume label (0-11 characters).</summary>
        public readonly byte CharacterCount;
        /// <summary>Volume label in Unicode (up to 11 characters, 22 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 22)]
        public readonly byte[] VolumeLabel;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] Reserved;
    }

#endregion

#region Nested type: FileDirectoryEntry

    /// <summary>File Directory Entry (Section 7.4). EntryType = 85h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileDirectoryEntry
    {
        /// <summary>Entry type (85h).</summary>
        public readonly byte EntryType;
        /// <summary>Number of secondary directory entries following this entry.</summary>
        public readonly byte SecondaryCount;
        /// <summary>Checksum of the directory entry set.</summary>
        public readonly ushort SetChecksum;
        /// <summary>File attributes.</summary>
        public readonly FileAttributes FileAttributes;
        /// <summary>Reserved.</summary>
        public readonly ushort Reserved1;
        /// <summary>Creation timestamp.</summary>
        public readonly uint CreateTimestamp;
        /// <summary>Last modification timestamp.</summary>
        public readonly uint LastModifiedTimestamp;
        /// <summary>Last access timestamp.</summary>
        public readonly uint LastAccessedTimestamp;
        /// <summary>10ms increment for creation time (0-199).</summary>
        public readonly byte Create10msIncrement;
        /// <summary>10ms increment for last modification time (0-199).</summary>
        public readonly byte LastModified10msIncrement;
        /// <summary>UTC offset for creation time.</summary>
        public readonly byte CreateUtcOffset;
        /// <summary>UTC offset for last modification time.</summary>
        public readonly byte LastModifiedUtcOffset;
        /// <summary>UTC offset for last access time.</summary>
        public readonly byte LastAccessedUtcOffset;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 7)]
        public readonly byte[] Reserved2;
    }

#endregion

#region Nested type: VolumeGuidDirectoryEntry

    /// <summary>Volume GUID Directory Entry (Section 7.5). EntryType = A0h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VolumeGuidDirectoryEntry
    {
        /// <summary>Entry type (A0h).</summary>
        public readonly byte EntryType;
        /// <summary>Number of secondary entries (shall be 0).</summary>
        public readonly byte SecondaryCount;
        /// <summary>Checksum of the directory entry set.</summary>
        public readonly ushort SetChecksum;
        /// <summary>General primary flags.</summary>
        public readonly ushort GeneralPrimaryFlags;
        /// <summary>Volume GUID.</summary>
        public readonly Guid VolumeGuid;
        /// <summary>Reserved.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public readonly byte[] Reserved;
    }

#endregion

#region Nested type: StreamExtensionDirectoryEntry

    /// <summary>Stream Extension Directory Entry (Section 7.6). EntryType = C0h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct StreamExtensionDirectoryEntry
    {
        /// <summary>Entry type (C0h).</summary>
        public readonly byte EntryType;
        /// <summary>General secondary flags.</summary>
        public readonly byte GeneralSecondaryFlags;
        /// <summary>Reserved.</summary>
        public readonly byte Reserved1;
        /// <summary>Length of the file name (1-255).</summary>
        public readonly byte NameLength;
        /// <summary>Hash of the up-cased file name.</summary>
        public readonly ushort NameHash;
        /// <summary>Reserved.</summary>
        public readonly ushort Reserved2;
        /// <summary>Valid data length.</summary>
        public readonly ulong ValidDataLength;
        /// <summary>Reserved.</summary>
        public readonly uint Reserved3;
        /// <summary>Index of the first cluster of the data stream.</summary>
        public readonly uint FirstCluster;
        /// <summary>Size in bytes of the data stream.</summary>
        public readonly ulong DataLength;
    }

#endregion

#region Nested type: FileNameDirectoryEntry

    /// <summary>File Name Directory Entry (Section 7.7). EntryType = C1h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct FileNameDirectoryEntry
    {
        /// <summary>Entry type (C1h).</summary>
        public readonly byte EntryType;
        /// <summary>General secondary flags.</summary>
        public readonly byte GeneralSecondaryFlags;
        /// <summary>Portion of the file name (up to 15 Unicode characters, 30 bytes).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
        public readonly byte[] FileName;
    }

#endregion

#region Nested type: VendorExtensionDirectoryEntry

    /// <summary>Vendor Extension Directory Entry (Section 7.8). EntryType = E0h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VendorExtensionDirectoryEntry
    {
        /// <summary>Entry type (E0h).</summary>
        public readonly byte EntryType;
        /// <summary>General secondary flags.</summary>
        public readonly byte GeneralSecondaryFlags;
        /// <summary>Vendor GUID identifying the extension.</summary>
        public readonly Guid VendorGuid;
        /// <summary>Vendor-defined data.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        public readonly byte[] VendorDefined;
    }

#endregion

#region Nested type: VendorAllocationDirectoryEntry

    /// <summary>Vendor Allocation Directory Entry (Section 7.9). EntryType = E1h.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct VendorAllocationDirectoryEntry
    {
        /// <summary>Entry type (E1h).</summary>
        public readonly byte EntryType;
        /// <summary>General secondary flags.</summary>
        public readonly byte GeneralSecondaryFlags;
        /// <summary>Vendor GUID identifying the allocation.</summary>
        public readonly Guid VendorGuid;
        /// <summary>Vendor-defined data.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public readonly byte[] VendorDefined;
        /// <summary>Index of the first cluster of the vendor allocation.</summary>
        public readonly uint FirstCluster;
        /// <summary>Size in bytes of the vendor allocation.</summary>
        public readonly ulong DataLength;
    }

#endregion

#region Nested type: Timestamp

    /// <summary>Timestamp field structure (Section 7.4.8).</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    readonly struct Timestamp
    {
        /// <summary>
        ///     Packed timestamp value:
        ///     Bits 0-4: DoubleSeconds (0-29, representing 0-58 seconds in 2-second increments)
        ///     Bits 5-10: Minute (0-59)
        ///     Bits 11-15: Hour (0-23)
        ///     Bits 16-20: Day (1-31)
        ///     Bits 21-24: Month (1-12)
        ///     Bits 25-31: Year (0-127, relative to 1980)
        /// </summary>
        public readonly uint Value;
    }

#endregion
}