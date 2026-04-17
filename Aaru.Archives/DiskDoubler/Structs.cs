using System;

namespace Aaru.Archives;

public sealed partial class DiskDoubler
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
        public byte DataMethod;
        public byte DataDelta;

        // Resource fork
        public long ResourceOffset;
        public long ResourceCompressedSize;
        public long ResourceUncompressedSize;
        public byte ResourceMethod;
        public byte ResourceDelta;

        // Mac metadata
        public uint     FileType;
        public uint     Creator;
        public ushort   FinderFlags;
        public DateTime CreationTime;
        public DateTime ModificationTime;

        // DiskDoubler-specific
        public byte Info1;
        public byte Info2;
    }

#endregion
}