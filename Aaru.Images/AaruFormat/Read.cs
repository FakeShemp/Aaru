using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_read_media_tag(void *context, uint8_t *data, const int32_t tag, uint32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_read_media_tag", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_read_media_tag(IntPtr context, byte[] data, MediaTagType tag, ref uint length);
}