using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Filesystems;

namespace Aaru.Core.Image;

internal static class AacsBdExtentResolver
{
    internal readonly struct LbaRange
    {
        internal ulong Start { get; init; }
        internal ulong End   { get; init; }
    }

    /// <summary>
    ///    Resolves the stream file extents for a Blu-ray optical media image.
    ///    It searches for the stream file extents in the <c>/BDMV/STREAM</c> and
    ///    <c>/BDMV/STREAM/SSIF</c> directories.
    /// </summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="ranges">Stream file extents.</param>
    /// <param name="error">Error message.</param>
    /// <returns>Error number.</returns>
    internal static ErrorNumber ResolveStreamFileExtents(IOpticalMediaImage            inputOptical,
                                                         PluginRegister                plugins,
                                                         out List<LbaRange>            ranges,
                                                         out string                    error)
    {
        ranges = [];
        error  = null;

        IReadOnlyFilesystem udfRo = null;
        UDF                 udf   = null;

        foreach(IFilesystem fs in plugins.Filesystems.Values)
        {
            if(fs is not UDF udfPlugin) continue;

            Partition wholeImage = new()
            {
                Start    = 0,
                Length   = inputOptical.Info.Sectors,
                Size     = inputOptical.Info.Sectors * inputOptical.Info.SectorSize,
                Sequence = 0,
                Type     = "UDF"
            };

            if(!udfPlugin.Identify(inputOptical, wholeImage)) continue;

            ErrorNumber mountErr = udfPlugin.Mount(inputOptical, wholeImage, Encoding.UTF8, null, null);

            if(mountErr != ErrorNumber.NoError)
            {
                error = "UDF mount failed while resolving Blu-ray stream extents.";

                return mountErr;
            }

            udfRo = udfPlugin;
            udf   = udfPlugin;

            break;
        }

        if(udfRo is null || udf is null)
        {
            error = "Could not mount UDF filesystem for Blu-ray stream extent discovery.";

            return ErrorNumber.NotSupported;
        }

        try
        {
            ErrorNumber errno = AddDirectoryExtents(udfRo, udf, "/BDMV/STREAM", ".M2TS", ranges);

            if(errno != ErrorNumber.NoError)
            {
                error = "Could not resolve extents for /BDMV/STREAM.";

                return errno;
            }

            errno = AddDirectoryExtents(udfRo, udf, "/BDMV/STREAM/SSIF", ".SSIF", ranges);

            if(errno != ErrorNumber.NoError && errno != ErrorNumber.NoSuchFile)
            {
                error = "Could not resolve extents for /BDMV/STREAM/SSIF.";

                return errno;
            }

            if(ranges.Count == 0)
            {
                error = "No stream file extents found under /BDMV/STREAM.";

                return ErrorNumber.NoData;
            }

            ranges.Sort(static (a, b) => a.Start.CompareTo(b.Start));
            ranges = MergeRanges(ranges);

            return ErrorNumber.NoError;
        }
        finally
        {
            udfRo.Unmount();
        }
    }

    /// <summary>Checks if a given LBA is allowed for decryption.</summary>
    /// <param name="ranges">Stream file extents.</param>
    /// <param name="lba">LBA to check.</param>
    /// <returns>True if the LBA is allowed for decryption.</returns>
    internal static bool IsLbaAllowed(List<LbaRange> ranges, ulong lba)
    {
        int lo = 0;
        int hi = ranges.Count - 1;

        while(lo <= hi)
        {
            int mid = lo + (hi - lo) / 2;
            LbaRange range = ranges[mid];

            if(lba < range.Start)
                hi = mid - 1;
            else if(lba > range.End)
                lo = mid + 1;
            else
                return true;
        }

        return false;
    }


    /// <summary>Adds the extents for a given directory to the list of stream file extents.</summary>
    /// <param name="roFs">Read-only filesystem.</param>
    /// <param name="udf">UDF filesystem.</param>
    /// <param name="dirPath">Directory path.</param>
    /// <param name="extension">Extension of the files to add.</param>
    /// <param name="ranges">List of stream file extents.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber AddDirectoryExtents(IReadOnlyFilesystem roFs, UDF udf, string dirPath, string extension,
                                           List<LbaRange> ranges)
    {
        ErrorNumber errno = roFs.OpenDir(dirPath, out IDirNode dirNode);

        if(errno != ErrorNumber.NoError) return errno;

        try
        {
            while(true)
            {
                errno = roFs.ReadDir(dirNode, out string filename);

                if(errno != ErrorNumber.NoError) return errno;

                if(filename is null) break;

                if(!filename.EndsWith(extension, StringComparison.OrdinalIgnoreCase)) continue;

                string fullPath = dirPath + "/" + filename;

                errno = udf.GetFilePhysicalSectorExtents(fullPath, out List<(ulong startSector, uint sectorCount)> extents);

                if(errno != ErrorNumber.NoError) return errno;

                foreach((ulong startSector, uint sectorCount) extent in extents)
                {
                    if(extent.sectorCount == 0) continue;

                    ranges.Add(new LbaRange
                    {
                        Start = extent.startSector,
                        End   = extent.startSector + extent.sectorCount - 1
                    });
                }
            }
        }
        finally
        {
            roFs.CloseDir(dirNode);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Merges the extents for a given directory to the list of stream file extents.</summary>
    /// <param name="ranges">List of stream file extents.</param>
    /// <returns>Merged list of stream file extents.</returns>
    static List<LbaRange> MergeRanges(List<LbaRange> ranges)
    {
        if(ranges.Count <= 1) return ranges;

        List<LbaRange> merged = [];
        LbaRange current = ranges[0];

        for(int i = 1; i < ranges.Count; i++)
        {
            LbaRange next = ranges[i];

            if(next.Start <= current.End + 1)
            {
                current = new LbaRange { Start = current.Start, End = Math.Max(current.End, next.End) };
                continue;
            }

            merged.Add(current);
            current = next;
        }

        merged.Add(current);

        return merged;
    }
}
