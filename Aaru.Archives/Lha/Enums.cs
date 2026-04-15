namespace Aaru.Archives;

public sealed partial class Lha
{
#region Method enum

    enum Method
    {
        /// <summary>Uncompressed (-lh0-, -lz4-, -pm0-)</summary>
        Stored,
        /// <summary>LH1 dynamic Huffman (-lh1-)</summary>
        Lh1,
        /// <summary>LH2 experimental (-lh2-)</summary>
        Lh2,
        /// <summary>LH3 experimental (-lh3-)</summary>
        Lh3,
        /// <summary>LH4 static Huffman, 4KB window (-lh4-)</summary>
        Lh4,
        /// <summary>LH5 static Huffman, 8KB window (-lh5-)</summary>
        Lh5,
        /// <summary>LH6 static Huffman, 32KB window (-lh6-)</summary>
        Lh6,
        /// <summary>LH7 static Huffman, 64KB window (-lh7-)</summary>
        Lh7,
        /// <summary>LARC LZS (-lzs-)</summary>
        Lzs,
        /// <summary>LARC LZ5 (-lz5-)</summary>
        Lz5,
        /// <summary>PMARC method 1 (-pm1-)</summary>
        Pm1,
        /// <summary>PMARC method 2 (-pm2-)</summary>
        Pm2,
        /// <summary>Directory entry (-lhd-)</summary>
        Directory
    }

#endregion

#region OsType enum

    enum OsType : byte
    {
        Generic  = 0,
        MsDos    = (byte)'M',
        Os2      = (byte)'2',
        Os9      = (byte)'9',
        Os68K    = (byte)'K',
        Os386    = (byte)'3',
        Human    = (byte)'H',
        Unix     = (byte)'U',
        Cpm      = (byte)'C',
        Flex     = (byte)'F',
        MacOs    = (byte)'m',
        Windows9 = (byte)'w',
        WindowsN = (byte)'W',
        Runser   = (byte)'R',
        TownsOs  = (byte)'T',
        Xosk     = (byte)'X',
        Java     = (byte)'J'
    }

#endregion
}