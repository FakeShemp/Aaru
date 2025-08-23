// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Zoo plugin.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

// Copied from zoo.h
/*
The contents of this file are hereby released to the public domain.

                                    -- Rahul Dhesi 1986/11/14
*/

using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Zoo
{
#region Nested type: Direntry

    record Direntry
    {
        /// <summary>length of comment, 0 if none </summary>
        ushort cmt_size;
        /// <summary>points to comment;  zero if none </summary>
        int comment;
        /// <summary>DOS format date </summary>
        ushort date;
        /// <summary>will be 1 if deleted, 0 if not </summary>
        byte deleted;
        /// <summary>CRC of directory entry </summary>
        ushort dir_crc;
        /// <summary>length of directory name </summary>
        byte dirlen;
        /// <summary>directory name </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = PATHSIZE)]
        byte[] dirname;
        /// <summary>File attributes -- 24 bits </summary>
        uint fattr;
        /// <summary>CRC of this file </summary>
        ushort file_crc;
        /// <summary>filename </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FNAMESIZE)]
        byte[] fname;
        /// <summary>long filename </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LFNAMESIZE)]
        char lfname;
        byte major_ver;
        /// <summary>minimum version needed to extract </summary>
        byte minor_ver;

        /* fields for variable part of directory entry follow */

        /// <summary>length of long filename </summary>
        byte namlen;
        /// <summary>pos'n of next directory entry </summary>
        int next;
        /// <summary>position of this file </summary>
        int offset;
        int org_size;
        /// <summary>0 = no packing, 1 = normal LZW </summary>
        byte packing_method;
        int size_now;
        /// <summary>file structure if any </summary>
        byte struc;
        /// <summary>Filesystem ID </summary>
        ushort system_id;
        /// <summary>DOS format time </summary>
        ushort time;
        /// <summary>type of directory entry.	always 1 for now </summary>
        byte type;
        /// <summary>timezone where file was archived </summary>
        byte tz;

        /// <summary>length of variable part of dir entry </summary>
        int var_dir_len;
        /// <summary>file version number if any </summary>
        ushort version_no;
        /// <summary>version flag bits -- one byte in archive </summary>
        ushort vflag;
        /// <summary>tag -- redundancy check </summary>
        uint zoo_tag;
    }

#endregion

#region Nested type: ZooHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    record ZooHeader
    {
        /// <summary>length of archive comment </summary>
        ushort acmt_len;
        /// <summary>position of archive comment </summary>
        int acmt_pos;
        byte major_ver;
        /// <summary>minimum version to extract all files	</summary>
        byte minor_ver;
        /// <summary>archive header text </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIZ_TEXT)]
        byte[] text;
        /// <summary>type of archive header </summary>
        byte type;
        /// <summary>byte in archive;	data about versions </summary>
        ushort vdata;
        /// <summary>for consistency checking of zoo_start </summary>
        int zoo_minus;
        /// <summary>where the archive's data starts </summary>
        int zoo_start;
        /// <summary>identifies archives </summary>
        uint zoo_tag;
    }

#endregion
}