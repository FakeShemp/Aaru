using System;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Ha : IArchive
{
#region IArchive Members

    /// <inheritdoc />
    public string Name => "HA";
    /// <inheritdoc />
    public Guid Id { get; }
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => ArchiveSupportedFeature.HasEntryTimestamp      |
                                                      ArchiveSupportedFeature.SupportsCompression    |
                                                      ArchiveSupportedFeature.SupportsFilenames      |
                                                      ArchiveSupportedFeature.SupportsSubdirectories |
                                                      ArchiveSupportedFeature.HasExplicitDirectories;
    /// <inheritdoc />
    public int NumberOfEntries { get; }

#endregion
}