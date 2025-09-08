using System;
using System.Collections.Generic;
using System.IO;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Ha : IArchive
{
    List<Entry> _entries;
    Stream      _stream;

#region IArchive Members

    /// <inheritdoc />
    public string Name => "HA";
    /// <inheritdoc />
    public Guid Id => new("2FB42964-82A0-4819-9C2D-CC2F24E35526");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; private set; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => ArchiveSupportedFeature.HasEntryTimestamp      |
                                                      ArchiveSupportedFeature.SupportsCompression    |
                                                      ArchiveSupportedFeature.SupportsFilenames      |
                                                      ArchiveSupportedFeature.SupportsSubdirectories |
                                                      ArchiveSupportedFeature.HasExplicitDirectories;
    /// <inheritdoc />
    public int NumberOfEntries => Opened ? _entries.Count : -1;

#endregion
}