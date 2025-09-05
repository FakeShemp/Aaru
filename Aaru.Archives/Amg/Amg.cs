using System;
using System.Collections.Generic;
using System.IO;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Amg : IArchive
{
    List<FileEntry> _files;
    Stream          _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "AMG";
    /// <inheritdoc />
    public Guid Id => new("3BB1D752-1C45-42BB-B771-76CDF08F82F8");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => ArchiveSupportedFeature.HasEntryTimestamp   |
                                                      ArchiveSupportedFeature.SupportsCompression |
                                                      ArchiveSupportedFeature.SupportsFilenames   |
                                                      ArchiveSupportedFeature.SupportsSubdirectories;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _files.Count : -1;

#endregion
}