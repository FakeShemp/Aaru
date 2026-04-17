using System;

namespace Aaru.Archives;

public sealed partial class CompactPro
{
#region Nested type: Entry

    struct Entry
    {
        public string Filename;
        public string DirectoryPath;
        public bool   IsDirectory;

        // Data fork
        public long DataOffset;
        public long DataCompressedSize;
        public long DataUncompressedSize;
        public bool DataLzh;

        // Resource fork
        public long ResourceOffset;
        public long ResourceCompressedSize;
        public long ResourceUncompressedSize;
        public bool ResourceLzh;

        // Mac metadata
        public uint     FileType;
        public uint     Creator;
        public ushort   FinderFlags;
        public DateTime CreationTime;
        public DateTime ModificationTime;
        public uint     Crc32;
        public bool     Encrypted;
    }

#endregion
}