namespace Aaru.Archives;

public sealed partial class Ace
{
    const int ACE_SIGNATURE_LEN = 7;

    /// <summary>Offset of the signature within the main header (after CRC16 + SIZE + TYPE + FLAGS = 7 bytes)</summary>
    const int SIGNATURE_HEADER_OFFSET = 7;

    /// <summary>Maximum offset to search for the ACE signature in SFX archives</summary>
    const int MAX_SFX_SEARCH = 1024 * 1024;

    /// <summary>Minimum header size: CRC(2) + SIZE(2) + TYPE(1) + FLAGS(2) + SIGN(7) = 14</summary>
    const int MIN_MAIN_HEADER_SIZE = 14;

    // Common header flags (apply to all block types)
    const ushort FLAG_ADDSIZE = 1 << 0;
    const ushort FLAG_COMMENT = 1 << 1;
    const ushort FLAG_64BIT   = 1 << 2;

    // Main header flags
    const ushort FLAG_V20FORMAT   = 1 << 8;
    const ushort FLAG_SFX         = 1 << 9;
    const ushort FLAG_LIMITSFXJR  = 1 << 10;
    const ushort FLAG_MULTIVOLUME = 1 << 11;
    const ushort FLAG_AV          = 1 << 12;
    const ushort FLAG_RECOVERY    = 1 << 13;
    const ushort FLAG_LOCKED      = 1 << 14;
    const ushort FLAG_SOLID       = 1 << 15;

    // File header flags
    const ushort FLAG_SECURITY    = 1 << 10;
    const ushort FLAG_SPLITBEFORE = 1 << 12;
    const ushort FLAG_SPLITAFTER  = 1 << 13;
    const ushort FLAG_PASSWORD    = 1 << 14;

    // FLAG_SOLID (1 << 15) is also used for file headers

    /// <summary>CRC32 polynomial used by ACE</summary>
    const uint CRC_POLY = 0xEDB88320;
    /// <summary>CRC32 initial value</summary>
    const uint CRC_MASK = 0xFFFFFFFF;

    /// <summary>Directory attribute bit in the file attributes field</summary>
    const uint ATTR_DIRECTORY = 0x10;
    /// <summary>ACE signature string: **ACE**</summary>
    static readonly byte[] ACE_SIGNATURE =
    {
        0x2A, 0x2A, 0x41, 0x43, 0x45, 0x2A, 0x2A
    };
}