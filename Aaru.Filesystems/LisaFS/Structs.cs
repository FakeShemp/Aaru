// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple Lisa filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System;
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Filesystems;

public sealed partial class LisaFS
{
#region Nested type: CatalogEntry

    /// <summary>
    ///     An entry in the catalog from V3. The first entry is bigger than the rest, may be a header, I have not needed
    ///     any of its values so I just ignored it. Each catalog is divided in 4-sector blocks, and if it needs more than a
    ///     block there are previous and next block pointers, effectively making the V3 catalog a double-linked list. Garbage
    ///     is not zeroed.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct CatalogEntry
    {
        /// <summary>0x00, B-tree record key length, 0x24 for normal catalog entries</summary>
        public byte   key_length;
        /// <summary>0x01, parent directory ID for this file, 0 for root directory</summary>
        public ushort parentID;
        /// <summary>0x03, filename, 32-bytes, null-padded</summary>
        public byte[] filename;
        /// <summary>0x23, null-termination</summary>
        public byte   terminator;
        /// <summary>
        ///     At 0x24 0x01 here for subdirectories, entries 48 bytes long 0x03 here for entries 64 bytes long 0x08 here for
        ///     entries 78 bytes long This is incomplete, may fail, mostly works...
        /// </summary>
        public byte   entry_type;
        /// <summary>0x25, high byte / padding for entry type</summary>
        public byte   entry_type_pad;
        /// <summary>0x26, file ID, must be positive and bigger than 4</summary>
        public short  fileID;
        /// <summary>0x28, creation date</summary>
        public uint   dtc;
        /// <summary>0x2C, last modification date</summary>
        public uint   dtm;
        /// <summary>0x30, file length in bytes</summary>
        public int    length;
        /// <summary>0x34, physical file size in bytes</summary>
        public int    physSize;
        /// <summary>0x38, filesystem overhead in pages for object records</summary>
        public ushort fsOvrhd;
        /// <summary>0x3A, object status flags for object records</summary>
        public ushort flags;
        /// <summary>0x3C, unused value for object records</summary>
        public uint   unused;
    }

#endregion

#region Nested type: CatalogEntryV2

    /// <summary>
    ///     The catalog entry for the V1 and V2 volume formats. It stores an `e_name` followed by the tail of LisaOS's
    ///     `centry` record. Contrary to V3, it has no header and instead of being a double-linked list it is fragmented
    ///     using an Extents File. The Extents File position for the root catalog is then stored in the S-Records File.
    ///     Its entries are not filed sequentially denoting some kind of in-memory structure while at the same time forcing
    ///     LisaOS to read the whole catalog. Empty entries just contain a 0-len filename. Garbage is not zeroed.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct CatalogEntryV2
    {
        /// <summary>0x00, Pascal string length</summary>
        public byte   filenameLen;
        /// <summary>0x01, filename characters, 32 bytes</summary>
        public byte[] filename;
        /// <summary>0x21, alignment padding after the Pascal string</summary>
        public byte   filename_pad;
        /// <summary>0x22, catalog entry type</summary>
        public byte   entry_type;
        /// <summary>0x23, padding after entry type</summary>
        public byte   entry_type_pad;
        /// <summary>0x24, S-file number</summary>
        public short  sfile;
        /// <summary>0x26, reserved attributes field</summary>
        public uint   attributes;
        /// <summary>0x2A, logical beginning page of a pipe</summary>
        public uint   read_page;
        /// <summary>0x2E, logical beginning offset of a pipe</summary>
        public ushort read_offset;
        /// <summary>0x30, logical ending page of a pipe</summary>
        public uint   write_page;
        /// <summary>0x34, logical ending offset of a pipe</summary>
        public ushort write_offset;
    }

#endregion

#region Nested type: Extent

    /// <summary>An extent indicating a start and a run of sectors.</summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct Extent
    {
        public int   start;
        public short length;
    }

#endregion

#region Nested type: ExtentFile

    /// <summary>
    ///     The Extents File. There is one Extents File per each file stored on disk. The file ID present on the sectors
    ///     tags for the Extents File is the negated value of the file ID it represents. e.g. file = 5 (0x0005) extents = -5
    ///     (0xFFFB) It spans a single sector on V2 and V3 but 2 sectors on V1. It contains all information about a file, and
    ///     is indexed in the S-Records file. It also contains the label. Garbage is zeroed.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct ExtentFile
    {
        /// <summary>0x00, filename length</summary>
        public byte     filenameLen;
        /// <summary>0x01, filename</summary>
        public byte[]   filename;
        /// <summary>0x20, file format version</summary>
        public ushort   version;
        /// <summary>0x22, unique identifier</summary>
        public ulong    unique_id;
        /// <summary>0x2A, unknown</summary>
        public byte     unknown2;
        /// <summary>0x2B, entry type? gets modified</summary>
        public byte     etype;
        /// <summary>0x2C, file type</summary>
        public FileType ftype;
        /// <summary>0x2D, unknown</summary>
        public byte     unknown3;
        /// <summary>0x2E, creation time</summary>
        public uint     dtc;
        /// <summary>0x32, last access time</summary>
        public uint     dta;
        /// <summary>0x36, modification time</summary>
        public uint     dtm;
        /// <summary>0x3A, backup time</summary>
        public uint     dtb;
        /// <summary>0x3E, scavenge time</summary>
        public uint     dts;
        /// <summary>0x42, machine serial number</summary>
        public uint     serial;
        /// <summary>0x46, file marked as killed</summary>
        public byte     killed;
        /// <summary>0x47, safety switch state</summary>
        public byte     safety_on;
        /// <summary>0x48, protected file</summary>
        public byte     protected_file;
        /// <summary>0x49, master file</summary>
        public byte     master;
        /// <summary>0x4A, scavenged file</summary>
        public byte     scavenged;
        /// <summary>0x4B, file closed by os</summary>
        public byte     closed_by_os;
        /// <summary>0x4C, file left open</summary>
        public byte     file_open;
        /// <summary>0x4D, padding before result scavenge</summary>
        public byte     result_scavenge_pad;
        /// <summary>0x4E, effect on this file of last scavenge</summary>
        public ushort   result_scavenge;
        /// <summary>0x50, reserved for future use</summary>
        public ushort   unusedi1;
        /// <summary>0x52, file type field for system objects</summary>
        public ushort   system_type;
        /// <summary>0x54, user-defined file type field</summary>
        public ushort   user_type;
        /// <summary>0x56, extension to user-defined type field</summary>
        public ushort   user_subtype;
        /// <summary>0x58, Release number</summary>
        public ushort   release_number;
        /// <summary>0x5A, Build number</summary>
        public ushort   build_number;
        /// <summary>0x5C, Compatibility level</summary>
        public ushort   compatibility_level;
        /// <summary>0x5E, Revision level</summary>
        public ushort   revision_level;
        /// <summary>0x60, portion of large file split across media</summary>
        public ushort   file_portion;
        /// <summary>0x62, password length</summary>
        public byte     password_length;
        /// <summary>0x63, 8 bytes, scrambled password</summary>
        public byte[]   password;
        /// <summary>0x6B, identifier of parent directory object</summary>
        public ushort   parent_id;
        /// <summary>0x6D, padding before fs overhead</summary>
        public byte     parent_id_pad;
        /// <summary>0x6E, filesystem overhead</summary>
        public ushort   fs_overhead;
        /// <summary>0x70, padding before file length fields</summary>
        public byte[]   hint_padding;
        /// <summary>0x80, 0x200 in v1, file length in blocks</summary>
        public int      length;
        /// <summary>0x84, 0x204 in v1, physical file size in bytes</summary>
        public int      phys_length;
        /// <summary>
        ///     0x88, 0x208 in v1, extents, can contain up to 41 extents (85 in v1), dunno LisaOS maximum (never seen more
        ///     than 3)
        /// </summary>
        public Extent[] extents;
        /// <summary>0x17E, empty padding before label</summary>
        public short    label_padding;
        /// <summary>
        ///     At 0x180, this is the label. While 1982 pre-release documentation says the label can be up to 448 bytes, v1
        ///     onward only have space for a 128 bytes one. Any application can write whatever they want in the label, however,
        ///     Lisa Office uses it to store its own information, something that will effectively overwrite any information a user
        ///     application wrote there. The information written here by Lisa Office is like the information Finder writes in the
        ///     FinderInfo structures, plus the non-unique name that is shown on the GUI. For this reason I called it LisaInfo. I
        ///     have not tried to reverse engineer it.
        /// </summary>
        public byte[]   LisaInfo;
    }

#endregion

#region Nested type: LisaDirNode

    sealed class LisaDirNode : IDirNode
    {
        internal string[] Contents;
        internal int      Position;

#region IDirNode Members

        /// <inheritdoc />
        public string Path { get; init; }

#endregion
    }

#endregion

#region Nested type: LisaFileNode

    sealed class LisaFileNode : IFileNode
    {
        internal short FileId;

#region IFileNode Members

        /// <inheritdoc />
        public string Path { get; init; }

        /// <inheritdoc />
        public long Length { get; init; }

        /// <inheritdoc />
        public long Offset { get; set; }

#endregion
    }

#endregion

#region Nested type: MDDF

    /// <summary>
    ///     The MDDF is the most import block on a Lisa FS volume. It describes the volume and its contents. On
    ///     initialization the memory where it resides is not emptied so it tends to contain a lot of garbage. This has
    ///     difficulted its reverse engineering.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct MDDF
    {
        /// <summary>0x00, Filesystem version</summary>
        public ushort   fsversion;
        /// <summary>0x02, Volume ID</summary>
        public ulong    volid;
        /// <summary>0x0A, Volume sequence number</summary>
        public ushort   volnum;
        /// <summary>0x0C, Pascal string, 32+1 bytes, volume name</summary>
        public string   volname;
        /// <summary>0x2D, padding byte after volume name</summary>
        public byte     volname_pad;
        /// <summary>0x2E, Pascal string, 32+1 bytes, password</summary>
        public string   password;
        /// <summary>0x4F, padding byte after volume password</summary>
        public byte     password_pad;
        /// <summary>0x50, Lisa serial number that init'ed this disk</summary>
        public uint     machine_id;
        /// <summary>0x54, Lisa serial number of the master machine</summary>
        public uint     master_machine_id;
        /// <summary>0x58, Date of volume creation</summary>
        public DateTime dtvc;
        /// <summary>0x5C, Date...</summary>
        public DateTime dtcc;
        /// <summary>0x60, Date of volume backup</summary>
        public DateTime dtvb;
        /// <summary>0x64, Date of volume scavenging</summary>
        public DateTime dtvs;
        /// <summary>0x68, copy thread</summary>
        public uint     copy_thread;
        /// <summary>0x6C, block the MDDF is residing on</summary>
        public uint     mddf_block;
        /// <summary>0x70, volsize-1</summary>
        public uint     volsize_minus_one;
        /// <summary>0x74, volsize-1-mddf_block</summary>
        public uint     volsize_minus_mddf_minus_one;
        /// <summary>0x78, Volume size in blocks</summary>
        public uint     vol_size;
        /// <summary>0x7C, Blocks size of underlying drive (data+tags)</summary>
        public ushort   blocksize;
        /// <summary>0x7E, Data only block size</summary>
        public ushort   datasize;
        /// <summary>0x80, unknown</summary>
        public ushort   unknown4;
        /// <summary>0x82, unknown</summary>
        public uint     unknown5;
        /// <summary>0x86, unknown</summary>
        public uint     unknown6;
        /// <summary>0x8A, Size in sectors of filesystem clusters</summary>
        public ushort   clustersize;
        /// <summary>0x8C, Filesystem size in blocks</summary>
        public uint     fs_size;
        /// <summary>0x90, unknown</summary>
        public uint     unknown7;
        /// <summary>0x94, Pointer to S-Records</summary>
        public uint     srec_ptr;
        /// <summary>0x98, S-list entries per data block</summary>
        public ushort   slist_packing;
        /// <summary>0x9A, S-Records length</summary>
        public ushort   srec_len;
        /// <summary>0x9C, first allocatable S-file number</summary>
        public ushort   first_file;
        /// <summary>0x9E, first empty S-list slot</summary>
        public ushort   empty_file;
        /// <summary>0xA0, last usable file slot</summary>
        public ushort   maxfiles;
        /// <summary>0xA2, pages allocated for each file leader</summary>
        public ushort   hintsize;
        /// <summary>0xA4, page offset of file leader in hints</summary>
        public ushort   leader_offset;
        /// <summary>0xA6, number of pages in the leader</summary>
        public ushort   leader_pages;
        /// <summary>0xA8, byte offset of the file label in the first hint page</summary>
        public ushort   flabel_offset;
        /// <summary>0xAA, reserved for future use</summary>
        public ushort   unusedi1;
        /// <summary>0xAC, page offset of the file map in hints</summary>
        public ushort   map_offset;
        /// <summary>0xAE, file map entries per page</summary>
        public ushort   map_size;
        /// <summary>0xB0, Files in volume</summary>
        public ushort   filecount;
        /// <summary>0xB2, spare field reserved for future use</summary>
        public uint     unusedl1;
        /// <summary>0xB6, first free page in the allocation list</summary>
        public uint     freestart;
        /// <summary>0xBA, Free blocks</summary>
        public uint     freecount;
        /// <summary>0xBE, maximum catalog entries in the root catalog</summary>
        public ushort   rootmaxentries;
        /// <summary>0xC0, mount state information</summary>
        public uint     mountinfo;
        /// <summary>0xC4, no idea</summary>
        public ulong    overmount_stamp;
        /// <summary>0xCC, machine ID for this copy of parameter memory</summary>
        public uint     pmem_id;
        /// <summary>0xD0, parameter memory alarm reference</summary>
        public ushort   pmem_alarm_ref;
        /// <summary>0xD2, parameter memory payload</summary>
        public ushort[] pmem_parm_mem;
        /// <summary>0x112, volume modified by scavenger</summary>
        public byte     vol_scavenged;
        /// <summary>0x113, volume has been copied</summary>
        public byte     tbt_copied;
        /// <summary>0x114, ID of volume where this volume was backed up</summary>
        public ulong    backup_volid;
        /// <summary>0x11C, Return code of Scavenger</summary>
        public ushort   result_scavenge;
        /// <summary>0x11E, byte offset of the small map in the hint page</summary>
        public ushort   smallmap_offset;
        /// <summary>0x120, byte offset of hentry in first page of hints</summary>
        public ushort   hentry_offset;
        /// <summary>0x122, reserved for future use</summary>
        public ushort   boot_code;
        /// <summary>0x124, reserved for future use</summary>
        public ushort   boot_environ;
        /// <summary>0x126, size of user-defined file label</summary>
        public ushort   flabel_size;
        /// <summary>0x128, filesystem overhead in pages</summary>
        public ushort   fs_overhead;
        /// <summary>0x12A, OEM identifier</summary>
        public uint     oem_id;
        /// <summary>0x12E, root page for the catalog B-tree</summary>
        public uint     root_page;
        /// <summary>0x132, catalog B-tree depth</summary>
        public ushort   tree_depth;
        /// <summary>0x134, last allocated catalog node ID</summary>
        public ushort   node_id;
        /// <summary>0x136, total volumes in sequence</summary>
        public ushort   vol_seq_no;
        /// <summary>0x138, volume mounted flag persisted on disk</summary>
        public byte     vol_mounted;
#pragma warning disable CS0649
        /// <summary>Is password present? (On-disk position unknown)</summary>
        public byte passwd_present;
        /// <summary>Opened files (memory-only?) (On-disk position unknown)</summary>
        public uint opencount;
        /// <summary>Copy thread cached in memory (On-disk position unknown)</summary>
        public uint copy_thread_runtime;

        // Flags are boolean, but Pascal seems to use them as full unsigned 8 bit values
        /// <summary>No idea (On-disk position unknown)</summary>
        public byte privileged;
        /// <summary>Read-only volume (On-disk position unknown)</summary>
        public byte write_protected;
        /// <summary>Master disk (On-disk position unknown)</summary>
        public byte master;
        /// <summary>Copy disk (On-disk position unknown)</summary>
        public byte copy;
        /// <summary>No idea (On-disk position unknown)</summary>
        public byte copy_flag;
        /// <summary>No idea (On-disk position unknown)</summary>
        public byte scavenge_flag;
#pragma warning restore CS0649
    }

#endregion

#region Nested type: SRecord

    /// <summary>
    ///     The S-Records File is a hashtable of S-Records, where the hash is the file ID they belong to. The S-Records
    ///     File cannot be fragmented or grown, and it can easily become full before the 32766 file IDs are exhausted. Each
    ///     S-Record entry contains a block pointer to the Extents File that correspond to that file ID as well as the real
    ///     file size, the only important information about a file that's not inside the Extents File. It also contains a low
    ///     value (less than 0x200) variable field of unknown meaning and another one that seems to be flags, with values like
    ///     0, 1, 3 and 5.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    struct SRecord
    {
        /// <summary>0x00, block where ExtentsFile for this entry resides</summary>
        public uint   extent_ptr;
        /// <summary>0x04, unknown</summary>
        public uint   unknown;
        /// <summary>0x08, filesize in bytes</summary>
        public uint   filesize;
        /// <summary>0x0C, some kind of flags, meaning unknown</summary>
        public ushort flags;
    }

#endregion
}