namespace Aaru.Core.Image.PS3;

/// <summary>Parsed IRD (ISO Rebuild Data) file structure for PlayStation 3 disc images.</summary>
struct IrdFile
{
    /// <summary>IRD format version (6–9).</summary>
    public byte Version;
    /// <summary>9-character game ID (e.g., "BLES00905").</summary>
    public string GameId;
    /// <summary>Game name.</summary>
    public string GameName;
    /// <summary>PS3 system/update version (4 chars).</summary>
    public string UpdateVer;
    /// <summary>Game version (5 chars).</summary>
    public string GameVer;
    /// <summary>App version (5 chars).</summary>
    public string AppVer;
    /// <summary>Data1 key (16 bytes).</summary>
    public byte[] D1;
    /// <summary>Data2 key (16 bytes).</summary>
    public byte[] D2;
    /// <summary>PIC data (115 bytes). Null if not present.</summary>
    public byte[] Pic;
    /// <summary>True if PIC data was present in the IRD.</summary>
    public bool HasPic;
    /// <summary>True if parsing succeeded.</summary>
    public bool Valid;
}