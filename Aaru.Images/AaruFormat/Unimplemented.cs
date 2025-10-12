using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Structs;
using TapeFile = Aaru.CommonTypes.Structs.TapeFile;
using TapePartition = Aaru.CommonTypes.Structs.TapePartition;
using Track = Aaru.CommonTypes.Structs.Track;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   sectorSize) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetDumpHardware(List<DumpHardware> dumpHardware) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetImageInfo(ImageInfo imageInfo) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetTracks(List<Track> tracks) => throw new NotImplementedException();

#endregion

#region IWritableTapeImage Members

    /// <inheritdoc />
    public List<TapeFile> Files { get; }
    /// <inheritdoc />
    public List<TapePartition> TapePartitions { get; }

#endregion
}