// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DxpLh1.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     LH1 (LZHUF) decompressor used by Disk eXPress (DXP) 1.x images.
//
//     Based on the work of Michal Necasek (fdimg) which itself is adapted
//     from LZHUF by Haruyasu Yoshizaki (1988), as published in the English
//     port edited by Kenji Rikitake. The algorithm is equivalent to the
//     -lh1- compression method of LHA 1.x.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

namespace Aaru.Images;

/// <summary>DXP 1.x LH1 (LZHUF / -lh1-) decompressor. Buffer based, single-shot.</summary>
sealed class Lh1
{
    // LZSS Parameters.
    const int N         = 4096;
    const int F         = 60;
    const int THRESHOLD = 2;

    // Huffman coding parameters.
    const int N_CHAR   = 256 - THRESHOLD + F;
    const int T        = N_CHAR * 2      - 1;
    const int R        = T               - 1;
    const int MAX_FREQ = 0x8000;

    // Tables for decoding upper 6 bits of sliding dictionary pointer.
    static readonly byte[] _dCode =
    [
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0x01, 0x01,
        0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
        0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x05, 0x05,
        0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x09, 0x09, 0x09, 0x09, 0x09, 0x09,
        0x09, 0x09, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0A, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B, 0x0B,
        0x0C, 0x0C, 0x0C, 0x0C, 0x0D, 0x0D, 0x0D, 0x0D, 0x0E, 0x0E, 0x0E, 0x0E, 0x0F, 0x0F, 0x0F, 0x0F, 0x10, 0x10,
        0x10, 0x10, 0x11, 0x11, 0x11, 0x11, 0x12, 0x12, 0x12, 0x12, 0x13, 0x13, 0x13, 0x13, 0x14, 0x14, 0x14, 0x14,
        0x15, 0x15, 0x15, 0x15, 0x16, 0x16, 0x16, 0x16, 0x17, 0x17, 0x17, 0x17, 0x18, 0x18, 0x19, 0x19, 0x1A, 0x1A,
        0x1B, 0x1B, 0x1C, 0x1C, 0x1D, 0x1D, 0x1E, 0x1E, 0x1F, 0x1F, 0x20, 0x20, 0x21, 0x21, 0x22, 0x22, 0x23, 0x23,
        0x24, 0x24, 0x25, 0x25, 0x26, 0x26, 0x27, 0x27, 0x28, 0x28, 0x29, 0x29, 0x2A, 0x2A, 0x2B, 0x2B, 0x2C, 0x2C,
        0x2D, 0x2D, 0x2E, 0x2E, 0x2F, 0x2F, 0x30, 0x31, 0x32, 0x33, 0x34, 0x35, 0x36, 0x37, 0x38, 0x39, 0x3A, 0x3B,
        0x3C, 0x3D, 0x3E, 0x3F
    ];

    static readonly byte[] _dLen =
    [
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03,
        0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04,
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
        0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
        0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
        0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
        0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
        0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
        0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
        0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
        0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        0x08, 0x08, 0x08, 0x08
    ];
    readonly ushort[] _freq = new ushort[T + 1];
    readonly int[]    _prnt = new int[T    + N_CHAR];
    readonly short[]  _son  = new short[T];

    readonly byte[] _textBuf = new byte[N + F - 1];

    ushort _getbuf;
    byte   _getlen;

    byte[] _in;
    int    _inEnd;
    int    _inPos;

    byte[] _out;
    int    _outEnd;
    int    _outPos;

    /// <summary>Decompress a DXP LH1 block from <paramref name="src" /> into <paramref name="dst" />.</summary>
    /// <param name="src">Compressed track data.</param>
    /// <param name="srcLength">Number of valid bytes in <paramref name="src" />.</param>
    /// <param name="dst">Uncompressed destination buffer.</param>
    /// <param name="dstLength">Expected uncompressed size.</param>
    /// <returns>Number of bytes written to <paramref name="dst" />. Should equal <paramref name="dstLength" /> on success.</returns>
    public int Decode(byte[] src, int srcLength, byte[] dst, int dstLength)
    {
        _in     = src;
        _inPos  = 0;
        _inEnd  = srcLength;
        _out    = dst;
        _outPos = 0;
        _outEnd = dstLength;
        _getbuf = 0;
        _getlen = 0;

        StartHuff();

        // Pre-initialize text buffer with spaces.
        for(var i = 0; i < N - F; i++) _textBuf[i] = (byte)' ';

        int r     = N - F;
        var count = 0;

        while(count < _outEnd)
        {
            int c = DecodeChar();

            if(c < 256)
            {
                WriteByte((byte)c);
                _textBuf[r++] =  (byte)c;
                r             &= N - 1;
                count++;
            }
            else
            {
                int i = r - DecodePosition() - 1 & N - 1;
                int j = c - 255 + THRESHOLD;

                for(var k = 0; k < j; k++)
                {
                    byte b = _textBuf[i + k & N - 1];
                    WriteByte(b);
                    _textBuf[r++] =  b;
                    r             &= N - 1;
                    count++;

                    if(count >= _outEnd) break;
                }
            }
        }

        return count;
    }

    int ReadByte()
    {
        if(_inPos < _inEnd) return _in[_inPos++];

        return -1;
    }

    void WriteByte(byte c)
    {
        if(_outPos < _outEnd) _out[_outPos++] = c;
    }

    int GetBit()
    {
        while(_getlen <= 8)
        {
            int b       = ReadByte();
            if(b < 0) b = 0;
            _getbuf |= (ushort)(b << 8 - _getlen);
            _getlen += 8;
        }

        bool msb = (_getbuf & 0x8000) != 0;
        _getbuf <<= 1;
        _getlen--;

        return msb ? 1 : 0;
    }

    int GetByte()
    {
        while(_getlen <= 8)
        {
            int b       = ReadByte();
            if(b < 0) b = 0;
            _getbuf |= (ushort)(b << 8 - _getlen);
            _getlen += 8;
        }

        int i = _getbuf;
        _getbuf <<= 8;
        _getlen -=  8;

        return (ushort)i >> 8;
    }

    void StartHuff()
    {
        int i;

        for(i = 0; i < N_CHAR; i++)
        {
            _freq[i]     = 1;
            _son[i]      = (short)(i + T);
            _prnt[i + T] = i;
        }

        i = 0;
        int j = N_CHAR;

        while(j <= R)
        {
            _freq[j] =  (ushort)(_freq[i] + _freq[i + 1]);
            _son[j]  =  (short)i;
            _prnt[i] =  _prnt[i + 1] = j;
            i        += 2;
            j++;
        }

        _freq[T] = 0xFFFF;
        _prnt[R] = 0;
    }

    void Reconst()
    {
        int i, j, k;

        j = 0;

        for(i = 0; i < T; i++)
        {
            if(_son[i] >= T)
            {
                _freq[j] = (ushort)((_freq[i] + 1) / 2);
                _son[j]  = _son[i];
                j++;
            }
        }

        for(i = 0, j = N_CHAR; j < T; i += 2, j++)
        {
            k = i + 1;
            ushort f = _freq[j] = (ushort)(_freq[i] + _freq[k]);

            for(k = j - 1; f < _freq[k]; k--) {}

            k++;

            int l = j - k;

            // Shift freq[k..j-1] right by one.
            for(int m = j; m > k; m--) _freq[m] = _freq[m - 1];
            _freq[k] = f;
            for(int m = j; m > k; m--) _son[m] = _son[m - 1];
            _son[k] = (short)i;

            // Silence 'unused' in Release builds.
            _ = l;
        }

        for(i = 0; i < T; i++)
        {
            k = _son[i];

            if(k >= T)
                _prnt[k] = i;
            else
                _prnt[k] = _prnt[k + 1] = i;
        }
    }

    void Update(int c)
    {
        if(_freq[R] == MAX_FREQ) Reconst();

        c = _prnt[c + T];

        do
        {
            int k = ++_freq[c];
            int l = c + 1;

            if(k > _freq[l])
            {
                while(k > _freq[++l]) {}

                l--;
                _freq[c] = _freq[l];
                _freq[l] = (ushort)k;

                int i = _son[c];
                _prnt[i] = l;
                if(i < T) _prnt[i + 1] = l;

                int jj = _son[l];
                _son[l]   = (short)i;
                _prnt[jj] = c;
                if(jj < T) _prnt[jj + 1] = c;
                _son[c] = (short)jj;

                c = l;
            }

            c = _prnt[c];
        } while(c != 0);
    }

    int DecodeChar()
    {
        int c = _son[R];

        while(c < T)
        {
            c += GetBit();
            c =  _son[c];
        }

        c -= T;
        Update(c);

        return c;
    }

    int DecodePosition()
    {
        int i = GetByte();
        int c = _dCode[i] << 6;
        int j = _dLen[i];

        j -= 2;
        while(j-- != 0) i = (i << 1) + GetBit();

        return c | i & 0x3F;
    }
}