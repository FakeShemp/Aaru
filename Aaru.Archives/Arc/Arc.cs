using System;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Arc : IArchive
{
    const string MODULE_NAME = "arc Archive Plugin";

#region IArchive Members

    /// <inheritdoc />
    public string Name => "arc";
    /// <inheritdoc />
    public Guid Id => new("D5C49A41-B10D-4DFE-B75E-3DAD11818818");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures =>
        ArchiveSupportedFeature.SupportsCompression | ArchiveSupportedFeature.SupportsFilenames;
    /// <inheritdoc />
    public int NumberOfEntries { get; }

#endregion
}