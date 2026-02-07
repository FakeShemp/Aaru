// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple DOS filesystem plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Methods to handle Apple DOS extended attributes (file type).
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
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

public sealed partial class AppleDOS
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        string filename = pathElements[0].ToUpperInvariant();

        if(filename.Length > 30) return ErrorNumber.NameTooLong;

        xattrs = [];

        if(_debug &&
           (string.Equals(path, "$",     StringComparison.InvariantCulture) ||
            string.Equals(path, "$Boot", StringComparison.InvariantCulture) ||
            string.Equals(path, "$Vtoc", StringComparison.InvariantCulture))) {}
        else
        {
            if(!_catalogCache.ContainsKey(filename)) return ErrorNumber.NoSuchFile;

            xattrs.Add(Xattrs.XATTR_APPLE_DOS_TYPE);

            if(_debug) xattrs.Add(Xattrs.XATTR_APPLE_DOS_TYPE);
        }

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        string filename = pathElements[0].ToUpperInvariant();

        if(filename.Length > 30) return ErrorNumber.NameTooLong;

        if(_debug &&
           (string.Equals(path, "$",     StringComparison.InvariantCulture) ||
            string.Equals(path, "$Boot", StringComparison.InvariantCulture) ||
            string.Equals(path, "$Vtoc", StringComparison.InvariantCulture)))
            return ErrorNumber.NoSuchExtendedAttribute;

        if(!_catalogCache.ContainsKey(filename)) return ErrorNumber.NoSuchFile;

        if(string.Equals(xattr, Xattrs.XATTR_APPLE_DOS_TYPE, StringComparison.InvariantCulture))
        {
            if(!_fileTypeCache.TryGetValue(filename, out byte type)) return ErrorNumber.InvalidArgument;

            buf    = new byte[1];
            buf[0] = type;

            return ErrorNumber.NoError;
        }

        if(!string.Equals(xattr, Xattrs.XATTR_APPLE_DOS_TYPE, StringComparison.InvariantCulture) || !_debug)
            return ErrorNumber.NoSuchExtendedAttribute;

        if(!_extentCache.TryGetValue(filename, out byte[] ts)) return ErrorNumber.InvalidArgument;

        buf = new byte[ts.Length];
        Array.Copy(ts, 0, buf, 0, buf.Length);

        return ErrorNumber.NoError;
    }

#endregion
}