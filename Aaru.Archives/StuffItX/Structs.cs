using System;
using System.Collections.Generic;

namespace Aaru.Archives;

public sealed partial class StuffItX
{
#region Nested type: Element

    struct Element
    {
        public int    Something;
        public int    Type;
        public long[] Attribs;
        public long[] AlgList;
        public long   AlgList3Extra;
        public long   DataOffset;
        public long   ActualSize;
        public uint   DataCrc;
    }

#endregion

#region Nested type: Entry

    struct Entry
    {
        public string   Filename;
        public string   DirectoryPath;
        public bool     IsDirectory;
        public bool     IsLink;
        public bool     IsEmptyStream;
        public bool     Encrypted;
        public string   Comment;
        public long     SolidOffset;
        public long     CompressedSize;
        public long     UncompressedSize;
        public byte[]   FinderInfo;
        public uint     PosixPermissions;
        public uint     PosixUser;
        public uint     PosixGroup;
        public bool     HasPosixOwner;
        public DateTime CreationTime;
        public DateTime ModificationTime;
        public bool     IsResourceFork;
        public Element  SolidElement;
        public bool     HasSolidElement;
    }

#endregion

#region Nested type: ForkInfo

    struct ForkInfo
    {
        public List<long> EntryIds;
        public ForkType   Type;
        public long       Length;
    }

#endregion
}