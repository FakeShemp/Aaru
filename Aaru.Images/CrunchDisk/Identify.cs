// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Identify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies CrunchDisk disk images.
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

using System.IO;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class CrunchDisk
{
#region IMediaImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();

        if(stream.Length < HEADER_SIZE) return false;

        stream.Seek(0, SeekOrigin.Begin);

        var buffer = new byte[HEADER_SIZE];
        stream.EnsureRead(buffer, 0, HEADER_SIZE);

        Header header = Marshal.ByteArrayToStructureBigEndian<Header>(buffer);

        if(header.Id != HEADER_MAGIC) return false;

        if(header.BlockSize == 0) return false;

        if(header.Heads == 0) return false;

        if(header.BlocksPerTrack == 0) return false;

        if(header.HighCyl < header.LowCyl) return false;

        if(header.PackerType > 2) return false;

        return true;
    }

#endregion
}