using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>HD DVD AACS: 16-sector CPS units, UDF <c>.EVO</c> extent filter, per-sector <see cref="SectorStatus" />.</summary>
    ErrorNumber ConvertAacsHddvdOpticalSectors(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical)
    {
        ErrorNumber errno = AacsHddvdExtentResolver.ResolveEvoFileExtents(inputOptical,
                                                                          _plugins,
                                                                          out List<AacsHddvdExtentResolver.LbaRange> ranges,
                                                                          out string err);

        Func<ulong, bool> allowDecryptLba = null;

        if(errno == ErrorNumber.NoError)
            allowDecryptLba = lba => AacsHddvdExtentResolver.IsLbaAllowed(ranges, lba);
        else if(_force)
            ErrorMessage?.Invoke(err + " " + Aaru.Localization.Core.Aacs_hddvd_broad_decrypt_scope_continuing);
        else
        {
            StoppingErrorMessage?.Invoke(err);

            return errno;
        }

        return AacsHddvdOpticalPipeline.Run(inputOptical,
                                            outputOptical,
                                            _count,
                                            _force,
                                            ref _aborted,
                                            _aacsDecryptedCpsUnitKeys,
                                            allowDecryptLba,
                                            new AacsHddvdOpticalPipeline.Ui
                                            {
                                                OnInitProgress         = () => InitProgress?.Invoke(),
                                                OnInitProgress2        = () => InitProgress2?.Invoke(),
                                                OnEndProgress          = () => EndProgress?.Invoke(),
                                                OnEndProgress2         = () => EndProgress2?.Invoke(),
                                                OnTrackProgress        = (i, n) => UpdateProgress?.Invoke(string.Format(UI.Converting_sectors_in_track_0_of_1, i + 1, n), i, n),
                                                OnSectorRangeProgress = (start, end, seq, done, tot) =>
                                                    UpdateProgress2?.Invoke(string.Format(UI.Converting_sectors_0_to_1_in_track_2,
                                                                                          start,
                                                                                          end,
                                                                                          seq),
                                                                            done,
                                                                            tot),
                                                ErrorMessage           = msg => ErrorMessage?.Invoke(msg),
                                                StoppingErrorMessage   = msg => StoppingErrorMessage?.Invoke(msg)
                                            });
    }
}
