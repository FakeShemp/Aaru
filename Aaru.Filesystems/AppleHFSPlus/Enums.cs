using System;
using System.Diagnostics.CodeAnalysis;

// ReSharper disable UnusedMember.Local

// ReSharper disable InconsistentNaming

namespace Aaru.Filesystems;

// Information from Apple TechNote 1150: https://developer.apple.com/legacy/library/technotes/tn/tn1150.html
/// <inheritdoc />
/// <summary>Implements detection of Apple Hierarchical File System Plus (HFS+)</summary>
public sealed partial class AppleHFSPlus
{
    /// <summary>
    ///     Volume attribute flags stored in the volume header. These flags indicate the state and characteristics of an
    ///     HFS+ volume.
    /// </summary>
    [Flags]
    [SuppressMessage("Roslynator", "RCS1135:Declare enum member with zero value (when enum has FlagsAttribute)")]
    public enum VolumeAttributes : uint
    {
        /* Bits 0-6 are reserved */
        /// <summary>Set if the volume is locked by hardware (e.g., write-protect tab on removable media).</summary>
        kHFSVolumeHardwareLockBit = 1 << 7,
        /// <summary>Set if the volume was successfully unmounted. Should be cleared when mounting read-write.</summary>
        kHFSVolumeUnmountedBit = 1 << 8,
        /// <summary>Set if there are any bad blocks for this volume in the extents overflow file.</summary>
        kHFSVolumeSparedBlocksBit = 1 << 9,
        /// <summary>Set if the volume should not be cached (used for RAM or ROM disks).</summary>
        kHFSVolumeNoCacheRequiredBit = 1 << 10,
        /// <summary>Set if the volume's boot state is inconsistent (cleared after successful boot).</summary>
        kHFSBootVolumeInconsistentBit = 1 << 11,
        /// <summary>Set if the catalog node IDs have wrapped around and been reused.</summary>
        kHFSCatalogNodeIDsReusedBit = 1 << 12,
        /// <summary>Set if the volume has a journal for maintaining file system consistency.</summary>
        kHFSVolumeJournaledBit = 1 << 13,
        /* Bit 14 is reserved */
        /// <summary>Set if the volume is locked by software.</summary>
        kHFSVolumeSoftwareLockBit = 1 << 15
        /* Bits 16-31 are reserved */
    }


    /// <summary>B-tree node kind. Identifies the type of a node within a B-tree.</summary>
    enum BTNodeKind : sbyte
    {
        /// <summary>Leaf node containing actual data records.</summary>
        kBTLeafNode = -1,
        /// <summary>Index node containing pointers to other nodes.</summary>
        kBTIndexNode = 0,
        /// <summary>Header node containing the B-tree header record, reserved record, and bitmap record.</summary>
        kBTHeaderNode = 1,
        /// <summary>Map node containing additional allocation bitmap for node allocation.</summary>
        kBTMapNode = 2
    }

    /// <summary>B-tree type. Identifies the type of B-tree file.</summary>
    enum BTreeTypes : byte
    {
        /// <summary>HFS B-tree type used by the catalog file and extents overflow file.</summary>
        kHFSBTreeType = 0,
        /// <summary>User B-tree type (for future use); starts from 128.</summary>
        kUserBTreeType = 128,
        /// <summary>Reserved B-tree type.</summary>
        kReservedBTreeType = 255
    }

    /// <summary>B-tree attributes stored in the header record. These flags describe characteristics of the B-tree.</summary>
    [Flags]
    enum BTreeAttributes : uint
    {
        /// <summary>Set if the B-tree was not closed properly and needs to be checked for consistency.</summary>
        kBTBadCloseMask = 0x00000001,
        /// <summary>Set if the B-tree uses 16-bit key length fields (required for HFS+ catalog and extents files).</summary>
        kBTBigKeysMask = 0x00000002,
        /// <summary>Set if the B-tree uses variable-length keys in index nodes (required for HFS+ catalog file).</summary>
        kBTVariableIndexKeysMask = 0x00000004
    }

    /// <summary>Catalog record type. Identifies the type of record in the catalog B-tree.</summary>
    enum BTreeRecordType : short
    {
        /// <summary>Catalog folder record containing information about a folder.</summary>
        kHFSPlusFolderRecord = 0x0001,
        /// <summary>Catalog file record containing information about a file.</summary>
        kHFSPlusFileRecord = 0x0002,
        /// <summary>Catalog folder thread record linking a folder's CNID to its parent and name.</summary>
        kHFSPlusFolderThreadRecord = 0x0003,
        /// <summary>Catalog file thread record linking a file's CNID to its parent and name.</summary>
        kHFSPlusFileThreadRecord = 0x0004
    }

    /// <summary>Catalog file/folder flags. Indicates characteristics of files and folders in the catalog.</summary>
    [Flags]
    enum BTCatalogFlags : ushort
    {
        /// <summary>Bit position for the file locked flag.</summary>
        kHFSFileLockedBit = 0x0000,
        /// <summary>Mask for the file locked flag; set if the file is locked.</summary>
        kHFSFileLockedMask = 0x0001,
        /// <summary>Bit position for the thread exists flag.</summary>
        kHFSThreadExistsBit = 0x0001,
        /// <summary>Mask for the thread exists flag; set if a thread record exists for this item.</summary>
        kHFSThreadExistsMask = 0x0002
    }

    /// <summary>Attribute record type. Identifies the type of record in the attributes B-tree.</summary>
    enum BTAttributeRecordType : uint
    {
        /// <summary>Inline data attribute record containing attribute data stored directly in the record.</summary>
        kHFSPlusAttrInlineData = 0x10,
        /// <summary>Fork data attribute record containing a fork data structure for larger attributes.</summary>
        kHFSPlusAttrForkData = 0x20,
        /// <summary>Extents attribute record containing overflow extents for large attributes.</summary>
        kHFSPlusAttrExtents = 0x30
    }
}