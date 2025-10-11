using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public ErrorNumber ReadMediaTag(MediaTagType tag, out byte[] buffer)
    {
        buffer = null;

        uint length = 0;

        Status res = aaruf_read_media_tag(_context, buffer, tag, ref length);

        if(res != Status.Ok) return StatusToErrorNumber(res);

        buffer = new byte[length];

        res = aaruf_read_media_tag(_context, buffer, tag, ref length);

        return StatusToErrorNumber(res);
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_read_media_tag(void *context, uint8_t *data, const int32_t tag, uint32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_read_media_tag", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_read_media_tag(IntPtr context, byte[] data, MediaTagType tag, ref uint length);
}