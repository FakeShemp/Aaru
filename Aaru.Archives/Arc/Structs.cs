using System.Runtime.InteropServices;

namespace Aaru.Archives;

public partial class Arc
{
#region Nested type: Header

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct Header
    {
        public readonly byte marker;
        public readonly byte method;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = FNLEN)]
        public readonly byte[] filename;
        public readonly int    compressed;
        public readonly ushort date;
        public readonly ushort time;
        public readonly ushort crc;
        public readonly int    uncompressed;
    }

#endregion
}