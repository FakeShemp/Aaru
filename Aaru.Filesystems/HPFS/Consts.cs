// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : OS/2 High Performance File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contants for the OS/2 High Performance File System.
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

// Information from an old unnamed document
/// <inheritdoc />
public sealed partial class HPFS
{
    const string FS_TYPE = "hpfs";

    /// <summary>Magic number for boot block signature</summary>
    const ushort BB_MAGIC = 0xAA55;

    /// <summary>Magic number for superblock</summary>
    const uint SB_MAGIC = 0xF995E849;

    /// <summary>Secondary magic number for superblock</summary>
    const uint SB_MAGIC2 = 0xFA53E9C5;

    /// <summary>Magic number for spare block</summary>
    const uint SP_MAGIC = 0xF9911849;

    /// <summary>Secondary magic number for spare block</summary>
    const uint SP_MAGIC2 = 0xFA5229C5;

    /// <summary>Magic number for dnode (directory node)</summary>
    const uint DNODE_MAGIC = 0x77E40AAE;

    /// <summary>Magic number for fnode (file node)</summary>
    const uint FNODE_MAGIC = 0xF7E40AAE;

    /// <summary>Magic number for anode (allocation node)</summary>
    const uint ANODE_MAGIC = 0x37E40AAE;

    /// <summary>Magic number for code page directory</summary>
    const uint CP_DIR_MAGIC = 0x494521F7;

    /// <summary>Magic number for code page data</summary>
    const uint CP_DATA_MAGIC = 0x894521F7;
}