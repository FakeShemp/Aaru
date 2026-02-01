// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
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

using System.Diagnostics.CodeAnalysis;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Universal Disk Format filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class UDF
{
    const           string FS_TYPE           = "udf";
    static readonly byte[] _magic            = "*OSTA UDF Compliant\0\0\0\0"u8.ToArray();
    static readonly byte[] _udf_Lv           = "*UDF LV Info"u8.ToArray();
    static readonly byte[] _udf_Free_Ea      = "*UDF FreeEASpace"u8.ToArray();
    static readonly byte[] _udf_Free_App_Ea  = "*UDF FreeAppEASpace"u8.ToArray();
    static readonly byte[] _dvd_Cgms         = "*UDF DVD CGMS Info"u8.ToArray();
    static readonly byte[] _os2_Ea           = "*UDF OS/2 EA"u8.ToArray();
    static readonly byte[] _os2_Ea_Len       = "*UDF OS/2 EALength"u8.ToArray();
    static readonly byte[] _mac_VolumeInfo   = "*UDF Mac VolumeInfo"u8.ToArray();
    static readonly byte[] _mac_FinderInfo   = "*UDF Mac FinderInfo"u8.ToArray();
    static readonly byte[] _mac_UniqueId     = "*UDF Mac UniqueID Table"u8.ToArray();
    static readonly byte[] _mac_ResourceFork = "*UDF Mac ResourceFork"u8.ToArray();
    static readonly byte[] _os400_DirInfo    = "*UDF OS/400 DirInfo"u8.ToArray();
    static readonly byte[] _bea              = "BEA01"u8.ToArray();
    static readonly byte[] _nsr              = "NSR02"u8.ToArray();
    static readonly byte[] _nsr3             = "NSR03"u8.ToArray();
    static readonly byte[] _nsr_Partition    = "+NSR02"u8.ToArray();
    static readonly byte[] _tea              = "TEA01"u8.ToArray();
    static readonly byte[] _boot2            = "BOOT2"u8.ToArray();

    // UDF 1.50 partition type identifiers
    static readonly byte[] _udf_VirtualPartition  = "*UDF Virtual Partition"u8.ToArray();
    static readonly byte[] _udf_SparablePartition = "*UDF Sparable Partition"u8.ToArray();
    static readonly byte[] _udf_SparingTable      = "*UDF Sparing Table"u8.ToArray();

    // UDF 2.50+ partition type identifiers
    static readonly byte[] _udf_MetadataPartition = "*UDF Metadata Partition"u8.ToArray();

    // UDF 2.00+ named stream identifiers
    const string STREAM_MAC_RESOURCE_FORK = "*UDF Macintosh Resource Fork";
    const string STREAM_OS2_EA            = "*UDF OS/2 EA";
    const string STREAM_NT_ACL            = "*UDF NT ACL";
    const string STREAM_UNIX_ACL          = "*UDF UNIX ACL";
    const string STREAM_BACKUP            = "*UDF Backup";
    const string STREAM_POWER_CAL         = "*UDF Power Cal Table";

    // UDF version constants
    const ushort UDF_VERSION_102 = 0x0102;
    const ushort UDF_VERSION_150 = 0x0150;
    const ushort UDF_VERSION_200 = 0x0200;
    const ushort UDF_VERSION_201 = 0x0201;
    const ushort UDF_VERSION_250 = 0x0250;
    const ushort UDF_VERSION_260 = 0x0260;
}