using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.Checksums;
using Aaru.CommonTypes.Enums;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IVerifiableImage Members

    /// <inheritdoc />
    public bool? VerifyMediaImage()
    {
        Status res = aaruf_verify_image(_context);

        ErrorMessage = StatusToErrorMessage(res);

        return res == Status.Ok;
    }

#endregion

#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool? VerifySector(ulong sectorAddress)
    {
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc) return null;

        ErrorNumber errno = ReadSectorLong(sectorAddress, out byte[] buffer);

        return errno != ErrorNumber.NoError ? null : CdChecksums.CheckCdSector(buffer);
    }

    /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas)
    {
        failingLbas = [];
        unknownLbas = [];

        // Right now only CompactDisc sectors are verifiable
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc)
        {
            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        ErrorNumber errno = ReadSectorsLong(sectorAddress, length, out byte[] buffer);

        if(errno != ErrorNumber.NoError) return null;

        var bps    = (int)(buffer.Length / length);
        var sector = new byte[bps];
        failingLbas = [];
        unknownLbas = [];

        for(var i = 0; i < length; i++)
        {
            Array.Copy(buffer, i * bps, sector, 0, bps);
            bool? sectorStatus = CdChecksums.CheckCdSector(sector);

            switch(sectorStatus)
            {
                case null:
                    unknownLbas.Add((ulong)i + sectorAddress);

                    break;
                case false:
                    failingLbas.Add((ulong)i + sectorAddress);

                    break;
            }
        }

        if(unknownLbas.Count > 0) return null;

        return failingLbas.Count <= 0;
    }
     /// <inheritdoc />
    public bool? VerifySectors(ulong           sectorAddress, uint length, uint track, out List<ulong> failingLbas,
                               out List<ulong> unknownLbas)
    {
        // Right now only CompactDisc sectors are verifiable
        if(_imageInfo.MetadataMediaType != MetadataMediaType.OpticalDisc || Tracks == null)
        {
            failingLbas = [];
            unknownLbas = [];

            for(ulong i = sectorAddress; i < sectorAddress + length; i++) unknownLbas.Add(i);

            return null;
        }

        failingLbas = [];
        unknownLbas = [];

        ErrorNumber errno = ReadSectorsLong(sectorAddress, length, track, out byte[] buffer);

        if(errno != ErrorNumber.NoError) return null;

        var bps    = (int)(buffer.Length / length);
        var sector = new byte[bps];

        for(var i = 0; i < length; i++)
        {
            Array.Copy(buffer, i * bps, sector, 0, bps);
            bool? sectorStatus = CdChecksums.CheckCdSector(sector);

            switch(sectorStatus)
            {
                case null:
                    unknownLbas.Add((ulong)i + sectorAddress);

                    break;
                case false:
                    failingLbas.Add((ulong)i + sectorAddress);

                    break;
            }
        }

        if(unknownLbas.Count > 0) return null;

        return failingLbas.Count <= 0;
    }

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_verify_image(void *context)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_verify_image", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_verify_image(IntPtr context);
}