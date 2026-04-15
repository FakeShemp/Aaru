using System;

namespace Aaru.Archives;

public sealed partial class Arj
{
#region Nested type: Entry

    struct Entry
    {
        public Method   Method;
        public string   Filename;
        public long     CompressedSize;
        public long     UncompressedSize;
        public long     DataOffset;
        public DateTime LastWriteTime;
        public DateTime CreationTime;
        public DateTime LastAccessTime;
        public HostOs   HostOs;
        public uint     FileCrc;
        public ushort   FileMode;
        public byte     ArjFlags;
        public byte     ArjxNbr;
        public string   Comment;
        public byte[]   ExtendedAttributes;
        public bool     IsDirectory;
    }

#endregion
}