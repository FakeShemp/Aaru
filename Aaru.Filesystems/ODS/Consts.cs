// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Files-11 On-Disk Structure plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Constants for the Files-11 On-Disk Structure.
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

namespace Aaru.Filesystems;

public sealed partial class ODS
{
    const string FS_TYPE = "files11";

    /// <summary>File ID for INDEXF.SYS (Index File)</summary>
    const ushort INDEXF_FID = 1;

    /// <summary>File ID for BITMAP.SYS (Storage Bitmap)</summary>
    const ushort BITMAP_FID = 2;

    /// <summary>File ID for BADBLK.SYS (Bad Block File)</summary>
    const ushort BADBLK_FID = 3;

    /// <summary>File ID for 000000.DIR (Master File Directory / root)</summary>
    const ushort MFD_FID = 4;

    /// <summary>File ID for CORIMG.SYS (Core Image)</summary>
    const ushort CORIMG_FID = 5;

    /// <summary>File ID for VOLSET.SYS (Volume Set List)</summary>
    const ushort VOLSET_FID = 6;

    /// <summary>File ID for CONTIN.SYS (Continuation File)</summary>
    const ushort CONTIN_FID = 7;

    /// <summary>File ID for BACKUP.SYS (Backup Log)</summary>
    const ushort BACKUP_FID = 8;

    /// <summary>File ID for BADLOG.SYS (Bad Block Log)</summary>
    const ushort BADLOG_FID = 9;

    /// <summary>Last reserved file ID</summary>
    const ushort LAST_RESERVED_FID = 16;

    /// <summary>ODS block size (always 512 bytes)</summary>
    const uint ODS_BLOCK_SIZE = 512;

    /// <summary>Maximum filename length for ODS-2</summary>
    const int ODS2_MAX_FILENAME = 39;

    /// <summary>Maximum filename length for ODS-5</summary>
    const int ODS5_MAX_FILENAME = 236;

    /// <summary>Marker for no more directory records in a block</summary>
    const ushort NO_MORE_RECORDS = 0xFFFF;
}