// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Amiga Fast File System plugin.
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
/// <summary>Implements detection of Amiga Fast File System (AFFS)</summary>
public sealed partial class AmigaDOSPlugin
{
#region Nested type: BootBlock

    /// <summary>Boot block, first 2 sectors</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BootBlock
    {
        /// <summary>Offset 0x00, "DOSx" disk type</summary>
        public uint diskType;
        /// <summary>Offset 0x04, Checksum</summary>
        public uint checksum;
        /// <summary>Offset 0x08, Pointer to root block, mostly invalid</summary>
        public uint root_ptr;
        /// <summary>Offset 0x0C, Boot code, til completion. Size is intentionally incorrect to allow marshaling to work.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] bootCode;
    }

#endregion

#region Nested type: RootBlock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlock
    {
        /// <summary>Offset 0x00, block type, value = T_HEADER (2)</summary>
        public uint type;
        /// <summary>Offset 0x04, unused</summary>
        public uint headerKey;
        /// <summary>Offset 0x08, unused</summary>
        public uint highSeq;
        /// <summary>Offset 0x0C, longs used by hash table</summary>
        public uint hashTableSize;
        /// <summary>Offset 0x10, unused</summary>
        public uint firstData;
        /// <summary>Offset 0x14, Rootblock checksum</summary>
        public uint checksum;
        /// <summary>
        ///     Offset 0x18, Hashtable, size = (block size / 4) - 56 or size = hashTableSize. Size intentionally bad to allow
        ///     marshalling to work.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] hashTable;
        /// <summary>Offset 0x18+hashTableSize*4+0, bitmap flag, 0xFFFFFFFF if valid</summary>
        public uint bitmapFlag;
        /// <summary>Offset 0x18+hashTableSize*4+4, bitmap pages, 25 entries</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 25)]
        public uint[] bitmapPages;
        /// <summary>Offset 0x18+hashTableSize*4+104, pointer to bitmap extension block</summary>
        public uint bitmapExtensionBlock;
        /// <summary>Offset 0x18+hashTableSize*4+108, last root alteration days since 1978/01/01</summary>
        public uint rDays;
        /// <summary>Offset 0x18+hashTableSize*4+112, last root alteration minutes past midnight</summary>
        public uint rMins;
        /// <summary>Offset 0x18+hashTableSize*4+116, last root alteration ticks (1/50 secs)</summary>
        public uint rTicks;
        /// <summary>Offset 0x18+hashTableSize*4+120, disk name, pascal string, 31 bytes</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
        public byte[] diskName;
        /// <summary>Offset 0x18+hashTableSize*4+151, unused</summary>
        public byte padding;
        /// <summary>Offset 0x18+hashTableSize*4+152, unused</summary>
        public uint reserved1;
        /// <summary>Offset 0x18+hashTableSize*4+156, unused</summary>
        public uint reserved2;
        /// <summary>Offset 0x18+hashTableSize*4+160, last disk alteration days since 1978/01/01</summary>
        public uint vDays;
        /// <summary>Offset 0x18+hashTableSize*4+164, last disk alteration minutes past midnight</summary>
        public uint vMins;
        /// <summary>Offset 0x18+hashTableSize*4+168, last disk alteration ticks (1/50 secs)</summary>
        public uint vTicks;
        /// <summary>Offset 0x18+hashTableSize*4+172, filesystem creation days since 1978/01/01</summary>
        public uint cDays;
        /// <summary>Offset 0x18+hashTableSize*4+176, filesystem creation minutes since 1978/01/01</summary>
        public uint cMins;
        /// <summary>Offset 0x18+hashTableSize*4+180, filesystem creation ticks since 1978/01/01</summary>
        public uint cTicks;
        /// <summary>Offset 0x18+hashTableSize*4+184, unused</summary>
        public uint nextHash;
        /// <summary>Offset 0x18+hashTableSize*4+188, unused</summary>
        public uint parentDir;
        /// <summary>Offset 0x18+hashTableSize*4+192, first directory cache block</summary>
        public uint extension;
        /// <summary>Offset 0x18+hashTableSize*4+196, block secondary type = ST_ROOT (1)</summary>
        public uint sec_type;
    }

#endregion
}