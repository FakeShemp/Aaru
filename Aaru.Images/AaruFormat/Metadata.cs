using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
    // AARU_EXPORT int32_t AARU_CALL aaruf_get_image_info(const void *context, ImageInfo *image_info)
    [DllImport("libaaruformat",
               EntryPoint = "aaruf_get_image_info",
               SetLastError = true,
               CallingConvention = CallingConvention.StdCall)]
#pragma warning disable SYSLIB1054
    private static extern Status aaruf_get_image_info(IntPtr context, ref AaruFormatImageInfo imageInfo);
#pragma warning restore SYSLIB1054

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_sequence(const void *context, int32_t *sequence, int32_t *last_sequence)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_sequence", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_sequence(IntPtr context, out int sequence, out int lastSequence);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_creator(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_creator", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_creator(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_comments(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_comments", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_comments(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_title(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_title", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_title(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_manufacturer(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_manufacturer", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_manufacturer(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_model(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_model", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_model(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_serial_number(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_serial_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_serial_number(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_barcode(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_barcode", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_barcode(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_media_part_number(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_media_part_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_media_part_number(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_drive_manufacturer(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_drive_manufacturer", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_drive_manufacturer(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_drive_model(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_drive_model", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_drive_model(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_drive_serial_number(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_drive_serial_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_drive_serial_number(IntPtr context, byte[] buffer, ref int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_drive_firmware_revision(const void *context, uint8_t *buffer, int32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_drive_firmware_revision", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_drive_firmware_revision(IntPtr context, byte[] buffer, ref int length);
}