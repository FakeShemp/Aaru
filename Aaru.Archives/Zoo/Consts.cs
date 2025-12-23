// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Consts.cs
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

// Copied from zoo.h
/*
The contents of this file are hereby released to the public domain.

                                    -- Rahul Dhesi 1986/11/14
*/

namespace Aaru.Archives;

public sealed partial class Zoo
{
    const uint ZOO_TAG = 0xFDC4A7DC;
    /// <summary>Size of header text</summary>
    const int SIZ_TEXT = 20;
    /// <summary>Max length of pathname</summary>
    const int PATHSIZE = 256;
    /// <summary>Size of DOS filename</summary>
    const int FNAMESIZE = 13;
    /// <summary>Size of long filename</summary>
    const int LFNAMESIZE = 256;
    /// <summary>Size of fname without extension</summary>
    const int ROOTSIZE = 8;
    /// <summary>Size of extension</summary>
    const int EXTLEN = 3;
    /// <summary>4 chars plus null</summary>
    const int SIZ_FLDR = 5;
    /// <summary>max packing method we can handle</summary>
    const int MAX_PACK = 2;
    /// <summary>Allowing location of file data</summary>
    readonly byte[] FILE_LEADER = "@)#("u8.ToArray();
    readonly byte[] HEADER_TEXT = "ZOO 2.10 Archive.\x1A"u8.ToArray();
}