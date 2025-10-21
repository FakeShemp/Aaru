// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : SmartFileSystem plugin.
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

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Smart File System</summary>
public sealed partial class SFS
{
#region Nested type: RootBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlock
    {
        public uint   blockId;
        public uint   blockChecksum;
        public uint   blockSelfPointer;
        public ushort version;
        public ushort sequence;
        public uint   datecreated;
        public Flags  bits;
        public byte   padding1;
        public ushort padding2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] reserved1;
        public ulong firstbyte;
        public ulong lastbyte;
        public uint  totalblocks;
        public uint  blocksize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public uint[] reserved2;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public uint[] reserved3;
        public uint bitmapbase;
        public uint adminspacecontainer;
        public uint rootobjectcontainer;
        public uint extentbnoderoot;
        public uint objectnoderoot;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public uint[] reserved4;
    }

#endregion
}