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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class RBF
{
#region Nested type: IdSector

    /// <summary>Identification sector. Wherever the sector this resides on, becomes LSN 0.</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct IdSector
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
    internal partial struct NewIdSector
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

#region Nested type: FileDescriptor

    /// <summary>File Descriptor (FD) - describes a file or directory (256 bytes total, one sector)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct FileDescriptor
    {
        /// <summary>FD.ATT ($00) - File attributes (1 byte)</summary>
        public byte fd_att;
        /// <summary>FD.OWN ($01) - Owner's user ID (2 bytes)</summary>
        public ushort fd_own;
        /// <summary>FD.DAT ($03) - Date last modified (5 bytes: YY MM DD HH MM)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] fd_date;
        /// <summary>FD.LNK ($08) - Link count (1 byte)</summary>
        public byte fd_link;
        /// <summary>FD.SIZ ($09) - File size in bytes (4 bytes)</summary>
        public uint fd_fsize;
        /// <summary>FD.CREAT ($0D) - Date created (3 bytes: YY MM DD)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] fd_dcr;
        /// <summary>FD.SEG ($10) - Segment list: 48 entries of 5 bytes each = 240 bytes (max 48 segments)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 240)]
        public byte[] fd_seg;
    }

#endregion

#region Nested type: NewFileDescriptor

    /// <summary>File Descriptor for OS-9000</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct NewFileDescriptor
    {
        /// <summary>File attributes</summary>
        public byte fd_att;
        /// <summary>Owner's user ID</summary>
        public ushort fd_own;
        /// <summary>Group ID</summary>
        public ushort fd_grp;
        /// <summary>Date last modified (time_t format)</summary>
        public uint fd_date;
        /// <summary>Link count</summary>
        public ushort fd_link;
        /// <summary>File size (in bytes)</summary>
        public uint fd_fsize;
        /// <summary>Date created (time_t format)</summary>
        public uint fd_dcr;
        /// <summary>Segment list</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 240)]
        public byte[] fd_seg;
    }

#endregion

#region Nested type: DirectoryEntry

    /// <summary>Directory entry structure (32 bytes total)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct DirectoryEntry
    {
        /// <summary>Filename (28 bytes, MSB of last char set for termination)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] dir_name;
        /// <summary>Reserved byte (must be zero)</summary>
        public byte dir_res1;
        /// <summary>LSN of file descriptor (3 bytes, big-endian)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] dir_fd;
    }

#endregion

#region Nested type: NewDirectoryEntry

    /// <summary>Directory entry for OS-9000 (32 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct NewDirectoryEntry
    {
        /// <summary>Filename (up to 28 characters, null terminated)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        public byte[] dir_name;
        /// <summary>LSN of file descriptor sector (4 bytes)</summary>
        public uint dir_fd;
    }

#endregion

#region Nested type: Segment

    /// <summary>Segment descriptor - describes a contiguous block of disk space (5 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct Segment
    {
        /// <summary>Physical sector number (LSN) of segment start (3 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] seg_lsn;
        /// <summary>Number of sectors in segment (2 bytes)</summary>
        public ushort seg_size;
    }

#endregion

#region Nested type: NewSegment

    /// <summary>Segment descriptor for OS-9000 (8 bytes)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct NewSegment
    {
        /// <summary>Physical sector number (LSN) of segment start</summary>
        public uint seg_lsn;
        /// <summary>Number of sectors in segment</summary>
        public uint seg_size;
    }

#endregion

#region Nested type: PathDescriptor

    /// <summary>Path descriptor options structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct PathDescriptor
    {
        /// <summary>Device type</summary>
        public byte pd_dtp;
        /// <summary>Drive number</summary>
        public byte pd_drv;
        /// <summary>Step rate</summary>
        public byte pd_stp;
        /// <summary>Device type (usually RBF)</summary>
        public byte pd_typ;
        /// <summary>Density capability</summary>
        public byte pd_dns;
        /// <summary>Number of cylinders (tracks)</summary>
        public ushort pd_cyl;
        /// <summary>Number of sides</summary>
        public byte pd_sid;
        /// <summary>Verify writes flag</summary>
        public byte pd_vfy;
        /// <summary>Sectors per track</summary>
        public ushort pd_sct;
        /// <summary>Sectors per track (track 0)</summary>
        public ushort pd_t0s;
        /// <summary>Sector interleave factor</summary>
        public byte pd_ilv;
        /// <summary>Segment allocation size</summary>
        public byte pd_sas;
        /// <summary>DMA transfer mode</summary>
        public byte pd_tfm;
        /// <summary>Controller address</summary>
        public ushort pd_att;
        /// <summary>Reserved</summary>
        public byte pd_res1;
        /// <summary>Path descriptor options</summary>
        public byte pd_options;
    }

#endregion

#region Nested type: ExtendedFileDescriptor

    /// <summary>Extended file descriptor with additional OS-9 Level II fields</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    internal partial struct ExtendedFileDescriptor
    {
        /// <summary>File attributes</summary>
        public byte fd_att;
        /// <summary>Owner's user ID</summary>
        public ushort fd_own;
        /// <summary>Date last modified</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] fd_date;
        /// <summary>Link count</summary>
        public byte fd_link;
        /// <summary>File size</summary>
        public uint fd_fsize;
        /// <summary>Date created</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] fd_dcr;
        /// <summary>Segment list (240 bytes for 48 segments of 5 bytes each)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 240)]
        public byte[] fd_seg;
        /// <summary>Group ID (Level II extension)</summary>
        public ushort fd_grp;
        /// <summary>Permissions (Level II extension)</summary>
        public ushort fd_perm;
    }

#endregion
}