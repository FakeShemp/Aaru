using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class StuffIt5 : IArchive
{
    const string MODULE_NAME = "StuffIt 5 Archive Plugin";

    string                  _archiveComment;
    Encoding                _encoding;
    List<Entry>             _entries;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "stuffit5";
    /// <inheritdoc />
    public Guid Id => new("B4C2D1E3-6F7A-8B9C-0D1E-2F3A4B5C6D7E");
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