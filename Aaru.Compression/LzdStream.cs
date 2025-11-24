using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

namespace Aaru.Compression;

public partial class LzdStream : Stream
{
    private const int INPUT_CHUNK  = 8192;
    private const int OUTPUT_CHUNK = 8192;

    private readonly Stream _baseStream;
    private readonly byte[] _inBuffer  = new byte[INPUT_CHUNK];
    private readonly byte[] _outBuffer = new byte[OUTPUT_CHUNK];

    private IntPtr _ctx;
    private bool   _disposed;
    private bool   _eof;     // native says DONE
    private bool   _flushed; // whether we've sent the final empty feed
    private int    _outCount;
    private int    _outOffset;

    public LzdStream(Stream compressedStream)
    {
        _baseStream = compressedStream ?? throw new ArgumentNullException(nameof(compressedStream));
        _ctx        = CreateLZDContext();

        if(_ctx == IntPtr.Zero) throw new InvalidOperationException("Failed to create LZD context");
    }

    public override bool CanRead  => true;
    public override bool CanSeek  => false;
    public override bool CanWrite => false;
    public override long Length   => throw new NotSupportedException();

    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    [LibraryImport("libAaru.Compression.Native")]
    private static partial IntPtr CreateLZDContext();

    [LibraryImport("libAaru.Compression.Native")]
    private static partial void DestroyLZDContext(IntPtr ctx);

    [LibraryImport("libAaru.Compression.Native")]
    private static partial int LZD_FeedNative(IntPtr ctx, [In] byte[] inputBuffer, nuint inputSize);

    [LibraryImport("libAaru.Compression.Native")]
    private static partial int LZD_DrainNative(IntPtr    ctx, [Out] byte[] outputBuffer, nuint outputCapacity,
                                               out nuint produced);

    public override void Flush() {}

    public override int Read(byte[] buffer, int offset, int count)
    {
        if(_disposed) throw new ObjectDisposedException(nameof(LzdStream));
        if(buffer == null) throw new ArgumentNullException(nameof(buffer));
        if(offset < 0 || count < 0 || offset + count > buffer.Length) throw new ArgumentOutOfRangeException();

        var totalRead = 0;
        var totalOut  = 0;
        var iter      = 0;

        while(totalRead < count)
        {
            Debug.WriteLine($"-- LOOP iter={iter++} total_out={totalOut} --");

            // serve leftovers first
            if(_outOffset < _outCount)
            {
                int toCopy = Math.Min(count - totalRead, _outCount - _outOffset);
                Buffer.BlockCopy(_outBuffer, _outOffset, buffer, offset + totalRead, toCopy);
                _outOffset += toCopy;
                totalRead  += toCopy;
                totalOut   += toCopy;

                continue;
            }

            if(_eof) break; // nothing more to decode

            // drain from native
            LZDStatus status = TryDrain(out int produced);
            Debug.WriteLine($"[DRAIN] produced={produced} status={status} flushed={_flushed} eof={_eof}");


            if(produced > 0)
            {
                _outCount  = produced;
                _outOffset = 0;

                continue; // go copy them on next loop
            }

            if(status == LZDStatus.NEED_INPUT)
            {
                int n = _baseStream.Read(_inBuffer, 0, _inBuffer.Length);

                if(n > 0)
                {
                    _eof = false; // we're not done, fresh data incoming
                    Debug.WriteLine($"[FEED] size={n} flushed={_flushed} (real data)");

                    var f = (LZDStatus)LZD_FeedNative(_ctx, _inBuffer, (UIntPtr)n);
                    if(f == LZDStatus.ERROR) ThrowDecoderError();

                    continue;
                }

                // end of base stream: flush native once
                if(!_flushed)
                {
                    Debug.WriteLine($"[FEED] size=0 flushed={_flushed} (final empty feed)");
                    var f = (LZDStatus)LZD_FeedNative(_ctx, [], UIntPtr.Zero);
                    if(f == LZDStatus.ERROR) ThrowDecoderError();
                    _flushed = true;
                    Debug.WriteLine(">>> SET _flushed=true");

                    continue;
                }

                // no more to give
                _eof = true;
                Debug.WriteLine(">>> SET _eof=true (DONE from decoder)");

                break;
            }

            if(status != LZDStatus.DONE) continue;

            _eof = true;
            Debug.WriteLine(">>> SET _eof=true (no more data and already flushed)");

            break;

            // if OK but no bytes, loop again
        }

        Debug.WriteLine($"TOTAL OUT={totalOut} bytes");

        return totalRead;
    }

    private LZDStatus TryDrain(out int produced)
    {
        var st = (LZDStatus)LZD_DrainNative(_ctx, _outBuffer, (UIntPtr)_outBuffer.Length, out UIntPtr p);
        if(st == LZDStatus.ERROR) ThrowDecoderError();
        if(st == LZDStatus.DONE) _eof = true;
        produced = (int)p;

        return st;
    }

    private static void ThrowDecoderError() => throw new IOException("LZD decompression error");

    protected override void Dispose(bool disposing)
    {
        if(!_disposed)
        {
            DestroyLZDContext(_ctx);
            _ctx      = IntPtr.Zero;
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    public override long Seek(long      offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value)                         => throw new NotSupportedException();
    public override void Write(byte[]   buffer, int offset, int count) => throw new NotSupportedException();

#region Nested type: LZDStatus

    internal enum LZDStatus
    {
        OK          = 0,
        NEED_INPUT  = 1,
        NEED_OUTPUT = 2,
        DONE        = 3,
        ERROR       = -1
    }

#endregion
}