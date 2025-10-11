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
}