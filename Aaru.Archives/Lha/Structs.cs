using System;

namespace Aaru.Archives;

public sealed partial class Lha
{
#region Nested type: Entry

    struct Entry
    {
        public Method   Method;
        public string   Filename;
        public string   DirectoryPath;
        public long     CompressedSize;
        public long     UncompressedSize;
        public long     DataOffset;
        public DateTime LastWriteTime;
        public DateTime CreationTime;
        public DateTime LastAccessTime;
        public OsType   Os;
        public ushort   Crc16;
        public byte     DosAttributes;
        public ushort   UnixPermissions;
        public ushort   Uid;
        public ushort   Gid;
        public string   UserName;
        public string   GroupName;
        public string   Comment;
        public bool     IsDirectory;
        public bool     HasUnixPermissions;
    }

#endregion
}