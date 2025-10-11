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

        // TODO: Convert between error codes
        if(ret != Status.Ok) return (ErrorNumber)ret;

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
        // TODO: geometry
        // TODO: metadata from media tags

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