using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Structs;

namespace Aaru.Tests;

/// <summary>Class to define expected data when testing media info</summary>
public class MediaInfoTest
{
    /// <summary>Expected media type</summary>
    public MediaType MediaType;
    /// <summary>Expected number of sectors in media</summary>
    public ulong Sectors;
    /// <summary>Expected media sector size</summary>
    public uint SectorSize;
    /// <summary>File that contains the image to test</summary>
    public string TestFile;

    public override string ToString() => TestFile;
}

/// <inheritdoc />
/// <summary>Class to define expected data when testing filesystem info</summary>
public class FileSystemTest : MediaInfoTest
{
    /// <summary>Application ID</summary>
    public string ApplicationId;
    /// <summary>Can the volume boot?</summary>
    public bool Bootable;
    /// <summary>Clusters in volume</summary>
    public long Clusters;
    /// <summary>Bytes per cluster</summary>
    public uint ClusterSize;
    public Dictionary<string, FileData> Contents;
    public string                       ContentsJson;
    public Encoding                     Encoding;
    public string                       Namespace;
    /// <summary>System or OEM ID</summary>
    public string SystemId;
    /// <summary>Filesystem type. null if always the same, as defined in test class</summary>
    public string Type;
    /// <summary>Volume name</summary>
    public string VolumeName;
    /// <summary>Volume serial number or set identifier</summary>
    public string VolumeSerial;
}

public class BlockImageTestExpected : MediaInfoTest
{
    public string                  Md5;
    public BlockPartitionVolumes[] Partitions;
}

public class TrackInfoTestExpected
{
    public ulong            End;
    public FileSystemTest[] FileSystems;
    public byte?            Flags;
    public byte             Number;
    public ulong            Pregap;
    public int              Session;
    public ulong            Start;
}

public class OpticalImageTestExpected : BlockImageTestExpected
{
    public string                  LongMd5;
    public string                  SubchannelMd5;
    public TrackInfoTestExpected[] Tracks;
}

public class TapeImageTestExpected : BlockImageTestExpected
{
    public     TapeFile[]      Files;
    public new TapePartition[] Partitions;
}

public class FluxCaptureTestExpected
{
    /// <summary>Physical head (0-based)</summary>
    public uint Head;
    /// <summary>Physical track (0-based)</summary>
    public ushort Track;
    /// <summary>Physical sub-track (0-based, e.g. half-track)</summary>
    public byte SubTrack;
    /// <summary>Capture index for this head/track/subTrack combination</summary>
    public uint CaptureIndex;
    /// <summary>Expected index resolution in picoseconds</summary>
    public ulong IndexResolution;
    /// <summary>Expected data resolution in picoseconds</summary>
    public ulong DataResolution;
}

public class FluxImageTestExpected : BlockImageTestExpected
{
    /// <summary>Expected number of flux captures in the image</summary>
    public uint FluxCaptureCount;
    /// <summary>Expected flux captures to validate</summary>
    public FluxCaptureTestExpected[] FluxCaptures;
}

public class PartitionTest
{
    public Partition[] Partitions;
    /// <summary>File that contains the partition scheme to test</summary>
    public string TestFile;
}

public class FsExtractHashData
{
    public PartitionVolumes[] Partitions;
}

public class PartitionVolumes
{
    public VolumeData[] Volumes;
}

public class FileData
{
    public Dictionary<string, FileData> Children      { get; set; }
    public FileEntryInfo                Info          { get; set; }
    public string                       LinkTarget    { get; set; }
    public string                       Md5           { get; set; }
    public Dictionary<string, string>   XattrsWithMd5 { get; set; }
}

public class VolumeData
{
    public List<string>                 Directories;
    public Dictionary<string, FileData> Files;
    public string                       VolumeName;
}

public class BlockPartitionVolumes
{
    public ulong Length;
    public ulong Start;
}