// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Internal.cs
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

using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class UDF
{
#region Nested type: UdfDirectoryEntry

    sealed class UdfDirectoryEntry
    {
        public FileCharacteristics      FileCharacteristics;
        public string                   Filename;
        public LongAllocationDescriptor Icb;
    }

#endregion

#region Nested type: UdfDirNode

    sealed class UdfDirNode : IDirNode
    {
        internal UdfDirectoryEntry[] Entries;
        internal int                 Position;

        /// <inheritdoc />
        public string Path { get; init; }
    }

#endregion

#region Nested type: UdfFileNode

    sealed class UdfFileNode : IFileNode
    {
        internal byte[]                   FileEntryBuffer;
        internal UdfFileEntryInfo         FileEntryInfo;
        internal LongAllocationDescriptor Icb;
        internal ushort                   PartitionReferenceNumber;

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }
    }

#endregion

#region Nested type: UdfExtent

    /// <summary>
    ///     Unified representation of a single UDF allocation descriptor (short or long)
    ///     after following any type-3 <c>EXT_NEXT_EXTENT_ALLOCDESCS</c> continuation chains.
    ///     Only contains data extents (types 0, 1, 2); continuation extents are consumed internally.
    /// </summary>
    readonly struct UdfExtent
    {
        /// <summary>Logical block number of the extent within its partition.</summary>
        public readonly uint LogicalBlock;

        /// <summary>Partition reference number the extent is located in.</summary>
        public readonly ushort PartitionReferenceNumber;

        /// <summary>Length of the extent in bytes (masked, without type bits).</summary>
        public readonly uint Length;

        /// <summary>
        ///     Extent type: 0 = recorded and allocated, 1 = not recorded but allocated (zeros),
        ///     2 = not recorded and not allocated (zeros).
        /// </summary>
        public readonly byte Type;

        public UdfExtent(uint logicalBlock, ushort partitionReferenceNumber, uint length, byte type)
        {
            LogicalBlock             = logicalBlock;
            PartitionReferenceNumber = partitionReferenceNumber;
            Length                   = length;
            Type                     = type;
        }
    }

#endregion

#region Nested type: UdfFileEntryInfo

    /// <summary>
    ///     Unified structure for holding information from both FileEntry and ExtendedFileEntry
    /// </summary>
    sealed class UdfFileEntryInfo
    {
        public Timestamp                AccessTime;
        public Timestamp                AttributeTime;
        public Timestamp                CreationTime; // Only in ExtendedFileEntry
        public LongAllocationDescriptor ExtendedAttributeICB;
        public ushort                   FileLinkCount;
        public uint                     Gid;
        public IcbTag                   IcbTag;
        public ulong                    InformationLength;
        public bool                     IsExtended;
        public uint                     LengthOfAllocationDescriptors;
        public uint                     LengthOfExtendedAttributes;
        public ulong                    LogicalBlocksRecorded;
        public Timestamp                ModificationTime;
        public Permissions              Permissions;
        public LongAllocationDescriptor StreamDirectoryICB; // Only in ExtendedFileEntry
        public uint                     Uid;
        public ulong                    UniqueId;
    }

#endregion

#region Nested type: UdfNamedStream

    /// <summary>
    ///     Represents a named stream in UDF 2.00+
    /// </summary>
    sealed class UdfNamedStream
    {
        public LongAllocationDescriptor Icb;
        public ulong                    Length;
        public string                   Name;
        public string                   XattrName; // Mapped xattr name
    }

#endregion
}