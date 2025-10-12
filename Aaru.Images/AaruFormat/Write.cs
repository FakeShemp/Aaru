using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        Status res = aaruf_write_sector_tag(_context, sectorAddress, false, data, (ulong)data.Length, tag);

        if(res == Status.Ok) return true;

        ErrorMessage = StatusToErrorMessage(res);

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
                                                         ulong length, SectorTagType tag);
}