namespace Aaru.Archives;

public sealed partial class StuffIt5
{
    const uint ENTRY_ID = 0xA5A5A5A5;

    const int  MIN_HEADER_SIZE = 100;
    const byte ARCHIVE_VERSION = 5;

    // Entry flags
    const byte FLAGS_DIRECTORY = 0x40;
    const byte FLAGS_ENCRYPTED = 0x20;

    // Archive-level flags
    const byte ARCHIVE_FLAGS_14BYTES = 0x10;
    const byte ARCHIVE_FLAGS_20      = 0x20;
    const byte ARCHIVE_FLAGS_40      = 0x40;
    const byte ARCHIVE_FLAGS_CRYPTED = 0x80;

    const int KEY_LENGTH = 5;

    const string XATTR_COMMENT             = "comment";
    const string XATTR_APPLE_RESOURCE_FORK = "com.apple.ResourceFork";
    const string XATTR_APPLE_FINDER_INFO   = "com.apple.FinderInfo";
    const string XATTR_APPLE_HFS_TYPE      = "hfs.type";
    const string XATTR_APPLE_HFS_CREATOR   = "hfs.creator";
}