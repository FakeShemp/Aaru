namespace Aaru.Archives;

public sealed partial class Ha
{
#region Nested type: MdiSource

    enum MdiSource : byte
    {
        MSDOS = 1,
        UNIX  = 2
    }

#endregion

#region Nested type: Method

    enum Method : byte
    {
        Copy      = 0,
        ASC       = 1,
        HSC       = 2,
        Directory = 14,
        Special   = 15
    }

#endregion
}