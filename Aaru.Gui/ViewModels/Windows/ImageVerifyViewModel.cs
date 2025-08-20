// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ImageVerifyViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the image verification window.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Windows.Input;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using Aaru.Gui.Models;
using Aaru.Localization;
using Aaru.Logging;
using Avalonia.Controls;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Humanizer.Localisation;
using Sentry;

namespace Aaru.Gui.ViewModels.Windows;

public sealed partial class ImageVerifyViewModel : ViewModelBase
{
    readonly IMediaImage _inputFormat;
    readonly Window      _view;
    [ObservableProperty]
    bool _cancel;
    [ObservableProperty]
    bool _closeVisible;
    [ObservableProperty]
    string _imageResultText;
    [ObservableProperty]
    bool _imageResultVisible;
    [ObservableProperty]
    bool _optionsVisible;
    [ObservableProperty]
    bool _progress2Indeterminate;
    [ObservableProperty]
    double _progress2MaxValue;
    [ObservableProperty]
    string _progress2Text;
    [ObservableProperty]
    double _progress2Value;
    [ObservableProperty]
    bool _progress2Visible;
    [ObservableProperty]
    bool _progressIndeterminate;
    [ObservableProperty]
    double _progressMaxValue;
    [ObservableProperty]
    string _progressText;
    [ObservableProperty]
    double _progressValue;
    [ObservableProperty]
    bool _progressVisible;
    [ObservableProperty]
    bool _resultsVisible;
    [ObservableProperty]
    string _sectorErrorsText;
    [ObservableProperty]
    bool _sectorErrorsVisible;
    [ObservableProperty]
    string _sectorsErrorsAllText;
    [ObservableProperty]
    bool _sectorsErrorsAllVisible;
    [ObservableProperty]
    bool _sectorSummaryVisible;
    [ObservableProperty]
    string _sectorsUnknownAllText;
    [ObservableProperty]
    bool _sectorsUnknownAllVisible;
    [ObservableProperty]
    string _sectorsUnknownsText;
    [ObservableProperty]
    bool _sectorsUnknownsVisible;
    [ObservableProperty]
    bool _startVisible;
    [ObservableProperty]
    bool _stopEnabled;
    [ObservableProperty]
    bool _stopVisible;
    [ObservableProperty]
    string _totalSectorErrorsText;
    [ObservableProperty]
    string _totalSectorErrorsUnknownsText;
    [ObservableProperty]
    string _totalSectorsText;
    [ObservableProperty]
    string _totalSectorUnknownsText;
    [ObservableProperty]
    bool _verifyImageChecked;
    [ObservableProperty]
    bool _verifyImageEnabled;
    [ObservableProperty]
    bool _verifySectorsChecked;
    [ObservableProperty]
    bool _verifySectorsEnabled;
    [ObservableProperty]
    bool _verifySectorsVisible;

    public ImageVerifyViewModel(IMediaImage inputFormat, Window view)
    {
        _view                = view;
        StartCommand         = new RelayCommand(Start);
        CloseCommand         = new RelayCommand(Close);
        StopCommand          = new RelayCommand(Stop);
        _inputFormat         = inputFormat;
        _cancel              = false;
        ErrorList            = [];
        UnknownList          = [];
        VerifyImageEnabled   = true;
        VerifySectorsEnabled = true;
        CloseVisible         = true;
        StartVisible         = true;
        OptionsVisible       = true;
    }

    public string VerifyImageLabel   => UI.Verify_media_image_if_supported;
    public string VerifySectorsLabel => UI.Verify_all_sectors_if_supported;
    public string LBALabel           => UI.Title_LBA;
    public string StartLabel         => UI.ButtonLabel_Start;
    public string CloseLabel         => UI.ButtonLabel_Close;
    public string StopLabel          => UI.ButtonLabel_Stop;

    public ObservableCollection<LbaModel> ErrorList    { get; }
    public ObservableCollection<LbaModel> UnknownList  { get; }
    public ICommand                       StartCommand { get; }
    public ICommand                       CloseCommand { get; }
    public ICommand                       StopCommand  { get; }


    void Start()
    {
        VerifyImageEnabled   = false;
        VerifySectorsEnabled = false;
        CloseVisible         = false;
        StartVisible         = false;
        StopVisible          = true;
        ProgressVisible      = true;
        Progress2Visible     = false;

        VerifySectorsVisible = _inputFormat is IOpticalMediaImage or IVerifiableSectorsImage;

        // TODO: Do not offer the option to use this form if the image does not support any kind of verification
        new Thread(DoWork).Start();
    }

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void DoWork()
    {
        bool formatHasTracks;
        var  inputOptical           = _inputFormat as IOpticalMediaImage;
        var  verifiableSectorsImage = _inputFormat as IVerifiableSectorsImage;

        try
        {
            formatHasTracks = inputOptical?.Tracks?.Count > 0;
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            formatHasTracks = false;
        }

        // Setup progress bars
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ProgressVisible  = true;
            ProgressMaxValue = 0;

            if(VerifyImageChecked || VerifySectorsChecked) ProgressMaxValue = 1;

            if(formatHasTracks && inputOptical != null)
                ProgressMaxValue += inputOptical.Tracks.Count;
            else
            {
                if(VerifySectorsChecked)
                {
                    ProgressMaxValue = 2;
                    Progress2Visible = false;
                    Progress2Visible = false;
                }
                else
                {
                    Progress2Visible = true;
                    Progress2Visible = true;
                }
            }

            ProgressMaxValue++;
        });

        if(VerifyImageChecked)
        {
            if(_inputFormat is not IVerifiableImage verifiableImage)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ImageResultVisible = true;
                    ImageResultText    = UI.Disc_image_does_not_support_verification;
                });
            }
            else
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ProgressText = UI.Checking_media_image;

                    if(VerifySectorsChecked)
                        ProgressValue = 1;
                    else
                        ProgressIndeterminate = true;

                    Progress2Indeterminate = true;
                });

                var chkStopwatch = new Stopwatch();
                chkStopwatch.Start();
                bool? discCheckStatus = verifiableImage.VerifyMediaImage();
                chkStopwatch.Stop();

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    ImageResultVisible = true;

                    ImageResultText = discCheckStatus switch
                                      {
                                          true  => UI.Disc_image_checksums_are_correct,
                                          false => UI.Disc_image_checksums_are_incorrect,
                                          null  => UI.Disc_image_does_not_contain_checksums
                                      };
                });

                AaruLogging.Verbose(UI.Checking_disc_image_checksums_took_0,
                                    chkStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second));
            }
        }

        if(VerifySectorsChecked)
        {
            var         chkStopwatch = new Stopwatch();
            List<ulong> failingLbas  = [];
            List<ulong> unknownLbas  = [];

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                Progress2Visible       = true;
                Progress2Indeterminate = false;
                Progress2MaxValue      = _inputFormat.Info.Sectors / 512d;
                StopEnabled            = true;
            });

            if(formatHasTracks)
            {
                ulong currentSectorAll = 0;

                chkStopwatch.Restart();

                foreach(Track currentTrack in inputOptical.Tracks)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ProgressText = string.Format(UI.Verifying_track_0_of_1,
                                                     currentTrack.Sequence,
                                                     inputOptical.Tracks.Count);

                        ProgressValue++;
                    });

                    ulong remainingSectors = currentTrack.EndSector - currentTrack.StartSector;
                    ulong currentSector    = 0;

                    while(remainingSectors > 0)
                    {
                        if(_cancel)
                        {
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                CloseVisible = true;
                                StartVisible = false;
                                StopVisible  = false;
                            });

                            return;
                        }

                        ulong all = currentSectorAll;

                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Progress2Value = all / 512d;

                            Progress2Text = string.Format(UI.Checking_sector_0_of_1_on_track_2,
                                                          all,
                                                          _inputFormat.Info.Sectors,
                                                          currentTrack.Sequence);
                        });

                        List<ulong> tempFailingLbas;
                        List<ulong> tempUnknownLbas;

                        if(remainingSectors < 512)
                        {
                            inputOptical.VerifySectors(currentSector,
                                                       (uint)remainingSectors,
                                                       currentTrack.Sequence,
                                                       out tempFailingLbas,
                                                       out tempUnknownLbas);
                        }
                        else
                        {
                            inputOptical.VerifySectors(currentSector,
                                                       512,
                                                       currentTrack.Sequence,
                                                       out tempFailingLbas,
                                                       out tempUnknownLbas);
                        }

                        failingLbas.AddRange(tempFailingLbas);

                        unknownLbas.AddRange(tempUnknownLbas);

                        if(remainingSectors < 512)
                        {
                            currentSector    += remainingSectors;
                            currentSectorAll += remainingSectors;
                            remainingSectors =  0;
                        }
                        else
                        {
                            currentSector    += 512;
                            currentSectorAll += 512;
                            remainingSectors -= 512;
                        }
                    }
                }

                chkStopwatch.Stop();
            }
            else if(verifiableSectorsImage is not null)
            {
                ulong remainingSectors = _inputFormat.Info.Sectors;
                ulong currentSector    = 0;

                chkStopwatch.Restart();

                while(remainingSectors > 0)
                {
                    if(_cancel)
                    {
                        await Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            CloseVisible = true;
                            StartVisible = false;
                            StopVisible  = false;
                        });

                        return;
                    }

                    ulong sector = currentSector;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Progress2Value = (int)(sector / 512);
                        Progress2Text  = string.Format(UI.Checking_sector_0_of_1, sector, _inputFormat.Info.Sectors);
                    });

                    List<ulong> tempFailingLbas;
                    List<ulong> tempUnknownLbas;

                    if(remainingSectors < 512)
                    {
                        verifiableSectorsImage.VerifySectors(currentSector,
                                                             (uint)remainingSectors,
                                                             out tempFailingLbas,
                                                             out tempUnknownLbas);
                    }
                    else
                    {
                        verifiableSectorsImage.VerifySectors(currentSector,
                                                             512,
                                                             out tempFailingLbas,
                                                             out tempUnknownLbas);
                    }

                    failingLbas.AddRange(tempFailingLbas);

                    unknownLbas.AddRange(tempUnknownLbas);

                    if(remainingSectors < 512)
                    {
                        currentSector    += remainingSectors;
                        remainingSectors =  0;
                    }
                    else
                    {
                        currentSector    += 512;
                        remainingSectors -= 512;
                    }
                }

                chkStopwatch.Stop();
            }

            AaruLogging.Verbose(UI.Checking_sector_checksums_took_0,
                                chkStopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second));

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if(failingLbas.Count > 0)
                {
                    if(failingLbas.Count == (int)_inputFormat.Info.Sectors)
                    {
                        SectorsErrorsAllVisible = true;
                        SectorsErrorsAllText    = UI.All_sectors_contain_errors;
                    }
                    else
                    {
                        SectorErrorsText    = UI.LBAs_with_error;
                        SectorErrorsVisible = true;

                        foreach(ulong t in failingLbas)
                        {
                            ErrorList.Add(new LbaModel
                            {
                                Lba = t.ToString()
                            });
                        }
                    }
                }

                if(unknownLbas.Count > 0)
                {
                    if(unknownLbas.Count == (int)_inputFormat.Info.Sectors)
                    {
                        SectorsUnknownAllVisible = true;
                        SectorsUnknownAllText    = UI.All_sectors_are_unknown;
                    }
                    else
                    {
                        SectorsUnknownsText    = UI.Unknown_LBAs;
                        SectorsUnknownsVisible = true;

                        foreach(ulong t in unknownLbas)
                        {
                            UnknownList.Add(new LbaModel
                            {
                                Lba = t.ToString()
                            });
                        }
                    }
                }

                SectorSummaryVisible    = true;
                TotalSectorsText        = string.Format(UI.Total_sectors,  _inputFormat.Info.Sectors);
                TotalSectorErrorsText   = string.Format(UI.Total_errors,   failingLbas.Count);
                TotalSectorUnknownsText = string.Format(UI.Total_unknowns, unknownLbas.Count);

                TotalSectorErrorsUnknownsText =
                    string.Format(UI.Total_errors_plus_unknowns, failingLbas.Count + unknownLbas.Count);
            });
        }

        Statistics.AddCommand("verify");

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            OptionsVisible  = false;
            ResultsVisible  = true;
            ProgressVisible = false;
            StartVisible    = false;
            StopVisible     = false;
            CloseVisible    = true;
        });
    }

    void Close() => _view.Close();

    internal void Stop()
    {
        _cancel     = true;
        StopEnabled = false;
    }
}