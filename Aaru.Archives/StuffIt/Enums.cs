namespace Aaru.Archives;

public sealed partial class StuffIt
{
#region Nested type: CompressionMethod

    enum CompressionMethod : byte
    {
        None      = 0,
        Rle       = 1,
        Compress  = 2,
        Huffman   = 3,
        Lzah      = 5,
        Mw        = 8,
        LzHuffman = 13,
        Installer = 14,
        Arsenic   = 15
    }

#endregion
}