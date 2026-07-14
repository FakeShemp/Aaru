using System;
using System.Collections.Generic;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    /// <summary>Merges HD DVD AACS optical sectors.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <returns>Error number.</returns>
    ErrorNumber CopyAacsHddvdOpticalSectorsPrimary(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical)
    {
        ErrorNumber errno = AacsHddvdExtentResolver.ResolveEvoFileExtents(inputOptical,
                                                                          _plugins,
                                                                          out List<AacsHddvdExtentResolver.LbaRange> ranges,
                                                                          out string err);

        if(errno != ErrorNumber.NoError)
        {
            StoppingErrorMessage?.Invoke(err);

            return errno;
        }

        Func<ulong, bool> allowDecryptLba = lba => AacsHddvdExtentResolver.IsLbaAllowed(ranges, lba);

        return AacsHddvdOpticalPipeline.Run(inputOptical,
                                            outputOptical,
                                            (uint)count,
                                            false,
                                            ref _aborted,
                                            _aacsDecryptedCpsUnitKeys,
                                            allowDecryptLba,
                                            new AacsHddvdOpticalPipeline.Ui
                                            {
                                                OnInitProgress         = () => InitProgress?.Invoke(),
                                                OnInitProgress2        = () => InitProgress2?.Invoke(),
                                                OnEndProgress          = () => EndProgress?.Invoke(),
                                                OnEndProgress2         = () => EndProgress2?.Invoke(),
                                                OnTrackProgress        = (i, n) => UpdateProgress?.Invoke(string.Format(UI.Copying_sectors_in_track_0_of_1, i + 1, n), i, n),
                                                OnSectorRangeProgress = (start, end, seq, done, tot) =>
                                                    UpdateProgress2?.Invoke(string.Format(UI.Copying_sectors_0_to_1_in_track_2,
                                                                                          start,
                                                                                          end,
                                                                                          seq),
                                                                            done,
                                                                            tot),
                                                ErrorMessage           = s => ErrorMessage?.Invoke(s),
                                                StoppingErrorMessage   = s => StoppingErrorMessage?.Invoke(s)
                                            });
    }
}
