using System;
using System.IO;
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

#region Nested type: FileEntry

    struct FileEntry
    {
        public uint           Uncompressed;
        public uint           Compressed;
        public DateTime       LastWrite;
        public FileAttributes Attributes;
        public byte           Flags;
        public uint           Crc;
        public string         Filename;
        public string         Comment;
        public long           Offset;

        public override string ToString() => Filename;
    }

#endregion

#region Nested type: FileHeader

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    readonly struct FileHeader
    {
        public readonly ushort magic;
        public readonly uint   compressed;
        public readonly uint   uncompressed;
        public readonly ushort time;
        public readonly ushort date;
        public readonly byte   attr;
        public readonly byte   flags;

        // Something changes in processing when 0x80 is set
        public readonly byte unknown;
        public readonly uint crc;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public readonly byte[] filename;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public readonly byte[] extension;
        public readonly byte   pathLength;
        public readonly ushort commentLength;
    }

#endregion
}