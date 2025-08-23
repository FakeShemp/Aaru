// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Unimplemented.cs
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

using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using FileAttributes = System.IO.FileAttributes;

namespace Aaru.Archives;

public sealed partial class Zoo
{
#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter filter, Encoding encoding) => throw new NotImplementedException();

    /// <inheritdoc />
    public void Close()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public ErrorNumber GetFilename(int entryNumber, out string fileName) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntryNumber(string fileName, bool caseInsensitiveMatch, out int entryNumber) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetCompressedSize(int entryNumber, out long length) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetUncompressedSize(int entryNumber, out long length) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetAttributes(int entryNumber, out FileAttributes attributes) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Stat(int entryNumber, out FileEntryInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetEntry(int entryNumber, out IFilter filter) => throw new NotImplementedException();

#endregion
}