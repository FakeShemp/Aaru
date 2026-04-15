namespace Aaru.Archives;

public sealed partial class Ace
{
#region CompressionType enum

    enum CompressionType : byte
    {
        /// <summary>Stored (no compression)</summary>
        Stored = 0,
        /// <summary>ACE v1 LZ77 compression</summary>
        Lz77 = 1,
        /// <summary>ACE v2 blocked compression (LZ77, delta, exe, sound, pic subtypes)</summary>
        Blocked = 2
    }

#endregion

#region HostOs enum

    enum HostOs : byte
    {
        MsDos   = 0,
        Os2     = 1,
        Win32   = 2,
        Unix    = 3,
        MacOs   = 4,
        WinNt   = 5,
        Primos  = 6,
        AppleGs = 7,
        Atari   = 8,
        VaxVms  = 9,
        Amiga   = 10,
        Next    = 11,
        Linux   = 12
    }

#endregion

#region HeaderType enum

    enum HeaderType : byte
    {
        /// <summary>Main archive header</summary>
        Main = 0,
        /// <summary>File entry (32-bit sizes, ACE v1)</summary>
        File32 = 1,
        /// <summary>Recovery record (32-bit, ACE v1)</summary>
        Recovery32 = 2,
        /// <summary>File entry (64-bit sizes, ACE v2)</summary>
        File = 3,
        /// <summary>Recovery record (64-bit, ACE v2)</summary>
        Recovery = 4,
        /// <summary>Recovery record v2</summary>
        Recovery2 = 5
    }

#endregion
}