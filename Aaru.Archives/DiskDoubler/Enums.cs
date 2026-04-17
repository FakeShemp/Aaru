namespace Aaru.Archives;

public sealed partial class DiskDoubler
{
#region Nested type: DiskDoublerMethod

    enum DiskDoublerMethod : byte
    {
        None       = 0,
        Compress   = 1,
        Method2    = 2,
        Rle        = 3,
        Huffman    = 4,
        Method5    = 5,
        Ads        = 6,
        StacLzs    = 7,
        CompactPro = 8,
        Ad         = 9,
        Ddn        = 10
    }

#endregion
}