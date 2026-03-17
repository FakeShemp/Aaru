using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace Aaru.Core.Image.PS3;

/// <summary>
///     IRD (ISO Rebuild Data) file parser for PlayStation 3 disc images.
///     Supports versions 6–9 as documented by ps3dev.
///     Handles both raw and gzip-compressed IRD files.
/// </summary>
static class IrdParser
{
    static readonly byte[] IRD_MAGIC = "3IRD"u8.ToArray();

    /// <summary>Parse an IRD file from disk.</summary>
    /// <param name="path">Path to the IRD file.</param>
    /// <param name="ird">Output: parsed IRD structure.</param>
    /// <returns>true on success, false on error.</returns>
    public static bool Parse(string path, out IrdFile ird)
    {
        ird = default(IrdFile);

        if(!File.Exists(path)) return false;

        byte[] raw;

        try
        {
            raw = File.ReadAllBytes(path);
        }
        catch
        {
            return false;
        }

        if(raw.Length < 20) return false;

        // Decompress if gzip
        byte[] data = raw.Length >= 2 && raw[0] == 0x1F && raw[1] == 0x8B ? Decompress(raw) : raw;

        if(data == null || data.Length < 20) return false;

        return ParseData(data, ref ird);
    }

    static byte[] Decompress(byte[] compressed)
    {
        try
        {
            using var input  = new MemoryStream(compressed);
            using var gzip   = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);

            return output.ToArray();
        }
        catch
        {
            return null;
        }
    }

    static bool ParseData(byte[] data, ref IrdFile ird)
    {
        int length = data.Length;

        // Validate magic "3IRD"
        if(data[0] != IRD_MAGIC[0] || data[1] != IRD_MAGIC[1] || data[2] != IRD_MAGIC[2] || data[3] != IRD_MAGIC[3])
            return false;

        byte version = data[4];

        if(version < 6 || version > 9) return false;

        ird.Version = version;
        var pos = 5;

        // Game ID: 9 bytes
        if(pos + 9 > length) return false;

        ird.GameId =  Encoding.ASCII.GetString(data, pos, 9).TrimEnd('\0');
        pos        += 9;

        // Game name: .NET BinaryReader length-prefixed string (7-bit encoded length for values < 128)
        if(pos >= length) return false;

        int nameLen = data[pos++];

        if(nameLen > 0 && pos + nameLen <= length)
        {
            ird.GameName =  Encoding.UTF8.GetString(data, pos, nameLen);
            pos          += nameLen;
        }
        else
            ird.GameName = "";

        // Fixed-width version strings
        if(pos + 4 <= length)
        {
            ird.UpdateVer =  Encoding.ASCII.GetString(data, pos, 4).TrimEnd('\0');
            pos           += 4;
        }

        if(pos + 5 <= length)
        {
            ird.GameVer =  Encoding.ASCII.GetString(data, pos, 5).TrimEnd('\0');
            pos         += 5;
        }

        if(pos + 5 <= length)
        {
            ird.AppVer =  Encoding.ASCII.GetString(data, pos, 5).TrimEnd('\0');
            pos        += 5;
        }

        // v7: extra ID field (4 bytes, skipped)
        if(version == 7) pos += 4;

        // Header gz: u32 LE length + data (skip)
        if(pos + 4 > length)
        {
            ird.Valid = true;

            return true;
        }

        var hdrLen = BitConverter.ToUInt32(data, pos);
        pos += 4;
        pos += (int)Math.Min(hdrLen, (uint)(length - pos));

        // Footer gz: u32 LE length + data (skip)
        if(pos + 4 > length)
        {
            ird.Valid = true;

            return true;
        }

        var ftrLen = BitConverter.ToUInt32(data, pos);
        pos += 4;
        pos += (int)Math.Min(ftrLen, (uint)(length - pos));

        // Region count + hashes (skip)
        if(pos < length)
        {
            byte rc = data[pos++];
            pos += rc * 16;
        }

        // File count + entries (skip)
        if(pos + 4 <= length)
        {
            var fc = BitConverter.ToUInt32(data, pos);
            pos += 4;
            pos += (int)fc * 24; // key(8) + md5(16) per entry
        }

        // Padding (4 bytes)
        pos += 4;

        // v9: PIC(115) then d1(16), d2(16)
        // v<9: d1(16), d2(16) then PIC(115)
        if(version >= 9)
        {
            if(pos + 115 <= length)
            {
                ird.Pic = new byte[115];
                Array.Copy(data, pos, ird.Pic, 0, 115);
                ird.HasPic =  true;
                pos        += 115;
            }
        }

        if(pos + 32 <= length)
        {
            ird.D1 = new byte[16];
            ird.D2 = new byte[16];
            Array.Copy(data, pos, ird.D1, 0, 16);
            pos += 16;
            Array.Copy(data, pos, ird.D2, 0, 16);
            pos += 16;
        }

        if(version < 9)
        {
            if(pos + 115 <= length)
            {
                ird.Pic = new byte[115];
                Array.Copy(data, pos, ird.Pic, 0, 115);
                ird.HasPic = true;
            }
        }

        ird.Valid = true;

        return true;
    }
}