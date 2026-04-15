using System;

namespace Aaru.Archives;

public sealed partial class Ace
{
#region Nested type: Entry

    struct Entry
    {
        public CompressionType Method;
        public string          Filename;
        public long            CompressedSize;
        public long            UncompressedSize;
        public long            DataOffset;
        public DateTime        LastWriteTime;
        public uint            Crc32;
        public uint            Attributes;
        public byte            Quality;
        public ushort          DecompParam;
        public HostOs          Host;
        public string          Comment;
        public bool            IsDirectory;
        public bool            IsSolid;
        public bool            IsEncrypted;
        public bool            IsSplit;
    }

#endregion
}