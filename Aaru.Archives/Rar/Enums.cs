namespace Aaru.Archives;

public sealed partial class Rar
{
#region Rar4BlockType enum

    /// <summary>RAR 1.x-4.x block types.</summary>
    enum Rar4BlockType : byte
    {
        /// <summary>Archive marker / signature block.</summary>
        Marker = 0x72,
        /// <summary>Archive header.</summary>
        ArchiveHeader = 0x73,
        /// <summary>File header.</summary>
        FileHeader = 0x74,
        /// <summary>Comment header (old style).</summary>
        CommentHeader = 0x75,
        /// <summary>Extra information (old style).</summary>
        OldExtra = 0x76,
        /// <summary>Sub-block (old style).</summary>
        OldSub = 0x77,
        /// <summary>Recovery record (old style).</summary>
        OldRecovery = 0x78,
        /// <summary>Authentication information (old style).</summary>
        OldAuth = 0x79,
        /// <summary>Sub-block (new style).</summary>
        NewSub = 0x7A,
        /// <summary>End of archive.</summary>
        EndArchive = 0x7B
    }

#endregion

#region Rar5BlockType enum

    /// <summary>RAR 5.0 block types.</summary>
    enum Rar5BlockType : ulong
    {
        /// <summary>Main archive header.</summary>
        Main = 1,
        /// <summary>File header.</summary>
        File = 2,
        /// <summary>Service header (comment, etc.).</summary>
        Service = 3,
        /// <summary>Encryption header.</summary>
        Encryption = 4,
        /// <summary>End of archive.</summary>
        End = 5
    }

#endregion

#region CompressionMethod enum

    /// <summary>RAR 1.x-4.x compression methods.</summary>
    enum CompressionMethod : byte
    {
        /// <summary>Stored (no compression).</summary>
        Store = 0x30,
        /// <summary>Fastest compression.</summary>
        Fastest = 0x31,
        /// <summary>Fast compression.</summary>
        Fast = 0x32,
        /// <summary>Normal compression.</summary>
        Normal = 0x33,
        /// <summary>Good compression.</summary>
        Good = 0x34,
        /// <summary>Best compression.</summary>
        Best = 0x35
    }

#endregion

#region HostOs enum

    /// <summary>RAR 1.x-4.x host operating system.</summary>
    enum HostOs : byte
    {
        MsDos = 0,
        Os2   = 1,
        Win32 = 2,
        Unix  = 3
    }

#endregion

#region Rar5Os enum

    /// <summary>RAR 5.0 host operating system.</summary>
    enum Rar5Os : ulong
    {
        Windows = 0,
        Unix    = 1
    }

#endregion
}