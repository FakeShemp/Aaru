// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Xattr.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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

/// <inheritdoc />
public sealed partial class PFS
{
    /// <summary>Extended attribute name for the Amiga file comment</summary>
    const string XATTR_AMIGA_COMMENT = "amiga.comment";

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = [];

        if(!_mounted) return ErrorNumber.AccessDenied;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory has no comment
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ErrorNumber.NoError;

        // Find the entry
        ErrorNumber errno = GetEntryForPath(normalizedPath, out DirEntryCacheItem entry);

        if(errno != ErrorNumber.NoError) return errno;

        // If there's a comment, add the xattr to the list
        if(!string.IsNullOrEmpty(entry.Comment)) xattrs.Add(XATTR_AMIGA_COMMENT);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        // We only support the amiga.comment xattr
        if(!string.Equals(xattr, XATTR_AMIGA_COMMENT, StringComparison.Ordinal)) return ErrorNumber.NotSupported;

        // Normalize the path
        string normalizedPath = path ?? "/";

        if(normalizedPath is "" or ".") normalizedPath = "/";

        // Root directory has no comment
        if(normalizedPath == "/" || string.Equals(normalizedPath, "/", StringComparison.OrdinalIgnoreCase))
            return ErrorNumber.NoSuchExtendedAttribute;

        // Find the entry
        ErrorNumber errno = GetEntryForPath(normalizedPath, out DirEntryCacheItem entry);

        if(errno != ErrorNumber.NoError) return errno;

        // Check if there's a comment
        if(string.IsNullOrEmpty(entry.Comment)) return ErrorNumber.NoSuchExtendedAttribute;

        // Return the comment as bytes
        buf = _encoding.GetBytes(entry.Comment);

        return ErrorNumber.NoError;
    }
}