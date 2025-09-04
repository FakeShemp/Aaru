using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Amg
{
#region Nested type: ArcHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct ArcHeader
    {
        public readonly ushort magic;
        public readonly byte   version;
        public readonly byte   padding;
        public readonly ushort files;
        public readonly uint   size;
        public readonly ushort commentLength;
    }

#endregion
}