using System;

namespace Aaru.Archives;

public sealed partial class StuffIt
{
#region Nested type: Entry

    struct Entry
    {
        public string Filename;
        public string DirectoryPath;
        public bool   IsDirectory;
        public bool   Encrypted;

        // Data fork
        public long              DataOffset;
        public long              DataCompressedSize;
        public long              DataUncompressedSize;
        public CompressionMethod DataMethod;
        public ushort            DataCrc16;

        // Resource fork
        public long              ResourceOffset;
        public long              ResourceCompressedSize;
        public long              ResourceUncompressedSize;
        public CompressionMethod ResourceMethod;
        public ushort            ResourceCrc16;

        // Mac metadata
        public uint     FileType;
        public uint     Creator;
        public ushort   FinderFlags;
        public DateTime CreationTime;
        public DateTime ModificationTime;
    }

#endregion
}