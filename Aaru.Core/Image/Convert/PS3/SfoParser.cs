using System;
using System.Buffers.Binary;
using System.Text;

namespace Aaru.Core.Image.PS3;

/// <summary>A single key-value entry from a PARAM.SFO file.</summary>
struct SfoEntry
{
    /// <summary>Parameter name.</summary>
    public string Key;
    /// <summary>UTF-8 string value (for string-type entries).</summary>
    public string Value;
    /// <summary>Integer value (for integer-type entries).</summary>
    public int IntValue;
    /// <summary>Data format: 0x0004 = UTF-8, 0x0204 = UTF-8 (special), 0x0404 = int32.</summary>
    public ushort Format;
}

/// <summary>Parsed PARAM.SFO file.</summary>
struct SfoFile
{
    /// <summary>Array of entries.</summary>
    public SfoEntry[] Entries;
}

/// <summary>
///     PARAM.SFO parser for PlayStation 3 game metadata.
///     Port of tool/ps3/sfo.c.
/// </summary>
static class SfoParser
{
    /// <summary>SFO magic: "\0PSF" as little-endian uint32 (bytes: 00 50 53 46).</summary>
    const uint SFO_MAGIC = 0x46535000;

    const ushort SFO_FORMAT_UTF8         = 0x0004;
    const ushort SFO_FORMAT_UTF8_SPECIAL = 0x0204;
    const ushort SFO_FORMAT_INT32        = 0x0404;

    /// <summary>Header size in bytes.</summary>
    const int HEADER_SIZE = 20;

    /// <summary>Index entry size in bytes.</summary>
    const int INDEX_ENTRY_SIZE = 16;

    /// <summary>
    ///     Parses a PARAM.SFO from an in-memory buffer.
    /// </summary>
    /// <param name="data">SFO file data.</param>
    /// <param name="sfo">Output: parsed SFO structure.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool Parse(byte[] data, out SfoFile sfo)
    {
        sfo = default(SfoFile);

        if(data is null || data.Length < HEADER_SIZE) return false;

        uint magic = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(0, 4));

        if(magic != SFO_MAGIC) return false;

        uint keyTableOffset  = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(8,  4));
        uint dataTableOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(12, 4));
        uint entryCount      = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(16, 4));

        if(entryCount == 0)
        {
            sfo.Entries = [];

            return true;
        }

        uint indexEnd = HEADER_SIZE + entryCount * INDEX_ENTRY_SIZE;

        if(indexEnd > data.Length || keyTableOffset > data.Length || dataTableOffset > data.Length) return false;

        sfo.Entries = new SfoEntry[entryCount];

        for(uint i = 0; i < entryCount; i++)
        {
            int offset = HEADER_SIZE + (int)i * INDEX_ENTRY_SIZE;

            ushort keyOffset  = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset,      2));
            ushort dataFormat = BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(offset + 2,  2));
            uint   dataLen    = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 4,  4));
            uint   dataOffset = BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(offset + 12, 4));

            sfo.Entries[i].Format = dataFormat;

            // Read key from key table (null-terminated)
            uint absKey = keyTableOffset + keyOffset;

            if(absKey < data.Length)
            {
                int maxLen = data.Length - (int)absKey;
                int keyLen = FindNullTerminator(data, (int)absKey, maxLen);
                sfo.Entries[i].Key = Encoding.ASCII.GetString(data, (int)absKey, keyLen);
            }

            // Read value from data table
            uint absData = dataTableOffset + dataOffset;

            if(absData >= data.Length || dataLen == 0) continue;

            var avail = (uint)(data.Length - (int)absData);

            if(dataLen > avail) dataLen = avail;

            if(dataFormat is SFO_FORMAT_UTF8 or SFO_FORMAT_UTF8_SPECIAL)
            {
                // String value: read up to first null or dataLen
                int strLen = FindNullTerminator(data, (int)absData, (int)dataLen);
                sfo.Entries[i].Value = Encoding.UTF8.GetString(data, (int)absData, strLen);
            }
            else if(dataFormat == SFO_FORMAT_INT32 && dataLen >= 4)
            {
                sfo.Entries[i].IntValue = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan((int)absData, 4));
            }
        }

        return true;
    }

    /// <summary>Looks up a string value by key in a parsed SFO.</summary>
    /// <param name="sfo">Parsed SFO structure.</param>
    /// <param name="key">Key to search for.</param>
    /// <returns>The string value, or <c>null</c> if not found.</returns>
    public static string GetString(SfoFile sfo, string key)
    {
        if(sfo.Entries is null || key is null) return null;

        for(var i = 0; i < sfo.Entries.Length; i++)
        {
            if(sfo.Entries[i].Key != null && sfo.Entries[i].Key == key) return sfo.Entries[i].Value;
        }

        return null;
    }

    /// <summary>Finds the offset of the first null byte within a region, or returns maxLen if none found.</summary>
    static int FindNullTerminator(byte[] data, int start, int maxLen)
    {
        for(var i = 0; i < maxLen; i++)
        {
            if(data[start + i] == 0) return i;
        }

        return maxLen;
    }
}