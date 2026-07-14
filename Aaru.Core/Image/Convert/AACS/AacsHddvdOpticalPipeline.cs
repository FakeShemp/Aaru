using System;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decryption.Aacs;
using Aaru.Localization;

namespace Aaru.Core.Image;

/// <summary>HD DVD AACS sector pipeline: per-pack decryption driven by NV_PCK CPI / TITLE_KEY_PTR.</summary>
internal static class AacsHddvdOpticalPipeline
{
    internal readonly struct Ui
    {
        public Action OnInitProgress { get; init; }
        public Action OnInitProgress2 { get; init; }
        public Action OnEndProgress { get; init; }
        public Action OnEndProgress2 { get; init; }
        public Action<int, int> OnTrackProgress { get; init; }
        public Action<ulong, ulong, uint, long, long> OnSectorRangeProgress { get; init; }
        public Action<string> ErrorMessage { get; init; }
        public Action<string> StoppingErrorMessage { get; init; }
    }

    internal static ErrorNumber Run(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical, uint batchCount,
                                    bool force, ref bool aborted, byte[][] decryptedTitleKeys,
                                    Func<ulong, bool> allowDecryptLba, Ui ui)
    {
        ui.OnInitProgress?.Invoke();
        ui.OnInitProgress2?.Invoke();

        int    currentTrack    = 0;
        byte[] currentCpi      = null;
        int    currentKeyPtr   = 0;

        foreach(Track track in inputOptical.Tracks)
        {
            if(aborted)
                break;

            ui.OnTrackProgress?.Invoke(currentTrack, inputOptical.Tracks.Count);

            ulong doneSectors  = 0;
            ulong trackSectors = track.EndSector - track.StartSector + 1;

            while(doneSectors < trackSectors)
            {
                if(aborted)
                    break;

                uint sectorsToDo = trackSectors - doneSectors >= batchCount
                                       ? batchCount
                                       : (uint)(trackSectors - doneSectors);

                ui.OnSectorRangeProgress?.Invoke(doneSectors + track.StartSector,
                                                 doneSectors + sectorsToDo + track.StartSector,
                                                 track.Sequence,
                                                 (long)doneSectors,
                                                 (long)trackSectors);

                ErrorNumber errno = inputOptical.ReadSectors(doneSectors + track.StartSector,
                                                             false,
                                                             sectorsToDo,
                                                             out byte[] sectorData,
                                                             out SectorStatus[] sectorStatusArray);

                if(errno != ErrorNumber.NoError)
                {
                    if(force)
                    {
                        ui.ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing,
                                                              errno,
                                                              doneSectors + track.StartSector));
                    }
                    else
                    {
                        ui.StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                                      errno,
                                                                      doneSectors + track.StartSector));

                        return errno;
                    }

                    doneSectors += sectorsToDo;

                    continue;
                }

                int bps = sectorData.Length / (int)sectorsToDo;

                if(bps != AacsStreamDecrypt.SectorLen || sectorData.Length % sectorsToDo != 0)
                {
                    ui.StoppingErrorMessage?.Invoke(
                        string.Format(Aaru.Localization.Core.Aacs_hddvd_decrypt_requires_2048_byte_sectors, bps));

                    return ErrorNumber.InOutError;
                }

                for(uint i = 0; i < sectorsToDo; i++)
                {
                    ulong lba = doneSectors + track.StartSector + i;

                    byte[] sector = new byte[AacsStreamDecrypt.SectorLen];

                    Buffer.BlockCopy(sectorData,
                                     (int)(i * AacsStreamDecrypt.SectorLen),
                                     sector,
                                     0,
                                     AacsStreamDecrypt.SectorLen);

                    bool         allowDecrypt = allowDecryptLba is null || allowDecryptLba(lba);
                    SectorStatus outStatus    = sectorStatusArray[i];

                    if(allowDecrypt)
                    {
                        if(AacsStreamDecrypt.IsHddvdNavPack(sector))
                        {
                            AacsStreamDecrypt.ParseHddvdNavPack(sector, out currentCpi, out currentKeyPtr);
                        }
                        else if(AacsStreamDecrypt.IsHddvdPackEncrypted(sector))
                        {
                            ErrorNumber dec = TryDecryptSector(sector,
                                                               decryptedTitleKeys,
                                                               currentCpi,
                                                               currentKeyPtr,
                                                               lba,
                                                               force,
                                                               ui,
                                                               out bool decrypted);

                            if(dec != ErrorNumber.NoError)
                                return dec;

                            if(decrypted)
                                outStatus = SectorStatus.Unencrypted;
                        }
                    }

                    bool ok = outputOptical.WriteSector(sector, lba, false, outStatus);

                    if(!ok)
                    {
                        if(force)
                        {
                            ui.ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_continuing,
                                                                  outputOptical.ErrorMessage,
                                                                  lba));
                        }
                        else
                        {
                            ui.StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                                          outputOptical.ErrorMessage,
                                                                          lba));

                            return ErrorNumber.WriteError;
                        }
                    }
                }

                doneSectors += sectorsToDo;
            }

            currentTrack++;
        }

        ui.OnEndProgress2?.Invoke();
        ui.OnEndProgress?.Invoke();

        return ErrorNumber.NoError;
    }

    static ErrorNumber TryDecryptSector(byte[] sector, byte[][] decryptedTitleKeys, byte[] currentCpi,
                                        int currentKeyPtr, ulong lba, bool force, Ui ui, out bool decrypted)
    {
        decrypted = false;

        if(currentCpi is null)
        {
            if(!force)
            {
                ui.StoppingErrorMessage?.Invoke(
                    string.Format(Aaru.Localization.Core.Aacs_hddvd_no_cpi_for_encrypted_pack_at_lba_0, lba));

                return ErrorNumber.NoData;
            }

            ui.ErrorMessage?.Invoke(
                string.Format(Aaru.Localization.Core.Aacs_hddvd_no_cpi_for_encrypted_pack_at_lba_0_continuing, lba));

            return ErrorNumber.NoError;
        }

        if(currentKeyPtr >= 1 &&
           currentKeyPtr <= decryptedTitleKeys.Length &&
           !AacsKeyResolver.IsAllZero(decryptedTitleKeys[currentKeyPtr - 1]))
        {
            AacsStreamDecrypt.DecryptHddvdPack(sector, decryptedTitleKeys[currentKeyPtr - 1], currentCpi);
            decrypted = true;

            return ErrorNumber.NoError;
        }

        for(int k = 0; k < decryptedTitleKeys.Length; k++)
        {
            if(AacsKeyResolver.IsAllZero(decryptedTitleKeys[k]))
                continue;

            AacsStreamDecrypt.DecryptHddvdPack(sector, decryptedTitleKeys[k], currentCpi);
            decrypted = true;

            return ErrorNumber.NoError;
        }

        if(!force)
        {
            ui.StoppingErrorMessage?.Invoke(
                string.Format(Aaru.Localization.Core.Aacs_hddvd_no_title_key_for_pack_at_lba_0, lba));

            return ErrorNumber.NoData;
        }

        ui.ErrorMessage?.Invoke(
            string.Format(Aaru.Localization.Core.Aacs_hddvd_no_title_key_for_pack_at_lba_0_continuing, lba));

        return ErrorNumber.NoError;
    }
}