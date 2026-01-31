// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Enums.cs
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
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Universal Disk Format filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class UDF
{
#region Nested type: EntityFlags

    [Flags]
    enum EntityFlags : byte
    {
        Dirty     = 0x01,
        Protected = 0x02
    }

#endregion

#region Nested type: TagIdentifier

    enum TagIdentifier : ushort
    {
        PrimaryVolumeDescriptor           = 1,
        AnchorVolumeDescriptorPointer     = 2,
        VolumeDescriptorPointer           = 3,
        ImplementationUseVolumeDescriptor = 4,
        PartitionDescriptor               = 5,
        LogicalVolumeDescriptor           = 6,
        UnallocatedSpaceDescriptor        = 7,
        TerminatingDescriptor             = 8,
        LogicalVolumeIntegrityDescriptor  = 9,
        FileSetDescriptor                 = 256,
        FileIdentifierDescriptor          = 257,
        AllocationExtentDescriptor        = 258,
        IndirectEntry                     = 259,
        TerminalEntry                     = 260,
        FileEntry                         = 261,
        ExtendedAttributeHeaderDescriptor = 262,
        UnallocatedSpaceEntry             = 263,
        SpaceBitmapDescriptor             = 264,
        PartitionIntegrityEntry           = 265
    }

#endregion

    [Flags]
    enum BootFlags : ushort
    {
        Erase = 1
    }

    [Flags]
    enum VolumeDescriptorFlags : ushort
    {
        Common = 1
    }

    [Flags]
    enum PartitionFlags : ushort
    {
        Allocated = 1
    }

    enum PartitionAccess : uint
    {
        Unspecified  = 0,
        ReadOnly     = 1,
        WriteOnce    = 2,
        Rewritable   = 3,
        Overwritable = 4
    }

    enum IntegrityType : uint
    {
        Unspecified = 0,
        Open        = 1,
        Close       = 2
    }

    [Flags]
    enum FileCharacteristics : byte
    {
        Hidden    = 1,
        Directory = 2,
        Deleted   = 4,
        Parent    = 8
    }

    enum FileType : byte
    {
        Unspecified             = 0,
        UnallocatedSpaceEntry   = 1,
        PartitionIntegrityEntry = 2,
        IndirectEntry           = 3,
        Directory               = 4,
        File                    = 5,
        BlockDevice             = 6,
        CharacterDevice         = 7,
        ExtendedAttribute       = 8,
        Fifo                    = 9,
        Socket                  = 10,
        TerminalEntry           = 11,
        SymbolicLink            = 12
    }

    [Flags]
    enum FileFlags : ushort
    {
        Sorted         = 8,
        NonRelocatable = 16,
        Archive        = 32,
        Setuid         = 64,
        Setgid         = 128,
        Sticky         = 256,
        Contiguous     = 512,
        System         = 1024,
        Transformed    = 2048,
        Multiversion   = 4096
    }

    [Flags]
    enum Permissions : uint
    {
        OtherExecute         = 0x01,
        OtherWrite           = 0x02,
        OtherRead            = 0x04,
        OtherChangeAttribute = 0x08,
        OtherDelete          = 0x10,
        GroupExecute         = 0x20,
        GroupWrite           = 0x40,
        GroupRead            = 0x80,
        GroupChangeAttribute = 0x100,
        GroupDelete          = 0x200,
        OwnerExecute         = 0x400,
        OwnerWrite           = 0x800,
        OwnerRead            = 0x1000,
        OwnerChangeAttribute = 0x2000,
        OwnerDelete          = 0x4000
    }
}