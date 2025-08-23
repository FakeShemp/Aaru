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

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    struct Direntry
    {
        /// <summary>tag -- redundancy check </summary>
        public readonly uint zoo_tag;
        /// <summary>type of directory entry.	always 1 for now </summary>
        public readonly byte type;
        /// <summary>0 = no packing, 1 = normal LZW </summary>
        public readonly byte packing_method;
        /// <summary>pos'n of next directory entry </summary>
        public readonly int next;
        /// <summary>position of this file </summary>
        public readonly int offset;
        /// <summary>DOS format date </summary>
        public readonly ushort date;
        /// <summary>DOS format time </summary>
        public readonly ushort time;
        /// <summary>CRC of this file </summary>
        public readonly ushort file_crc;
        public readonly int  org_size;
        public readonly int  size_now;
        public readonly byte major_ver;
        /// <summary>minimum version needed to extract </summary>
        public readonly byte minor_ver;
        /// <summary>will be 1 if deleted, 0 if not </summary>
        public readonly byte deleted;
        /// <summary>file structure if any </summary>
        public readonly byte struc;
        /// <summary>points to comment;  zero if none </summary>
        public readonly int comment;
        /// <summary>length of comment, 0 if none </summary>
        public readonly ushort cmt_size;
        /// <summary>filename </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FNAMESIZE)]
        public readonly byte[] fname;

        /// <summary>length of variable part of dir entry </summary>
        public readonly short var_dir_len;
        /// <summary>timezone where file was archived </summary>
        public readonly byte tz;
        /// <summary>CRC of directory entry </summary>
        public readonly ushort dir_crc;

        /* fields for variable part of directory entry follow */

        /// <summary>length of long filename </summary>
        public readonly byte namlen;
        /// <summary>length of directory name </summary>
        public readonly byte dirlen;
        /// <summary>long filename </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = LFNAMESIZE)]
        public byte[] lfname;
        /// <summary>directory name </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = PATHSIZE)]
        public byte[] dirname;
        /// <summary>Filesystem ID </summary>
        public ushort system_id;
        /// <summary>File attributes -- 24 bits </summary>
        public uint fattr;
        /// <summary>version flag bits -- one byte in archive </summary>
        public ushort vflag;
        /// <summary>file version number if any </summary>
        public ushort version_no;
    }

#endregion

#region Nested type: ZooHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct ZooHeader
    {
        /// <summary>archive header text </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SIZ_TEXT)]
        public readonly byte[] text;
        /// <summary>identifies archives </summary>
        public readonly uint zoo_tag;
        /// <summary>where the archive's data starts </summary>
        public readonly int zoo_start;
        /// <summary>for consistency checking of zoo_start </summary>
        public readonly int zoo_minus;
        public readonly byte major_ver;
        /// <summary>minimum version to extract all files	</summary>
        public readonly byte minor_ver;
        /// <summary>type of archive header </summary>
        public readonly byte type;
        /// <summary>position of archive comment </summary>
        public readonly int acmt_pos;
        /// <summary>length of archive comment </summary>
        public readonly ushort acmt_len;
        /// <summary>byte in archive;	data about versions </summary>
        public readonly ushort vdata;
    }

#endregion
}