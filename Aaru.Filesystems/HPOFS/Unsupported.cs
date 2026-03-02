// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Unsupported.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Unsupported operations for the High Performance Optical File System.
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
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs)
    {
        xattrs = [];

        if(!_mounted) return ErrorNumber.AccessDenied;

        xattrs = new List<string>();

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf) => ErrorNumber.NotSupported;

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest)
    {
        dest = null;

        return ErrorNumber.NotSupported;
    }
}