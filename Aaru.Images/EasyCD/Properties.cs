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
//     Manages Easy CD Creator disc images.
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

namespace Aaru.Images;

public sealed partial class EasyCD
{
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public Metadata AaruMetadata => null;
    /// <inheritdoc />
    public List<DumpHardware> DumpHardware => null;
    /// <inheritdoc />
    public string Format => "EasyCD";
    /// <inheritdoc />
    public Guid Id => new("64106380-D6EB-49A7-903B-1FAB4CC1B923");
    /// <inheritdoc />
    public ImageInfo Info { get; }

    /// <inheritdoc />
    public string Name => "Easy CD Creator disc image";
    /// <inheritdoc />
    public List<Partition> Partitions { get; private set; }
    /// <inheritdoc />
    public List<Track> Tracks { get; private set; }
    /// <inheritdoc />
    public List<Session> Sessions { get; private set; }
}