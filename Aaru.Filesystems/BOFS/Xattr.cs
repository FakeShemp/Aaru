// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS old filesystem plugin.
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

public sealed partial class BOFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = [];

        if(string.IsNullOrEmpty(path) || path == "/")
        {
            // Root directory - no xattrs
            return ErrorNumber.NoError;
        }

        lock(_rootDirectoryCache)
        {
            if(!_rootDirectoryCache.TryGetValue(path.TrimStart('/'), out FileEntry entry))
                return ErrorNumber.NoSuchFile;

            // Expose FileType as xattr only if it's not 0 or -1
            if(entry.FileType != 0 && entry.FileType != -1) xattrs.Add("com.be.filetype");

            return ErrorNumber.NoError;
        }
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(string.IsNullOrEmpty(path) || path == "/" || xattr != "com.be.filetype")
            return ErrorNumber.NoSuchExtendedAttribute;

        lock(_rootDirectoryCache)
        {
            if(!_rootDirectoryCache.TryGetValue(path.TrimStart('/'), out FileEntry entry))
                return ErrorNumber.NoSuchFile;

            // Don't expose FileType if it's 0 or -1
            if(entry.FileType == 0 || entry.FileType == -1) return ErrorNumber.NoSuchExtendedAttribute;

            // FileType is 4 bytes (int), export as-is without endian conversion
            byte[] fileTypeBytes = BitConverter.GetBytes(entry.FileType);

            if(buf == null)
            {
                buf = fileTypeBytes;

                return ErrorNumber.NoError;
            }

            if(buf.Length < fileTypeBytes.Length) return ErrorNumber.InvalidArgument;

            Array.Copy(fileTypeBytes, buf, fileTypeBytes.Length);

            return ErrorNumber.NoError;
        }
    }
}