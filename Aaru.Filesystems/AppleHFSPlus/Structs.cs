// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System Plus plugin.
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
using HFSCatalogNodeID = uint;

// ReSharper disable UseSymbolAlias

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
#region Nested type: VolumeHeader

    /// <summary>HFS+ Volume Header, should be at offset 0x0400 bytes in volume with a size of 532 bytes</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct VolumeHeader
    {
        /// <summary>0x000, "H+" for HFS+, "HX" for HFSX</summary>
        public ushort signature;
        /// <summary>0x002, 4 for HFS+, 5 for HFSX</summary>
        public ushort version;
        /// <summary>0x004, Volume attributes</summary>
        public VolumeAttributes attributes;
        /// <summary>
        ///     0x008, Implementation that last mounted the volume. Reserved by Apple: "8.10" Mac OS 8.1 to 9.2.2 "10.0" Mac
        ///     OS X "HFSJ" Journaled implementation "fsck" /sbin/fsck
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] lastMountedVersion;
        /// <summary>0x00C, Allocation block number containing the journal</summary>
        public uint journalInfoBlock;
        /// <summary>0x010, Date of volume creation</summary>
        public uint createDate;
        /// <summary>0x014, Date of last volume modification</summary>
        public uint modifyDate;
        /// <summary>0x018, Date of last backup</summary>
        public uint backupDate;
        /// <summary>0x01C, Date of last consistency check</summary>
        public uint checkedDate;
        /// <summary>0x020, File on the volume</summary>
        public uint fileCount;
        /// <summary>0x024, Folders on the volume</summary>
        public uint folderCount;
        /// <summary>0x028, Bytes per allocation block</summary>
        public uint blockSize;
        /// <summary>0x02C, Allocation blocks on the volume</summary>
        public uint totalBlocks;
        /// <summary>0x030, Free allocation blocks</summary>
        public uint freeBlocks;
        /// <summary>0x034, Hint for next allocation block</summary>
        public uint nextAllocation;
        /// <summary>0x038, Resource fork clump size</summary>
        public uint rsrcClumpSize;
        /// <summary>0x03C, Data fork clump size</summary>
        public uint dataClumpSize;
        /// <summary>0x040, Next unused CNID</summary>
        public uint nextCatalogID;
        /// <summary>0x044, Times that the volume has been mounted writable</summary>
        public uint writeCount;
        /// <summary>0x048, Used text encoding hints</summary>
        public ulong encodingsBitmap;
        /// <summary>0x050, finderInfo[0], CNID for bootable system's directory</summary>
        public uint drFndrInfo0;
        /// <summary>0x054, finderInfo[1], CNID of the directory containing the boot application</summary>
        public uint drFndrInfo1;
        /// <summary>0x058, finderInfo[2], CNID of the directory that should be opened on boot</summary>
        public uint drFndrInfo2;
        /// <summary>0x05C, finderInfo[3], CNID for Mac OS 8 or 9 directory</summary>
        public uint drFndrInfo3;
        /// <summary>0x060, finderInfo[4], Reserved</summary>
        public uint drFndrInfo4;
        /// <summary>0x064, finderInfo[5], CNID for Mac OS X directory</summary>
        public uint drFndrInfo5;
        /// <summary>0x068, finderInfo[6], first part of Mac OS X volume ID</summary>
        public uint drFndrInfo6;
        /// <summary>0x06C, finderInfo[7], second part of Mac OS X volume ID</summary>
        public uint drFndrInfo7;

        /// <summary>0x070</summary>
        public HFSPlusForkData allocationFile;
        /// <summary>0x0C0</summary>
        public HFSPlusForkData extentsFile;
        /// <summary>0x110</summary>
        public HFSPlusForkData catalogFile;
        /// <summary>0x160</summary>
        public HFSPlusForkData attributesFile;
        /// <summary>0x1B0</summary>
        public HFSPlusForkData startupFile;
    }

#endregion

    /// <summary>
    ///     HFS+ stores most strings in a fully decomposed form known as Canonical Decomposition, a Unicode
    ///     normalization form. This structure represents a Unicode string with a 16-bit length prefix and up to 255
    ///     UTF-16 code units.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    [SwapEndian]
    partial struct HFSUniStr255
    {
        /// <summary>Number of UTF-16 code units in the string.</summary>
        public ushort length;
        /// <summary>UTF-16 encoded Unicode characters (big-endian).</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 510)]
        public byte[] unicode;
    }

    /// <summary>
    ///     BSD-style file permissions, used by Mac OS X for access control. Contains the owner and group IDs,
    ///     administrative and owner flags, file mode, and a union value that can represent iNode number, link count,
    ///     or raw device number depending on the file type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusBSDInfo
    {
        /// <summary>User ID of the file's owner.</summary>
        public uint ownerID;
        /// <summary>Group ID of the file.</summary>
        public uint groupID;
        /// <summary>Super-user changeable flags (UF_NODUMP, UF_IMMUTABLE, UF_APPEND, UF_OPAQUE).</summary>
        public byte adminFlags;
        /// <summary>Owner changeable flags (SF_ARCHIVED, SF_IMMUTABLE, SF_APPEND).</summary>
        public byte ownerFlags;
        /// <summary>File type and permissions (similar to stat st_mode).</summary>
        public short fileMode;
        /// <summary>
        ///     can be iNodeNum, linkCount, or rawDevice
        /// </summary>
        public uint special;
    }

    /// <summary>
    ///     Fork data structure that describes the size and location of a fork (either data or resource). In HFS+,
    ///     each file can have two forks, a data fork and a resource fork, each described by this structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusForkData
    {
        /// <summary>Size in bytes of the valid data in the fork.</summary>
        public ulong logicalSize;
        /// <summary>Clump size for allocating additional space to the fork.</summary>
        public uint clumpSize;
        /// <summary>Total number of allocation blocks used by all extents in this fork.</summary>
        public uint totalBlocks;
        /// <summary>First 8 extent descriptors for this fork.</summary>
        public HFSPlusExtentRecord extents;
    }

    /// <summary>
    ///     An extent record contains an array of 8 extent descriptors. The extents in a record are ordered by the
    ///     relative position of the data within the fork.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusExtentRecord
    {
        /// <summary>Array of 8 extent descriptors.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public HFSPlusExtentDescriptor[] extentDescriptors;
    }

    /// <summary>
    ///     An extent descriptor represents a contiguous region of allocation blocks in the volume. It contains the
    ///     first allocation block and the number of allocation blocks in the extent.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusExtentDescriptor
    {
        /// <summary>First allocation block in this extent.</summary>
        public uint startBlock;
        /// <summary>Number of allocation blocks in this extent.</summary>
        public uint blockCount;
    }

    /// <summary>
    ///     B-tree node descriptor. Each node in a B-tree begins with this descriptor, which contains links to
    ///     related nodes and information about the node itself.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BTNodeDescriptor
    {
        /// <summary>Node number of the next node of this kind, or 0 if this is the last node.</summary>
        public uint fLink;
        /// <summary>Node number of the previous node of this kind, or 0 if this is the first node.</summary>
        public uint bLink;
        /// <summary>Kind of node (leaf, index, header, or map).</summary>
        public BTNodeKind kind;
        /// <summary>Level of this node in the B-tree hierarchy (0 for leaf nodes).</summary>
        public byte height;
        /// <summary>Number of records contained in this node.</summary>
        public ushort numRecords;
        /// <summary>Reserved; set to zero.</summary>
        public ushort reserved;
    }

    /// <summary>
    ///     B-tree header record. This record is always the first record in the header node (node 0) of a B-tree.
    ///     It contains general information about the B-tree such as its size, depth, and the location of the
    ///     first and last leaf nodes.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct BTHeaderRec
    {
        /// <summary>Current depth of the B-tree.</summary>
        public ushort treeDepth;
        /// <summary>Node number of the root node.</summary>
        public uint rootNode;
        /// <summary>Number of data records in all leaf nodes.</summary>
        public uint leafRecords;
        /// <summary>Node number of the first leaf node.</summary>
        public uint firstLeafNode;
        /// <summary>Node number of the last leaf node.</summary>
        public uint lastLeafNode;
        /// <summary>Size of a node in bytes.</summary>
        public ushort nodeSize;
        /// <summary>Maximum length of a key in an index or leaf node.</summary>
        public ushort maxKeyLength;
        /// <summary>Total number of nodes in the B-tree (free and used).</summary>
        public uint totalNodes;
        /// <summary>Number of unused nodes in the B-tree.</summary>
        public uint freeNodes;
        /// <summary>Reserved; set to zero.</summary>
        public ushort reserved1;
        /// <summary>Clump size for the B-tree file (misaligned).</summary>
        public uint clumpSize;
        /// <summary>Type of B-tree (0 for HFS catalog/extents, 128 for user, 255 for reserved).</summary>
        public BTreeTypes btreeType;
        /// <summary>Key comparison type (case-folding for HFSX).</summary>
        public byte keyCompareType;
        /// <summary>Persistent attributes of the B-tree (long aligned).</summary>
        public BTreeAttributes attributes;
        /// <summary>Reserved; set to zero.</summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public uint[] reserved3;
    }

    /// <summary>
    ///     Catalog file key. Used to search for records in the catalog B-tree. The key consists of the parent
    ///     folder's CNID and the name of the file or folder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusCatalogKey
    {
        /// <summary>Length of the key data in bytes (not including this field).</summary>
        public ushort keyLength;
        /// <summary>Catalog node ID (CNID) of the parent folder.</summary>
        public HFSCatalogNodeID parentID;
        /// <summary>Name of the file or folder.</summary>
        public HFSUniStr255 nodeName;
    }

    /// <summary>
    ///     Catalog folder record. Stores information about a folder in the catalog file, including timestamps,
    ///     permissions, Finder info, and the number of items contained in the folder.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusCatalogFolder
    {
        /// <summary>Type of catalog record (folder, file, folder thread, or file thread).</summary>
        public BTreeRecordType recordType;
        /// <summary>Catalog folder flags.</summary>
        public BTCatalogFlags flags;
        /// <summary>Number of files and folders directly contained in this folder.</summary>
        public uint valence;
        /// <summary>Catalog node ID (CNID) of this folder.</summary>
        public HFSCatalogNodeID folderID;
        /// <summary>Date and time the folder was created.</summary>
        public uint createDate;
        /// <summary>Date and time the folder's content was last modified.</summary>
        public uint contentModDate;
        /// <summary>Date and time the folder's attributes were last modified.</summary>
        public uint attributeModDate;
        /// <summary>Date and time the folder was last accessed.</summary>
        public uint accessDate;
        /// <summary>Date and time the folder was last backed up.</summary>
        public uint backupDate;
        /// <summary>BSD-style permissions for Mac OS X.</summary>
        public HFSPlusBSDInfo permissions;
        /// <summary>Finder information for the folder.</summary>
        public AppleCommon.DInfo userInfo;
        /// <summary>Extended Finder information for the folder.</summary>
        public AppleCommon.DXInfo finderInfo;
        /// <summary>Hint for the text encoding to use for the folder name.</summary>
        public uint textEncoding;
        /// <summary>Reserved; set to zero.</summary>
        public uint reserved;
    }

    /// <summary>
    ///     Catalog file record. Stores information about a file in the catalog file, including timestamps,
    ///     permissions, Finder info, and the data and resource fork information.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusCatalogFile
    {
        /// <summary>Type of catalog record (folder, file, folder thread, or file thread).</summary>
        public BTreeRecordType recordType;
        /// <summary>Catalog file flags.</summary>
        public BTCatalogFlags flags;
        /// <summary>Reserved; set to zero.</summary>
        public uint reserved1;
        /// <summary>Catalog node ID (CNID) of this file.</summary>
        public HFSCatalogNodeID fileID;
        /// <summary>Date and time the file was created.</summary>
        public uint createDate;
        /// <summary>Date and time the file's content was last modified.</summary>
        public uint contentModDate;
        /// <summary>Date and time the file's attributes were last modified.</summary>
        public uint attributeModDate;
        /// <summary>Date and time the file was last accessed.</summary>
        public uint accessDate;
        /// <summary>Date and time the file was last backed up.</summary>
        public uint backupDate;
        /// <summary>BSD-style permissions for Mac OS X.</summary>
        public HFSPlusBSDInfo permissions;
        /// <summary>Finder information for the file.</summary>
        public AppleCommon.FInfo userInfo;
        /// <summary>Extended Finder information for the file.</summary>
        public AppleCommon.FXInfo finderInfo;
        /// <summary>Hint for the text encoding to use for the file name.</summary>
        public uint textEncoding;
        /// <summary>Reserved; set to zero.</summary>
        public uint reserved2;

        /// <summary>Information about the file's data fork.</summary>
        public HFSPlusForkData dataFork;
        /// <summary>Information about the file's resource fork.</summary>
        public HFSPlusForkData resourceFork;
    }

    /// <summary>
    ///     Catalog thread record. Provides a link from a file or folder's CNID back to its parent directory and name.
    ///     Thread records allow efficient lookup of a file or folder by CNID alone.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusCatalogThread
    {
        /// <summary>Type of catalog record (folder thread or file thread).</summary>
        public BTreeRecordType recordType;
        /// <summary>Reserved; set to zero.</summary>
        public short reserved;
        /// <summary>Catalog node ID (CNID) of the parent folder.</summary>
        public HFSCatalogNodeID parentID;
        /// <summary>Name of the file or folder.</summary>
        public HFSUniStr255 nodeName;
    }

    /// <summary>
    ///     Extents overflow file key. Used to locate additional extents for a fork when the fork's data
    ///     extends beyond the 8 extents stored in the catalog record.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusExtentKey
    {
        /// <summary>Length of the key data in bytes (not including this field).</summary>
        public ushort keyLength;
        /// <summary>Fork type: 0x00 for data fork, 0xFF for resource fork.</summary>
        public byte forkType;
        /// <summary>Padding; set to zero.</summary>
        public byte pad;
        /// <summary>Catalog node ID (CNID) of the file.</summary>
        public HFSCatalogNodeID fileID;
        /// <summary>Start block of this extent record within the fork.</summary>
        public uint startBlock;
    }

    /// <summary>
    ///     Attribute fork data record. Used in the attributes file to store large attribute data
    ///     that cannot fit inline. Contains a complete fork data structure.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusAttrForkData
    {
        /// <summary>Type of attribute record (kHFSPlusAttrForkData).</summary>
        public BTAttributeRecordType recordType;
        /// <summary>Reserved; set to zero.</summary>
        public uint reserved;
        /// <summary>Fork data describing the attribute's extents.</summary>
        public HFSPlusForkData theFork;
    }

    /// <summary>
    ///     Attribute extents record. Used in the attributes file to store overflow extents for
    ///     attribute data when more than 8 extents are needed.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct HFSPlusAttrExtents
    {
        /// <summary>Type of attribute record (kHFSPlusAttrExtents).</summary>
        public BTAttributeRecordType recordType;
        /// <summary>Reserved; set to zero.</summary>
        public uint reserved;
        /// <summary>Additional extent descriptors for the attribute.</summary>
        public HFSPlusExtentRecord extents;
    }
}