using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool WriteSector(byte[] data, ulong sectorAddress)
    {
        Status res = aaruf_write_sector(_context, sectorAddress, false, data, SectorStatus.Dumped, (uint)data.Length);

        if(res == Status.Ok) return true;

        ErrorMessage = StatusToErrorMessage(res);

        return false;
    }

    /// <inheritdoc />
    public bool WriteSectorLong(byte[] data, ulong sectorAddress)
    {
        Status res =
            aaruf_write_sector_long(_context, sectorAddress, false, data, SectorStatus.Dumped, (uint)data.Length);

        if(res == Status.Ok) return true;

        ErrorMessage = StatusToErrorMessage(res);

        return false;
    }

    /// <inheritdoc />
    public bool WriteMediaTag(byte[] data, MediaTagType tag)
    {
        Status res = aaruf_write_media_tag(_context, data, tag, (uint)data.Length);

        if(res == Status.Ok) return true;

        ErrorMessage = StatusToErrorMessage(res);

        return false;
    }

    /// <inheritdoc />
    public bool WriteSectorTag(byte[] data, ulong sectorAddress, SectorTagType tag)
    {
        Status res = aaruf_write_sector_tag(_context, sectorAddress, false, data, (nuint)data.Length, tag);

        if(res == Status.Ok) return true;

        ErrorMessage = StatusToErrorMessage(res);

        return false;
    }

    /// <inheritdoc />
    public bool WriteSectors(byte[] data, ulong sectorAddress, uint length)
    {
        var sectorSize = (uint)(data.Length / length);

        for(uint i = 0; i < length; i++)
        {
            var sectorData = new byte[sectorSize];
            Array.Copy(data, i * sectorSize, sectorData, 0, sectorSize);

            if(!WriteSector(sectorData, sectorAddress + i)) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool WriteSectorsLong(byte[] data, ulong sectorAddress, uint length)
    {
        var sectorSize = (uint)(data.Length / length);

        for(uint i = 0; i < length; i++)
        {
            var sectorData = new byte[sectorSize];
            Array.Copy(data, i * sectorSize, sectorData, 0, sectorSize);

            if(!WriteSectorLong(sectorData, sectorAddress + i)) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool WriteSectorsTag(byte[] data, ulong sectorAddress, uint length, SectorTagType tag)
    {
        var sectorSize = (uint)(data.Length / length);

        for(uint i = 0; i < length; i++)
        {
            var sectorData = new byte[sectorSize];
            Array.Copy(data, i * sectorSize, sectorData, 0, sectorSize);

            if(!WriteSectorTag(sectorData, sectorAddress + i, tag)) return false;
        }

        return true;
    }

    /// <inheritdoc />
    public bool Create(string path, MediaType mediaType, Dictionary<string, string> options, ulong sectors,
                       uint   sectorSize)
    {
        // Get application major and minor version
        Version version      = typeof(AaruFormat).Assembly.GetName().Version;
        var     majorVersion = (byte)(version?.Major ?? 0);
        var     minorVersion = (byte)(version?.Minor ?? 0);

        // Convert options dictionary to string format
        string optionsString = options is { Count: > 0 }
                                   ? string.Join(";", options.Select(kvp => $"{kvp.Key}={kvp.Value}"))
                                   : null;

        const string applicationName = "Aaru";

        // Create the image context
        _context = aaruf_create(path,
                                mediaType,
                                sectorSize,
                                sectors,
                                0,
                                0,
                                optionsString,
                                applicationName,
                                (byte)applicationName.Length,
                                majorVersion,
                                minorVersion,
                                IsTape);

        if(_context != IntPtr.Zero) return true;

        int errno  = Marshal.GetLastWin32Error();
        var status = (Status)errno;
        ErrorMessage = StatusToErrorMessage(status);

        return false;
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_write_sector(void *context, uint64_t sector_address, bool negative,
    // const uint8_t *data, uint8_t sector_status, uint32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_write_sector", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_write_sector(IntPtr                             context, ulong sectorAddress,
                                                     [MarshalAs(UnmanagedType.I4)] bool negative, [In] byte[] data,
                                                     SectorStatus                       sectorStatus, uint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_write_sector_long(void *context, uint64_t sector_address, bool negative,
    // const uint8_t *data, uint8_t sector_status, uint32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_write_sector_long", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_write_sector_long(IntPtr context, ulong sectorAddress,
                                                          [MarshalAs(UnmanagedType.I4)] bool negative, [In] byte[] data,
                                                          SectorStatus sectorStatus, uint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_write_media_tag(void *context, const uint8_t *data, const int32_t type,
    // const uint32_t length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_write_media_tag", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_write_media_tag(IntPtr context, [In] byte[] data, MediaTagType type,
                                                        uint   length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_write_sector_tag(void *context, const uint64_t sector_address, const bool negative,
    // const uint8_t *data, const size_t length, const int32_t tag)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_write_sector_tag", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_write_sector_tag(IntPtr context, ulong sectorAddress,
                                                         [MarshalAs(UnmanagedType.I4)] bool negative, [In] byte[] data,
                                                         nuint length, SectorTagType tag);

    // AARU_EXPORT void AARU_CALL *aaruf_create(const char *filepath, const uint32_t media_type, const uint32_t sector_size,
    // const uint64_t user_sectors, const uint64_t negative_sectors,
    // const uint64_t overflow_sectors, const char *options,
    // const uint8_t *application_name, const uint8_t application_name_length,
    // const uint8_t application_major_version,
    // const uint8_t application_minor_version, const bool is_tape)
    [LibraryImport("libaaruformat",
                   EntryPoint = "aaruf_create",
                   SetLastError = true,
                   StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial IntPtr aaruf_create(string filepath, MediaType mediaType, uint sectorSize, ulong userSectors,
                                               ulong negativeSectors, ulong overflowSectors, string options,
                                               string applicationName, byte applicationNameLength,
                                               byte applicationMajorVersion, byte applicationMinorVersion,
                                               [MarshalAs(UnmanagedType.I4)] bool isTape);
}