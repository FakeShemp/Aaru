// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
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

namespace Aaru.Filesystems;

// Information from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/FileIO/exfat-specification.md
/// <inheritdoc />
/// <summary>Implements detection of the exFAT filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]

// ReSharper disable once InconsistentNaming
public sealed partial class exFAT
{
#region Nested type: VolumeFlags

    /// <summary>Volume flags (Section 3.1.13).</summary>
    [Flags]
    enum VolumeFlags : ushort
    {
        /// <summary>If set, the Second FAT and Second Allocation Bitmap are active.</summary>
        ActiveFat = 1 << 0,
        /// <summary>If set, the volume is probably in an inconsistent state.</summary>
        VolumeDirty = 1 << 1,
        /// <summary>If set, the hosting media has reported failures.</summary>
        MediaFailure = 1 << 2,
        /// <summary>If set, implementations shall clear this field to 0 prior to modifying any file system structures.</summary>
        ClearToZero = 1 << 3
    }

#endregion

#region Nested type: FileAttributes

    /// <summary>File attributes (Section 7.4.4).</summary>
    [Flags]
    enum FileAttributes : ushort
    {
        /// <summary>File is read-only.</summary>
        ReadOnly = 1 << 0,
        /// <summary>File is hidden.</summary>
        Hidden = 1 << 1,
        /// <summary>File is a system file.</summary>
        System = 1 << 2,
        /// <summary>Reserved.</summary>
        Reserved1 = 1 << 3,
        /// <summary>Entry is a directory.</summary>
        Directory = 1 << 4,
        /// <summary>File has been modified since last backup.</summary>
        Archive = 1 << 5
    }

#endregion

#region Nested type: EntryType

    /// <summary>Directory entry type codes (Section 6.2.1).</summary>
    enum EntryType : byte
    {
        /// <summary>End of directory marker.</summary>
        EndOfDirectory = 0x00,
        /// <summary>Allocation Bitmap (critical primary, TypeCode=1, TypeImportance=0, TypeCategory=0).</summary>
        AllocationBitmap = 0x81,
        /// <summary>Up-case Table (critical primary, TypeCode=2, TypeImportance=0, TypeCategory=0).</summary>
        UpcaseTable = 0x82,
        /// <summary>Volume Label (critical primary, TypeCode=3, TypeImportance=0, TypeCategory=0).</summary>
        VolumeLabel = 0x83,
        /// <summary>File (critical primary, TypeCode=5, TypeImportance=0, TypeCategory=0).</summary>
        File = 0x85,
        /// <summary>Volume GUID (benign primary, TypeCode=0, TypeImportance=1, TypeCategory=0).</summary>
        VolumeGuid = 0xA0,
        /// <summary>TexFAT Padding (benign primary, TypeCode=1, TypeImportance=1, TypeCategory=0).</summary>
        TexFatPadding = 0xA1,
        /// <summary>Stream Extension (critical secondary, TypeCode=0, TypeImportance=0, TypeCategory=1).</summary>
        StreamExtension = 0xC0,
        /// <summary>File Name (critical secondary, TypeCode=1, TypeImportance=0, TypeCategory=1).</summary>
        FileName = 0xC1,
        /// <summary>Vendor Extension (benign secondary, TypeCode=0, TypeImportance=1, TypeCategory=1).</summary>
        VendorExtension = 0xE0,
        /// <summary>Vendor Allocation (benign secondary, TypeCode=1, TypeImportance=1, TypeCategory=1).</summary>
        VendorAllocation = 0xE1
    }

#endregion

#region Nested type: GeneralPrimaryFlags

    /// <summary>General primary flags (Section 6.3.4).</summary>
    [Flags]
    enum GeneralPrimaryFlags : ushort
    {
        /// <summary>If set, an allocation in the Cluster Heap is possible.</summary>
        AllocationPossible = 1 << 0,
        /// <summary>If set, the allocation is contiguous and FAT entries are invalid.</summary>
        NoFatChain = 1 << 1
    }

#endregion

#region Nested type: GeneralSecondaryFlags

    /// <summary>General secondary flags (Section 6.4.2).</summary>
    [Flags]
    enum GeneralSecondaryFlags : byte
    {
        /// <summary>If set, an allocation in the Cluster Heap is possible.</summary>
        AllocationPossible = 1 << 0,
        /// <summary>If set, the allocation is contiguous and FAT entries are invalid.</summary>
        NoFatChain = 1 << 1
    }

#endregion

#region Nested type: BitmapFlags

    /// <summary>Allocation Bitmap flags (Section 7.1.2).</summary>
    [Flags]
    enum BitmapFlags : byte
    {
        /// <summary>If set, this is the Second Allocation Bitmap; otherwise, First Allocation Bitmap.</summary>
        BitmapIdentifier = 1 << 0
    }

#endregion
}