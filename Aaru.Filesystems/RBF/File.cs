// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Random Block File filesystem plugin
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

using System.Collections.Generic;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class RBF
{
    /// <summary>Reads an RBF filename from directory entry (MSB of last char set)</summary>
    static string ReadRbfFilename(byte[] nameBytes)
    {
        if(nameBytes == null || nameBytes.Length == 0) return null;

        var chars = new List<char>();

        foreach(byte b in nameBytes)
        {
            if(b == 0) break;

            // Check if MSB is set (indicates last character)
            if((b & 0x80) != 0)
            {
                chars.Add((char)(b & 0x7F));

                break;
            }

            chars.Add((char)b);
        }

        return chars.Count > 0 ? new string(chars.ToArray()) : null;
    }
}