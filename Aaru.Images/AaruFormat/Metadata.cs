using System;
using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_get_image_info(const void *context, ImageInfo *image_info)
    [DllImport("libaaruformat",
               EntryPoint = "aaruf_get_image_info",
               SetLastError = true,
               CallingConvention = CallingConvention.StdCall)]
    private static extern Status aaruf_get_image_info(IntPtr context, ref AaruFormatImageInfo imageInfo);
}