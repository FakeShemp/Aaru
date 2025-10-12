using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool SetMetadata(Metadata metadata)
    {
        var jsonMs = new MemoryStream();

        JsonSerializer.Serialize(jsonMs,
                                 new MetadataJson
                                 {
                                     AaruMetadata = AaruMetadata
                                 },
                                 typeof(MetadataJson),
                                 MetadataJsonContext.Default);

        byte[] buffer = jsonMs.ToArray();

        return aaruf_set_aaru_json_metadata(_context, buffer, (ulong)buffer.Length) == Status.Ok;
    }

    /// <inheritdoc />
    public bool SetGeometry(uint cylinders, uint heads, uint sectorsPerTrack)
    {
        _imageInfo.Cylinders       = cylinders;
        _imageInfo.Heads           = heads;
        _imageInfo.SectorsPerTrack = sectorsPerTrack;

        return aaruf_set_geometry(_context, cylinders, heads, sectorsPerTrack) == Status.Ok;
    }

    /// <inheritdoc />
    public bool SetImageInfo(ImageInfo imageInfo)
    {
        _imageInfo.MediaSequence         = imageInfo.MediaSequence;
        _imageInfo.LastMediaSequence     = imageInfo.LastMediaSequence;
        _imageInfo.Creator               = imageInfo.Creator;
        _imageInfo.Comments              = imageInfo.Comments;
        _imageInfo.MediaTitle            = imageInfo.MediaTitle;
        _imageInfo.MediaManufacturer     = imageInfo.MediaManufacturer;
        _imageInfo.MediaModel            = imageInfo.MediaModel;
        _imageInfo.MediaSerialNumber     = imageInfo.MediaSerialNumber;
        _imageInfo.MediaBarcode          = imageInfo.MediaBarcode;
        _imageInfo.MediaPartNumber       = imageInfo.MediaPartNumber;
        _imageInfo.DriveManufacturer     = imageInfo.DriveManufacturer;
        _imageInfo.DriveModel            = imageInfo.DriveModel;
        _imageInfo.DriveSerialNumber     = imageInfo.DriveSerialNumber;
        _imageInfo.DriveFirmwareRevision = imageInfo.DriveFirmwareRevision;

        aaruf_set_media_sequence(_context, _imageInfo.MediaSequence, _imageInfo.LastMediaSequence);

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.Creator))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.Creator);
            aaruf_set_creator(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.Comments))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.Comments);
            aaruf_set_comments(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaTitle))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaTitle);
            aaruf_set_media_title(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaManufacturer))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaManufacturer);
            aaruf_set_media_manufacturer(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaModel))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaModel);
            aaruf_set_media_model(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaSerialNumber))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaSerialNumber);
            aaruf_set_media_serial_number(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaBarcode))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaBarcode);
            aaruf_set_media_barcode(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.MediaPartNumber))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.MediaPartNumber);
            aaruf_set_media_part_number(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.DriveManufacturer))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.DriveManufacturer);
            aaruf_set_drive_manufacturer(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.DriveModel))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.DriveModel);
            aaruf_set_drive_model(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(!string.IsNullOrEmpty(_imageInfo.DriveSerialNumber))
        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.DriveSerialNumber);
            aaruf_set_drive_serial_number(_context, buffer, buffer.Length);
        }

        // TODO: Clear fields if empty
        if(string.IsNullOrEmpty(_imageInfo.DriveFirmwareRevision)) return true;

        {
            byte[] buffer = Encoding.Unicode.GetBytes(_imageInfo.DriveFirmwareRevision);
            aaruf_set_drive_firmware_revision(_context, buffer, buffer.Length);
        }

        return true;
    }

#endregion

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

    // AARU_EXPORT int32_t AARU_CALL aaruf_get_geometry(const void *context, uint32_t *cylinders, uint32_t *heads,
    // uint32_t *sectors_per_track)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_get_geometry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_get_geometry(IntPtr   context, out uint cylinders, out uint heads,
                                                     out uint sectorsPerTrack);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_aaru_json_metadata(void *context, uint8_t *data, size_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_aaru_json_metadata", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_aaru_json_metadata(IntPtr context, [In] byte[] data, ulong length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_geometry(void *context, const uint32_t cylinders, const uint32_t heads,
    // const uint32_t sectors_per_track)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_geometry", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_geometry(IntPtr context, uint cylinders, uint heads, uint sectorsPerTrack);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_sequence(void *context, const int32_t sequence,
    // const int32_t last_sequence)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_sequence", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_sequence(IntPtr context, int sequence, int lastSequence);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_creator(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_creator", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_creator(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_comments(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_comments", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_comments(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_title(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_title", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_title(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_manufacturer(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_manufacturer", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_manufacturer(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_model(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_model", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_model(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_serial_number(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_serial_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_serial_number(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_barcode(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_barcode", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_barcode(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_media_part_number(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_media_part_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_media_part_number(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_drive_manufacturer(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_drive_manufacturer", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_drive_manufacturer(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_drive_model(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_drive_model", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_drive_model(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_drive_serial_number(void *context, const uint8_t *data, const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_drive_serial_number", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_drive_serial_number(IntPtr context, [In] byte[] data, int length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_set_drive_firmware_revision(void *context, const uint8_t *data,
    // const int32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_set_drive_firmware_revision", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_set_drive_firmware_revision(IntPtr context, [In] byte[] data, int length);
}