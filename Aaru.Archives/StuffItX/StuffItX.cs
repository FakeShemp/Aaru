using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class StuffItX : IArchive
{
    const string MODULE_NAME = "StuffIt X Archive Plugin";

    Encoding                _encoding;
    List<Entry>             _entries;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "stuffitx";

    /// <inheritdoc />
    public Guid Id => new("3A7C1E5D-9B2F-4D8A-B6E3-1F5C7D9A2B4E");

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