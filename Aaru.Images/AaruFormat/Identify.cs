using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Images;

public sealed partial class AaruFormat
{
#region IWritableOpticalImage Members

    /// <inheritdoc />
    public bool Identify(IFilter imageFilter)
    {
        string imagePath = imageFilter.BasePath;

        int ret = aaruf_identify(imagePath);

        return ret >= 100;
    }

#endregion

    // AARU_EXPORT int AARU_CALL aaruf_identify(const char *filename)
    [LibraryImport("libaaruformat", EntryPoint = "aaruf_identify", StringMarshalling = StringMarshalling.Utf8)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvStdcall)])]
    private static partial int aaruf_identify(string filename);
}