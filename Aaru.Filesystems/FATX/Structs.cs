// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : FATX filesystem plugin.
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

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class XboxFatPlugin
{
#region Nested type: DirectoryEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirectoryEntry
    {
        public byte       filenameSize;
        public Attributes attributes;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = MAX_FILENAME)]
        public byte[] filename;
        public uint   firstCluster;
        public uint   length;
        public ushort lastWrittenTime;
        public ushort lastWrittenDate;
        public ushort lastAccessTime;
        public ushort lastAccessDate;
        public ushort creationTime;
        public ushort creationDate;
    }

#endregion

#region Nested type: FatxDirNode

    sealed class FatxDirNode : IDirNode
    {
        internal DirectoryEntry[] Entries;
        internal int              Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: FatxFileNode

    sealed class FatxFileNode : IFileNode
    {
        internal uint[] Clusters;

#region IFileNode Members

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

#endregion
    }

#endregion

#region Nested type: Superblock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Superblock
    {
        public uint magic;
        public uint id;
        public uint sectorsPerCluster;
        public uint rootDirectoryCluster;

        // TODO: Undetermined size
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] volumeLabel;
    }

#endregion
}