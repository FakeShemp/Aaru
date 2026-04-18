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
//     Identifies partclone disk images.
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
using System.Linq;
using System.Text;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;

namespace Aaru.Images;

public sealed partial class PartClone
{
#region IMediaImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        Stream stream = imageFilter.GetDataForkStream();
        stream.Seek(0, SeekOrigin.Begin);

        if(stream.Length < 512) return false;

        // Read enough bytes to cover the version field, common to both v0001 and v0002 layouts.
        var head = new byte[34];

        if(stream.EnsureRead(head, 0, head.Length) != head.Length) return false;

        // First 15 bytes of the magic are identical in both versions.
        for(var i = 0; i < _partCloneMagic.Length; i++)
        {
            if(head[i] != _partCloneMagic[i]) return false;
        }

        string version = Encoding.ASCII.GetString(head, 30, 4);

        switch(version)
        {
            case VERSION_0001:
            {
                // Re-parse the full v1 header so the BiTmAgIc check can run.
                stream.Seek(0, SeekOrigin.Begin);
                var pHdrB = new byte[Marshal.SizeOf<Header>()];
                stream.EnsureRead(pHdrB, 0, Marshal.SizeOf<Header>());
                Header hdr = Marshal.ByteArrayToStructureLittleEndian<Header>(pHdrB);

                if(stream.Position + (long)hdr.totalBlocks > stream.Length) return false;

                stream.Seek((long)hdr.totalBlocks, SeekOrigin.Current);

                var bitmagic = new byte[8];
                stream.EnsureRead(bitmagic, 0, 8);

                return _biTmAgIc.SequenceEqual(bitmagic);
            }

            case VERSION_0002:
                // Endianness marker validates the rest of the header.
                return head[32] == (ENDIAN_MAGIC & 0xFF) && head[33] == ENDIAN_MAGIC >> 8;

            default:
                return false;
        }
    }

#endregion
}