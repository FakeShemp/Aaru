using System;

namespace Aaru.Archives;

public sealed partial class Arj
{
#region Method enum

    enum Method : byte
    {
        /// <summary>Uncompressed (stored)</summary>
        Stored = 0,
        /// <summary>Compressed with method 1 (most compression, LZH)</summary>
        Method1 = 1,
        /// <summary>Compressed with method 2 (medium compression, LZH)</summary>
        Method2 = 2,
        /// <summary>Compressed with method 3 (fast compression, LZH)</summary>
        Method3 = 3,
        /// <summary>Compressed with method 4 (fastest, Golomb-Rice)</summary>
        Fastest = 4
    }

#endregion

#region HostOs enum

    enum HostOs : byte
    {
        MsDos   = 0,
        Primos  = 1,
        Unix    = 2,
        Amiga   = 3,
        MacOs   = 4,
        Os2     = 5,
        AppleGs = 6,
        AtariSt = 7,
        Next    = 8,
        Vax     = 9,
        Win95   = 10,
        WinNt   = 11
    }

#endregion

#region FileType enum

    enum FileType : byte
    {
        Binary      = 0,
        Text        = 1,
        Comment     = 2,
        Directory   = 3,
        VolumeLabel = 4,
        Chapter     = 5,
        UnixSpecial = 6
    }

#endregion

#region ArjFlags enum

    [Flags]
    enum ArjFlags : byte
    {
        None    = 0x00,
        Garbled = 0x01,
        Volume  = 0x04,
        ExtFile = 0x08,
        PathSym = 0x10,
        Backup  = 0x20
    }

#endregion
}