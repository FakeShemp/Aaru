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
// Copyright © 2011-2026 Natalia Portillo
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

    /// <summary>Root block containing volume information</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlock
    {
        /// <summary>Disk type ('PFS\1', 'PFS\2', 'muAF', 'muPF', 'AFS\1')</summary>
        public uint diskType;
        /// <summary>Options (MODE_* flags)</summary>
        public ModeFlags options;
        /// <summary>Current datestamp</summary>
        public uint datestamp;
        /// <summary>Volume creation day (days since Jan 1, 1978)</summary>
        public ushort creationday;
        /// <summary>Volume creation minute (minutes past midnight)</summary>
        public ushort creationminute;
        /// <summary>Volume creation tick (ticks past minute)</summary>
        public ushort creationtick;
        /// <summary>AmigaDOS protection bits</summary>
        public ushort protection;
        /// <summary>Volume label (Pascal string - first byte is length)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] diskname;
        /// <summary>Sector number of last reserved block</summary>
        public uint lastreserved;
        /// <summary>Sector number of first reserved block</summary>
        public uint firstreserved;
        /// <summary>Number of free reserved blocks</summary>
        public uint reservedfree;
        /// <summary>Size of reserved blocks in bytes</summary>
        public ushort reservedblocksize;
        /// <summary>Number of sectors in rootblock, including bitmap</summary>
        public ushort rootblockclusters;
        /// <summary>Free blocks</summary>
        public uint blocksfree;
        /// <summary>Minimum number of blocks to keep always free</summary>
        public uint alwaysfree;
        /// <summary>Current LONG bitmap field number for allocation (roving pointer)</summary>
        public uint rovingPointer;
        /// <summary>Deldir location (versions &lt;= 17.8)</summary>
        public uint delDir;
        /// <summary>Disk size in sectors</summary>
        public uint diskSize;
        /// <summary>Rootblock extension block number (version 16.4+)</summary>
        public uint extension;
        /// <summary>Not used</summary>
        public uint notUsed;

        // Followed by either small or large index arrays depending on disk size
    }

#endregion

#region Nested type: RootBlockSmallIndex

    /// <summary>Small disk index structure in rootblock (for disks up to ~5GB)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlockSmallIndex
    {
        /// <summary>5 bitmap index blocks with 253 bitmap blocks each</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public uint[] bitmapindex;
        /// <summary>99 index blocks with 253+ anode blocks each</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 99)]
        public uint[] indexblocks;
    }

#endregion

#region Nested type: RootBlockLargeIndex

    /// <summary>Large disk index structure in rootblock (for disks larger than ~5GB)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlockLargeIndex
    {
        /// <summary>104 bitmap index blocks (max 104GB for 1K reserved blocks)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 104)]
        public uint[] bitmapindex;
    }

#endregion

#region Nested type: BitmapBlock

    /// <summary>Bitmap block structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BitmapBlock
    {
        /// <summary>Block ID ('BM' = 0x424D)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Sequence number</summary>
        public uint seqnr;

        // Followed by bitmap data (ULONG array)
    }

#endregion

#region Nested type: IndexBlock

    /// <summary>Index block structure (for anode index or bitmap index)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct IndexBlock
    {
        /// <summary>Block ID ('AI' for anode index, 'BI' for bitmap index, 'MI' for bitmap index)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Sequence number</summary>
        public uint seqnr;

        // Followed by index array (LONG array)
    }

#endregion

#region Nested type: Anode

    /// <summary>Anode structure - allocation node for tracking file/directory extents</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct Anode
    {
        /// <summary>Number of blocks in this cluster/extent</summary>
        public uint clustersize;
        /// <summary>Starting block number of this extent</summary>
        public uint blocknr;
        /// <summary>Next anode number (0 = end of file)</summary>
        public uint next;
    }

#endregion

#region Nested type: AnodeBlock

    /// <summary>Anode block containing multiple anodes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct AnodeBlock
    {
        /// <summary>Block ID ('AB' = 0x4142)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Sequence number</summary>
        public uint seqnr;
        /// <summary>Not used</summary>
        public uint notUsed2;

        // Followed by anode array
    }

#endregion

#region Nested type: DirBlock

    /// <summary>Directory block containing directory entries</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirBlock
    {
        /// <summary>Block ID ('DB' = 0x4442)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Not used</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ushort[] notUsed2;
        /// <summary>Anode number belonging to this directory (points to first block of dir)</summary>
        public uint anodenr;
        /// <summary>Parent directory anode number</summary>
        public uint parent;

        // Followed by directory entries
    }

#endregion

#region Nested type: DirEntry

    /// <summary>Directory entry structure</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DirEntry
    {
        /// <summary>Size of this directory entry (offset to next entry)</summary>
        public byte next;
        /// <summary>Entry type (file, directory, link, etc.)</summary>
        public EntryType type;
        /// <summary>Anode number for this entry</summary>
        public uint anode;
        /// <summary>File size in bytes (for files)</summary>
        public uint fsize;
        /// <summary>Creation day (days since Jan 1, 1978)</summary>
        public ushort creationday;
        /// <summary>Creation minute (minutes past midnight)</summary>
        public ushort creationminute;
        /// <summary>Creation tick (ticks past minute)</summary>
        public ushort creationtick;
        /// <summary>Protection bits (like AmigaDOS)</summary>
        public ProtectionBits protection;
        /// <summary>Length of filename</summary>
        public byte nlength;

        // Followed by: filename (nlength bytes), filenote length (1 byte), filenote
    }

#endregion

#region Nested type: ExtraFields

    /// <summary>Extra fields that follow a directory entry (for links, muFS, rollover files)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct ExtraFields
    {
        /// <summary>Link anode number (for hard/soft links)</summary>
        public uint link;
        /// <summary>User ID (muFS)</summary>
        public ushort uid;
        /// <summary>Group ID (muFS)</summary>
        public ushort gid;
        /// <summary>Extended protection bits (bytes 1-3)</summary>
        public ExtendedProtectionBits prot;
        /// <summary>Virtual rollover file size in bytes (as shown by Examine())</summary>
        public uint virtualsize;
        /// <summary>Current start of file AND end of file pointer (rollover)</summary>
        public uint rollpointer;
        /// <summary>Extended bits 32-47 of direntry.fsize (large file support)</summary>
        public ushort fsizex;
    }

#endregion

#region Nested type: DelDirEntry

    /// <summary>Deleted directory entry (in deldir block)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DelDirEntry
    {
        /// <summary>Anode number</summary>
        public uint anodenr;
        /// <summary>Size of file</summary>
        public uint fsize;
        /// <summary>Creation day (days since Jan 1, 1978)</summary>
        public ushort creationday;
        /// <summary>Creation minute</summary>
        public ushort creationminute;
        /// <summary>Creation tick</summary>
        public ushort creationtick;
        /// <summary>Filename (16 bytes)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] filename;
        /// <summary>Extended bits 32-47 of fsize (large file support)</summary>
        public ushort fsizex;
    }

#endregion

#region Nested type: DelDirBlock

    /// <summary>Deleted directory block containing deleted file entries</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct DelDirBlock
    {
        /// <summary>Block ID ('DD' = 0x4444)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Sequence number</summary>
        public uint seqnr;
        /// <summary>Not used</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public ushort[] notUsed2;
        /// <summary>Not used (was roving in versions &lt; 17.9)</summary>
        public ushort notUsed3;
        /// <summary>User ID</summary>
        public ushort uid;
        /// <summary>Group ID</summary>
        public ushort gid;
        /// <summary>Protection bits</summary>
        public uint protection;
        /// <summary>Creation day</summary>
        public ushort creationday;
        /// <summary>Creation minute</summary>
        public ushort creationminute;
        /// <summary>Creation tick</summary>
        public ushort creationtick;

        // Followed by up to 31 DelDirEntry structures
    }

#endregion

#region Nested type: PostponedOp

    /// <summary>Postponed operation structure (stored in rootblock extension)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct PostponedOp
    {
        /// <summary>Which operation is postponed (PP_FREEBLOCKS_FREE=1, PP_FREEBLOCKS_KEEP=2, PP_FREEANODECHAIN=3)</summary>
        public uint operationId;
        /// <summary>Operation argument 1 (e.g., number of blocks)</summary>
        public uint argument1;
        /// <summary>Operation argument 2</summary>
        public uint argument2;
        /// <summary>Operation argument 3</summary>
        public uint argument3;
    }

#endregion

#region Nested type: RootBlockExtension

    /// <summary>Root block extension (version 16.4+)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct RootBlockExtension
    {
        /// <summary>Block ID ('EX' = 0x4558)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed1;
        /// <summary>Extended options</summary>
        public uint extOptions;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>PFS2 revision under which the disk was formatted</summary>
        public uint pfs2version;
        /// <summary>Root directory datestamp (day, minute, tick)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] rootDate;
        /// <summary>Volume datestamp (day, minute, tick)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] volumeDate;
        /// <summary>Postponed operation (currently only delete)</summary>
        public PostponedOp tobedone;
        /// <summary>Reserved roving pointer</summary>
        public uint reservedRoving;
        /// <summary>Bit number in rootblock->roving_ptr bitmap field</summary>
        public ushort rovingbit;
        /// <summary>Anode allocation roving pointer</summary>
        public ushort curranseqnr;
        /// <summary>Deldir roving pointer</summary>
        public ushort deldirroving;
        /// <summary>Size of deldir</summary>
        public ushort deldirsize;
        /// <summary>Filename size (version 18.1+, default 18)</summary>
        public ushort fnsize;
        /// <summary>Not used</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] notUsed2;
        /// <summary>Super index blocks (MODE_SUPERINDEX only)</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] superindex;
        /// <summary>Deldir user ID (version 17.9+)</summary>
        public ushort ddUid;
        /// <summary>Deldir group ID</summary>
        public ushort ddGid;
        /// <summary>Deldir protection</summary>
        public uint ddProtection;
        /// <summary>Deldir creation day</summary>
        public ushort ddCreationday;
        /// <summary>Deldir creation minute</summary>
        public ushort ddCreationminute;
        /// <summary>Deldir creation tick</summary>
        public ushort ddCreationtick;
        /// <summary>Not used</summary>
        public ushort notUsed3;
        /// <summary>32 deldir block numbers</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public uint[] deldir;
    }

#endregion

#region Nested type: SuperBlock

    /// <summary>Super block for MODE_SUPERINDEX (large disks)</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct SuperBlock
    {
        /// <summary>Block ID ('SB' = 0x5342)</summary>
        public ushort id;
        /// <summary>Not used</summary>
        public ushort notUsed;
        /// <summary>Datestamp</summary>
        public uint datestamp;
        /// <summary>Sequence number</summary>
        public uint seqnr;

        // Followed by index array pointing to index blocks
    }

#endregion
}