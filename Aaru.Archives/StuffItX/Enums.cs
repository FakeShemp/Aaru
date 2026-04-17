namespace Aaru.Archives;

public sealed partial class StuffItX
{
#region Nested type: CompressionAlgorithm

    enum CompressionAlgorithm : long
    {
        None      = -1,
        Brimstone = 0,
        Cyanide   = 1,
        Darkhorse = 2,
        Deflate   = 3,
        Blend     = 4,
        Rc4       = 5,
        Iron      = 6
    }

#endregion

#region Nested type: ElementType

    enum ElementType
    {
        End       = 0,
        Data      = 1,
        File      = 2,
        Fork      = 3,
        Directory = 4,
        Catalog   = 5,
        Clue      = 6,
        Root      = 7,
        Boundary  = 8
    }

#endregion

#region Nested type: ForkType

    enum ForkType : long
    {
        Data     = 0,
        Resource = 1
    }

#endregion

#region Nested type: PreprocessAlgorithm

    enum PreprocessAlgorithm : long
    {
        None    = -1,
        English = 0,
        X86     = 2
    }

#endregion
}