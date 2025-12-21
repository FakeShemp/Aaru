using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    static (List<ulong> overrideSectors, List<uint> overrideNegativeSectors) ParseOverrideSectorsList(
        string overrideSectorsListPath)
    {
        if(overrideSectorsListPath is null) return ([], []);

        List<ulong> overrideSectors         = [];
        List<uint>  overrideNegativeSectors = [];

        StreamReader sr = new(overrideSectorsListPath);

        while(sr.ReadLine() is {} line)
        {
            line = line.Trim();

            if(line.Length == 0) continue;

            if(line.StartsWith('-'))
            {
                if(long.TryParse(line[1..], CultureInfo.InvariantCulture, out long negativeSector))
                    overrideNegativeSectors.Add((uint)(negativeSector * -1));
            }
            else
            {
                if(ulong.TryParse(line, CultureInfo.InvariantCulture, out ulong sector)) overrideSectors.Add(sector);
            }
        }

        sr.Close();

        return (overrideSectors, overrideNegativeSectors);
    }
}