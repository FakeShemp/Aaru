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
//     Identifies Software Pirates SNATCH-IT disk images.
//
//     Based on the work of Michal Necasek (fdimg).
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
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class SnatchIt
{
#region IMediaImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();
        stream.Seek(0, SeekOrigin.Begin);

        // Header is 30 bytes.
        if(stream.Length < 30) return false;

        var hdr = new byte[30];

        if(stream.EnsureRead(hdr, 0, 30) != 30) return false;

        string sig = Encoding.ASCII.GetString(hdr, 0, 16);

        if(sig != SOFTWARE_PIRATES) return false;

        string ver = Encoding.ASCII.GetString(hdr, 16, 7);

        if(ver != RELEASE_PREFIX) return false;

        // Reject multi-volume split images; only volume 0 is a self-contained image.
        if(hdr[28] != (byte)'$') return false;
        if(hdr[29] != (byte)'0') return false;

        return true;
    }

#endregion
}