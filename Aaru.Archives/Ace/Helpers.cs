using System;
using System.Text;

namespace Aaru.Archives;

public sealed partial class Ace
{
    // =====================================================================
    // Comment decompression — ACE uses a custom LZP + Huffman scheme.
    // =====================================================================

    /// <summary>Maximum Huffman code width for main symbol table.</summary>
    const int COMMENT_MAX_CODE_WIDTH = 11;
    /// <summary>Maximum Huffman code width for the precode (width-of-widths) table.</summary>
    const int COMMENT_MAX_PRECODE_WIDTH = 7;
    /// <summary>Maximum number of main symbols: 260 + MAX_DIC_BITS(22) + 2 = 284.</summary>
    const int COMMENT_MAX_MAIN_CODE = 284;
    /// <summary>Maximum precode symbol count.</summary>
    const int COMMENT_MAX_PRECODE = 15;
    /// <summary>Precomputed CRC32 lookup table.</summary>
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

    /// <summary>Compute CRC16 (lower 16 bits of CRC32) for header validation.</summary>
    static ushort ComputeHeaderCrc(ReadOnlySpan<byte> headerData) => (ushort)ComputeCrc(CRC_MASK, headerData);

    /// <summary>Convert MS-DOS date/time to DateTime.</summary>
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

    /// <summary>Decompress an ACE comment from its compressed form.</summary>
    /// <returns>The decompressed comment string, or null on failure.</returns>
    static string DecompressComment(byte[] compressedData)
    {
        if(compressedData is null || compressedData.Length < 4) return null;

        // Initialize bitstream reader state
        var bitPos    = 0; // bit position within current 32-bit word
        var wordIndex = 0; // index into uint array

        // Copy compressed data into uint array (LE words)
        int wordCount = (compressedData.Length + 3) / 4;
        var words     = new uint[wordCount + 1]; // +1 for safe lookahead

        for(var i = 0; i < compressedData.Length; i += 4)
        {
            uint w         = 0;
            int  remaining = compressedData.Length - i;

            if(remaining >= 4)
                w = BitConverter.ToUInt32(compressedData, i);
            else
            {
                for(var j = 0; j < remaining; j++) w |= (uint)compressedData[i + j] << j * 8;
            }

            words[i / 4] = w;
        }

        // Current 32-bit code register (MSB-first extraction)
        uint code = words[0];

        // ---- Helper: read N bits from the bitstream (MSB-first) ----
        uint ReadBits(int bits)
        {
            uint result = code >> 32 - bits;

            bitPos += bits;

            if(bitPos >= 32)
            {
                bitPos -= 32;
                wordIndex++;
            }

            // Reload code register
            uint b0 = wordIndex     < words.Length ? words[wordIndex] : 0;
            uint b1 = wordIndex + 1 < words.Length ? words[wordIndex + 1] : 0;

            code = (b0 << bitPos) + (b1 >> 32 - bitPos & (bitPos != 0 ? 0xFFFFFFFF : 0));

            return result;
        }

        // ---- Build lookup table from code widths ----
        bool BuildTable(ushort[] widths, int tabSize, int maxWidth, ushort[] symbols)
        {
            // Sort symbols by width using counting sort
            int maxCodePos  = 1 << maxWidth;
            var sortedSyms  = new int[tabSize + 2];
            var sortedFreqs = new int[tabSize + 2];
            var count       = 0;

            // Collect non-zero widths
            for(var i = 0; i <= tabSize; i++)
            {
                if(widths[i] > 0)
                {
                    sortedSyms[count]  = i;
                    sortedFreqs[count] = widths[i];
                    count++;
                }
            }

            // Sort by width (ascending), stable
            for(var i = 0; i < count - 1; i++)
            {
                for(int j = i + 1; j < count; j++)
                {
                    if(sortedFreqs[j] < sortedFreqs[i])
                    {
                        (sortedSyms[i], sortedSyms[j])   = (sortedSyms[j], sortedSyms[i]);
                        (sortedFreqs[i], sortedFreqs[j]) = (sortedFreqs[j], sortedFreqs[i]);
                    }
                }
            }

            if(count == 0)
            {
                // No symbols — fill with 0
                Array.Clear(symbols, 0, Math.Min(symbols.Length, maxCodePos));

                return true;
            }

            if(count == 1)
            {
                // Single symbol — give it width 1
                widths[sortedSyms[0]] = 1;
                sortedFreqs[0]        = 1;
            }

            var codePos = 0;

            for(int i = count - 1; i >= 0 && codePos < maxCodePos; i--)
            {
                int numCodes = 1 << maxWidth - sortedFreqs[i];

                if(codePos + numCodes > maxCodePos) return false;

                var sym = (ushort)sortedSyms[i];

                for(var j = 0; j < numCodes && codePos < maxCodePos; j++) symbols[codePos++] = sym;
            }

            return true;
        }

        // ---- Read Huffman widths from bitstream ----
        bool ReadWidths(ushort[] widths, ushort[] symbols, int maxWidth, int maxSize)
        {
            Array.Clear(widths,  0, maxSize + 1);
            Array.Clear(symbols, 0, 1 << maxWidth);

            uint numWidths = ReadBits(9);

            if(numWidths > (uint)maxSize) numWidths = (uint)maxSize;

            uint lowerWidth = ReadBits(4);
            uint upperWidth = ReadBits(4);

            var precodeWidths  = new ushort[COMMENT_MAX_PRECODE + 1];
            var precodeSymbols = new ushort[1 << COMMENT_MAX_PRECODE_WIDTH];

            for(uint i = 0; i <= upperWidth; i++) precodeWidths[i] = (ushort)ReadBits(3);

            if(!BuildTable(precodeWidths, (int)upperWidth, COMMENT_MAX_PRECODE_WIDTH, precodeSymbols)) return false;

            uint widthPos = 0;

            while(widthPos <= numWidths)
            {
                uint sym = precodeSymbols[code >> 32 - COMMENT_MAX_PRECODE_WIDTH];
                ReadBits(precodeWidths[sym]);

                if(sym < upperWidth)
                    widths[widthPos++] = (ushort)sym;
                else
                {
                    // Run of zeros
                    uint runLen = ReadBits(4) + 4;

                    while(runLen-- > 0 && widthPos <= numWidths) widths[widthPos++] = 0;
                }
            }

            // Delta-decode widths
            if(upperWidth > 0)
            {
                for(uint i = 1; i <= numWidths; i++) widths[i] = (ushort)((widths[i] + widths[i - 1]) % upperWidth);
            }

            // Add lower width offset to non-zero entries
            for(uint i = 0; i <= numWidths; i++)
            {
                if(widths[i] > 0) widths[i] += (ushort)lowerWidth;
            }

            return BuildTable(widths, (int)numWidths, maxWidth, symbols);
        }

        // ---- Decompress comment using LZP + Huffman ----
        var mainWidths  = new ushort[COMMENT_MAX_MAIN_CODE         + 2];
        var mainSymbols = new ushort[(1 << COMMENT_MAX_CODE_WIDTH) + 1];

        // First 15 bits = decompressed length
        var commentLen = (int)ReadBits(15);

        if(commentLen <= 0 || commentLen > 32768) return null;

        if(!ReadWidths(mainWidths, mainSymbols, COMMENT_MAX_CODE_WIDTH, COMMENT_MAX_MAIN_CODE)) return null;

        var output  = new byte[commentLen];
        var outPos  = 0;
        var hashTab = new short[256 + 255 + 1]; // hash(a, b) = a + b, max = 255 + 255 = 510

        while(outPos < commentLen)
        {
            var matchPos = 0;

            if(outPos > 1)
            {
                int hashValue = output[outPos - 1] + output[outPos - 2];
                matchPos           = hashTab[hashValue];
                hashTab[hashValue] = (short)outPos;
            }

            uint sym = mainSymbols[code >> 32 - COMMENT_MAX_CODE_WIDTH];
            ReadBits(mainWidths[sym]);

            if(sym <= 255)
                output[outPos++] = (byte)sym;
            else
            {
                var copyLen = (int)(sym - 256 + 2);

                for(var i = 0; i < copyLen && outPos < commentLen; i++)
                    output[outPos++] = matchPos + i < output.Length ? output[matchPos + i] : (byte)0;
            }
        }

        // Convert to string — ACE comments are typically OEM (CP850) encoded
        // but may contain arbitrary bytes. Use Latin1 as a safe fallback.
        return Encoding.Latin1.GetString(output, 0, commentLen).TrimEnd('\0');
    }
}