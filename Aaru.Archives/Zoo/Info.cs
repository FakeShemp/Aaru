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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Archives;

public sealed partial class Zoo
{
#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < Marshal.SizeOf<ZooHeader>()) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        byte[] hdr = new byte[Marshal.SizeOf<ZooHeader>()];

        stream.ReadExactly(hdr, 0, hdr.Length);

        ZooHeader header = Marshal.ByteArrayToStructureLittleEndian<ZooHeader>(hdr);

        return header.zoo_tag == ZOO_TAG && header.zoo_start + header.zoo_minus == 0;
    }

#endregion
}