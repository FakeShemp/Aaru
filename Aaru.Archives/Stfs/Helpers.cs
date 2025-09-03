namespace Aaru.Archives;

public sealed partial class Stfs
{
    static int BlockToPosition(int block, int headerSize) => (headerSize + 0xFFF & 0xF000) + (block << 12);

    static int ComputeBlockNumber(int block, int headerSize, int blockSeparation, bool console)
    {
        int blockShift;

        if((headerSize + 0xFFF & 0xF000) == 0xB000)
            blockShift = 1;
        else if((blockSeparation & 1) == 1)
            blockShift = 0;
        else
            blockShift = 1;

        int @base         = (block + 0xAA) / 0xAA;
        if(console) @base <<= blockShift;

        int @return = @base + block;

        if(block <= 0xAA) return @return;

        @base = (block + 0x70E4) / 0x70E4;
        if(console) @base <<= blockShift;
        @return += @base;

        if(block <= 0x70E4) return @return;

        @base = (block + 0x4AF768) / 0x4AF768;
        if(console) @base <<= 1;
        @return += @base;

        return @return;
    }
}