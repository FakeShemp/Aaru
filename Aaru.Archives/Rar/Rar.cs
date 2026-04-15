using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Rar : IArchive
{
    const string            MODULE_NAME = "RAR Archive Plugin";
    string                  _archiveComment;
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    bool                    _isRar5;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => Localization.Rar_Name;
    /// <inheritdoc />
    public Guid Id => new("A3F29E1B-7C4D-4E8F-B6A2-9D1E5F3C7B8A");
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