// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : CPIO plugin.
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

using System;

namespace Aaru.Archives;

public sealed partial class Cpio
{
    struct Entry
    {
        public string       Filename;
        public long         Size;
        public long         DataOffset;
        public long         CompressedSize;
        public uint         Mode;
        public uint         Uid;
        public uint         Gid;
        public uint         Nlink;
        public DateTime     LastWriteTimeUtc;
        public uint         DevMajor;
        public uint         DevMinor;
        public uint         RdevMajor;
        public uint         RdevMinor;
        public uint         Inode;
        public uint         Checksum;
        public bool         HasChecksum;
        public CpioFileType FileType;
    }
}