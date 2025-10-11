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

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, out byte[] buffer)
    {
        buffer = null;
        uint length = 0;

        // TODO: Sector status API
        Status res = aaruf_read_sector(_context, sectorAddress, false, buffer, ref length, out _);

        if(res != Status.BufferTooSmall) return StatusToErrorNumber(res);

        buffer = new byte[length];

        res = aaruf_read_sector(_context, sectorAddress, false, buffer, ref length, out _);

        return StatusToErrorNumber(res);
    }

    /// <inheritdoc />
    public ErrorNumber ReadSector(ulong sectorAddress, uint track, out byte[] buffer)
    {
        buffer = null;
        uint length = 0;

        Status res = aaruf_read_track_sector(_context, buffer, sectorAddress, ref length, (byte)track, out _);

        if(res != Status.BufferTooSmall) return StatusToErrorNumber(res);

        buffer = new byte[length];

        res = aaruf_read_track_sector(_context, buffer, sectorAddress, ref length, (byte)track, out _);

        return StatusToErrorNumber(res);
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_read_media_tag(void *context, uint8_t *data, const int32_t tag, uint32_t *length)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_read_media_tag", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_read_media_tag(IntPtr context, byte[] data, MediaTagType tag, ref uint length);

    // AARU_EXPORT int32_t AARU_CALL aaruf_read_sector(void *context, const uint64_t sector_address, bool negative,
    // uint8_t *data, uint32_t *length, uint8_t *sector_status)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_read_sector", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_read_sector(IntPtr                             context,  ulong    sectorAddress,
                                                    [MarshalAs(UnmanagedType.I4)] bool negative, byte[]   data,
                                                    ref                           uint length,   out byte sectorStatus);
 // AARU_EXPORT int32_t AARU_CALL aaruf_read_track_sector(void *context, uint8_t *data, const uint64_t sector_address,
    // uint32_t *length, const uint8_t track, uint8_t *sector_status)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_read_track_sector", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_read_track_sector(IntPtr   context, byte[] data,  ulong    sectorAddress,
                                                          ref uint length,  byte   track, out byte sectorStatus);
}