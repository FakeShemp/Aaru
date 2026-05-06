using System;
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Decryption.Aacs;
using Aaru.Localization;

namespace Aaru.Core.Image;

/// <summary>Blu-ray AACS sector pipeline for convert/merge: LBA-aligned 6144-byte CPS units with output staging.</summary>
internal static class AacsBdOpticalPipeline
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

    /// <summary>Runs the Blu-ray AACS sector pipeline.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="batchCount">Batch count.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="aborted">Aborted flag.</param>
    /// <param name="decryptedCpsUnitKeys">Decrypted CPS unit keys.</param>
    /// <param name="allowDecryptLba">Allow decrypt LBA function.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    internal static ErrorNumber Run(IOpticalMediaImage inputOptical, IWritableOpticalImage outputOptical, uint batchCount,
                                    bool force, in bool aborted, byte[][] decryptedCpsUnitKeys,
                                    Func<ulong, bool> allowDecryptLba, in Ui ui)
    {
        ui.OnInitProgress?.Invoke();
        ui.OnInitProgress2?.Invoke();

        int currentTrack = 0;

        foreach(Track track in inputOptical.Tracks)
        {
            if(aborted) break;

            ui.OnTrackProgress?.Invoke(currentTrack, inputOptical.Tracks.Count);

            ulong doneSectors  = 0;
            ulong trackSectors = track.EndSector - track.StartSector + 1;

            List<byte[]>       pending    = [];
            List<ulong>        pendingLba = [];
            List<SectorStatus> pendingSt  = [];

            while(doneSectors < trackSectors)
            {
                if(aborted) break;

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
                        ErrorNumber pend = FlushPendingOnReadSkip(outputOptical, pending, pendingLba,
                            pendingSt, force, in ui);

                        if(pend != ErrorNumber.NoError) return pend;

                        ui.ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing,
                                                              errno,
                                                              doneSectors + track.StartSector));
                    }
                    else
                    {
                        if(pending.Count > 0)
                        {
                            ui.StoppingErrorMessage?.Invoke(string.Format(
                                                                Aaru.Localization.Core.Aacs_incomplete_cps_unit_pending_before_read_error,
                                                                pendingLba[0]));

                            return errno;
                        }

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
                    ui.StoppingErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_bd_decrypt_requires_0_byte_sectors,
                                                                bps));

                    return ErrorNumber.InOutError;
                }

                for(uint i = 0; i < sectorsToDo; i++)
                {
                    byte[] one = new byte[AacsStreamDecrypt.SectorLen];
                    Buffer.BlockCopy(sectorData, (int)(i * AacsStreamDecrypt.SectorLen), one, 0, AacsStreamDecrypt.SectorLen);
                    ulong        lba = doneSectors + track.StartSector + i;
                    SectorStatus st  = sectorStatusArray[i];

                    bool allowDecrypt = allowDecryptLba is null || allowDecryptLba(lba);

                    ErrorNumber step = ProcessOneUserSector(outputOptical,
                                                            decryptedCpsUnitKeys,
                                                            one,
                                                            lba,
                                                            allowDecrypt,
                                                            st,
                                                            pending,
                                                            pendingLba,
                                                            pendingSt,
                                                            force,
                                                            in ui);

                    if(step != ErrorNumber.NoError) return step;
                }

                doneSectors += sectorsToDo;
            }

            ErrorNumber flushErr = FlushPendingAtTrackEnd(outputOptical,
                                                          pending,
                                                          pendingLba,
                                                          pendingSt,
                                                          force,
                                                          in ui);

            if(flushErr != ErrorNumber.NoError) return flushErr;

            currentTrack++;
        }

        ui.OnEndProgress2?.Invoke();
        ui.OnEndProgress?.Invoke();

        return ErrorNumber.NoError;
    }

    /// <summary>Flushes the pending sectors on read skip.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="pending">List of pending sectors.</param>
    /// <param name="pendingLba">List of pending LBA.</param>
    /// <param name="pendingSt">List of pending sector statuses.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber FlushPendingOnReadSkip(IWritableOpticalImage outputOptical, List<byte[]> pending,
                                              List<ulong> pendingLba, List<SectorStatus> pendingSt, bool force,
                                              in Ui ui)
    {
        if(pending.Count == 0) return ErrorNumber.NoError;

        ui.ErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_incomplete_cps_unit_after_read_error_continuing,
                                              pending.Count,
                                              pendingLba[0]));

        for(int i = 0; i < pending.Count; i++)
        {
            ErrorNumber step = WriteOne(outputOptical, pendingLba[i], pending[i], SectorStatus.Dumped, force, in ui);

            if(step != ErrorNumber.NoError) return step;
        }

        pending.Clear();
        pendingLba.Clear();
        pendingSt.Clear();

        return ErrorNumber.NoError;
    }

    /// <summary>Processes one user sector.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="decryptedCpsUnitKeys">Decrypted CPS unit keys.</param>
    /// <param name="sector">Sector data.</param>
    /// <param name="lba">LBA of the sector.</param>
    /// <param name="allowDecrypt">Allow decrypt flag.</param>
    /// <param name="readStatus">Read status of the sector.</param>
    /// <param name="pending">List of pending sectors.</param>
    /// <param name="pendingLba">List of pending LBA.</param>
    /// <param name="pendingSt">List of pending sector statuses.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber ProcessOneUserSector(IWritableOpticalImage outputOptical, byte[][] decryptedCpsUnitKeys,
                                            byte[] sector, ulong lba, bool allowDecrypt, SectorStatus readStatus,
                                            List<byte[]> pending,
                                            List<ulong> pendingLba, List<SectorStatus> pendingSt, bool force,
                                            in Ui ui)
    {
        if(!allowDecrypt)
        {
            ErrorNumber flush = FlushPendingAsDumped(outputOptical, pending, pendingLba, pendingSt, force, in ui);

            if(flush != ErrorNumber.NoError) return flush;

            return WriteOne(outputOptical, lba, sector, SectorStatus.Dumped, force, in ui);
        }

        if(pending.Count == 0)
        {
            if((sector[0] & 0xc0) == 0)
                return WriteOne(outputOptical, lba, sector, SectorStatus.Dumped, force, in ui);

            pending.Add(sector);
            pendingLba.Add(lba);
            pendingSt.Add(readStatus);

            return ErrorNumber.NoError;
        }

        pending.Add(sector);
        pendingLba.Add(lba);
        pendingSt.Add(readStatus);

        if(pending.Count < 3) return ErrorNumber.NoError;

        return FlushCompleteUnit(outputOptical,
                                 decryptedCpsUnitKeys,
                                 pending,
                                 pendingLba,
                                 pendingSt,
                                 force,
                                 in ui);
    }

    /// <summary>Flushes a complete unit.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="decryptedCpsUnitKeys">Decrypted CPS unit keys.</param>
    /// <param name="pending">List of pending sectors.</param>
    /// <param name="pendingLba">List of pending LBA.</param>
    /// <param name="pendingSt">List of pending sector statuses.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber FlushCompleteUnit(IWritableOpticalImage outputOptical, byte[][] decryptedCpsUnitKeys,
                                         List<byte[]> pending, List<ulong> pendingLba,
                                         List<SectorStatus> pendingSt, bool force, in Ui ui)
    {
        byte[] unit = new byte[AacsStreamDecrypt.AlignedUnitLen];
        Buffer.BlockCopy(pending[0], 0, unit, 0, AacsStreamDecrypt.SectorLen);
        Buffer.BlockCopy(pending[1], 0, unit, AacsStreamDecrypt.SectorLen, AacsStreamDecrypt.SectorLen);
        Buffer.BlockCopy(pending[2], 0, unit, AacsStreamDecrypt.SectorLen * 2, AacsStreamDecrypt.SectorLen);

        if(!AacsStreamDecrypt.TryDecryptAlignedUnit(unit, decryptedCpsUnitKeys))
        {
            if(!force)
            {
                ui.StoppingErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_could_not_decrypt_cps_unit_starting_at_0,
                                                                pendingLba[0]));

                return ErrorNumber.WriteError;
            }

            ui.ErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_could_not_decrypt_cps_unit_starting_at_0_continuing,
                                                    pendingLba[0]));

            ErrorNumber w = WriteThree(outputOptical,
                                       pending[0],
                                       pending[1],
                                       pending[2],
                                       pendingLba[0],
                                       pendingSt[0],
                                       pendingSt[1],
                                       pendingSt[2],
                                       force,
                                       in ui);

            pending.Clear();
            pendingLba.Clear();
            pendingSt.Clear();

            return w;
        }

        SectorStatus[] ok = [SectorStatus.Unencrypted, SectorStatus.Unencrypted, SectorStatus.Unencrypted];
        ErrorNumber    e  = WriteSectorsFromBuffer(outputOptical, unit, pendingLba[0], ok, force, in ui);

        pending.Clear();
        pendingLba.Clear();
        pendingSt.Clear();

        return e;
    }

    /// <summary>Writes three sectors from a buffer.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="unit">Unit data.</param>
    /// <param name="firstLba">First LBA of the sectors.</param>
    /// <param name="threeStatuses">Statuses of the sectors.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber WriteSectorsFromBuffer(IWritableOpticalImage outputOptical, byte[] unit, ulong firstLba,
                                              SectorStatus[] threeStatuses, bool force, in Ui ui)
    {
        bool result = outputOptical.WriteSectors(unit, firstLba, false, 3, threeStatuses);

        if(!result)
        {
            if(force)
            {
                ui.ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_continuing,
                                                      outputOptical.ErrorMessage,
                                                      firstLba));
            }
            else
            {
                ui.StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                              outputOptical.ErrorMessage,
                                                              firstLba));

                return ErrorNumber.WriteError;
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Flushes the pending sectors as dumped.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="pending">List of pending sectors.</param>
    /// <param name="pendingLba">List of pending LBA.</param>
    /// <param name="pendingSt">List of pending sector statuses.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber FlushPendingAsDumped(IWritableOpticalImage outputOptical, List<byte[]> pending,
                                            List<ulong> pendingLba, List<SectorStatus> pendingSt, bool force,
                                            in Ui ui)
    {
        for(int i = 0; i < pending.Count; i++)
        {
            ErrorNumber step = WriteOne(outputOptical, pendingLba[i], pending[i], SectorStatus.Dumped, force, in ui);

            if(step != ErrorNumber.NoError) return step;
        }

        pending.Clear();
        pendingLba.Clear();
        pendingSt.Clear();

        return ErrorNumber.NoError;
    }

    /// <summary>Writes three sectors from a buffer.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="a">First sector data.</param>
    /// <param name="b">Second sector data.</param>
    /// <param name="c">Third sector data.</param>
    /// <param name="firstLba">First LBA of the sectors.</param>
    /// <param name="sa">Status of the first sector.</param>
    /// <param name="sb">Status of the second sector.</param>
    /// <param name="sc">Status of the third sector.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber WriteThree(IWritableOpticalImage outputOptical, byte[] a, byte[] b, byte[] c, ulong firstLba,
                                  SectorStatus sa, SectorStatus sb, SectorStatus sc, bool force, in Ui ui)
    {
        byte[] buf = new byte[AacsStreamDecrypt.AlignedUnitLen];
        Buffer.BlockCopy(a, 0, buf, 0, AacsStreamDecrypt.SectorLen);
        Buffer.BlockCopy(b, 0, buf, AacsStreamDecrypt.SectorLen, AacsStreamDecrypt.SectorLen);
        Buffer.BlockCopy(c, 0, buf, AacsStreamDecrypt.SectorLen * 2, AacsStreamDecrypt.SectorLen);

        SectorStatus[] sts = [sa, sb, sc];

        return WriteSectorsFromBuffer(outputOptical, buf, firstLba, sts, force, in ui);
    }

    /// <summary>Writes one sector.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="lba">LBA of the sector.</param>
    /// <param name="sector">Sector data.</param>
    /// <param name="status">Status of the sector.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber WriteOne(IWritableOpticalImage outputOptical, ulong lba, byte[] sector, SectorStatus status,
                                bool force, in Ui ui)
    {
        bool result = outputOptical.WriteSector(sector, lba, false, status);

        if(!result)
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

        return ErrorNumber.NoError;
    }

    /// <summary>Flushes the pending sectors at track end.</summary>
    /// <param name="outputOptical">Output optical media image.</param>
    /// <param name="pending">List of pending sectors.</param>
    /// <param name="pendingLba">List of pending LBA.</param>
    /// <param name="pendingSt">List of pending sector statuses.</param>
    /// <param name="force">Force flag.</param>
    /// <param name="ui">UI.</param>
    /// <returns>Error number.</returns>
    static ErrorNumber FlushPendingAtTrackEnd(IWritableOpticalImage outputOptical, List<byte[]> pending,
                                              List<ulong> pendingLba, List<SectorStatus> pendingSt, bool force,
                                              in Ui ui)
    {
        if(pending.Count == 0) return ErrorNumber.NoError;

        if(!force)
        {
            ui.StoppingErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_incomplete_cps_unit_at_track_end,
                                                            pending.Count,
                                                            pendingLba[0]));

            return ErrorNumber.WriteError;
        }

        ui.ErrorMessage?.Invoke(string.Format(Aaru.Localization.Core.Aacs_incomplete_cps_unit_at_track_end_continuing,
                                              pending.Count,
                                              pendingLba[0]));

        for(int i = 0; i < pending.Count; i++)
        {
            ErrorNumber step = WriteOne(outputOptical, pendingLba[i], pending[i], SectorStatus.Dumped, force, in ui);

            if(step != ErrorNumber.NoError) return step;
        }

        pending.Clear();
        pendingLba.Clear();
        pendingSt.Clear();

        return ErrorNumber.NoError;
    }
}
