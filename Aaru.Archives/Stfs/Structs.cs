using System;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Attributes;

namespace Aaru.Archives;

public sealed partial class Stfs
{
#region Nested type: ConsolePackage

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    [SwapEndian]
    partial struct ConsolePackage
    {
        public PackageMagic Magic;
        public ushort       PkCertificateSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] CertificateOwnerConsoleId;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string CertificateOwnerConsolePartNumber;
        public ConsoleType CertificateOwnerConsoleType;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
        public string CertificateDateOfGeneration;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] PublicExponent;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] PublicModulus;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] CertificateSignature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] Signature;
        public Metadata Metadata;
    }

#endregion

#region Nested type: FileEntry

    struct FileEntry
    {
        public string   Filename;
        public int      StartingBlock;
        public int      FileSize;
        public DateTime LastWrite;
        public DateTime LastAccess;
        public bool     IsDirectory;
    }

#endregion

#region Nested type: FileTableEntry

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [SwapEndian]
    partial struct FileTableEntry
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x28)]
        public byte[] Filename;
        public byte FilenameLength;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] AllocatedBlocks;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] AllocatedBlocksCopy;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] StartingBlock;
        public short  PathIndicator;
        public int    FileSize;
        public ushort LastWriteDate;
        public ushort LastWriteTime;
        public ushort LastAccessDate;
        public ushort LastAccessTime;
    }

#endregion

#region Nested type: LicenseEntry

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    [SwapEndian]
    partial struct LicenseEntry
    {
        public long LicenseId;
        public int  LicenseBits;
        public int  LicenseFlags;
    }

#endregion

#region Nested type: LocalizedString

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    [SwapEndian]
    partial struct LocalizedString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string Name;
    }

#endregion

#region Nested type: Metadata

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    [SwapEndian]
    partial struct Metadata
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public LicenseEntry[] Licensing;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] HeaderSha1Hash;
        public uint        HeaderSize;
        public ContentType ContentType;
        public int         MetadataVersion;
        public long        ContentSize;
        public int         MediaId;
        public int         Version;
        public int         BaseVersion;
        public int         TitleId;
        public byte        Platform;
        public byte        ExecutableType;
        public byte        DiscNumber;
        public byte        DiscInSet;
        public int         SaveGameId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public byte[] ConsoleId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] ProfileId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 36)]
        public byte[] VolumeDescriptor;
        public int  DataFileCount;
        public long DataFileCombinedSize;
        public int  DescriptorType;
        public int  Reserved;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] SeriesId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] SeasonId;
        public short SeasonNo;
        public short EpisodeNo;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] Padding;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] DeviceId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public LocalizedString[] DisplayName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 9)]
        public LocalizedString[] DisplayDescription;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string PublisherName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string TitleName;
        public TransferFlags TransferFlags;
        public int           ThumbnailImageSize;
        public int           TitleThumbnailImageSize;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16384)]
        public byte[] ThumbnailImage;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16384)]
        public byte[] TitleThumbnailImage;
    }

#endregion

#region Nested type: RemotePackage

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    [SwapEndian]
    partial struct RemotePackage
    {
        public PackageMagic Magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Signature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
        public byte[] Padding;
        public Metadata Metadata;
    }

#endregion

#region Nested type: VolumeDescriptor

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct VolumeDescriptor
    {
        public byte  Length;
        public byte  Reserved;
        public byte  BlockSeparation;
        public short FileTableBlockCount;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] FileTableBlockNumber;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x14)]
        public byte[] TopHashTableHash;
        public int TotalAllocatedBlocks;
        public int TotalUnallocatedBlocks;
    }

#endregion
}