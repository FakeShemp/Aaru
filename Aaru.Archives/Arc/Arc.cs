using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Arc : IArchive
{
    const string            MODULE_NAME = "arc Archive Plugin";
    Encoding                _encoding;
    ArchiveSupportedFeature _features;
    Stream                  _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "arc";
    /// <inheritdoc />
    public Guid Id => new("D5C49A41-B10D-4DFE-B75E-3DAD11818818");
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