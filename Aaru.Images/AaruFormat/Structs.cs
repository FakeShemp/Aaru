using System.Runtime.InteropServices;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region Nested type: AaruFormatImageInfo

    /// <summary>
    ///     This structure aggregates essential information extracted from an Aaru format image file, providing callers
    ///     with a comprehensive view of the imaged media without requiring access to internal image structures. All fields
    ///     are read-only from the caller's perspective and reflect the state at the time the image was created or last
    ///     modified.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct AaruFormatImageInfo
    {
        /// <summary>
        ///     Image contains partitions (or tracks for optical media); 0=no, non-zero=yes
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool HasPartitions;
        /// <summary>
        ///     Image contains multiple sessions (optical media); 0=single/none, non-zero=multi
        /// </summary>
        [MarshalAs(UnmanagedType.U1)]
        public bool HasSessions;
        /// <summary>
        ///     Size of the image payload in bytes (excludes headers/metadata)
        /// </summary>
        public ulong ImageSize;
        /// <summary>
        ///     Total count of addressable logical sectors/blocks
        /// </summary>
        public ulong Sectors;
        /// <summary>
        ///     Size of each logical sector in bytes (512, 2048, 2352, 4096, etc.)
        /// </summary>
        public uint SectorSize;
        /// <summary>
        ///     Image format version string (NUL-terminated, e.g., "6.0")
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] Version;
        /// <summary>
        ///     Name of application that created the image (NUL-terminated)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] Application;
        /// <summary>
        ///     Version of the creating application (NUL-terminated)
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] ApplicationVersion;
        /// <summary>
        ///     Image creation timestamp (Windows FILETIME: 100ns since 1601-01-01 UTC)
        /// </summary>
        public long CreationTime;
        /// <summary>
        ///     Last modification timestamp (Windows FILETIME format)
        /// </summary>
        public long LastModificationTime;
        /// <summary>
        ///     Media type identifier (see \ref MediaType enum; 0=Unknown)
        /// </summary>
        public MediaType MediaType;
        /// <summary>
        ///     Media type for sidecar generation (internal archival use)
        /// </summary>
        public MetadataMediaType MetadataMediaType;
    }

#endregion

#region Nested type: DumpHardwareEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DumpHardwareEntry
    {
        /// <summary>
        ///     Length in bytes of manufacturer UTF-8 string.
        /// </summary>
        public uint ManufacturerLength;
        /// <summary>
        ///     Length in bytes of model UTF-8 string.
        /// </summary>
        public uint ModelLength;
        /// <summary>
        ///     Length in bytes of revision / hardware revision string.
        /// </summary>
        public uint RevisionLength;
        /// <summary>
        ///     Length in bytes of firmware version string.
        /// </summary>
        public uint FirmwareLength;
        /// <summary>
        ///     Length in bytes of device serial number string.
        /// </summary>
        public uint SerialLength;
        /// <summary>
        ///     Length in bytes of dumping software name string.
        /// </summary>
        public uint SoftwareNameLength;
        /// <summary>
        ///     Length in bytes of dumping software version string.
        /// </summary>
        public uint SoftwareVersionLength;
        /// <summary>
        ///     Length in bytes of host operating system string.
        /// </summary>
        public uint SoftwareOperatingSystemLength;
        /// <summary>
        ///     Number of DumpExtent records following the strings (0 = none).
        /// </summary>
        public uint Extents;
    }

#endregion

#region Nested type: DumpHardwareHeader

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct DumpHardwareHeader
    {
        /// <summary>
        ///     Block identifier, must be BlockType::DumpHardwareBlock.
        /// </summary>
        public BlockType Identifier;
        /// <summary>
        ///     Number of DumpHardwareEntry records that follow.
        /// </summary>
        public ushort Entries;
        /// <summary>
        ///     Total payload bytes after this header (sum of entries, strings, and extents arrays).
        /// </summary>
        public uint Length;
        /// <summary>
        ///     CRC64-ECMA of the payload (byte-swapped for legacy v1 images, handled automatically).
        /// </summary>
        public ulong Crc64;
    }

#endregion

#region Nested type: TapeFileEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TapeFileEntry
    {
        /// <summary>
        ///     File number (unique within the partition). Identifies this file among all files in the same
        ///     partition. Numbering scheme is tape-format-dependent.
        /// </summary>
        public uint File;
        /// <summary>
        ///     Partition number containing this file. References a partition defined in the
        ///     TapePartitionHeader block. Valid range: 0-255.
        /// </summary>
        public byte Partition;
        /// <summary>
        ///     First block of the file (inclusive). This is the starting block address of the file data.
        ///     Block addresses are 0-based within the partition.
        /// </summary>
        public ulong FirstBlock;
        /// <summary>
        ///     Last block of the file (inclusive). This is the ending block address of the file data. Must be
        ///     ≥ FirstBlock. The file contains all blocks from FirstBlock through LastBlock inclusive.
        /// </summary>
        public ulong LastBlock;
    }

#endregion

#region Nested type: TrackEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct TrackEntry
    {
        /// <summary>
        ///     Track number (1..99 typical for CD audio/data). 0 may indicate placeholder/non-standard.
        /// </summary>
        public byte Sequence;
        /// <summary>
        ///     Track type (value from \ref TrackType).
        /// </summary>
        public byte Type;
        /// <summary>
        ///     Inclusive starting LBA of the track.
        /// </summary>
        public long Start;
        /// <summary>
        ///     Inclusive ending LBA of the track.
        /// </summary>
        public long End;
        /// <summary>
        ///     Pre-gap length in sectors preceding track start (0 if none).
        /// </summary>
        public long Pregap;
        /// <summary>
        ///     Session number (1-based). 1 for single-session discs.
        /// </summary>
        public byte Session;
        /// <summary>
        ///     ISRC raw 13-byte code (no null terminator). All zeros if not present.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public byte[] Isrc;
        /// <summary>
        ///     Control / attribute bitfield (see file documentation for suggested bit mapping).
        /// </summary>
        public byte Flags;
    }

#endregion
}