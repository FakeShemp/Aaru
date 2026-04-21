// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DxpLh5.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     LH5 decompressor used by Disk eXPress (DXP) 2.x images.
//
//     Based on the work of Michal Necasek (fdimg), which itself is adapted
//     from the ar002 archiver by Haruhiko Okumura (1990). The algorithm
//     matches the -lh5- compression method of LHA 2.x.
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

using System;

namespace Aaru.Images;

/// <summary>DXP 2.x LH5 (-lh5-) decompressor. Buffer based, single-shot.</summary>
sealed class Lh5
{
    const int DICBIT    = 13;
    const int DICSIZ    = 1 << DICBIT;
    const int MAXMATCH  = 256;
    const int THRESHOLD = 3;
    const int UCHAR_MAX = 255;

    const int NC        = UCHAR_MAX + MAXMATCH + 2 - THRESHOLD; // 510
    const int CBIT      = 9;
    const int CODE_BIT  = 16;
    const int BITBUFSIZ = 16;

    const    int      NP      = DICBIT   + 1; // 14
    const    int      NT      = CODE_BIT + 3; // 19
    const    int      PBIT    = 4;
    const    int      TBIT    = 5;
    const    int      NPT     = NT;
    readonly byte[]   _cLen   = new byte[NC];
    readonly ushort[] _cTable = new ushort[4096];

    // Tables.
    readonly ushort[] _left    = new ushort[2 * NC - 1];
    readonly byte[]   _ptLen   = new byte[NPT];
    readonly ushort[] _ptTable = new ushort[256];
    readonly ushort[] _right   = new ushort[2 * NC - 1];
    ushort            _bitbuf;
    int               _bitcount;
    int               _blocksize;
    long              _compsize;

    bool _decodeError;
    int  _i;

    // Cross-call state inside the main decode loop.
    int _j;

    // I/O state.
    byte[] _src;
    int    _srcEnd;
    int    _srcPos;
    int    _subbitbuf;

    /// <summary>Decompress a DXP LH5 block from <paramref name="src" /> into <paramref name="dst" />.</summary>
    /// <param name="src">Compressed track data.</param>
    /// <param name="srcLength">Number of valid bytes in <paramref name="src" />.</param>
    /// <param name="dst">Uncompressed destination buffer.</param>
    /// <param name="dstLength">Expected uncompressed size.</param>
    /// <returns>Number of bytes written to <paramref name="dst" />. Should equal <paramref name="dstLength" /> on success.</returns>
    public int Decode(byte[] src, int srcLength, byte[] dst, int dstLength)
    {
        _src         = src;
        _srcPos      = 0;
        _srcEnd      = srcLength;
        _compsize    = srcLength;
        _decodeError = false;

        DecodeStart();

        var buffer    = new byte[DICSIZ];
        int remaining = dstLength;
        var dstPos    = 0;

        while(remaining > 0)
        {
            int n = remaining > DICSIZ ? DICSIZ : remaining;
            DecodeChunk(n, buffer);

            if(_decodeError) return dstPos;

            Buffer.BlockCopy(buffer, 0, dst, dstPos, n);
            dstPos    += n;
            remaining -= n;
        }

        return dstPos;
    }

#region Table builder

    void MakeTable(int nchar, byte[] bitlen, int tablebits, ushort[] table)
    {
        var count  = new ushort[17];
        var weight = new ushort[17];
        var start  = new int[18];

        for(var i = 1; i <= 16; i++) count[i] = 0;
        for(var i = 0; i < nchar; i++) count[bitlen[i]]++;

        start[1] = 0;
        for(var i = 1; i <= 16; i++) start[i + 1] = start[i] + (count[i] << 16 - i);

        if(start[17] != 1 << 16)
        {
            _decodeError = true;

            return;
        }

        int jutbits = 16 - tablebits;

        {
            int i;

            for(i = 1; i <= tablebits; i++)
            {
                start[i]  >>= jutbits;
                weight[i] =   (ushort)(1 << tablebits - i);
            }

            while(i <= 16) weight[i++] = (ushort)(1 << 16 - i);
        }

        {
            int i = start[tablebits + 1] >> jutbits;

            if(i != 1 << 16)
            {
                int k                    = 1 << tablebits;
                while(i != k) table[i++] = 0;
            }
        }

        int  avail = nchar;
        uint mask  = 1U << 15 - tablebits;

        for(var ch = 0; ch < nchar; ch++)
        {
            int len = bitlen[ch];

            if(len == 0) continue;

            int nextcode = start[len] + weight[len];

            if(len <= tablebits)
                for(int i = start[len]; i < nextcode; i++)
                    table[i] = (ushort)ch;
            else
            {
                int k        = start[len];
                int pi       = k >> jutbits; // index into table[]
                var useRight = 0;            // 0 = table, 1 = left, 2 = right
                int pIdx     = pi;
                int i        = len - tablebits;

                while(i != 0)
                {
                    // Follow existing chain of left/right, allocating new nodes as needed.
                    ushort node = useRight switch
                                  {
                                      0 => table[pIdx],
                                      1 => _left[pIdx],
                                      _ => _right[pIdx]
                                  };

                    if(node == 0)
                    {
                        _right[avail] = 0;
                        _left[avail]  = 0;
                        node          = (ushort)avail++;

                        switch(useRight)
                        {
                            case 0:
                                table[pIdx] = node;

                                break;
                            case 1:
                                _left[pIdx] = node;

                                break;
                            default:
                                _right[pIdx] = node;

                                break;
                        }
                    }

                    if((k & mask) != 0)
                    {
                        pIdx     = node;
                        useRight = 2;
                    }
                    else
                    {
                        pIdx     = node;
                        useRight = 1;
                    }

                    k <<= 1;
                    i--;
                }

                // Store the character at the final node.
                switch(useRight)
                {
                    case 0:
                        table[pIdx] = (ushort)ch;

                        break;
                    case 1:
                        _left[pIdx] = (ushort)ch;

                        break;
                    default:
                        _right[pIdx] = (ushort)ch;

                        break;
                }
            }

            start[len] = nextcode;
        }
    }

#endregion

#region Bit reader

    int ReadByte()
    {
        if(_srcPos < _srcEnd) return _src[_srcPos++];

        return -1;
    }

    void FillBuf(int n)
    {
        _bitbuf = (ushort)(_bitbuf << n);

        while(n > _bitcount)
        {
            _bitbuf |= (ushort)(_subbitbuf << (n -= _bitcount));

            if(_compsize != 0)
            {
                _compsize--;
                int b = ReadByte();
                _subbitbuf = b < 0 ? 0 : b & 0xFF;
            }
            else
                _subbitbuf = 0;

            _bitcount = 8;
        }

        _bitbuf |= (ushort)(_subbitbuf >> (_bitcount -= n));
    }

    int GetBits(int n)
    {
        int x = _bitbuf >> BITBUFSIZ - n;
        FillBuf(n);

        return x;
    }

    void InitGetBits()
    {
        _bitbuf    = 0;
        _subbitbuf = 0;
        _bitcount  = 0;
        FillBuf(BITBUFSIZ);
    }

#endregion

#region Huffman decoder

    void ReadPtLen(int nn, int nbit, int iSpecial)
    {
        int n = GetBits(nbit);

        if(n == 0)
        {
            int c                                    = GetBits(nbit);
            for(var i = 0; i < nn; i++) _ptLen[i]    = 0;
            for(var i = 0; i < 256; i++) _ptTable[i] = (ushort)c;
        }
        else
        {
            var i = 0;

            while(i < n)
            {
                int c = _bitbuf >> BITBUFSIZ - 3;

                if(c == 7)
                {
                    uint mask = 1U << BITBUFSIZ - 1 - 3;

                    while((mask & _bitbuf) != 0)
                    {
                        mask >>= 1;
                        c++;
                    }
                }

                FillBuf(c < 7 ? 3 : c - 3);
                _ptLen[i++] = (byte)c;

                if(i == iSpecial)
                {
                    c = GetBits(2);
                    while(--c >= 0) _ptLen[i++] = 0;
                }
            }

            while(i < nn) _ptLen[i++] = 0;
            MakeTable(nn, _ptLen, 8, _ptTable);
        }
    }

    void ReadCLen()
    {
        int n = GetBits(CBIT);

        if(n == 0)
        {
            int c                                    = GetBits(CBIT);
            for(var i = 0; i < NC; i++) _cLen[i]     = 0;
            for(var i = 0; i < 4096; i++) _cTable[i] = (ushort)c;
        }
        else
        {
            var i = 0;

            while(i < n)
            {
                int c = _ptTable[_bitbuf >> BITBUFSIZ - 8];

                if(c >= NT)
                {
                    uint mask = 1U << BITBUFSIZ - 1 - 8;

                    do
                    {
                        c    =   (_bitbuf & mask) != 0 ? _right[c] : _left[c];
                        mask >>= 1;
                    } while(c >= NT);
                }

                FillBuf(_ptLen[c]);

                if(c <= 2)
                {
                    if(c == 0)
                        c = 1;
                    else if(c == 1)
                        c = GetBits(4) + 3;
                    else
                        c = GetBits(CBIT) + 20;

                    while(--c >= 0) _cLen[i++] = 0;
                }
                else
                    _cLen[i++] = (byte)(c - 2);
            }

            while(i < NC) _cLen[i++] = 0;
            MakeTable(NC, _cLen, 12, _cTable);
        }
    }

    int DecodeC()
    {
        if(_blocksize == 0)
        {
            _blocksize = GetBits(16);
            ReadPtLen(NT, TBIT, 3);
            ReadCLen();
            ReadPtLen(NP, PBIT, -1);
        }

        _blocksize--;
        int j = _cTable[_bitbuf >> BITBUFSIZ - 12];

        if(j >= NC)
        {
            uint mask = 1U << BITBUFSIZ - 1 - 12;

            do
            {
                j    =   (_bitbuf & mask) != 0 ? _right[j] : _left[j];
                mask >>= 1;
            } while(j >= NC);
        }

        FillBuf(_cLen[j]);

        return j;
    }

    int DecodeP()
    {
        int j = _ptTable[_bitbuf >> BITBUFSIZ - 8];

        if(j >= NP)
        {
            uint mask = 1U << BITBUFSIZ - 1 - 8;

            do
            {
                j    =   (_bitbuf & mask) != 0 ? _right[j] : _left[j];
                mask >>= 1;
            } while(j >= NP);
        }

        FillBuf(_ptLen[j]);

        if(j != 0) j = (1 << j - 1) + GetBits(j - 1);

        return j;
    }

    void HufDecodeStart()
    {
        InitGetBits();
        _blocksize = 0;
    }

    void DecodeStart()
    {
        HufDecodeStart();
        _j = 0;
        _i = 0;
    }

    /// <summary>Decode up to <paramref name="count" /> bytes into the sliding dictionary <paramref name="buffer" />.</summary>
    void DecodeChunk(int count, byte[] buffer)
    {
        var r = 0;

        while(--_j >= 0)
        {
            buffer[r] = buffer[_i];
            _i        = _i + 1 & DICSIZ - 1;

            if(++r == count) return;
        }

        while(true)
        {
            int c = DecodeC();

            if(_decodeError) return;

            if(c <= UCHAR_MAX)
            {
                buffer[r] = (byte)c;

                if(++r == count) return;
            }
            else
            {
                _j = c - (UCHAR_MAX + 1 - THRESHOLD);
                _i = r - DecodeP() - 1 & DICSIZ - 1;

                if(_decodeError) return;

                while(--_j >= 0)
                {
                    buffer[r] = buffer[_i];
                    _i        = _i + 1 & DICSIZ - 1;

                    if(++r == count) return;
                }
            }
        }
    }

#endregion
}