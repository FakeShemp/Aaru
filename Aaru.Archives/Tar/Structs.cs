// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Structs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Tar plugin.
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
using System.Collections.Generic;

namespace Aaru.Archives;

public sealed partial class Tar
{
    struct Entry
    {
        public string             Filename;
        public long               Size;
        public long               DataOffset;
        public long               CompressedSize;
        public uint               Mode;
        public uint               Uid;
        public uint               Gid;
        public string             UserName;
        public string             GroupName;
        public DateTime           LastWriteTimeUtc;
        public DateTime?          AccessTimeUtc;
        public DateTime?          CreationTimeUtc;
        public TypeFlag           Type;
        public string             LinkTarget;
        public uint               DevMajor;
        public uint               DevMinor;
        public string             Comment;
        public bool               IsSparse;
        public long               RealSize;
        public List<SparseRegion> SparseRegions;
    }

    internal struct SparseRegion
    {
        public long Offset;
        public long Length;
    }
}