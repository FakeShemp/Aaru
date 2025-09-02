using System;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Stfs : IArchive
{
#region IArchive Members

    /// <inheritdoc />
    public string Name => "Secure Transacted File System (STFS)";
    /// <inheritdoc />
    public Guid Id => new("6DDA6F47-B1B1-407E-892C-7DF0F46741A9");
    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;
    /// <inheritdoc />
    public bool Opened { get; }
    /// <inheritdoc />
    public ArchiveSupportedFeature ArchiveFeatures => ArchiveSupportedFeature.HasEntryTimestamp |
                                                      ArchiveSupportedFeature.SupportsFilenames;
    /// <inheritdoc />
    public int NumberOfEntries { get; }

#endregion
}