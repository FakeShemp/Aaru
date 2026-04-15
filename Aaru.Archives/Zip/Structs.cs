using System;
using System.Collections.Generic;

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
        public DateTime          BackupTime;
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
        public bool              IsSymlink;
        public string            SymlinkTarget;
        public ulong?            DeviceNo;

        // Mac metadata
        public byte[] ResourceFork;
        public byte[] FinderInfo;
        public string MacFileType;
        public string MacCreator;

        // OS/2
        public Dictionary<string, byte[]> Os2Eas;
        public byte[]                     Os2Acl;

        // Windows
        public byte[] NtSecurityDescriptor;

        // BeOS
        public Dictionary<string, byte[]> BeOsAttributes;

        // Acorn RISC OS
        public byte[] AcornLoadAddr;
        public byte[] AcornExecAddr;
        public byte[] AcornAttr;

        // OpenVMS
        public byte[] OpenVmsAttributes;

        // FWKCS
        public byte[] Md5Hash;

        // Unicode
        public string UnicodeComment;

        // Obscure platforms — stored as raw blobs
        public byte[] S390Attributes;
        public byte[] VmCmsAttributes;
        public byte[] MvsAttributes;
        public byte[] TheosAttributes;
        public byte[] QdosAttributes;
        public byte[] TandemAttributes;
        public byte[] AosVsAttributes;
    }

#endregion
}