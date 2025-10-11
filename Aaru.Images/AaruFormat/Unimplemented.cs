using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using TapeFile = Aaru.CommonTypes.Structs.TapeFile;
using TapePartition = Aaru.CommonTypes.Structs.TapePartition;
using Track = Aaru.CommonTypes.Structs.Track;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IVerifiableImage Members

    /// <inheritdoc />
    public bool? VerifyMediaImage() => throw new NotImplementedException();

#endregion

#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   sectorSize) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetMetadata(Metadata metadata) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetDumpHardware(List<DumpHardware> dumpHardware) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetImageInfo(ImageInfo imageInfo) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteMediaTag(byte[] data, MediaTagType tag) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSector(byte[] data, ulong sectorAddress) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorLong(byte[] data, ulong sectorAddress) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectors(byte[] data, ulong sectorAddress, uint length) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorsLong(byte[] data, ulong sectorAddress, uint length) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorsTag(byte[] data, ulong sectorAddress, uint length, SectorTagType tag) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool WriteSectorTag(byte[] data, ulong sectorAddress, SectorTagType tag) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySector(ulong sectorAddress) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas) => throw new NotImplementedException();


    /// <inheritdoc />
    public ErrorNumber ReadSectorTag(ulong sectorAddress, uint track, SectorTagType tag, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectors(ulong sectorAddress, uint length, uint track, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsTag(ulong      sectorAddress, uint length, uint track, SectorTagType tag,
                                      out byte[] buffer) => throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorLong(ulong sectorAddress, uint track, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public ErrorNumber ReadSectorsLong(ulong sectorAddress, uint length, uint track, out byte[] buffer) =>
        throw new NotImplementedException();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(Session session) => throw new NotImplementedException();

    /// <inheritdoc />
    public List<Track> GetSessionTracks(ushort session) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetTracks(List<Track> tracks) => throw new NotImplementedException();

#endregion

#region IWritableTapeImage Members

    /// <inheritdoc />
    public List<TapeFile> Files { get; }
    /// <inheritdoc />
    public List<TapePartition> TapePartitions { get; }
    /// <inheritdoc />
    public bool IsTape { get; }

    /// <inheritdoc />
    public bool AddFile(TapeFile file) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool AddPartition(TapePartition partition) => throw new NotImplementedException();

    /// <inheritdoc />
    public bool SetTape() => throw new NotImplementedException();

#endregion
}