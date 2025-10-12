using System;
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

#endregion

    // AARU_EXPORT int32_t AARU_CALL aaruf_verify_image(void *context)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_verify_image", SetLastError = true)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial Status aaruf_verify_image(IntPtr context);
}