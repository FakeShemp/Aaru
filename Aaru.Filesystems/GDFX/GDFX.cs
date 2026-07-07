// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : GDFX.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// References:
//   https://github.com/drpetersonfernandes/SimpleXisoDrive
//   https://github.com/emoose/xbox-winfsp
//   https://github.com/multimediamike/xbfuse
//   https://github.com/thrimbor/xbiso
//   https://github.com/XboxDev/extract-xiso
//   https://github.com/antangelo/xdvdfs
/// <inheritdoc />
/// <summary>Implements the Xbox DVD File System (XDVDFS / GDFX)</summary>
public sealed partial class GDFX : IFilesystem
{
    const string MODULE_NAME = "GDFX plugin";

    bool        _debug;
    Encoding    _encoding;
    IMediaImage _imagePlugin;
    bool        _mounted;
    Partition   _partition;

    /// <inheritdoc />
    public FileSystem                                                Metadata         { get; private set; }
    /// <inheritdoc />
    public IEnumerable<(string name, Type type, string description)> SupportedOptions => [];
    /// <inheritdoc />
    public Dictionary<string, string>                                Namespaces       => [];

#region IFilesystem Members

    /// <inheritdoc />
    public string Name => Localization.GDFX_Name;

    /// <inheritdoc />
    public Guid Id => new("4B5C6D7E-8F90-1234-5678-ABCDEF012345");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

#endregion
}