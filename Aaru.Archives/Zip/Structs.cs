using System;

namespace Aaru.Archives;

public sealed partial class Zip
{
#region Nested type: Entry

    struct Entry
    {
        public CompressionMethod Method;
        public string            Filename;
        public long              CompressedSize;
        public long              UncompressedSize;
        public long              DataOffset;
        public DateTime          LastWriteTime;
        public DateTime          LastAccessTime;
        public DateTime          CreationTime;
        public uint              Crc32;
        public HostOs            System;
        public uint              ExternalAttributes;
        public ushort            UnixPermissions;
        public uint              Uid;
        public uint              Gid;
        public string            Comment;
        public ushort            Flags;
        public bool              IsDirectory;
        public bool              IsEncrypted;
    }

#endregion
}