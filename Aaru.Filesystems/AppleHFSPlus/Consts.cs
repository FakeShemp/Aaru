// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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

// ReSharper disable InconsistentNaming
// ReSharper disable UnusedMember.Local

namespace Aaru.Filesystems;

using HFSCatalogNodeID = uint;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <inheritdoc />
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>Filesystem type identifier for HFS+.</summary>
    const string FS_TYPE_HFSP = "hfsplus";
    /// <summary>Filesystem type identifier for HFSX (case-sensitive HFS+).</summary>
    const string FS_TYPE_HFSX = "hfsx";

    /// <summary>Parent ID of the root folder (reserved).</summary>
    const HFSCatalogNodeID kHFSRootParentID = 1;
    /// <summary>Folder ID of the root folder.</summary>
    const HFSCatalogNodeID kHFSRootFolderID = 2;
    /// <summary>File ID of the extents overflow file.</summary>
    const HFSCatalogNodeID kHFSExtentsFileID = 3;
    /// <summary>File ID of the catalog file.</summary>
    const HFSCatalogNodeID kHFSCatalogFileID = 4;
    /// <summary>File ID of the bad blocks file.</summary>
    const HFSCatalogNodeID kHFSBadBlockFileID = 5;
    /// <summary>File ID of the allocation file (HFS+ only).</summary>
    const HFSCatalogNodeID kHFSAllocationFileID = 6;
    /// <summary>File ID of the startup file (HFS+ only).</summary>
    const HFSCatalogNodeID kHFSStartupFileID = 7;
    /// <summary>File ID of the attributes file (HFS+ only).</summary>
    const HFSCatalogNodeID kHFSAttributesFileID = 8;
    /// <summary>File ID used temporarily during catalog file repair.</summary>
    const HFSCatalogNodeID kHFSRepairCatalogFileID = 14;
    /// <summary>File ID used during extents file truncation.</summary>
    const HFSCatalogNodeID kHFSBogusExtentFileID = 15;
    /// <summary>First file ID available for user-visible files and folders.</summary>
    const HFSCatalogNodeID kHFSFirstUserCatalogNodeID = 16;

    /// <summary>B-tree comparison type: Binary comparison (case-sensitive, HFSX only).</summary>
    const byte kHFSBinaryCompare = 0;
    /// <summary>B-tree comparison type: Case-folding comparison (case-insensitive, HFS+ and HFSX).</summary>
    const byte kHFSCaseFolding = 0xCF;

    /// <summary>Compression magic for decmpfs header</summary>
    const uint DECMPFS_MAGIC = 0x636D7066; // "cmpf"

    /// <summary>Maximum inline compression size (compressed data stored in xattr)</summary>
    const int DECMPFS_INLINE_MAX = 3802; // Max size for inline compressed data

    /// <summary>Resource fork compression header size</summary>
    const int DECMPFS_RESOURCE_HEADER_SIZE = 0x100; // 256 bytes

    /// <summary>Extended attribute name for compression metadata</summary>
    const string DECMPFS_XATTR_NAME = "com.apple.decmpfs";
}