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

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }
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