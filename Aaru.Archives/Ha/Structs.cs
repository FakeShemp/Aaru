using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Ha
{
#region Nested type: FHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct FHeader
    {
        // First nibble is archive version, second nibble is compression type
        public readonly byte VerType;

        // Compressed length
        public readonly ushort clen;

        // Original length
        public readonly ushort olen;

        // Unclear if DOS packed date or what
        public readonly ushort date;
        public readonly ushort time;

        // CRC32
        public readonly uint crc;

        // Follows null-terminated path
        // Follows null-terminated filename
        // Follows 1-byte machine dependent information length
        // Follows machine dependent information
        // Follows compressed data
    }

#endregion

#region Nested type: HaHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct HaHeader
    {
        public readonly ushort magic;
        public readonly ushort count;
    }

#endregion
}