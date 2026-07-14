// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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

public sealed partial class GDFX
{
    const string MAGIC = "MICROSOFT*XBOX*MEDIA";

    const uint SECTOR_SIZE = 2048;

    // Standard single-layer image: volume descriptor at sector 32 from the start of the image
    const ulong STANDARD_OFFSET = 0x00000000;

    // Dual-layer disc with video layer prepended (XGD2 and similar)
    // Source: extract-xiso.c GLOBAL_LSEEK_OFFSET
    const ulong GLOBAL_PARTITION_OFFSET = 0x0FD90000;

    // XGD3 format partition offset
    // Source: extract-xiso.c XGD3_LSEEK_OFFSET
    const ulong XGD3_PARTITION_OFFSET = 0x02080000;

    // XGD1 dual-layer game partition offset (= 2048 * 32 * 6192)
    // Source: xbox-iso-vfs
    const ulong XGD1_PARTITION_OFFSET = 0x18300000;

    // Standard volume descriptor is at sector 32 within the game partition
    const uint VD_SECTOR = 32;

    // Rebuilt/extracted XISO images place the volume descriptor at sector 0
    const uint REBUILT_VD_SECTOR = 0;

    // Offset of the duplicate magic within the volume descriptor sector
    const int MAGIC1_OFFSET = 0x7EC;

    // Directory entry attribute flags
    const byte ATTR_READONLY  = 0x01;
    const byte ATTR_HIDDEN    = 0x02;
    const byte ATTR_SYSTEM    = 0x04;
    const byte ATTR_DIRECTORY = 0x10;
    const byte ATTR_ARCHIVE   = 0x20;
    const byte ATTR_NORMAL    = 0x80;

    // Sentinel value for absent left/right child in directory BST
    const ushort NO_CHILD = 0xFFFF;

    const string FS_TYPE = "gdfx";
}