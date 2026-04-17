using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class CompactPro : IArchive
{
    const string MODULE_NAME = "Compact Pro Archive Plugin";
    string       _comment;

    Encoding                _encoding;
    List<Entry>             _entries;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "compactpro";
    /// <inheritdoc />
    public Guid Id => new("B7E2C3A1-4F8D-4E6A-9C1B-3D5F7A2E8B4C");
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
                                                            ArchiveSupportedFeature.SupportsXAttrs
                                                          : _features;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}