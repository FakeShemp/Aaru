// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : XFS filesystem plugin.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of SGI's XFS</summary>
public sealed partial class XFS
{
#region Nested type: Superblock

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Superblock
    {
        public uint   magicnum;
        public uint   blocksize;
        public ulong  dblocks;
        public ulong  rblocks;
        public ulong  rextents;
        public Guid   uuid;
        public ulong  logstat;
        public ulong  rootino;
        public ulong  rbmino;
        public ulong  rsumino;
        public uint   rextsize;
        public uint   agblocks;
        public uint   agcount;
        public uint   rbmblocks;
        public uint   logblocks;
        public ushort version;
        public ushort sectsize;
        public ushort inodesize;
        public ushort inopblock;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
        public byte[] fname;
        public byte   blocklog;
        public byte   sectlog;
        public byte   inodelog;
        public byte   inopblog;
        public byte   agblklog;
        public byte   rextslog;
        public byte   inprogress;
        public byte   imax_pct;
        public ulong  icount;
        public ulong  ifree;
        public ulong  fdblocks;
        public ulong  frextents;
        public ulong  uquotino;
        public ulong  gquotino;
        public ushort qflags;
        public byte   flags;
        public byte   shared_vn;
        public ulong  inoalignmt;
        public ulong  unit;
        public ulong  width;
        public byte   dirblklog;
        public byte   logsectlog;
        public ushort logsectsize;
        public uint   logsunit;
        public uint   features2;
        public uint   bad_features2;
        public uint   features_compat;
        public uint   features_ro_compat;
        public uint   features_incompat;
        public uint   features_log_incompat;

        // This field is little-endian while rest of superblock is big-endian
        public uint  crc;
        public uint  spino_align;
        public ulong pquotino;
        public ulong lsn;
        public Guid  meta_uuid;
    }

#endregion
}