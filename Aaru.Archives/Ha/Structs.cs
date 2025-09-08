using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Ha
{
#region Nested type: Entry

    struct Entry
    {
        public Method         Method;
        public uint           Compressed;
        public uint           Uncompressed;
        public DateTime       LastWrite;
        public FileAttributes Attributes;
        public long           DataOffset;
        public string         Filename;
        public ushort?        Mode;
        public ushort?        Uid;
        public ushort?        Gid;
    }

#endregion

#region Nested type: FHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct FHeader
    {
        // First nibble is archive version, second nibble is compression type
        public readonly byte VerType;

        // Compressed length
        public readonly uint clen;

        // Original length
        public readonly uint olen;

        public readonly int time;

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

#region Nested type: UnixMdi

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct UnixMdi
    {
        public readonly byte   type;
        public readonly ushort attr;
        public readonly ushort user;
        public readonly ushort group;
    }

#endregion
}