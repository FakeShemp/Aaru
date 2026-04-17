namespace Aaru.Archives;

public sealed partial class DiskDoubler
{
    /// <summary>Magic for single compressed file: 0xABCD0054.</summary>
    const uint MAGIC_SINGLE = 0xABCD0054;

    /// <summary>Magic for DiskDoubler archive v1: 'DDAR'.</summary>
    const uint MAGIC_DDAR = 0x44444152;

    /// <summary>Magic for DiskDoubler archive v2: 'DDA2'.</summary>
    const uint MAGIC_DDA2 = 0x44444132;

    /// <summary>Terminator entry type for DDA2 archives.</summary>
    const ushort DDA2_TERMINATOR = 0xBBBB;

    /// <summary>Size of the single-file header after the 4-byte magic.</summary>
    const int FILE_HEADER_SIZE = 80;

    /// <summary>Size of a DDAR entry after the 4-byte magic.</summary>
    const int DDAR_PREAMBLE_SIZE = 74;

    /// <summary>Size of a DDA2 preamble after the 4-byte magic.</summary>
    const int DDA2_PREAMBLE_SIZE = 58;

    /// <summary>XOR mask byte for methods 1, 2, 4, 5.</summary>
    const byte XOR_MASK = 0x5A;

    /// <summary>XOR mask byte for Stac LZS (method 7).</summary>
    const byte XOR_STAC = 0xFF;

    /// <summary>Finder flag indicating inline (uncompressed) data in DDAR archives.</summary>
    const ushort FINDER_FLAG_INLINE = 0x0020;

    // XAttr name constants
    const string XATTR_APPLE_RESOURCE_FORK = "com.apple.ResourceFork";
    const string XATTR_APPLE_FINDER_INFO   = "com.apple.FinderInfo";
    const string XATTR_APPLE_HFS_TYPE      = "hfs.type";
    const string XATTR_APPLE_HFS_CREATOR   = "hfs.creator";
}