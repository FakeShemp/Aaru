using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class StuffIt : IArchive
{
    const string MODULE_NAME = "StuffIt Archive Plugin";

    Encoding                _encoding;
    List<Entry>             _entries;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "stuffit";
    /// <inheritdoc />
    public Guid Id => new("A3F1B2C4-5D6E-7F80-9A1B-2C3D4E5F6A7B");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => !Opened
                                                          ? ArchiveSupportedFeature.SupportsCompression    |
                                                            ArchiveSupportedFeature.SupportsFilenames      |
                                                            ArchiveSupportedFeature.HasEntryTimestamp      |
                                                            ArchiveSupportedFeature.SupportsSubdirectories |
                                                            ArchiveSupportedFeature.HasExplicitDirectories |
                                                            ArchiveSupportedFeature.SupportsXAttrs         |
                                                            ArchiveSupportedFeature.SupportsProtection
                                                          : _features;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}