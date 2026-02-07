// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// ReSharper disable UnusedType.Local

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Professional File System</summary>
public sealed partial class PFS
{
#region Rollover

    /// <summary>Rollover file type</summary>
    const int ST_ROLLOVERFILE = -16;

#endregion

    const string FS_TYPE = "pfs";

#region Disk Type Identifiers

    /// <summary>Identifier for AFS (PFS v1) - 'AFS\1'</summary>
    const uint AFS_DISK = 0x41465301;
    /// <summary>Identifier for PFS v2 - 'PFS\2'</summary>
    const uint PFS2_DISK = 0x50465302;
    /// <summary>Identifier for PFS v3 - 'PFS\1'</summary>
    const uint PFS_DISK = 0x50465301;
    /// <summary>Identifier for multi-user AFS - 'muAF'</summary>
    const uint MUAF_DISK = 0x6D754146;
    /// <summary>Identifier for multi-user PFS - 'muPF'</summary>
    const uint MUPFS_DISK = 0x6D755046;
    /// <summary>Identifier for BUSY state</summary>
    const uint ID_BUSY = 0x42555359;

#endregion

#region Block Identifiers

    /// <summary>Directory block ID ('DB')</summary>
    const ushort DBLKID = 0x4442;
    /// <summary>Anode block ID ('AB')</summary>
    const ushort ABLKID = 0x4142;
    /// <summary>Index block ID ('IB')</summary>
    const ushort IBLKID = 0x4942;
    /// <summary>Bitmap block ID ('BM')</summary>
    const ushort BMBLKID = 0x424D;
    /// <summary>Bitmap index block ID ('MI')</summary>
    const ushort BMIBLKID = 0x4D49;
    /// <summary>Deleted directory block ID ('DD')</summary>
    const ushort DELDIRID = 0x4444;
    /// <summary>Root block extension ID ('EX')</summary>
    const ushort EXTENSIONID = 0x4558;
    /// <summary>Super block ID ('SB')</summary>
    const ushort SBLKID = 0x5342;

#endregion

#region Limits and Sizes

    /// <summary>Maximum small bitmap index (for disks up to ~5GB)</summary>
    const int MAXSMALLBITMAPINDEX = 4;
    /// <summary>Maximum bitmap index (for large disks)</summary>
    const int MAXBITMAPINDEX = 103;
    /// <summary>Maximum number of reserved blocks</summary>
    const int MAXNUMRESERVED = 4096 + 255 * 1024 * 8;
    /// <summary>Maximum super index entries</summary>
    const int MAXSUPER = 15;
    /// <summary>Maximum small index number</summary>
    const int MAXSMALLINDEXNR = 98;
    /// <summary>Maximum deleted directory entries per block</summary>
    const int DELENTRIES_PER_BLOCK = 31;
    /// <summary>Maximum deleted directory blocks</summary>
    const int MAXDELDIR = 31;
    /// <summary>Filename size for compatibility (used for searching files)</summary>
    const int FNSIZE = 108;
    /// <summary>Maximum path size</summary>
    const int PATHSIZE = 256;
    /// <summary>Disk name size</summary>
    const int DNSIZE = 32;
    /// <summary>Comment size</summary>
    const int CMSIZE = 80;
    /// <summary>Deleted entry filename size (16 bytes, last 2 for extended file size)</summary>
    const int DELENTRYFNSIZE = 16;

#endregion

#region Predefined Anode Numbers

    /// <summary>End of file anode marker</summary>
    const uint ANODE_EOF = 0;
    /// <summary>Reserved anode 1 (not used by MODE_BIG)</summary>
    const uint ANODE_RESERVED_1 = 1;
    /// <summary>Reserved anode 2 (not used by MODE_BIG)</summary>
    const uint ANODE_RESERVED_2 = 2;
    /// <summary>Reserved anode 3 (not used by MODE_BIG)</summary>
    const uint ANODE_RESERVED_3 = 3;
    /// <summary>Bad blocks anode (not used yet)</summary>
    const uint ANODE_BADBLOCKS = 4;
    /// <summary>Root directory anode</summary>
    const uint ANODE_ROOTDIR = 5;
    /// <summary>First user anode</summary>
    const uint ANODE_USERFIRST = 6;

#endregion

#region Block Locations

    /// <summary>Boot block 1 location</summary>
    const uint BOOTBLOCK1 = 0;
    /// <summary>Boot block 2 location</summary>
    const uint BOOTBLOCK2 = 1;
    /// <summary>Root block location</summary>
    const uint ROOTBLOCK = 2;

#endregion

#region Postponed Operation IDs

    /// <summary>Free blocks and free anodes</summary>
    const uint PP_FREEBLOCKS_FREE = 1;
    /// <summary>Free blocks but keep anodes</summary>
    const uint PP_FREEBLOCKS_KEEP = 2;
    /// <summary>Free anode chain</summary>
    const uint PP_FREEANODECHAIN = 3;

#endregion

#region DelDir Constants

    /// <summary>Deleted entry separator character</summary>
    const char DELENTRY_SEP = '@';
    /// <summary>Deleted entry default protection</summary>
    const ushort DELENTRY_PROT = 0x0005;
    /// <summary>Deleted entry protection AND mask</summary>
    const ushort DELENTRY_PROT_AND_MASK = 0xAA0F;
    /// <summary>Deleted entry protection OR mask</summary>
    const ushort DELENTRY_PROT_OR_MASK = 0x0005;

#endregion

#region Other Constants

    /// <summary>Maximum file size reportable in DOS ULONG fields</summary>
    const uint MAXFILESIZE32 = 0x7FFFFFFF;
    /// <summary>Number of reserved anodes per anode block</summary>
    const int RESERVEDANODES = 6;

#endregion
}