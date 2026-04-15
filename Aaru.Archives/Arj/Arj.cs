using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Arj : IArchive
{
    const string            MODULE_NAME = "ARJ Archive Plugin";
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => Localization.ARJ_Name;
    /// <inheritdoc />
    public Guid Id => new("7B2F4E8C-3A1D-4F6E-9C5B-8D0E2A7F1B3C");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => !Opened
                                                          ? ArchiveSupportedFeature.SupportsCompression |
                                                            ArchiveSupportedFeature.SupportsFilenames   |
                                                            ArchiveSupportedFeature.HasEntryTimestamp
                                                          : _features;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}