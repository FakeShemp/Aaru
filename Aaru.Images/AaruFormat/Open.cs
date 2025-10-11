using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        string imagePath = imageFilter.BasePath;

        _context = aaruf_open(imagePath);

        if(_context != IntPtr.Zero) return ErrorNumber.NoError;

        int errno = Marshal.GetLastWin32Error();

        AaruLogging.Debug(MODULE_NAME,
                          "Failed to open AaruFormat image {0}, libaaruformat returned error number {1}",
                          imagePath,
                          errno);

        return (ErrorNumber)errno;
    }

#endregion

    // AARU_EXPORT void AARU_CALL *aaruf_open(const char *filepath)
    [LibraryImport("libaaruformat",
                   EntryPoint = "aaruf_open",
                   StringMarshalling = StringMarshalling.Utf8,
                   SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial IntPtr aaruf_open(string filepath);
}