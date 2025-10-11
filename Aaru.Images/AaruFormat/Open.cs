using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = System.Runtime.InteropServices.Marshal;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public ErrorNumber Open(IFilter imageFilter)
    {
        string imagePath = imageFilter.BasePath;

        _context = aaruf_open(imagePath);

        if(_context == IntPtr.Zero)
        {
            int errno = Marshal.GetLastWin32Error();

            AaruLogging.Debug(MODULE_NAME,
                              "Failed to open AaruFormat image {0}, libaaruformat returned error number {1}",
                              imagePath,
                              errno);

            return (ErrorNumber)errno;
        }

        AaruFormatImageInfo imageInfo = new();

        Status ret = aaruf_get_image_info(_context, ref imageInfo);

        if(ret != Status.Ok) return StatusToErrorNumber(ret);

        _imageInfo.Application          = StringHandlers.CToString(imageInfo.Application,        Encoding.UTF8);
        _imageInfo.Version              = StringHandlers.CToString(imageInfo.Version,            Encoding.UTF8);
        _imageInfo.ApplicationVersion   = StringHandlers.CToString(imageInfo.ApplicationVersion, Encoding.UTF8);
        _imageInfo.CreationTime         = DateTime.FromFileTimeUtc(imageInfo.CreationTime);
        _imageInfo.HasPartitions        = imageInfo.HasPartitions;
        _imageInfo.HasSessions          = imageInfo.HasSessions;
        _imageInfo.ImageSize            = imageInfo.ImageSize;
        _imageInfo.MediaType            = imageInfo.MediaType;
        _imageInfo.LastModificationTime = DateTime.FromFileTimeUtc(imageInfo.LastModificationTime);
        _imageInfo.SectorSize           = imageInfo.SectorSize;
        _imageInfo.Sectors              = imageInfo.Sectors;
        _imageInfo.MetadataMediaType    = imageInfo.MetadataMediaType;

        // TODO: rest of metadata
        ret = aaruf_get_media_sequence(_context, out int sequence, out int lastSequence);

        if(ret == Status.Ok)
        {
            _imageInfo.LastMediaSequence = lastSequence;
            _imageInfo.MediaSequence     = sequence;
        }

        var length = 0;
        ret = aaruf_get_creator(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_creator(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.Creator = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_creator(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_creator(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.Creator = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_comments(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_comments(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.Comments = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_title(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_title(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.MediaTitle = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_manufacturer(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_manufacturer(_context, buffer, ref length);

            if(ret == Status.Ok)
                _imageInfo.MediaManufacturer = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_model(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_model(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.MediaModel = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_serial_number(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_serial_number(_context, buffer, ref length);

            if(ret == Status.Ok)
                _imageInfo.MediaSerialNumber = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_barcode(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_barcode(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.MediaBarcode = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_media_part_number(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_media_part_number(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.MediaPartNumber = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_drive_manufacturer(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_drive_manufacturer(_context, buffer, ref length);

            if(ret == Status.Ok)
                _imageInfo.DriveManufacturer = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_drive_model(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_drive_model(_context, buffer, ref length);
            if(ret == Status.Ok) _imageInfo.DriveModel = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_drive_serial_number(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_drive_serial_number(_context, buffer, ref length);

            if(ret == Status.Ok)
                _imageInfo.DriveSerialNumber = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        length = 0;
        ret    = aaruf_get_drive_firmware_revision(_context, null, ref length);

        if(ret == Status.BufferTooSmall)
        {
            var buffer = new byte[length];
            ret = aaruf_get_drive_firmware_revision(_context, buffer, ref length);

            if(ret == Status.Ok)
                _imageInfo.DriveFirmwareRevision = StringHandlers.CToString(buffer, Encoding.Unicode, true);
        }

        ret = aaruf_get_geometry(_context, out uint cylinders, out uint heads, out uint sectorsPerTrack);

        if(ret == Status.Ok)
        {
            _imageInfo.Cylinders       = cylinders;
            _imageInfo.Heads           = heads;
            _imageInfo.SectorsPerTrack = sectorsPerTrack;
        }

        SetMetadataFromTags();

        return ErrorNumber.NoError;
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