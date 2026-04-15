namespace Aaru.Archives;

public sealed partial class Arj
{
    /// <summary>ARJ archive header signature (little-endian: 0x60, 0xEA)</summary>
    const ushort HEADER_ID = 0xEA60;
    /// <summary>Maximum basic header size in bytes</summary>
    const int HEADER_SIZE_MAX = 2600;
    /// <summary>Standard first header size (30 bytes of fixed fields)</summary>
    const int FIRST_HDR_SIZE = 30;
    /// <summary>Extended first header size with protection fields (34 bytes)</summary>
    const int FIRST_HDR_SIZE_V = 34;
    /// <summary>Minimum size for headers with access/creation timestamps</summary>
    const int R9_HDR_SIZE = 46;
    /// <summary>CRC32 mask for finalization (XOR with this for standard CRC32)</summary>
    const uint CRC_MASK = 0xFFFFFFFF;
    /// <summary>CRC32 polynomial (reversed, same as standard CRC-32/ISO)</summary>
    const uint CRC_POLY = 0xEDB88320;
    /// <summary>Extended header tag for OS/2 extended attributes</summary>
    const byte EA_TAG = (byte)'E';
    /// <summary>Extended header tag for UNIX special files</summary>
    const byte UXSPECIAL_TAG = (byte)'U';
    /// <summary>Extended header tag for owner info (string)</summary>
    const byte OWNER_TAG = (byte)'O';
    /// <summary>Extended header tag for owner info (numeric)</summary>
    const byte OWNER_NUM_TAG = (byte)'o';
    /// <summary>ARJZ minimum version that uses extended DEFLATE (deflatez)</summary>
    const byte ARJZ_DEFLATEZ_VERSION = 55;
    /// <summary>ARJZ minimum version that uses ArjzStream method 1</summary>
    const byte ARJZ_METHOD1_VERSION = 51;
    /// <summary>ARJZ minimum version that uses ArjzStream method 2</summary>
    const byte ARJZ_METHOD2_VERSION = 52;
    /// <summary>ARJZ minimum version that uses ArjzStream method 3</summary>
    const byte ARJZ_METHOD3_VERSION = 53;
    /// <summary>Minimum number of bytes needed to attempt identification</summary>
    const int MIN_HEADER_SIZE = 10;
    /// <summary>OS/2 FEA flag: critical EA</summary>
    const byte FEA_NEEDEA = 0x80;
}