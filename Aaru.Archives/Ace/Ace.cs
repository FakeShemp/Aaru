using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Ace : IArchive
{
    const string            MODULE_NAME = "ACE Archive Plugin";
    string                  _archiveComment;
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => Localization.Ace_Name;
    /// <inheritdoc />
    public Guid Id => new("7B1F4E8A-3C2D-4F6E-9A5B-1D8E3F7C2A4B");
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