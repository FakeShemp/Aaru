using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Stfs
{
#region Nested type: ConsolePackage

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct ConsolePackage
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

#region Nested type: LicenseEntry

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct LicenseEntry
    {
        public long LicenseId;
        public int  LicenseBits;
        public int  LicenseFlags;
    }

#endregion

#region Nested type: LocalizedString

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct LocalizedString
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string Name;
    }

#endregion

#region Nested type: Metadata

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    struct Metadata
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
        public LocalizedString[] DisplayName;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 18)]
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
    struct RemotePackage
    {
        public PackageMagic Magic;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public byte[] Signature;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 296)]
        public byte[] Padding;
        public Metadata Metadata;
    }

#endregion
}