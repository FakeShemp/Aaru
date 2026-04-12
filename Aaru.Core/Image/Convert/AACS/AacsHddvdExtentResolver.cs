using System;
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Filesystems;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Core.Image;

internal static class AacsHddvdExtentResolver
{
    internal readonly struct LbaRange
    {
        internal ulong Start { get; init; }
        internal ulong End   { get; init; }
    }

    /// <summary>Resolves <c>.EVO</c> stream file extents under <c>HVDVD_TS</c> for HD DVD-Video.</summary>
    internal static ErrorNumber ResolveEvoFileExtents(IOpticalMediaImage inputOptical, PluginRegister plugins,
                                                      out List<LbaRange> ranges, out string error)
    {
        ranges = [];
        error  = null;

        IReadOnlyFilesystem udfRo = null;
        UDF                 udf   = null;

        foreach(IFilesystem fs in plugins.Filesystems.Values)
        {
            if(fs is not UDF udfPlugin)
                continue;

            Partition wholeImage = new()
            {
                Start    = 0,
                Length   = inputOptical.Info.Sectors,
                Size     = inputOptical.Info.Sectors * inputOptical.Info.SectorSize,
                Sequence = 0,
                Type     = "UDF"
            };

            if(!udfPlugin.Identify(inputOptical, wholeImage))
                continue;

            ErrorNumber mountErr = udfPlugin.Mount(inputOptical, wholeImage, Encoding.UTF8, null, null);

            if(mountErr != ErrorNumber.NoError)
            {
                error = Aaru.Localization.Core.Aacs_hddvd_udf_mount_failed;

                return mountErr;
            }

            udfRo = udfPlugin;
            udf   = udfPlugin;

            break;
        }

        if(udfRo is null || udf is null)
        {
            error = Aaru.Localization.Core.Aacs_hddvd_udf_mount_failed;

            return ErrorNumber.NotSupported;
        }

        try
        {
            ErrorNumber errno = CollectEvoExtentsRecursive(udfRo, udf, "/HVDVD_TS", ref ranges);

            if(errno != ErrorNumber.NoError && errno != ErrorNumber.NoSuchFile)
            {
                error = Aaru.Localization.Core.Aacs_hddvd_no_evo_extents;

                return errno;
            }

            if(ranges.Count == 0)
            {
                error = Aaru.Localization.Core.Aacs_hddvd_no_evo_extents;

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

    static ErrorNumber CollectEvoExtentsRecursive(IReadOnlyFilesystem roFs, UDF udf, string dirPath,
                                                  ref List<LbaRange> ranges)
    {
        ErrorNumber errno = roFs.OpenDir(dirPath, out IDirNode dirNode);

        if(errno != ErrorNumber.NoError)
            return errno;

        try
        {
            while(true)
            {
                errno = roFs.ReadDir(dirNode, out string filename);

                if(errno != ErrorNumber.NoError)
                    return errno;

                if(filename is null)
                    break;

                if(filename is "." or "..")
                    continue;

                string fullPath = dirPath + "/" + filename;

                errno = roFs.Stat(fullPath, out FileEntryInfo st);

                if(errno != ErrorNumber.NoError)
                    continue;

                if(st.Attributes.HasFlag(FileAttributes.Directory))
                {
                    errno = CollectEvoExtentsRecursive(roFs, udf, fullPath, ref ranges);

                    if(errno != ErrorNumber.NoError && errno != ErrorNumber.NoSuchFile)
                        return errno;

                    continue;
                }

                if(!filename.EndsWith(".EVO", StringComparison.OrdinalIgnoreCase))
                    continue;

                errno = udf.GetFilePhysicalSectorExtents(fullPath, out List<(ulong startSector, uint sectorCount)> extents);

                if(errno != ErrorNumber.NoError)
                    return errno;

                foreach((ulong startSector, uint sectorCount) extent in extents)
                {
                    if(extent.sectorCount == 0)
                        continue;

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

    static List<LbaRange> MergeRanges(List<LbaRange> ranges)
    {
        if(ranges.Count <= 1)
            return ranges;

        List<LbaRange> merged = [];
        LbaRange       current = ranges[0];

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
