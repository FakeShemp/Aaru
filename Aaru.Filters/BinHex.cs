// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BinHex.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Filters.
//
// --[ Description ] ----------------------------------------------------------
//
//     Provides a filter to open BinHex 4.0 (.hqx) encoded Macintosh files.
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Helpers.IO;
using Aaru.Logging;
using Sentry;

namespace Aaru.Filters;

/// <inheritdoc />
/// <summary>Decodes BinHex 4.0 files (Yves Lempereur's 6-bit + 0x90 RLE text format).</summary>
public sealed class BinHex : IFilter
{
    const string MODULE_NAME = "BinHex Filter";

    /// <summary>Decoded-size threshold above which the forks are served via a streaming decoder instead of a memory buffer.</summary>
    const long STREAMING_THRESHOLD = 256L * 1024 * 1024;

    /// <summary>Banner marking a BinHex 4.0 file; must appear before the start-of-data colon.</summary>
    const string BANNER = "(This file must be converted with BinHex";

    /// <summary>Standard BinHex 4.0 6-bit alphabet (64 chars).</summary>
    const string ALPHABET = "!\"#$%&'()*+,-012345689@ABCDEFGHIJKLMNPQRSTUVXYZ[`abcdefhijklmpqr";

    /// <summary>RLE marker byte within the decoded 6-bit stream.</summary>
    const byte RLE_MARKER = 0x90;

    /// <summary>Fixed portion of the normalized header after the Pascal name.</summary>
    /// <remarks>null separator (1) + type (4) + creator (4) + flags (2) + dataLen (4) + rsrcLen (4) + CRC (2).</remarks>
    const int HEADER_FIXED_SIZE = 1 + 4 + 4 + 2 + 4 + 4 + 2;

    static readonly sbyte[] _decodeTable = BuildDecodeTable();
    long                    _bodyStart;
    long                    _dataForkOff;
    BinHexDecodeStream      _dataForkStream;

    byte[]             _decoded;
    Header             _header;
    bool               _isBytes;
    long               _rsrcForkOff;
    BinHexDecodeStream _rsrcForkStream;
    byte[]             _sourceBytes;
    Stream             _sourceStream;
    bool               _streamingMode;

    static sbyte[] BuildDecodeTable()
    {
        var table = new sbyte[256];

        for(var i = 0; i < 256; i++) table[i] = -1;

        for(var i = 0; i < ALPHABET.Length; i++) table[(byte)ALPHABET[i]] = (sbyte)i;

        return table;
    }

    Stream OpenSource() => _isBytes ? new MemoryStream(_sourceBytes, false) : _sourceStream;

    /// <summary>Locate the banner line and advance to the first byte of the 6-bit body (the ':' marker).</summary>
    /// <returns>Offset of ':' within <paramref name="buf" />, or -1 on failure.</returns>
    static int FindBodyStart(byte[] buf)
    {
        int bannerOff = IndexOfAscii(buf, buf.Length, BANNER);

        if(bannerOff < 0) return -1;

        int p = bannerOff + BANNER.Length;

        while(p < buf.Length && buf[p] != '\n' && buf[p] != '\r') p++;

        while(p < buf.Length && (buf[p] == '\n' || buf[p] == '\r' || buf[p] == '\t' || buf[p] == ' ')) p++;

        if(p >= buf.Length || buf[p] != ':') return -1;

        return p;
    }

    /// <summary>Shared Identify path: locate the banner, skip to the body, decode the header and validate its CRC.</summary>
    static bool TryIdentifyBuffer(byte[] probe)
    {
        int bodyStart = FindBodyStart(probe);

        if(bodyStart < 0) return false;

        MemoryStream src = new(probe, bodyStart, probe.Length - bodyStart, false);

        var headerBuf = new byte[1 + 63 + HEADER_FIXED_SIZE];

        int got;

        try
        {
            SixBitRleDecoder dec = new(src);

            got = dec.ReadAtMost(headerBuf, 0, headerBuf.Length);
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);
            AaruLogging.Exception(ex, "BinHex 4.0: identify decode threw: {0}", ex);

            return false;
        }

        Array.Resize(ref headerBuf, got);

        return TryParseHeader(headerBuf, 0, out _);
    }

    ErrorNumber OpenInternal(Stream source, long sourceLength)
    {
        source.Seek(0, SeekOrigin.Begin);

        var probe = new byte[Math.Min(sourceLength, 16384)];
        source.EnsureRead(probe, 0, probe.Length);

        int bodyStart = FindBodyStart(probe);

        if(bodyStart < 0) return ErrorNumber.InvalidArgument;

        _bodyStart = bodyStart;

        source.Seek(_bodyStart, SeekOrigin.Begin);

        // Decode the maximum-size header (up to 1 + 63 + fixed) to learn fork sizes.
        var headerBuf = new byte[1 + 63 + HEADER_FIXED_SIZE];

        int headerBytes;

        try
        {
            SixBitRleDecoder headerDec = new(new NonDisposingWrapper(source));

            headerBytes = headerDec.ReadAtMost(headerBuf, 0, headerBuf.Length);
        }
        catch(Exception ex)
        {
            AaruLogging.Debug(MODULE_NAME, "BinHex 4.0: header decode threw: {0}", ex);

            return ErrorNumber.InvalidArgument;
        }

        Array.Resize(ref headerBuf, headerBytes);

        if(!TryParseHeader(headerBuf, sourceLength * 4, out _header)) return ErrorNumber.InvalidArgument;

        _dataForkOff = _header.HeaderBytes;
        _rsrcForkOff = _dataForkOff + _header.DataLength + 2; // + data-fork CRC

        Filename = _header.Filename;

        long totalDecoded = _header.HeaderBytes + _header.DataLength + 2 + _header.ResourceLength + 2;

        _streamingMode = totalDecoded > STREAMING_THRESHOLD;

        if(_streamingMode) return ErrorNumber.NoError;

        // Decode the entire body in one shot.
        source.Seek(_bodyStart, SeekOrigin.Begin);

        SixBitRleDecoder dec = new(new NonDisposingWrapper(source));

        _decoded = new byte[totalDecoded];

        int total = dec.ReadAtMost(_decoded, 0, _decoded.Length);

        if(total < totalDecoded)
        {
            AaruLogging.Debug(MODULE_NAME, "BinHex 4.0: decoded body truncated ({0}/{1} bytes).", total, totalDecoded);
            Array.Resize(ref _decoded, total);
        }

        VerifyForkCrcs();

        return ErrorNumber.NoError;
    }

    void VerifyForkCrcs()
    {
        if(_decoded == null) return;

        long dOff = _dataForkOff;
        long dLen = _header.DataLength;

        if(_decoded.LongLength >= dOff + dLen + 2)
        {
            ushort stored   = ReadUInt16Be(_decoded, (int)(dOff + dLen));
            ushort computed = Crc16Ccitt(_decoded, (int)dOff, (int)dLen);
            computed = Crc16Ccitt(new byte[2], 0, 2, computed);

            if(stored != computed)
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "BinHex 4.0: data fork CRC mismatch (stored 0x{0:X4}, computed 0x{1:X4}).",
                                  stored,
                                  computed);
            }
        }

        long rOff = _rsrcForkOff;
        long rLen = _header.ResourceLength;

        if(rLen == 0 || _decoded.LongLength < rOff + rLen + 2) return;

        ushort storedR   = ReadUInt16Be(_decoded, (int)(rOff + rLen));
        ushort computedR = Crc16Ccitt(_decoded, (int)rOff, (int)rLen);
        computedR = Crc16Ccitt(new byte[2], 0, 2, computedR);

        if(storedR != computedR)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "BinHex 4.0: resource fork CRC mismatch (stored 0x{0:X4}, computed 0x{1:X4}).",
                              storedR,
                              computedR);
        }
    }

#region Nested type: Header

    struct Header
    {
        public string Filename;
        public uint   Type;
        public uint   Creator;
        public ushort FinderFlags;
        public uint   DataLength;
        public uint   ResourceLength;
        public int    HeaderBytes;
    }

#endregion

#region Nested type: SixBitRleDecoder

    /// <summary>Stateful BinHex 4.0 decoder: 6-bit alphabet → 3-byte groups with 0x90 RLE expansion.</summary>
    sealed class SixBitRleDecoder(Stream input)
    {
        bool            _eof;
        byte            _lastByte;
        int             _phase;
        int             _prevBits;
        int             _rleRemaining;

        /// <summary>Attempt to read up to <paramref name="count" /> decoded bytes. Returns the number actually produced.</summary>
        public int ReadAtMost(byte[] buffer, int offset, int count)
        {
            var produced = 0;

            while(produced < count)
            {
                if(!TryProduceOne(out byte b)) break;

                buffer[offset + produced++] = b;
            }

            return produced;
        }

        bool TryProduceOne(out byte b)
        {
            if(_rleRemaining > 0)
            {
                _rleRemaining--;
                b = _lastByte;

                return true;
            }

            if(!TryDecodeByte(out byte decoded))
            {
                b = 0;

                return false;
            }

            if(decoded != RLE_MARKER)
            {
                _lastByte = decoded;
                b         = decoded;

                return true;
            }

            if(!TryDecodeByte(out byte count))
            {
                b = 0;

                return false;
            }

            switch(count)
            {
                case 0:
                    _lastByte = RLE_MARKER;
                    b         = RLE_MARKER;

                    return true;

                case 1:
                    // Invalid per spec; emit last byte and continue.
                    b = _lastByte;

                    return true;

                default:
                    _rleRemaining = count - 2;
                    b             = _lastByte;

                    return true;
            }
        }

        bool TryDecodeByte(out byte result)
        {
            result = 0;

            int bits1, bits2;

            switch(_phase)
            {
                case 0:
                    if(!TryGetBits(out bits1)) return false;
                    if(!TryGetBits(out bits2)) return false;

                    _prevBits = bits2;
                    result    = (byte)(bits1 << 2 | bits2 >> 4);
                    _phase    = 1;

                    return true;

                case 1:
                    bits1 = _prevBits;

                    if(!TryGetBits(out bits2)) return false;

                    _prevBits = bits2;
                    result    = (byte)(bits1 << 4 | bits2 >> 2);
                    _phase    = 2;

                    return true;

                case 2:
                    bits1 = _prevBits;

                    if(!TryGetBits(out bits2)) return false;

                    result = (byte)(bits1 << 6 | bits2);
                    _phase = 0;

                    return true;

                default:
                    return false;
            }
        }

        bool TryGetBits(out int bits)
        {
            bits = 0;

            if(_eof) return false;

            for(;;)
            {
                int raw = input.ReadByte();

                switch(raw)
                {
                    case < 0:
                    case ':':
                        _eof = true;

                        return false;
                }

                sbyte mapped = _decodeTable[raw];

                if(mapped < 0) continue; // Skip CR/LF/whitespace and other non-alphabet bytes.

                bits = mapped;

                return true;
            }
        }
    }

#endregion

#region Nested type: NonDisposingWrapper

    /// <inheritdoc />
    /// <summary>Wraps a Stream so that Dispose()/Close() on the wrapper does not close the underlying stream.</summary>
    sealed class NonDisposingWrapper(Stream inner) : Stream
    {
        public override bool CanRead  => inner.CanRead;
        public override bool CanSeek  => inner.CanSeek;
        public override bool CanWrite => false;
        public override long Length   => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            // Intentionally do not dispose the inner stream.
        }
    }

#endregion

#region Nested type: BinHexDecodeStream

    /// <inheritdoc />
    /// <summary>Seek-capable stream that lazily decodes a BinHex 4.0 body fork with a 256 MiB slice cache.</summary>
    sealed class BinHexDecodeStream(Stream source, long bodyStart, long forkOffsetInBody, long forkLength) : Stream
    {
        const    int              SLICE_SHIFT     = 28; // 256 MiB
        const    int              SLICE_SIZE      = 1 << SLICE_SHIFT;
        const    int              RESIDENT_SLICES = 2;
        readonly LinkedList<long> _lru            = new();

        readonly Dictionary<long, byte[]> _residentSlices = new();
        readonly Dictionary<long, long>   _spillOffsets   = new();

        FileStream _spill;
        string     _spillPath;

        public override bool CanRead  => true;
        public override bool CanSeek  => true;
        public override bool CanWrite => false;
        public override long Length   => forkLength;

        public override long Position { get; set; }

        public override void Flush() {}

        public override int Read(byte[] buffer, int offset, int count)
        {
            if(Position >= forkLength) return 0;

            long remaining = forkLength - Position;

            if(count > remaining) count = (int)remaining;

            var produced = 0;

            while(produced < count)
            {
                long sliceIndex = Position / SLICE_SIZE;
                var  sliceOff   = (int)(Position % SLICE_SIZE);

                byte[] slice = GetSlice(sliceIndex);
                int    take  = Math.Min(count - produced, slice.Length - sliceOff);

                Buffer.BlockCopy(slice, sliceOff, buffer, offset + produced, take);

                produced += take;
                Position += take;
            }

            return produced;
        }

        byte[] GetSlice(long sliceIndex)
        {
            if(_residentSlices.TryGetValue(sliceIndex, out byte[] slice))
            {
                TouchLru(sliceIndex);

                return slice;
            }

            if(_spillOffsets.TryGetValue(sliceIndex, out long spillOff))
            {
                slice = new byte[GetSliceSize(sliceIndex)];
                _spill.Seek(spillOff, SeekOrigin.Begin);
                _spill.EnsureRead(slice, 0, slice.Length);
                InsertResident(sliceIndex, slice);

                return slice;
            }

            slice = DecodeSlice(sliceIndex);
            InsertResident(sliceIndex, slice);

            return slice;
        }

        int GetSliceSize(long sliceIndex)
        {
            long sliceStart = sliceIndex * SLICE_SIZE;
            long sliceEnd   = Math.Min(sliceStart + SLICE_SIZE, forkLength);

            return (int)(sliceEnd - sliceStart);
        }

        byte[] DecodeSlice(long sliceIndex)
        {
            long sliceStart = sliceIndex * SLICE_SIZE;
            int  sliceSize  = GetSliceSize(sliceIndex);

            // Always decode from the start of the body. The 256 MiB slice cache keeps re-decodes cheap in practice.
            source.Seek(bodyStart, SeekOrigin.Begin);

            SixBitRleDecoder dec = new(new NonDisposingWrapper(source));

            long skipTo  = forkOffsetInBody + sliceStart;
            var  discard = new byte[64 * 1024];

            long skipped = 0;

            while(skipped < skipTo)
            {
                var want = (int)Math.Min(discard.Length, skipTo - skipped);
                int got  = dec.ReadAtMost(discard, 0, want);

                if(got == 0) break;

                skipped += got;
            }

            var slice = new byte[sliceSize];
            var total = 0;

            while(total < sliceSize)
            {
                int got = dec.ReadAtMost(slice, total, sliceSize - total);

                if(got == 0) break;

                total += got;
            }

            if(total < sliceSize) Array.Resize(ref slice, total);

            return slice;
        }

        void InsertResident(long sliceIndex, byte[] slice)
        {
            _residentSlices[sliceIndex] = slice;
            _lru.AddFirst(sliceIndex);

            while(_lru.Count > RESIDENT_SLICES)
            {
                LinkedListNode<long> last = _lru.Last;

                if(last == null) break;

                long victim = last.Value;
                _lru.RemoveLast();

                if(_residentSlices.Remove(victim, out byte[] victimData)) SpillSlice(victim, victimData);
            }
        }

        void TouchLru(long sliceIndex)
        {
            _lru.Remove(sliceIndex);
            _lru.AddFirst(sliceIndex);
        }

        void SpillSlice(long sliceIndex, byte[] data)
        {
            if(_spillOffsets.ContainsKey(sliceIndex)) return;

            if(_spill == null)
            {
                _spillPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                                                    "aaru-binhex-" + Guid.NewGuid().ToString("N") + ".tmp");

                _spill = new FileStream(_spillPath,
                                        FileMode.CreateNew,
                                        FileAccess.ReadWrite,
                                        FileShare.None,
                                        65536,
                                        FileOptions.DeleteOnClose);
            }

            long off = _spill.Seek(0, SeekOrigin.End);
            _spill.Write(data, 0, data.Length);
            _spillOffsets[sliceIndex] = off;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            long newPos = origin switch
                          {
                              SeekOrigin.Begin   => offset,
                              SeekOrigin.Current => Position   + offset,
                              SeekOrigin.End     => forkLength + offset,
                              _                  => Position
                          };

            if(newPos < 0) newPos = 0;

            if(newPos > forkLength) newPos = forkLength;

            Position = newPos;

            return Position;
        }

        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        protected override void Dispose(bool disposing)
        {
            if(disposing)
            {
                _spill?.Close();

                if(_spillPath != null && File.Exists(_spillPath))
                {
                    try
                    {
                        File.Delete(_spillPath);
                    }
                    catch(IOException ex)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                          "BinHex 4.0: could not delete spill file {0}: {1}",
                                          _spillPath,
                                          ex.Message);
                    }
                }

                _residentSlices.Clear();
                _spillOffsets.Clear();
                _lru.Clear();
            }

            base.Dispose(disposing);
        }
    }

#endregion

#region IFilter Members

    /// <inheritdoc />
    public string Name => Localization.BinHex_Name;

    /// <inheritdoc />
    public Guid Id => new("A0E7D4F1-7C5B-4D2E-9B8A-2C1E7F3A4B5D");

    /// <inheritdoc />
    public string Author => Authors.NataliaPortillo;

    /// <inheritdoc />
    public string BasePath { get; private set; }

    /// <inheritdoc />
    public DateTime CreationTime { get; private set; }

    /// <inheritdoc />
    public long DataForkLength => _header.DataLength;

    /// <inheritdoc />
    public string Filename { get; private set; }

    /// <inheritdoc />
    public DateTime LastWriteTime { get; private set; }

    /// <inheritdoc />
    public long Length => _header.DataLength + _header.ResourceLength;

    /// <inheritdoc />
    public string ParentFolder => System.IO.Path.GetDirectoryName(BasePath);

    /// <inheritdoc />
    public string Path => BasePath;

    /// <inheritdoc />
    public long ResourceForkLength => _header.ResourceLength;

    /// <inheritdoc />
    public bool HasResourceFork => _header.ResourceLength > 0;

    /// <inheritdoc />
    public void Close()
    {
        _decoded     = null;
        _sourceBytes = null;
        _sourceStream?.Close();
        _sourceStream = null;
        _dataForkStream?.Dispose();
        _rsrcForkStream?.Dispose();
        _dataForkStream = null;
        _rsrcForkStream = null;
        _isBytes        = false;
        _streamingMode  = false;
    }

    /// <inheritdoc />
    public Stream GetDataForkStream()
    {
        if(_header.DataLength == 0) return null;

        if(!_streamingMode && _decoded != null)
            return new OffsetStream(_decoded, _dataForkOff, _dataForkOff + _header.DataLength - 1);

        _dataForkStream ??= new BinHexDecodeStream(OpenSource(), _bodyStart, _dataForkOff, _header.DataLength);

        _dataForkStream.Seek(0, SeekOrigin.Begin);

        return _dataForkStream;
    }

    /// <inheritdoc />
    public Stream GetResourceForkStream()
    {
        if(_header.ResourceLength == 0) return null;

        if(!_streamingMode && _decoded != null)
            return new OffsetStream(_decoded, _rsrcForkOff, _rsrcForkOff + _header.ResourceLength - 1);

        _rsrcForkStream ??= new BinHexDecodeStream(OpenSource(), _bodyStart, _rsrcForkOff, _header.ResourceLength);

        _rsrcForkStream.Seek(0, SeekOrigin.Begin);

        return _rsrcForkStream;
    }

    /// <inheritdoc />
    public bool Identify(byte[] buffer) => buffer != null && TryIdentifyBuffer(buffer);

    /// <inheritdoc />
    public bool Identify(Stream stream)
    {
        if(stream is not { CanRead: true }) return false;

        stream.Seek(0, SeekOrigin.Begin);

        var probe = new byte[Math.Min(stream.Length, 16384)];
        stream.EnsureRead(probe, 0, probe.Length);

        return TryIdentifyBuffer(probe);
    }

    /// <inheritdoc />
    public bool Identify(string path)
    {
        if(!File.Exists(path)) return false;

        using FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        return Identify(fs);
    }

    /// <inheritdoc />
    public ErrorNumber Open(byte[] buffer)
    {
        if(buffer == null) return ErrorNumber.InvalidArgument;

        MemoryStream ms = new(buffer, false);

        ErrorNumber err = OpenInternal(ms, buffer.LongLength);

        if(err != ErrorNumber.NoError) return err;

        _isBytes     = true;
        _sourceBytes = buffer;

        CreationTime  = DateTime.MinValue;
        LastWriteTime = DateTime.MinValue;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Open(Stream stream)
    {
        if(stream is not { CanRead: true, CanSeek: true }) return ErrorNumber.InvalidArgument;

        ErrorNumber err = OpenInternal(stream, stream.Length);

        if(err != ErrorNumber.NoError) return err;

        _sourceStream = stream;

        CreationTime  = DateTime.MinValue;
        LastWriteTime = DateTime.MinValue;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Open(string path)
    {
        if(!File.Exists(path)) return ErrorNumber.NoSuchFile;

        FileStream fs = new(path, FileMode.Open, FileAccess.Read, FileShare.Read);

        ErrorNumber err = OpenInternal(fs, fs.Length);

        if(err != ErrorNumber.NoError)
        {
            fs.Close();

            return err;
        }

        BasePath      = path;
        _sourceStream = fs;

        CreationTime  = File.GetCreationTime(path);
        LastWriteTime = File.GetLastWriteTime(path);

        return ErrorNumber.NoError;
    }

#endregion

#region Helpers

    /// <summary>CRC-16/CCITT (poly 0x1021, init 0, no reflection) used by the BinHex 4.0 header and fork trailers.</summary>
    static ushort Crc16Ccitt(byte[] data, int offset, int length, ushort seed = 0)
    {
        ushort crc = seed;

        for(var i = 0; i < length; i++)
        {
            crc ^= (ushort)(data[offset + i] << 8);

            for(var b = 0; b < 8; b++)
            {
                if((crc & 0x8000) != 0)
                    crc = (ushort)(crc << 1 ^ 0x1021);
                else
                    crc <<= 1;
            }
        }

        return crc;
    }

    static ushort ReadUInt16Be(byte[] buf, int off) => (ushort)(buf[off] << 8 | buf[off + 1]);

    /// <summary>Scan <paramref name="buffer" /> for an ASCII needle within the first <paramref name="limit" /> bytes.</summary>
    /// <returns>Offset of the match, or -1 if not found.</returns>
    static int IndexOfAscii(byte[] buffer, int limit, string needle)
    {
        if(buffer == null) return -1;

        int end = Math.Min(buffer.Length, limit);
        int nl  = needle.Length;

        if(end < nl) return -1;

        for(var i = 0; i <= end - nl; i++)
        {
            var match = true;

            for(var j = 0; j < nl; j++)
            {
                if(buffer[i + j] == (byte)needle[j]) continue;

                match = false;

                break;
            }

            if(match) return i;
        }

        return -1;
    }

    /// <summary>Strictly parse and validate the BinHex 4.0 normalized header.</summary>
    /// <param name="decoded">Decoded buffer; must hold at least the full header.</param>
    /// <param name="maxDecodedSize">Upper bound for data + resource fork sizes. Pass 0 to skip the bound check.</param>
    /// <param name="header">Parsed header on success.</param>
    static bool TryParseHeader(byte[] decoded, long maxDecodedSize, out Header header)
    {
        header = default(Header);

        if(decoded == null || decoded.Length < 1 + HEADER_FIXED_SIZE) return false;

        int nameLen = decoded[0];

        if(nameLen is < 1 or > 63) return false;

        int totalHeader = 1 + nameLen + HEADER_FIXED_SIZE;

        if(decoded.Length < totalHeader) return false;

        // Name must not contain embedded NULs.
        for(var i = 0; i < nameLen; i++)
            if(decoded[1 + i] == 0)
                return false;

        // Reserved null separator after the filename.
        if(decoded[1 + nameLen] != 0) return false;

        int off = 1 + nameLen + 1;

        var type = (uint)(decoded[off] << 24 | decoded[off + 1] << 16 | decoded[off + 2] << 8 | decoded[off + 3]);
        off += 4;
        var creator = (uint)(decoded[off] << 24 | decoded[off + 1] << 16 | decoded[off + 2] << 8 | decoded[off + 3]);
        off += 4;
        var flags = (ushort)(decoded[off] << 8 | decoded[off + 1]);
        off += 2;
        var dataLen = (uint)(decoded[off] << 24 | decoded[off + 1] << 16 | decoded[off + 2] << 8 | decoded[off + 3]);
        off += 4;
        var rsrcLen = (uint)(decoded[off] << 24 | decoded[off + 1] << 16 | decoded[off + 2] << 8 | decoded[off + 3]);
        off += 4;
        var storedCrc = (ushort)(decoded[off] << 8 | decoded[off + 1]);

        // The header CRC is computed over every byte preceding it, then over two trailing zero bytes
        // (equivalent to running the register out by 16 bits).
        ushort computed = Crc16Ccitt(decoded, 0, totalHeader - 2);
        computed = Crc16Ccitt(new byte[2], 0, 2, computed);

        if(computed != storedCrc) return false;

        if(maxDecodedSize > 0 && (long)dataLen + rsrcLen > maxDecodedSize) return false;

        var nameBytes = new byte[nameLen];
        Array.Copy(decoded, 1, nameBytes, 0, nameLen);

        header = new Header
        {
            Filename       = StringHandlers.CToString(nameBytes, Encoding.GetEncoding("macintosh")),
            Type           = type,
            Creator        = creator,
            FinderFlags    = flags,
            DataLength     = dataLen,
            ResourceLength = rsrcLen,
            HeaderBytes    = totalHeader
        };

        return true;
    }

#endregion
}