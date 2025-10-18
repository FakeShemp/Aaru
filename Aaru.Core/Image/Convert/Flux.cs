using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Core.Image;

public partial class Convert
{
    // TODO: Should we return error any time?
    // TODO: Add progress reporting
    ErrorNumber ConvertFlux(IFluxImage inputFlux, IWritableFluxImage outputFlux)
    {
        ErrorNumber error = inputFlux.GetAllFluxCaptures(out List<FluxCapture> captures);

        if(error != ErrorNumber.NoError) return error;

        if(captures is null || captures.Count == 0) return ErrorNumber.NoError;

        foreach(FluxCapture capture in captures)
        {
            error = inputFlux.ReadFluxCapture(capture.Head,
                                              capture.Track,
                                              capture.SubTrack,
                                              capture.CaptureIndex,
                                              out ulong indexResolution,
                                              out ulong dataResolution,
                                              out byte[] indexBuffer,
                                              out byte[] dataBuffer);

            if(error != ErrorNumber.NoError) continue;

            outputFlux.WriteFluxCapture(indexResolution,
                                        dataResolution,
                                        indexBuffer,
                                        dataBuffer,
                                        capture.Head,
                                        capture.Track,
                                        capture.SubTrack,
                                        capture.CaptureIndex);
        }

        return ErrorNumber.NoError;
    }
}