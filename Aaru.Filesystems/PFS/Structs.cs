// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

// ReSharper disable UnusedType.Local

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Professional File System</summary>
public sealed partial class PFS
{
#region Nested type: BootBlock

    /// <summary>Boot block, first 2 sectors</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BootBlock
    {
        /// <summary>"PFS\1" disk type</summary>
        public uint diskType;
        /// <summary>Boot code, til completion</summary>
        public byte[] bootCode;
    }

#endregion

#region Nested type: RootBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlock
    {
        /// <summary>Disk type</summary>
        public uint diskType;
        /// <summary>Options</summary>
        public uint options;
        /// <summary>Current datestamp</summary>
        public uint datestamp;
        /// <summary>Volume creation day</summary>
        public ushort creationday;
        /// <summary>Volume creation minute</summary>
        public ushort creationminute;
        /// <summary>Volume creation tick</summary>
        public ushort creationtick;
        /// <summary>AmigaDOS protection bits</summary>
        public ushort protection;
        /// <summary>Volume label (Pascal string)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] diskname;
        /// <summary>Last reserved block</summary>
        public uint lastreserved;
        /// <summary>First reserved block</summary>
        public uint firstreserved;
        /// <summary>Free reserved blocks</summary>
        public uint reservedfree;
        /// <summary>Size of reserved blocks in bytes</summary>
        public ushort reservedblocksize;
        /// <summary>Blocks in rootblock, including bitmap</summary>
        public ushort rootblockclusters;
        /// <summary>Free blocks</summary>
        public uint blocksfree;
        /// <summary>Blocks that must be always free</summary>
        public uint alwaysfree;
        /// <summary>Current bitmapfield number for allocation</summary>
        public uint rovingPointer;
        /// <summary>Pointer to deldir</summary>
        public uint delDirPtr;
        /// <summary>Disk size in sectors</summary>
        public uint diskSize;
        /// <summary>Rootblock extension</summary>
        public uint extension;
        /// <summary>Unused</summary>
        public uint unused;
    }

#endregion
}