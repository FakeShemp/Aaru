// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : EwfArchive.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : EWF logical evidence plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Manages Expert Witness Format logical evidence files.
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
using System.IO;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

/// <inheritdoc />
/// <summary>Implements reading Expert Witness Format logical evidence (L01/Lx01) files</summary>
public sealed partial class EwfArchive : IArchive
{
    const string MODULE_NAME = "EWF Logical Evidence plugin";
    uint         _bytesPerSector;

    /// <summary>Cache of decompressed chunks</summary>
    Dictionary<ulong, byte[]> _chunkCache;

    internal uint _chunkSize;

    /// <summary>Map from chunk index to location in segment file</summary>
    Dictionary<ulong, (int segmentIndex, long offset, uint size, bool compressed)> _chunkTable;

    /// <summary>Compression method for EWF v2</summary>
    EwfCompressionMethod _compressionMethod;

    /// <summary>Parsed file entries from ltree</summary>
    List<EwfFileEntry> _entries;

    /// <summary>Whether the image is EWF v2 format</summary>
    bool _isV2;

    int  _maxChunkCache;
    uint _sectorsPerChunk;

    /// <summary>Ordered list of open segment file streams</summary>
    List<Stream> _segmentStreams;

#region IArchive Members

    /// <inheritdoc />
    public string Name => Localization.EwfArchive_Name;

    /// <inheritdoc />
    public Guid Id => new("A7B9F4C1-8E63-4D2B-9A1C-5F7E3D6B8C02");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public bool Opened { get; private set; }

    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => ArchiveSupportedFeature.SupportsFilenames      |
                                                      ArchiveSupportedFeature.SupportsCompression    |
                                                      ArchiveSupportedFeature.SupportsSubdirectories |
                                                      ArchiveSupportedFeature.HasExplicitDirectories |
                                                      ArchiveSupportedFeature.HasEntryTimestamp      |
                                                      ArchiveSupportedFeature.SupportsXAttrs;

    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

    /// <inheritdoc />
    public void Close()
    {
        if(!Opened) return;

        _chunkCache?.Clear();
        _chunkTable?.Clear();
        _entries?.Clear();

        if(_segmentStreams != null)
        {
            foreach(Stream stream in _segmentStreams) stream.Close();

            _segmentStreams.Clear();
        }

        Opened = false;
    }

#endregion
}