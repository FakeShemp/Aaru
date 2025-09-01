using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Aaru.Archives;

public sealed partial class Arc
{
#region Nested type: Entry

    struct Entry
    {
        public Method         Method;
        public string         Filename;
        public int            Compressed;
        public int            Uncompressed;
        public DateTime       LastWriteTime;
        public long           DataOffset;
        public string         Comment;
        public FileAttributes Attributes;
    }

#endregion

#region Nested type: Header

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct Header
    {
        public readonly byte   marker;
        public readonly Method method;
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