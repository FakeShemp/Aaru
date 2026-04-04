using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    ErrorNumber MergeFlux(IFluxImage primaryFlux, IFluxImage secondaryFlux, IWritableFluxImage outputFlux)
    {
        ErrorNumber error = primaryFlux.GetAllFluxCaptures(out List<FluxCapture> primaryCaptures);

        if(error != ErrorNumber.NoError) return error;

        if(primaryCaptures is null) primaryCaptures = new List<FluxCapture>();

        List<FluxCapture> secondaryCaptures = new List<FluxCapture>();

        if(secondaryFlux != null)
        {
            error = secondaryFlux.GetAllFluxCaptures(out secondaryCaptures);

            if(error != ErrorNumber.NoError) return error;

            if(secondaryCaptures is null) secondaryCaptures = new List<FluxCapture>();
        }

        Dictionary<(uint Head, ushort Track, byte SubTrack), List<FluxCapture>> primaryByLocation =
            GroupFluxCapturesByLocation(primaryCaptures);

        Dictionary<(uint Head, ushort Track, byte SubTrack), List<FluxCapture>> secondaryByLocation =
            GroupFluxCapturesByLocation(secondaryCaptures);

        HashSet<(uint Head, ushort Track, byte SubTrack)> allKeys = new HashSet<(uint Head, ushort Track, byte SubTrack)>();

        foreach((uint Head, ushort Track, byte SubTrack) key in primaryByLocation.Keys) allKeys.Add(key);

        foreach((uint Head, ushort Track, byte SubTrack) key in secondaryByLocation.Keys) allKeys.Add(key);

        List<(uint Head, ushort Track, byte SubTrack)> sortedKeys =
            allKeys.OrderBy(static k => k.Track).ThenBy(static k => k.Head).ThenBy(static k => k.SubTrack).ToList();

        foreach((uint Head, ushort Track, byte SubTrack) key in sortedKeys)
        {
            List<FluxCapture> primaryGroup =
                primaryByLocation.TryGetValue(key, out List<FluxCapture> pg) ? pg : new List<FluxCapture>();

            List<FluxCapture> secondaryGroup =
                secondaryByLocation.TryGetValue(key, out List<FluxCapture> sg) ? sg : new List<FluxCapture>();

            primaryGroup.Sort((FluxCapture a, FluxCapture b) => a.CaptureIndex.CompareTo(b.CaptureIndex));
            secondaryGroup.Sort((FluxCapture a, FluxCapture b) => a.CaptureIndex.CompareTo(b.CaptureIndex));

            uint outputIndex = 0;

            foreach(FluxCapture capture in primaryGroup)
            {
                error = primaryFlux.ReadFluxCapture(capture.Head,
                                                    capture.Track,
                                                    capture.SubTrack,
                                                    capture.CaptureIndex,
                                                    out ulong indexResolution,
                                                    out ulong dataResolution,
                                                    out byte[] indexBuffer,
                                                    out byte[] dataBuffer);

                if(error != ErrorNumber.NoError) return error;

                error = outputFlux.WriteFluxCapture(indexResolution,
                                                   dataResolution,
                                                   indexBuffer,
                                                   dataBuffer,
                                                   capture.Head,
                                                   capture.Track,
                                                   capture.SubTrack,
                                                   outputIndex);

                if(error != ErrorNumber.NoError) return error;

                outputIndex++;
            }

            if(secondaryFlux != null)
            {
                foreach(FluxCapture capture in secondaryGroup)
                {
                    error = secondaryFlux.ReadFluxCapture(capture.Head,
                                                           capture.Track,
                                                           capture.SubTrack,
                                                           capture.CaptureIndex,
                                                           out ulong indexResolution,
                                                           out ulong dataResolution,
                                                           out byte[] indexBuffer,
                                                           out byte[] dataBuffer);

                    if(error != ErrorNumber.NoError) return error;

                    error = outputFlux.WriteFluxCapture(indexResolution,
                                                       dataResolution,
                                                       indexBuffer,
                                                       dataBuffer,
                                                       capture.Head,
                                                       capture.Track,
                                                       capture.SubTrack,
                                                       outputIndex);

                    if(error != ErrorNumber.NoError) return error;

                    outputIndex++;
                }
            }
        }

        return ErrorNumber.NoError;
    }

    static Dictionary<(uint Head, ushort Track, byte SubTrack), List<FluxCapture>> GroupFluxCapturesByLocation(
        List<FluxCapture> captures)
    {
        Dictionary<(uint Head, ushort Track, byte SubTrack), List<FluxCapture>> result =
            new Dictionary<(uint Head, ushort Track, byte SubTrack), List<FluxCapture>>();

        foreach(FluxCapture capture in captures)
        {
            (uint Head, ushort Track, byte SubTrack) key = (capture.Head, capture.Track, capture.SubTrack);

            if(!result.TryGetValue(key, out List<FluxCapture> list))
            {
                list = new List<FluxCapture>();
                result[key] = list;
            }

            list.Add(capture);
        }

        return result;
    }
}
