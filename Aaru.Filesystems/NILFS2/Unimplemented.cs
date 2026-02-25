// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Unimplemented.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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

// ReSharper disable UnusedMember.Local

using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Filesystems;

public sealed partial class NILFS2
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Unmount() => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ListXAttr(string path, out List<string> xattrs) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber GetXattr(string path, string xattr, ref byte[] buf) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadLink(string path, out string dest) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber OpenDir(string path, out IDirNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber CloseDir(IDirNode node) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadDir(IDirNode node, out string filename) => throw new NotImplementedException();
}