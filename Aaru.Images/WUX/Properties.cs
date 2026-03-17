// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Properties.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disc image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains properties for Nintendo Wii U compressed disc images (WUX format).
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
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Structs;
using Partition = Aaru.CommonTypes.Partition;
using Track = Aaru.CommonTypes.Structs.Track;
using Session = Aaru.CommonTypes.Structs.Session;

namespace Aaru.Images;

public sealed partial class Wux
{
#region IOpticalMediaImage Members

    /// <inheritdoc />

    // ReSharper disable once ConvertToAutoProperty
    public ImageInfo Info => _imageInfo;

    /// <inheritdoc />
    public string Name => "Nintendo Wii U WUX";

    /// <inheritdoc />
    public Guid Id => new("A8B84E3D-6F8C-4F82-B5E0-1C5A7F4A9D2E");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public string Format => "Nintendo Wii U WUX";

    /// <inheritdoc />
    public List<Partition> Partitions { get; private set; }

    /// <inheritdoc />
    public List<Track> Tracks { get; private set; }

    /// <inheritdoc />
    public List<Session> Sessions { get; private set; }

    /// <inheritdoc />
    public List<DumpHardware> DumpHardware => null;

    /// <inheritdoc />
    public Metadata AaruMetadata => null;

#endregion
}