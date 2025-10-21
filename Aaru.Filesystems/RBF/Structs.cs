// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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
/// <summary>Implements detection of the Locus filesystem</summary>
public sealed partial class RBF
{
#region Nested type: IdSector

    /// <summary>Identification sector. Wherever the sector this resides on, becomes LSN 0.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct IdSector
    {
        /// <summary>Sectors on disk</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] dd_tot;
        /// <summary>Tracks</summary>
        public byte dd_tks;
        /// <summary>Bytes in allocation map</summary>
        public ushort dd_map;
        /// <summary>Sectors per cluster</summary>
        public ushort dd_bit;
        /// <summary>LSN of root directory</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] dd_dir;
        /// <summary>Owner ID</summary>
        public ushort dd_own;
        /// <summary>Attributes</summary>
        public byte dd_att;
        /// <summary>Disk ID</summary>
        public ushort dd_dsk;
        /// <summary>Format byte</summary>
        public byte dd_fmt;
        /// <summary>Sectors per track</summary>
        public ushort dd_spt;
        /// <summary>Reserved</summary>
        public ushort dd_res;
        /// <summary>LSN of boot file</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] dd_bt;
        /// <summary>Size of boot file</summary>
        public ushort dd_bsz;
        /// <summary>Creation date</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] dd_dat;
        /// <summary>Volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] dd_nam;
        /// <summary>Path options</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] dd_opt;
        /// <summary>Reserved</summary>
        public byte reserved;
        /// <summary>Magic number</summary>
        public uint dd_sync;
        /// <summary>LSN of allocation map</summary>
        public uint dd_maplsn;
        /// <summary>Size of an LSN</summary>
        public ushort dd_lsnsize;
        /// <summary>Version ID</summary>
        public ushort dd_versid;
    }

#endregion

#region Nested type: NewIdSector

    /// <summary>
    ///     Identification sector. Wherever the sector this resides on, becomes LSN 0. Introduced on OS-9000, this can be
    ///     big or little endian.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct NewIdSector
    {
        /// <summary>Magic number</summary>
        public uint rid_sync;
        /// <summary>Disk ID</summary>
        public uint rid_diskid;
        /// <summary>Sectors on disk</summary>
        public uint rid_totblocks;
        /// <summary>Cylinders</summary>
        public ushort rid_cylinders;
        /// <summary>Sectors in cylinder 0</summary>
        public ushort rid_cyl0size;
        /// <summary>Sectors per cylinder</summary>
        public ushort rid_cylsize;
        /// <summary>Heads</summary>
        public ushort rid_heads;
        /// <summary>Bytes per sector</summary>
        public ushort rid_blocksize;
        /// <summary>Disk format</summary>
        public ushort rid_format;
        /// <summary>Flags</summary>
        public ushort rid_flags;
        /// <summary>Padding</summary>
        public ushort rid_unused1;
        /// <summary>Sector of allocation bitmap</summary>
        public uint rid_bitmap;
        /// <summary>Sector of debugger FD</summary>
        public uint rid_firstboot;
        /// <summary>Sector of bootfile FD</summary>
        public uint rid_bootfile;
        /// <summary>Sector of root directory FD</summary>
        public uint rid_rootdir;
        /// <summary>Group owner of media</summary>
        public ushort rid_group;
        /// <summary>Owner of media</summary>
        public ushort rid_owner;
        /// <summary>Creation time</summary>
        public uint rid_ctime;
        /// <summary>Last write time for this structure</summary>
        public uint rid_mtime;
        /// <summary>Volume name</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] rid_name;
        /// <summary>Endian flag</summary>
        public byte rid_endflag;
        /// <summary>Padding</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] rid_unused2;
        /// <summary>Parity</summary>
        public uint rid_parity;
    }

#endregion
}