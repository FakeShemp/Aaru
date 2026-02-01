// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Hierarchical File System plugin.
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

// ReSharper disable IdentifierTypo
// ReSharper disable MemberCanBePrivate.Local

using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Inside Macintosh
// https://developer.apple.com/legacy/library/documentation/mac/pdf/Files/File_Manager.pdf
//
// Catalog File Structure (per Inside Macintosh):
// ===============================================
// The catalog file is organized as a B-Tree and maintains the hierarchy of files and directories.
// Key CNID (Catalog Node ID) assignments used in the catalog:
// - CNID 1: Parent ID of the root directory (special marker for root's parent)
// - CNID 2: Directory ID of the root directory (the actual root directory)
// - CNID 3: File number of the extents overflow file
// - CNID 4: File number of the catalog file
// - CNID 5: File number of the bad allocation block file
// - CNID 6-15: Reserved for Apple
//
// Catalog File B-Tree Structure:
// - Header node (node 0): Contains B-Tree metadata in the header record (BTHdrRed)
// - Index nodes: Contain pointer records with search keys and child node pointers
// - Leaf nodes: Contain data records (directory, file, or thread records)
// - Map nodes: Used for tracking free/used nodes in large trees
//
// Catalog Key Format (used in both index and leaf nodes):
// - Key length (1 byte): Length of key data AFTER this field (does NOT include the key length byte itself)
//   Valid values: 0x7-0x25 in leaf nodes, 0x25 in index nodes. Value 0 indicates deleted record.
// - Reserved (1 byte): Must be 0
// - Parent directory ID (4 bytes): CNID of the parent directory
// - Name (variable, 0-31 bytes, padded to word): File or directory name
//
// IMPORTANT: Key length differs between node types:
// - INDEX NODES: keyLen is always 0x25 (37 decimal), name field always 32 bytes
//   Total key size = 0x25 + 1 (keyLen byte) = 0x26 = 38 bytes (word-aligned)
// - LEAF NODES: keyLen varies (0x7-0x25), name field varies in length
//   Total key size = keyLen + 1 (keyLen byte) + padding to word boundary
//   Minimum: 0x7 + 1 = 0x8 (empty name after ID)
//   Maximum: 0x25 + 1 = 0x26 (full 31-byte name + padding)
//
// Four types of catalog records (identified by first byte of record data):
// ========================================================================
// All record types start with a common 2-byte header:
// - cdrType (1 byte): Record type (1=cdrDirRec, 2=cdrFilRec, 3=cdrThdRec, 4=cdrFThdRec)
// - cdrResrv2 (1 byte): Reserved, must be 0
//
// 1. DIRECTORY RECORDS (cdrDirRec = 1):
//    Metadata about a single directory.
//    Fields:
//    - dirFlags: Directory flags (bitmap)
//    - dirVal: Directory valence (number of files/directories in this directory)
//    - dirDirID: Directory ID (CNID) - unique identifier for this directory
//    - dirCrDat: Creation date and time (Mac format)
//    - dirMdDat: Last modification date and time
//    - dirBkDat: Last backup date and time
//    - dirUsrInfo: Finder information (DInfo structure)
//    - dirFndrInfo: Additional Finder information (DXInfo structure)
//    - dirResrv: 4 reserved LongInts
//
// 2. FILE RECORDS (cdrFilRec = 2):
//    Metadata about a single file, including both data and resource forks.
//    Fields:
//    - filFlags: File flags (bitmap)
//    - filTyp: File type (should be 0)
//    - filUsrWds: Finder information (FInfo structure)
//    - filFlNum: File ID (CNID) - unique identifier for this file
//    - filStBlk: First allocation block of data fork
//    - filLgLen: Logical EOF of data fork
//    - filPyLen: Physical EOF of data fork
//    - filRStBlk: First allocation block of resource fork
//    - filRLgLen: Logical EOF of resource fork
//    - filRPyLen: Physical EOF of resource fork
//    - filCrDat: Creation date and time
//    - filMdDat: Last modification date and time
//    - filBkDat: Last backup date and time
//    - filFndrInfo: Additional Finder information (FXInfo structure)
//    - filClpSize: File clump size (allocation unit for file growth)
//    - filExtRec: First extent record of data fork (3 extents)
//    - filRExtRec: First extent record of resource fork (3 extents)
//    - filResrv: Reserved LongInt
//
// 3. DIRECTORY THREAD RECORDS (cdrThdRec = 3):
//    Provides a link between a directory and its parent directory.
//    Allows the File Manager to find the parent directory's name and ID.
//    Fields:
//    - thdResrv: 2 reserved LongInts
//    - thdParID: Directory ID (CNID) of the parent directory
//    - thdCName: Name of this directory (up to 31 bytes)
//
// 4. FILE THREAD RECORDS (cdrFThdRec = 4):
//    Provides a link between a file and its parent directory.
//    Allows the File Manager to find the parent directory's name and ID.
//    Fields:
//    - fthdResrv: 2 reserved LongInts
//    - fthdParID: Directory ID (CNID) of the parent directory
//    - fthdCName: Name of this file (up to 31 bytes)
//
// SPECIAL CASE - ROOT DIRECTORY:
// The root directory is special in that it has:
// - A thread record with parentID=1 (kRootParentCnid) and empty name
// - A directory record with dirDirID=2 (kRootCnid) and parentID=1
// This marks it as the root and allows the File Manager to distinguish it
// from all other directories and files in the catalog.

//
// Finding the Root Directory:
// ===========================
// 1. Read the MDB to get the catalog file location and validate it
// 2. Read the catalog header node (node 0) to get B-Tree metadata
// 3. Traverse the B-Tree starting from the root node to reach leaf nodes
// 4. In the leaf nodes, find the thread record with:
//    - Record type = cdrThdRec (3)
//    - Key parentID = 1 (kRootParentCnid - special marker for root's parent)
//    - Key name = empty (nameLen = 0)
//    This is the ROOT DIRECTORY THREAD RECORD
// 5. The next record in sort order (also with parentID=1) is the directory record:
//    - Record type = cdrDirRec (1)
//    - Key parentID = 1
//    - Key name = empty
//    - Data field: dirDirID = 2 (kRootCnid - the actual root directory ID)
//    This is the ROOT DIRECTORY RECORD
// 6. Extract and cache the root directory metadata from this directory record
//
// WHY TWO RECORDS FOR THE ROOT?
// Thread records serve as index entries that allow efficient lookup of parent directories.
// The thread record with parentID=1 and empty name is the "index entry" for the root.
// The following directory record contains the actual metadata (dates, flags, valence, etc.).
// This dual-record pattern is consistent with how all directories are handled in HFS,
// except the root directory is special in that its parent ID is hardcoded as 1.

public sealed partial class AppleHFS
{
    /// <summary>Mounts an HFS filesystem from a media image and partition</summary>
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        // Set default encoding to MacRoman as per specification
        encoding ??= Encoding.GetEncoding("macintosh");

        // Store parameters for later use
        _imagePlugin    = imagePlugin;
        _partitionStart = partition.Start;
        _encoding       = encoding;
        _sectorSize     = imagePlugin.Info.SectorSize;

        // Initialize metadata object
        Metadata = new FileSystem();

        // Try to read MDB sector(s) and parse the Master Directory Block
        ErrorNumber errno = ReadAndParseMdb();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to read MDB: {errno}");

            return errno;
        }

        // Verify the MDB has valid content
        errno = ValidateMdb();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"MDB validation failed: {errno}");

            return errno;
        }

        // Initialize directory cache dictionary
        _directoryCaches = new Dictionary<uint, Dictionary<string, CatalogEntry>>();

        // Initialize extents overflow B-Tree
        errno = InitializeExtentsOverflowBTree();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to initialize extents B-Tree: {errno}");

            return errno;
        }

        // Attempt to read and validate the Catalog File (B-Tree)
        errno = ReadAndValidateCatalog();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to read or validate catalog: {errno}");

            return errno;
        }

        // Cache the root directory for efficient future lookups
        errno = CacheRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to cache root directory: {errno}");

            return errno;
        }

        // Cache root directory entries
        errno = CacheRootDirectoryEntries();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, $"Failed to cache root directory entries: {errno}");

            return errno;
        }

        // Populate metadata from MDB
        PopulateMetadata();

        // Initialize cached filesystem information
        _fileSystemInfo = new FileSystemInfo
        {
            Blocks         = _mdb.drNmAlBlks,
            FilenameLength = 31,
            Files          = (ulong)_mdb.drFilCnt + _mdb.drDirCnt,
            FreeBlocks     = _mdb.drFreeBks,
            FreeFiles      = 0, // HFS doesn't track free files separately
            PluginId       = Id
        };

        // Mark filesystem as mounted
        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Filesystem mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and parses the Master Directory Block from the appropriate sector</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber ReadAndParseMdb()
    {
        byte[]      mdbSector = null;
        ErrorNumber errno;

        // According to Inside Macintosh, the MDB is located at sector 2 (512-byte sectors)
        // On CD media with 2048-byte sectors, we must search at 2KB-aligned boundaries
        // The MDB is always 512 bytes and can appear at offsets 0x400, 0x800, 0xC00, 0x1000, 0x1400, 0x1800 within read data

        if(_imagePlugin.Info.SectorSize is 2352 or 2448 or 2048)
        {
            // Read 2 sectors from the start of the partition to search for MDB
            errno = _imagePlugin.ReadSectors(_partitionStart, false, 2, out byte[] tmpSector, out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Search at standard 2KB-aligned offsets where the MDB might be located
            // This covers common CD layouts where HFS data starts at various alignments
            foreach(int offset in new[]
                    {
                        0x000, 0x200, 0x400, 0x600, 0x800, 0xA00
                    })
            {
                if(tmpSector.Length < offset + 512) continue;

                var drSigWord = BigEndianBitConverter.ToUInt16(tmpSector, offset);

                if(drSigWord != AppleCommon.HFS_MAGIC) continue;

                // Found the MDB, extract the 512-byte MDB sector
                mdbSector = new byte[512];
                Array.Copy(tmpSector, offset, mdbSector, 0, 512);

                break;
            }

            if(mdbSector == null) return ErrorNumber.InvalidArgument;
        }
        else
        {
            // For standard 512-byte sector devices, MDB is at sector 2
            errno = _imagePlugin.ReadSector(2 + _partitionStart, false, out mdbSector, out _);

            if(errno != ErrorNumber.NoError) return errno;

            if(mdbSector.Length < 512) return ErrorNumber.InvalidArgument;
        }

        // Verify the MDB signature
        var signature = BigEndianBitConverter.ToUInt16(mdbSector, 0);

        if(signature != AppleCommon.HFS_MAGIC) return ErrorNumber.InvalidArgument;

        // Check for embedded HFS+ signature - we only handle pure HFS, not HFS+
        var embedSig = BigEndianBitConverter.ToUInt16(mdbSector, 0x7C);

        if(embedSig == AppleCommon.HFSP_MAGIC) return ErrorNumber.InvalidArgument; // This is HFS+ or HFSX, not HFS

        // Parse the MDB structure from the byte array using big-endian marshaling
        _mdb = Marshal.ByteArrayToStructureBigEndian<MasterDirectoryBlock>(mdbSector);

        return ErrorNumber.NoError;
    }

    /// <summary>Validates the MDB structure for consistency and correctness</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber ValidateMdb()
    {
        // According to Inside Macintosh, critical MDB fields must be validated:

        // Allocation blocks define the total volume capacity
        if(_mdb.drNmAlBlks == 0) return ErrorNumber.InvalidArgument; // Must have at least one allocation block

        // Allocation block size must be a power of 2 (typically 512, 1024, 2048, 4096, etc.)
        if(_mdb.drAlBlkSiz == 0 || (_mdb.drAlBlkSiz & _mdb.drAlBlkSiz - 1) != 0)
            return ErrorNumber.InvalidArgument; // Block size must be a power of 2

        // Catalog B-Tree must exist and have a non-zero size
        if(_mdb.drCTFlSize == 0) return ErrorNumber.InvalidArgument;

        // Extents B-Tree must exist and have a non-zero size
        if(_mdb.drXTFlSize == 0) return ErrorNumber.InvalidArgument;

        // File and directory counts should be reasonable (not negative when cast to signed)
        // Use conservative upper limits to catch corrupted MDBs
        if(_mdb.drFilCnt > 0xFFFFFF || _mdb.drDirCnt > 0xFFFFFF) return ErrorNumber.InvalidArgument;

        // Volume bitmap start sector should be reasonable (typically sector 3 or later)
        // Cannot be before the MDB (sector 2 for 512-byte sectors)
        if(_mdb.drVBMSt < 3) return ErrorNumber.InvalidArgument;

        // First allocation block start sector must be after bitmap
        if(_mdb.drAlBlSt < _mdb.drVBMSt) return ErrorNumber.InvalidArgument;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the Catalog File B-Tree header node</summary>
    /// <returns>ErrorNumber indicating success or failure</returns>
    ErrorNumber ReadAndValidateCatalog()
    {
        // The catalog file extents are stored as 3 extent descriptors in the MDB
        // We'll read the first extent which should contain the header node

        ExtDescriptor firstExtent = _mdb.drCTExtRec.xdr[0];

        if(firstExtent.xdrNumABlks == 0) return ErrorNumber.InvalidArgument; // Catalog must have at least one block

        // Calculate the sector offset of the first allocation block of the catalog
        // Sectors are 512 bytes, but we need to convert allocation blocks to sectors
        ulong allocBlockSectorSize  = _mdb.drAlBlkSiz / 512;
        ulong firstAllocBlockSector = _mdb.drAlBlSt + firstExtent.xdrStABN * allocBlockSectorSize;

        // Read the first node of the B-Tree (header node)
        ErrorNumber errno =
            _imagePlugin.ReadSector(_partitionStart + firstAllocBlockSector, false, out byte[] headerSector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        int nodeDescSize = System.Runtime.InteropServices.Marshal.SizeOf(typeof(NodeDescriptor));
        int btHdrSize    = System.Runtime.InteropServices.Marshal.SizeOf(typeof(BTHdrRed));

        if(headerSector.Length < nodeDescSize + btHdrSize) return ErrorNumber.InvalidArgument;

        // Parse the node descriptor
        NodeDescriptor nodeDesc = Marshal.ByteArrayToStructureBigEndian<NodeDescriptor>(headerSector, 0, nodeDescSize);

        // Verify this is a header node
        if(nodeDesc.ndType != NodeType.ndHdrNode) return ErrorNumber.InvalidArgument;

        // Parse the B-Tree header record which follows the node descriptor
        BTHdrRed bthdr = Marshal.ByteArrayToStructureBigEndian<BTHdrRed>(headerSector, nodeDescSize, btHdrSize);

        // Verify B-Tree consistency
        // According to Inside Macintosh, depth must be at least 1 and tree must have records
        if(bthdr.bthDepth == 0 || bthdr.bthNRecs == 0) return ErrorNumber.InvalidArgument;

        // Node size must be a power of 2 and within valid range (512, 1024, 2048, 4096, 8192 bytes)
        // The node size is specified per B-Tree and does NOT need to match allocation block size
        // This allows flexibility: HFS can have 512-byte B-Tree nodes with 2048-byte allocation blocks
        if(bthdr.bthNodeSize == 0 || (bthdr.bthNodeSize & bthdr.bthNodeSize - 1) != 0)
            return ErrorNumber.InvalidArgument; // Not a power of 2

        // Standard B-Tree node sizes: 512, 1024, 2048, 4096, 8192 bytes
        if(bthdr.bthNodeSize < 512 || bthdr.bthNodeSize > 8192)
            return ErrorNumber.InvalidArgument; // Outside valid range

        return ErrorNumber.NoError;
    }


    /// <summary>Populates the Metadata object from the parsed MDB</summary>
    void PopulateMetadata()
    {
        Metadata.Type         = FS_TYPE;
        Metadata.Clusters     = _mdb.drNmAlBlks;
        Metadata.ClusterSize  = _mdb.drAlBlkSiz;
        Metadata.Files        = _mdb.drFilCnt;
        Metadata.FreeClusters = _mdb.drFreeBks;
        Metadata.Dirty        = !_mdb.drAtrb.HasFlag(AppleCommon.VolumeAttributes.Unmounted);
        Metadata.VolumeName   = StringHandlers.PascalToString(_mdb.drVN, _encoding);

        if(_mdb.drCrDate > 0) Metadata.CreationDate = DateHandlers.MacToDateTime(_mdb.drCrDate);

        if(_mdb.drLsMod > 0) Metadata.ModificationDate = DateHandlers.MacToDateTime(_mdb.drLsMod);

        if(_mdb.drVolBkUp > 0) Metadata.BackupDate = DateHandlers.MacToDateTime(_mdb.drVolBkUp);

        if(_mdb.drFndrInfo6 != 0 && _mdb.drFndrInfo7 != 0)
            Metadata.VolumeSerial = $"{_mdb.drFndrInfo6:X8}{_mdb.drFndrInfo7:X8}";

        Metadata.Bootable = _mdb.drFndrInfo0 != 0 || _mdb.drFndrInfo3 != 0 || _mdb.drFndrInfo5 != 0;
    }
}