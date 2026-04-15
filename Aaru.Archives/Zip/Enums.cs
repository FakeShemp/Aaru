namespace Aaru.Archives;

public sealed partial class Zip
{
#region CompressionMethod enum

    enum CompressionMethod : ushort
    {
        /// <summary>No compression</summary>
        Stored = 0,
        /// <summary>LZW-based shrink</summary>
        Shrink = 1,
        /// <summary>Reduce with compression factor 1</summary>
        Reduce1 = 2,
        /// <summary>Reduce with compression factor 2</summary>
        Reduce2 = 3,
        /// <summary>Reduce with compression factor 3</summary>
        Reduce3 = 4,
        /// <summary>Reduce with compression factor 4</summary>
        Reduce4 = 5,
        /// <summary>Shannon-Fano + LZSS imploding</summary>
        Implode = 6,
        /// <summary>Deflate (RFC 1951)</summary>
        Deflate = 8,
        /// <summary>Deflate64 / Enhanced Deflate (64KB window)</summary>
        Deflate64 = 9,
        /// <summary>BZip2</summary>
        BZip2 = 12,
        /// <summary>LZMA</summary>
        Lzma = 14,
        /// <summary>WinZip JPEG compressed data</summary>
        WinZipJpeg = 96,
        /// <summary>WinZip WavPack compressed data</summary>
        WavPack = 97,
        /// <summary>PPMd version I, Rev 1</summary>
        PPMd = 98,
        /// <summary>WinZip AES encryption marker (actual method in extra field)</summary>
        WinZipAes = 99
    }

#endregion

#region HostOs enum

    enum HostOs : byte
    {
        MsDos       = 0,
        Amiga       = 1,
        OpenVms     = 2,
        Unix        = 3,
        VmCms       = 4,
        AtariSt     = 5,
        Os2Hpfs     = 6,
        Macintosh   = 7,
        ZSystem     = 8,
        CpM         = 9,
        WindowsNtfs = 10,
        Mvs         = 11,
        Vse         = 12,
        AcornRisc   = 13,
        Vfat        = 14,
        AltMvs      = 15,
        BeOs        = 16,
        Tandem      = 17,
        Os400       = 18,
        OsX         = 19
    }

#endregion
}