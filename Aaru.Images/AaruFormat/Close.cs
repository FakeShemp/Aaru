using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IDisposable Members

    /// <inheritdoc />
    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

#endregion

#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Close()
    {
        if(_context == IntPtr.Zero) return false;

        int res = aaruf_close(_context);

        _context = IntPtr.Zero;

        return res == 0;
    }

#endregion

    void ReleaseUnmanagedResources()
    {
        if(_context == IntPtr.Zero) return;

        aaruf_close(_context);

        _context = IntPtr.Zero;
    }

    /// <inheritdoc />
    ~AaruFormat()
    {
        ReleaseUnmanagedResources();
    }

    // AARU_EXPORT int AARU_CALL aaruf_close(void *context)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_close", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial int aaruf_close(IntPtr context);
}