using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Zip : IArchive
{
    const string            MODULE_NAME = "ZIP Archive Plugin";
    string                  _archiveComment;
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => Localization.Zip_Name;
    /// <inheritdoc />
    public Guid Id => new("A3E2F8C1-6D4B-4E7A-B9C5-3F1D8A2E6B47");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => !Opened
                                                          ? ArchiveSupportedFeature.SupportsCompression    |
                                                            ArchiveSupportedFeature.SupportsFilenames      |
                                                            ArchiveSupportedFeature.SupportsSubdirectories |
                                                            ArchiveSupportedFeature.HasEntryTimestamp
                                                          : _features;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}