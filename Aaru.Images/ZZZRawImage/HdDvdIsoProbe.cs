// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : HdDvdIsoProbe.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Detects HD DVD-Video layout on bare .iso images (HVDVD_TS folder).
//     Aaru.Core.Partitions.GetAll cannot be called from this assembly because
//     Aaru.Core references Aaru.Images; the partition walk below mirrors the
//     non-tape, in Partitions.GetAll. If we find a simpler way to detect a 
//     HD DVD-Video layout, we should use that instead.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see
//     <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decryption.Aacs;

namespace Aaru.Images;

public sealed partial class ZZZRawImage
{
    /// <summary>True if a filesystem under some partition exposes the HD DVD-Video <c>HVDVD_TS</c> folder.</summary>
    bool TryDetectHdDvdVideoIso()
    {
        IMediaImage          image   = this;
        IOpticalMediaImage   optical = this;
        PluginRegister       plugins = PluginRegister.Singleton;
        List<Partition>      parts   = EnumeratePartitionsForHdDvdProbe(image);

        foreach(Partition partition in parts)
        {
            foreach(IFilesystem fs in plugins.Filesystems.Values)
            {
                if(fs is not IReadOnlyFilesystem rofs)
                    continue;

                if(!fs.Identify(image, partition))
                    continue;

                if(HDDVD.HasHdDvdVideoTsFolder(optical, rofs, partition))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Lists partitions the same way as <see cref="Aaru.Core.Partitions.GetAll"/> for images that are not
    ///     tape.
    /// </summary>
    static List<Partition> EnumeratePartitionsForHdDvdProbe(IMediaImage image)
    {
        PluginRegister  plugins          = PluginRegister.Singleton;
        List<Partition> foundPartitions  = [];
        List<Partition> childPartitions  = [];
        List<ulong>     checkedLocations = [];

        var partitionableImage = image as IPartitionableMediaImage;

        // Getting all partitions from device (e.g. tracks)
        if(partitionableImage?.Partitions != null)
        {
            foreach(Partition imagePartition in partitionableImage.Partitions)
            {
                foreach(IPartition plugin in plugins.Partitions.Values)
                {
                    if(plugin is null) continue;

                    if(!plugin.GetInformation(image, out List<Partition> partitions, imagePartition.Start)) continue;

                    foundPartitions.AddRange(partitions);
                }

                checkedLocations.Add(imagePartition.Start);
            }
        }

        if(!checkedLocations.Contains(0) && image.Info.Sectors > 0)
        {
            foreach(IPartition plugin in plugins.Partitions.Values)
            {
                if(plugin is null)
                    continue;

                if(!plugin.GetInformation(image, out List<Partition> partitions, 0))
                    continue;

                foundPartitions.AddRange(partitions);
            }

            checkedLocations.Add(0);
        }

        while(foundPartitions.Count > 0)
        {
            if(checkedLocations.Contains(foundPartitions[0].Start))
            {
                childPartitions.Add(foundPartitions[0]);
                foundPartitions.RemoveAt(0);

                continue;
            }

            List<Partition> children = [];

            foreach(IPartition plugin in plugins.Partitions.Values)
            {
                if(plugin is null)
                    continue;

                if(!plugin.GetInformation(image, out List<Partition> partitions, foundPartitions[0].Start))
                    continue;

                children.AddRange(partitions);
            }

            checkedLocations.Add(foundPartitions[0].Start);

            if(children.Count > 0)
            {
                foundPartitions.RemoveAt(0);

                foreach(Partition child in children)
                {
                    if(checkedLocations.Contains(child.Start))
                        childPartitions.Add(child);
                    else
                        foundPartitions.Add(child);
                }
            }
            else
            {
                childPartitions.Add(foundPartitions[0]);
                foundPartitions.RemoveAt(0);
            }
        }

        if(partitionableImage is not null)
        {
            List<ulong> startLocations =
                childPartitions.ConvertAll(static detectedPartition => detectedPartition.Start);

            if(partitionableImage.Partitions != null)
            {
                childPartitions.AddRange(partitionableImage.Partitions.Where(imagePartition =>
                                                                                 !startLocations.Contains(imagePartition
                                                                                    .Start)));
            }
        }

        Partition[] childArray = childPartitions.OrderBy(static part => part.Start)
                                                .ThenBy(static part => part.Length)
                                                .ThenBy(static part => part.Scheme)
                                                .ToArray();

        for(long i = 0; i < childArray.LongLength; i++)
            childArray[i].Sequence = (ulong)i;

        return childArray.ToList();
    }
}