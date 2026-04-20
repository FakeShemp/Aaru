// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Lzmw.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Compression algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Decompresses LZMW (Lempel-Ziv-Miller-Wegman) as used by IBM's SaveDskF
//     compressed ('Z' variant) disk images.
//
//     Based on code from fdimg (img_dskf / dsk_lzmw) by Michal Necasek,
//     Copyright (c) 2013-2026 Michal Necasek, distributed under the MIT
//     license. Translated from C to C# for the Aaru Data Preservation Suite.
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
// Copyright © 2013-2026 Michal Necasek (original C implementation)
// ****************************************************************************/

using System;

namespace Aaru.Compression;

/// <summary>
///     Implements LZMW (Lempel-Ziv-Miller-Wegman) decompression as used by IBM's SaveDskF compressed disk images.
///     Based on the reference C implementation in fdimg (<c>dsk_lzmw.c</c>) by Michal Necasek.
/// </summary>
public static class Lzmw
{
    const int DICT_SIZE = 4096;

    /// <summary>Set to <c>true</c> if this algorithm is supported, <c>false</c> otherwise.</summary>
    public static bool IsSupported => true;

    /// <summary>Decodes a buffer compressed with LZMW as used by SaveDskF.</summary>
    /// <param name="source">Encoded buffer.</param>
    /// <param name="destination">Buffer where to write the decoded data.</param>
    /// <returns>The number of decoded bytes, or <c>-1</c> on decompression failure.</returns>
    public static int DecodeBuffer(byte[] source, byte[] destination)
    {
        if(source == null || destination == null) return -1;

        var dict = new DictEntry[DICT_SIZE];

        // Initialize the dictionary (equivalent to lzmw_dict_init).
        dict[0].Str      = null;
        dict[0].Len      = 0;
        dict[0].Next     = 0;
        dict[0].Prev     = 0;
        dict[0].Ancestor = 0;

        for(var i = 1; i <= 256; i++)
        {
            dict[i].Str      = new byte[1];
            dict[i].Str[0]   = (byte)(i - 1);
            dict[i].Len      = 1;
            dict[i].Next     = -1;
            dict[i].Prev     = 0;
            dict[i].Ancestor = 0;
        }

        for(var i = 257; i < DICT_SIZE - 1; i++) dict[i].Next = (short)(i + 1);

        dict[DICT_SIZE - 1].Next = -1;

        var avail   = 257;
        var readTwo = true;
        var nibble  = 0;
        var inPos   = 0;
        var outPos  = 0;

        // The first code is output but not added to the dictionary.
        int prevCode = ReadNextCode(source, ref inPos, ref readTwo, ref nibble);

        if(prevCode < 0) return -1;

        int produced = DictOut(dict, prevCode, destination, outPos);

        if(produced < 0) return -1;

        outPos += produced;

        // Process the remaining codes.
        while(true)
        {
            int code = ReadNextCode(source, ref inPos, ref readTwo, ref nibble);

            if(code < 0) break;  // End of input.
            if(code == 0) break; // End marker.

            if(!DictAdd(dict, ref avail, code, prevCode)) return -1;

            produced = DictOut(dict, code, destination, outPos);

            if(produced < 0) return -1;

            prevCode =  code;
            outPos   += produced;
        }

        return outPos;
    }

    /// <summary>Read the next 12-bit code from the compressed stream.</summary>
    static int ReadNextCode(byte[] src, ref int pos, ref bool readTwo, ref int nibble)
    {
        int code;

        if(readTwo)
        {
            // Read the first 16 bits of a code tuple (big-endian in the stream).
            if(pos + 2 > src.Length) return -1;

            int tw = src[pos] << 8 | src[pos + 1];
            pos    += 2;
            nibble =  tw & 0x0F;
            code   =  tw >> 4;
        }
        else
        {
            // Read the final 8 bits of a code tuple.
            if(pos + 1 > src.Length) return -1;

            int tb = src[pos++];
            code = nibble << 8 | tb;
        }

        readTwo = !readTwo;

        return code;
    }

    /// <summary>Emit the string associated with <paramref name="code" /> to the destination buffer.</summary>
    static int DictOut(DictEntry[] dict, int code, byte[] dst, int dstPos)
    {
        if(code is <= 0 or >= DICT_SIZE) return -1;

        byte[] str = dict[code].Str;
        int    len = dict[code].Len;

        if(str == null || len == 0) return -1;

        if(dstPos + len > dst.Length) return -1;

        Buffer.BlockCopy(str, 0, dst, dstPos, len);

        return len;
    }

    /// <summary>Raise the reference count for a dictionary entry.</summary>
    static void DictAddRef(DictEntry[] dict, int entry)
    {
        if(entry == 0) return;

        // For (Next < 0), reference count is -Next.
        if(dict[entry].Next < 0)
        {
            // Increase reference count.
            dict[entry].Next--;
        }
        else
        {
            // Remove entry from the unreferenced (LRU) list.
            int k = dict[entry].Next;
            int l = dict[entry].Prev;
            dict[k].Prev     = (short)l;
            dict[l].Next     = (short)k;
            dict[entry].Next = -1; // Set reference count to one.
        }
    }

    /// <summary>Move an entry to the unreferenced (LRU) queue.</summary>
    static void DictMoveToUnref(DictEntry[] dict, int entry)
    {
        int k = dict[0].Prev;
        dict[entry].Prev = (short)k;
        dict[k].Next     = (short)entry;
        dict[entry].Next = 0;
        dict[0].Prev     = (short)entry;
    }

    /// <summary>Lower the reference count for a dictionary entry.</summary>
    static void DictRemoveRef(DictEntry[] dict, int entry)
    {
        if(entry == 0) return;

        // Decrease reference count.
        dict[entry].Next++;

        // If no longer referenced, move to the LRU queue.
        if(dict[entry].Next == 0) DictMoveToUnref(dict, entry);
    }

    /// <summary>Obtain the next available slot in the dictionary.</summary>
    static int DictGetAvailSlot(DictEntry[] dict, ref int avail)
    {
        int k;

        if(avail != -1)
        {
            // An unused entry is available.
            k     = avail;
            avail = dict[k].Next;
        }
        else
        {
            // Recycle the least recently used entry.
            if(dict[0].Prev == dict[0].Next) return -1;

            k = dict[0].Next;
            int l = dict[k].Next;

            if(k <= 256 || l <= 256) return -1;

            dict[0].Next = (short)l;
            dict[l].Prev = 0;
            DictRemoveRef(dict, dict[k].Ancestor);
            dict[k].Str = null;
        }

        return k;
    }

    /// <summary>Add a new dictionary entry formed by <paramref name="prevCode" /> + first byte of <paramref name="code" />.</summary>
    static bool DictAdd(DictEntry[] dict, ref int avail, int code, int prevCode)
    {
        if(code is 0 or >= DICT_SIZE) return false;
        if(prevCode is 0 or >= DICT_SIZE) return false;

        int j = DictGetAvailSlot(dict, ref avail);

        if(j is <= 256 or >= DICT_SIZE) return false;

        int pLen = dict[prevCode].Len;

        if(pLen == 0) return false;

        int srcCode = code;

        // The "cScSc" case: the newly-allocated slot is the same as the current code.
        if(j == srcCode) srcCode = prevCode;

        if(dict[srcCode].Str == null || dict[srcCode].Len == 0) return false;

        dict[j].Len = pLen + 1;
        dict[j].Str = new byte[dict[j].Len];
        DictAddRef(dict, prevCode);
        DictMoveToUnref(dict, j);
        dict[j].Ancestor = (short)prevCode;
        Buffer.BlockCopy(dict[prevCode].Str, 0, dict[j].Str, 0, pLen);
        dict[j].Str[pLen] = dict[srcCode].Str[0];

        return true;
    }

    struct DictEntry
    {
        public byte[] Str;      // String associated with this code.
        public int    Len;      // Length of the string.
        public short  Next;     // Next LRU queue entry (or negated refcount when < 0).
        public short  Prev;     // Previous LRU queue entry.
        public short  Ancestor; // Parent entry (sans last character).
    }
}