using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Core.Image;

public partial class Convert
{
    // TODO: Should we return error any time?
    // TODO: Add progress reporting
    ErrorNumber ConvertFlux(IFluxImage inputFlux, IWritableFluxImage outputFlux)
    {
        for(ushort track = 0; track < inputFlux.Info.Cylinders; track++)
        {
            for(uint head = 0; head < inputFlux.Info.Heads; head++)
            {
                ErrorNumber error = inputFlux.SubTrackLength(head, track, out byte subTrackLen);

                if(error != ErrorNumber.NoError) continue;

                for(byte subTrackIndex = 0; subTrackIndex < subTrackLen; subTrackIndex++)
                {
                    error = inputFlux.CapturesLength(head, track, subTrackIndex, out uint capturesLen);

                    if(error != ErrorNumber.NoError) continue;

                    for(uint captureIndex = 0; captureIndex < capturesLen; captureIndex++)
                    {
                        inputFlux.ReadFluxCapture(head,
                                                  track,
                                                  subTrackIndex,
                                                  captureIndex,
                                                  out ulong indexResolution,
                                                  out ulong dataResolution,
                                                  out byte[] indexBuffer,
                                                  out byte[] dataBuffer);

                        outputFlux.WriteFluxCapture(indexResolution,
                                                    dataResolution,
                                                    indexBuffer,
                                                    dataBuffer,
                                                    head,
                                                    track,
                                                    subTrackIndex,
                                                    captureIndex);
                    }
                }
            }
        }

        return ErrorNumber.NoError;
    }
}