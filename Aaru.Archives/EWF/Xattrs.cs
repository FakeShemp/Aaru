// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattrs.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains extended attribute methods for Expert Witness Format logical
//     evidence.
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
using System.Text;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class EwfArchive
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        EwfFileEntry entry = _entries[entryNumber];

        xattrs = [];

        if(!string.IsNullOrEmpty(entry.Md5Hash) && entry.Md5Hash != "0" && !entry.Md5Hash.StartsWith("00000000"))
            xattrs.Add("md5");

        if(!string.IsNullOrEmpty(entry.Sha1Hash) && entry.Sha1Hash != "0" && !entry.Sha1Hash.StartsWith("00000000"))
            xattrs.Add("sha1");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer)
    {
        buffer = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        EwfFileEntry entry = _entries[entryNumber];

        switch(xattr)
        {
            case "md5" when !string.IsNullOrEmpty(entry.Md5Hash):
                buffer = Encoding.UTF8.GetBytes(entry.Md5Hash);

                return ErrorNumber.NoError;
            case "sha1" when !string.IsNullOrEmpty(entry.Sha1Hash):
                buffer = Encoding.UTF8.GetBytes(entry.Sha1Hash);

                return ErrorNumber.NoError;
            default:
                return ErrorNumber.NoSuchExtendedAttribute;
        }
    }

#endregion
}