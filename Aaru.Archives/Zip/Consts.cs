using System;

namespace Aaru.Archives;

public sealed partial class Zip
{
    /// <summary>Local file header signature: PK\x03\x04</summary>
    const uint LOCAL_HEADER_SIG = 0x04034B50;
    /// <summary>Central directory file header signature: PK\x01\x02</summary>
    const uint CENTRAL_DIR_SIG = 0x02014B50;
    /// <summary>End of central directory record signature: PK\x05\x06</summary>
    const uint EOCD_SIG = 0x06054B50;
    /// <summary>ZIP64 end of central directory record signature: PK\x06\x06</summary>
    const uint ZIP64_EOCD_SIG = 0x06064B50;
    /// <summary>ZIP64 end of central directory locator signature: PK\x06\x07</summary>
    const uint ZIP64_LOCATOR_SIG = 0x07064B50;
    /// <summary>Data descriptor signature: PK\x07\x08</summary>
    const uint DATA_DESCRIPTOR_SIG = 0x08074B50;

    // Extra field header IDs
    const ushort EXTRA_ZIP64           = 0x0001;
    const ushort EXTRA_OS2_EA          = 0x0009;
    const ushort EXTRA_NTFS            = 0x000A;
    const ushort EXTRA_OPENVMS         = 0x000C;
    const ushort EXTRA_PKWARE_UNIX     = 0x000D;
    const ushort EXTRA_S390_UNCOMP     = 0x0065;
    const ushort EXTRA_S390_COMP       = 0x0066;
    const ushort EXTRA_MAC_OLD         = 0x07C8;
    const ushort EXTRA_ZIPIT_MAC1      = 0x2605;
    const ushort EXTRA_ZIPIT_MAC2      = 0x2705;
    const ushort EXTRA_MAC_NEW         = 0x334D;
    const ushort EXTRA_TANDEM          = 0x4154;
    const ushort EXTRA_ACORN           = 0x4341;
    const ushort EXTRA_NTSD            = 0x4453;
    const ushort EXTRA_VM_CMS          = 0x4704;
    const ushort EXTRA_MVS             = 0x470F;
    const ushort EXTRA_THEOS_OLD       = 0x4854;
    const ushort EXTRA_FWKCS_MD5       = 0x4B46;
    const ushort EXTRA_OS2_ACL         = 0x4C41;
    const ushort EXTRA_SMARTZIP_MAC    = 0x4D63;
    const ushort EXTRA_AOS_VS          = 0x5356;
    const ushort EXTRA_EXT_TIMESTAMP   = 0x5455;
    const ushort EXTRA_UNIX1           = 0x5855;
    const ushort EXTRA_UNICODE_COMMENT = 0x6375;
    const ushort EXTRA_BEOS            = 0x6542;
    const ushort EXTRA_THEOS           = 0x6854;
    const ushort EXTRA_UNICODE_PATH    = 0x7075;
    const ushort EXTRA_ASI_UNIX        = 0x756E;
    const ushort EXTRA_UNIX2           = 0x7855;
    const ushort EXTRA_UNIX3           = 0x7875;
    const ushort EXTRA_WINZIP_AES      = 0x9901;
    const ushort EXTRA_QDOS            = 0xFB4A;

    // Unix file type bits from st_mode
    const ushort S_IFMT  = 0xF000;
    const ushort S_IFLNK = 0xA000;
    const ushort S_IFBLK = 0x6000;
    const ushort S_IFCHR = 0x2000;

    // General purpose bit flags
    const ushort FLAG_ENCRYPTED        = 0x0001;
    const ushort FLAG_IMPLODE_8K       = 0x0002;
    const ushort FLAG_IMPLODE_LITERALS = 0x0004;
    const ushort FLAG_DATA_DESCRIPTOR  = 0x0008;
    const ushort FLAG_UTF8             = 0x0800;

    /// <summary>Minimum ZIP file size: empty EOCD record (22 bytes)</summary>
    const int MIN_FILE_SIZE = 22;

    /// <summary>Size of a fixed central directory entry (without variable-length fields)</summary>
    const int CENTRAL_DIR_ENTRY_SIZE = 46;

    /// <summary>Size of a fixed local file header (without variable-length fields)</summary>
    const int LOCAL_HEADER_SIZE = 30;

    /// <summary>Size of a fixed End of Central Directory record (without comment)</summary>
    const int EOCD_SIZE = 22;

    /// <summary>Chunk size for backward EOCD search</summary>
    const int EOCD_SEARCH_CHUNK = 0x10000;

    /// <summary>Directory attribute in DOS external file attributes</summary>
    const uint ATTR_DIRECTORY = 0x10;

    /// <summary>CRC32 polynomial used in ZIP format</summary>
    const uint CRC_POLY = 0xEDB88320;

    // Mac classic epoch: 1904-01-01 00:00:00 UTC
    static readonly DateTime _macEpoch = new(1904, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}