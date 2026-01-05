using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Metadata;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    List<ulong> CalculateSectorsToCopy(IMediaImage primaryImage,    IMediaImage secondaryImage, Resume primaryResume,
                                       Resume      secondaryResume, List<ulong> overrideSectorsList)
    {
        List<DumpHardware> primaryTries = (primaryResume != null ? primaryResume.Tries : primaryImage.DumpHardware) ??
                                          [
                                              new DumpHardware
                                              {
                                                  Extents =
                                                  [
                                                      new Extent
                                                      {
                                                          Start = 0,
                                                          End   = primaryImage.Info.Sectors - 1
                                                      }
                                                  ]
                                              }
                                          ];

        List<DumpHardware> secondaryTries =
            (secondaryResume != null ? secondaryResume.Tries : secondaryImage.DumpHardware) ??
            [
                new DumpHardware
                {
                    Extents =
                    [
                        new Extent
                        {
                            Start = 0,
                            End   = secondaryImage.Info.Sectors - 1
                        }
                    ]
                }
            ];

        // Get all sectors that appear in secondaryTries but not in primaryTries
        var sectorsToCopy = new List<ulong>();

        // Iterate through all extents in secondaryTries
        foreach(DumpHardware secondaryHardware in secondaryTries)
        {
            if(secondaryHardware?.Extents == null) continue;

            foreach(Extent secondaryExtent in secondaryHardware.Extents)
            {
                // For each sector in this secondary extent
                for(ulong sector = secondaryExtent.Start; sector <= secondaryExtent.End; sector++)
                {
                    // Check if this sector appears in any primary extent
                    var foundInPrimary = false;

                    foreach(DumpHardware primaryHardware in primaryTries)
                    {
                        if(primaryHardware?.Extents == null) continue;

                        if(primaryHardware.Extents.Any(primaryExtent =>
                                                           sector >= primaryExtent.Start &&
                                                           sector <= primaryExtent.End))
                            foundInPrimary = true;

                        if(foundInPrimary) break;
                    }

                    // If not found in primary, add to result
                    if(!foundInPrimary) sectorsToCopy.Add(sector);
                }
            }
        }

        sectorsToCopy.AddRange(overrideSectorsList.Where(t => !sectorsToCopy.Contains(t)));

        return sectorsToCopy;
    }

    List<DumpHardware> CalculateMergedDumpHardware(IMediaImage primaryImage,  IMediaImage secondaryImage,
                                                   Resume      primaryResume, Resume      secondaryResume,
                                                   List<ulong> overrideSectorsList)
    {
        List<DumpHardware> primaryTries = (primaryResume != null ? primaryResume.Tries : primaryImage.DumpHardware) ??
                                          [
                                              new DumpHardware
                                              {
                                                  Extents =
                                                  [
                                                      new Extent
                                                      {
                                                          Start = 0,
                                                          End   = primaryImage.Info.Sectors - 1
                                                      }
                                                  ]
                                              }
                                          ];

        List<DumpHardware> secondaryTries =
            (secondaryResume != null ? secondaryResume.Tries : secondaryImage.DumpHardware) ??
            [
                new DumpHardware
                {
                    Extents =
                    [
                        new Extent
                        {
                            Start = 0,
                            End   = secondaryImage.Info.Sectors - 1
                        }
                    ]
                }
            ];

        var mergedHardware = new List<DumpHardware>();

        // Create a mapping of which hardware each sector belongs to
        var sectorToHardware = new Dictionary<ulong, DumpHardware>();

        // Convert override list to HashSet for O(1) lookups
        var overrideSectorsSet = new HashSet<ulong>(overrideSectorsList);

        // First, build a lookup of which hardware each sector belongs to in primary tries
        var primarySectorToHardware = new Dictionary<ulong, DumpHardware>();

        foreach(DumpHardware primaryHardware in primaryTries)
        {
            if(_aborted) break;

            if(primaryHardware?.Extents == null) continue;

            foreach(Extent extent in primaryHardware.Extents)
            {
                for(ulong sector = extent.Start; sector <= extent.End; sector++)
                {
                    if(_aborted) break;

                    primarySectorToHardware[sector] = primaryHardware;
            }
        }

        // Build a lookup of which hardware each sector belongs to in secondary tries
        var secondarySectorToHardware = new Dictionary<ulong, DumpHardware>();

        foreach(DumpHardware secondaryHardware in secondaryTries)
        {
            if(_aborted) break;

            if(secondaryHardware?.Extents == null) continue;

            foreach(Extent extent in secondaryHardware.Extents)
            {
                if(_aborted) break;

                for(ulong sector = extent.Start; sector <= extent.End; sector++)
                    secondarySectorToHardware[sector] = secondaryHardware;
            }
        }

        // Now assign hardware to each sector: use primary hardware, unless sector is in override list
        foreach((ulong sector, DumpHardware primaryHardware) in primarySectorToHardware)
        {
            if(_aborted) break;

            // If this sector should be overridden, use secondary hardware instead
            if(overrideSectorsSet.Contains(sector))
            {
                if(secondarySectorToHardware.TryGetValue(sector, out DumpHardware secondaryHardware))
                    sectorToHardware[sector] = secondaryHardware;
            }
            else
            {
                // Use primary hardware
                sectorToHardware[sector] = primaryHardware;
            }
        }

        // Also add any sectors from override list that weren't in primary
        foreach(ulong overrideSector in overrideSectorsList)
        foreach(ulong overrideSector in overrideSectorsSet)
        {
            if(_aborted) break;

            if(!sectorToHardware.ContainsKey(overrideSector) &&
               secondarySectorToHardware.TryGetValue(overrideSector, out DumpHardware secondaryHardware))
                sectorToHardware[overrideSector] = secondaryHardware;
        }

        // Create extents preserving sector order, grouping contiguous sectors from same hardware
        var allSectors = sectorToHardware.Keys.Order().ToList();

        if(allSectors.Count == 0) return mergedHardware;

        // Start first extent
        DumpHardware currentHardware = sectorToHardware[allSectors[0]];
        ulong        extentStart     = allSectors[0];
        ulong        extentEnd       = allSectors[0];

        for(var i = 1; i < allSectors.Count; i++)
        {
            if(_aborted) break;

            ulong        sector = allSectors[i];
            DumpHardware hw     = sectorToHardware[sector];

            // Check if we should continue current extent or start new one
            if(hw == currentHardware && sector == extentEnd + 1)
            {
                // Same hardware and contiguous sector, extend current extent
                extentEnd = sector;
            }
            else
            {
                // Hardware changed or gap in sectors, save current extent and start new one
                AddOrUpdateHardware(mergedHardware, currentHardware, extentStart, extentEnd);

                currentHardware = hw;
                extentStart     = sector;
                extentEnd       = sector;
            }
        }

        // Add the last extent
        AddOrUpdateHardware(mergedHardware, currentHardware, extentStart, extentEnd);

        return mergedHardware;
    }

    static void AddOrUpdateHardware(List<DumpHardware> mergedHardware, DumpHardware originalHardware, ulong start,
                                    ulong              end)
    {
        // Check if we already have an entry for this hardware
        DumpHardware existing = mergedHardware.FirstOrDefault(h => h.Manufacturer == originalHardware.Manufacturer &&
                                                                   h.Model        == originalHardware.Model        &&
                                                                   h.Revision     == originalHardware.Revision     &&
                                                                   h.Firmware     == originalHardware.Firmware     &&
                                                                   h.Serial       == originalHardware.Serial);

        if(existing != null)
        {
            // Add extent to existing hardware
            existing.Extents.Add(new Extent
            {
                Start = start,
                End   = end
            });
        }
        else
        {
            // Create new hardware entry
            mergedHardware.Add(new DumpHardware
            {
                Manufacturer = originalHardware.Manufacturer,
                Model        = originalHardware.Model,
                Revision     = originalHardware.Revision,
                Firmware     = originalHardware.Firmware,
                Serial       = originalHardware.Serial,
                Software     = originalHardware.Software,
                Extents =
                [
                    new Extent
                    {
                        Start = start,
                        End   = end
                    }
                ]
            });
        }
    }
}