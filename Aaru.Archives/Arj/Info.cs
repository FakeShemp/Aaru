using System;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Archives;

public sealed partial class Arj
{
    /// <summary>Precomputed CRC32 lookup table (polynomial 0xEDB88320).</summary>
    static readonly uint[] _crcTable = BuildCrcTable();

    static uint[] BuildCrcTable()
    {
        var table = new uint[256];

        for(uint i = 0; i < 256; i++)
        {
            uint r = i;

            for(var j = 0; j < 8; j++) r = (r & 1) != 0 ? r >> 1 ^ CRC_POLY : r >> 1;

            table[i] = r;
        }

        return table;
    }

    /// <summary>Compute CRC32 over a byte span.</summary>
    static uint ComputeCrc(uint crc, ReadOnlySpan<byte> data)
    {
        foreach(byte b in data) crc = _crcTable[(byte)(crc ^ b)] ^ crc >> 8;

        return crc;
    }

    /// <summary>Convert MS-DOS FAT timestamp to DateTime.</summary>
    static DateTime DosToDateTime(uint dosDateTime)
    {
        int second = (int)(dosDateTime & 0x1F) * 2;
        var minute = (int)(dosDateTime >> 5  & 0x3F);
        var hour   = (int)(dosDateTime >> 11 & 0x1F);
        var day    = (int)(dosDateTime >> 16 & 0x1F);
        var month  = (int)(dosDateTime >> 21 & 0x0F);
        int year   = (int)(dosDateTime >> 25 & 0x7F) + 1980;

        if(day   < 1) day    = 1;
        if(month < 1) month  = 1;
        if(month > 12) month = 12;

        int maxDay = DateTime.DaysInMonth(year, month);

        if(day    > maxDay) day = maxDay;
        if(hour   > 23) hour    = 23;
        if(minute > 59) minute  = 59;
        if(second > 59) second  = 59;

        return new DateTime(year, month, day, hour, minute, second);
    }

    /// <summary>Convert a Unix timestamp to DateTime.</summary>
    static DateTime UnixToDateTime(uint unixTime) =>
        unixTime == 0 ? default(DateTime) : DateTimeOffset.FromUnixTimeSeconds(unixTime).DateTime;

    /// <summary>Convert a timestamp value to DateTime based on host OS.</summary>
    static DateTime TimestampToDateTime(uint raw, HostOs hostOs) =>
        hostOs is HostOs.Unix or HostOs.Next ? UnixToDateTime(raw) : DosToDateTime(raw);

#region IArchive Members

    /// <inheritdoc />
    public bool Identify(IFilter filter)
    {
        if(filter.DataForkLength < MIN_HEADER_SIZE) return false;

        Stream stream = filter.GetDataForkStream();
        stream.Position = 0;

        var header = new byte[MIN_HEADER_SIZE];
        stream.ReadExactly(header, 0, header.Length);

        // Check header signature (little-endian: 0x60, 0xEA)
        var headerSignature = BitConverter.ToUInt16(header, 0);

        if(headerSignature != HEADER_ID) return false;

        // Read basic header size
        var basicHdrSize = BitConverter.ToUInt16(header, 2);

        if(basicHdrSize is 0 or > HEADER_SIZE_MAX) return false;

        // Read the full header for CRC validation
        if(filter.DataForkLength < 4 + basicHdrSize + 4) return false;

        stream.Position = 4;
        var hdrData = new byte[basicHdrSize];
        stream.ReadExactly(hdrData, 0, basicHdrSize);

        var crcBytes = new byte[4];
        stream.ReadExactly(crcBytes, 0, 4);
        var storedCrc = BitConverter.ToUInt32(crcBytes, 0);

        // Compute and verify CRC32
        uint computedCrc = ComputeCrc(CRC_MASK, hdrData) ^ CRC_MASK;

        return computedCrc == storedCrc;
    }

    /// <inheritdoc />
    public void GetInformation(IFilter filter, Encoding encoding, out string information)
    {
        information = "";

        if(!Identify(filter)) return;

        var sb = new StringBuilder();
        sb.AppendLine(Localization.ARJ_archive);

        information = sb.ToString();
    }

#endregion
}