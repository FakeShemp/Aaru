namespace Aaru.Archives;

public sealed partial class StuffIt
{
    const uint MAGIC  = 0x53495421; // "SIT!"
    const uint MAGIC2 = 0x724C6175; // "rLau"

    const int MIN_HEADER_SIZE   = 22;
    const int FILE_HEADER_SIZE  = 112;
    const int HEADER_CRC_OFFSET = 110;

    const byte ENCRYPTED_FLAG            = 0x80;
    const byte START_FOLDER              = 0x20;
    const byte END_FOLDER                = 0x21;
    const byte FOLDER_CONTAINS_ENCRYPTED = 0x10;
    const byte METHOD_MASK               = 0x0F;
    const byte FOLDER_MASK               = 0x6F; // ~(ENCRYPTED_FLAG | FOLDER_CONTAINS_ENCRYPTED)

    const string XATTR_APPLE_RESOURCE_FORK = "com.apple.ResourceFork";
    const string XATTR_APPLE_FINDER_INFO   = "com.apple.FinderInfo";
    const string XATTR_APPLE_HFS_TYPE      = "hfs.type";
    const string XATTR_APPLE_HFS_CREATOR   = "hfs.creator";
}