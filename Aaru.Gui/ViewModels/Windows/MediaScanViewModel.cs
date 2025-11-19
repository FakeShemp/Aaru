// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaScanViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the media scan window.
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

using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Core;
using Aaru.Core.Devices.Scanning;
using Aaru.Devices;
using Aaru.Localization;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Humanizer.Bytes;
using Humanizer.Localisation;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

//using OxyPlot;

namespace Aaru.Gui.ViewModels.Windows;

public sealed partial class MediaScanViewModel : ViewModelBase
{
    readonly Device _device;
    readonly Window _view;
    [ObservableProperty]
    string _a;
    [ObservableProperty]
    string _avgSpeed;
    [ObservableProperty]
    Color _axesColor;
    [ObservableProperty]
    string _b;
    [ObservableProperty]
    ulong _blocks;
    ulong _blocksToRead;
    [ObservableProperty]
    string _c;
    [ObservableProperty]
    bool _closeVisible;
    [ObservableProperty]
    string _d;
    readonly string _devicePath;
    [ObservableProperty]
    string _e;
    [ObservableProperty]
    string _f;
    [ObservableProperty]
    Color _lineColor;
    ScanResults _localResults;
    [ObservableProperty]
    string _maxSpeed;
    [ObservableProperty]
    double _maxX;
    [ObservableProperty]
    double _maxY;
    [ObservableProperty]
    string _minSpeed;
    [ObservableProperty]
    double _minX;
    [ObservableProperty]
    double _minY;
    [ObservableProperty]
    bool _progress1Visible;
    [ObservableProperty]
    string _progress2Indeterminate;
    [ObservableProperty]
    string _progress2MaxValue;
    [ObservableProperty]
    string _progress2Text;
    [ObservableProperty]
    string _progress2Value;
    [ObservableProperty]
    string _progress2Visible;
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
    MediaScan _scanner;
    [ObservableProperty]
    bool _startVisible;
    [ObservableProperty]
    double _stepsX;
    [ObservableProperty]
    double _stepsY;
    [ObservableProperty]
    string _stopEnabled;
    [ObservableProperty]
    bool _stopVisible;
    [ObservableProperty]
    string _totalTime;
    [ObservableProperty]
    string _unreadableSectors;

    public MediaScanViewModel(Device device, string devicePath, Window view)
    {
        _device      = device;
        _devicePath  = devicePath;
        _view        = view;
        StopVisible  = false;
        StartCommand = new RelayCommand(Start);
        CloseCommand = new RelayCommand(Close);
        StopCommand  = new RelayCommand(Stop);
        StartVisible = true;
        CloseVisible = true;
        BlockMapList = [];

//        ChartPoints  = new ObservableCollection<DataPoint>();
        StepsX    = double.NaN;
        StepsY    = double.NaN;
        AxesColor = Colors.Black;
        LineColor = Colors.Yellow;
    }

    public string SpeedLabel => UI.ButtonLabel_Stop;
    public string KbsLabel   => UI.Kb_s;
    public string BlockLabel => UI.Title_Block;

    public ObservableCollection<(ulong block, double duration)> BlockMapList { get; }

//    public ObservableCollection<DataPoint>                      ChartPoints  { get; }

    public string Title { get; }

    public ICommand StartCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand StopCommand  { get; }

    void Close() => _view.Close();

    internal void Stop() => _scanner?.Abort();

    void Start()
    {
        StopVisible     = true;
        StartVisible    = false;
        CloseVisible    = false;
        ProgressVisible = true;
        ResultsVisible  = true;

//        ChartPoints.Clear();
        new Thread(DoWork).Start();
    }

    // TODO: Allow to save MHDD and ImgBurn log files
    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void DoWork()
    {
        _localResults                 =  new ScanResults();
        _scanner                      =  new MediaScan(null, null, _devicePath, _device, false);
        _scanner.ScanTime             += OnScanTime;
        _scanner.ScanUnreadable       += OnScanUnreadable;
        _scanner.UpdateStatus         += UpdateStatus;
        _scanner.StoppingErrorMessage += StoppingErrorMessage;
        _scanner.PulseProgress        += PulseProgress;
        _scanner.InitProgress         += InitProgress;
        _scanner.UpdateProgress       += UpdateProgress;
        _scanner.EndProgress          += EndProgress;
        _scanner.InitBlockMap         += InitBlockMap;
        _scanner.ScanSpeed            += ScanSpeed;

        ScanResults results = _scanner.Scan();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            TotalTime = string.Format(Localization.Core.Took_a_total_of_0_1_processing_commands,
                                      results.TotalTime.Seconds().Humanize(minUnit: TimeUnit.Second),
                                      results.ProcessingTime.Seconds().Humanize(minUnit: TimeUnit.Second));

            AvgSpeed = string.Format(Localization.Core.Average_speed_0,
                                     ByteSize.FromMegabytes(results.AvgSpeed).Per(1.Seconds()).Humanize());

            MaxSpeed = string.Format(Localization.Core.Fastest_speed_burst_0,
                                     ByteSize.FromMegabytes(results.MaxSpeed).Per(1.Seconds()).Humanize());

            MinSpeed = string.Format(Localization.Core.Slowest_speed_burst_0,
                                     ByteSize.FromMegabytes(results.MinSpeed).Per(1.Seconds()).Humanize());

            A = string.Format(Localization.Core._0_sectors_took_less_than_3_ms,                        results.A);
            B = string.Format(Localization.Core._0_sectors_took_less_than_10_ms_but_more_than_3_ms,    results.B);
            C = string.Format(Localization.Core._0_sectors_took_less_than_50_ms_but_more_than_10_ms,   results.C);
            D = string.Format(Localization.Core._0_sectors_took_less_than_150_ms_but_more_than_50_ms,  results.D);
            E = string.Format(Localization.Core._0_sectors_took_less_than_500_ms_but_more_than_150_ms, results.E);
            F = string.Format(Localization.Core._0_sectors_took_more_than_500_ms,                      results.F);

            UnreadableSectors = string.Format(Localization.Core._0_sectors_could_not_be_read,
                                              results.UnreadableSectors.Count);
        });

        // TODO: Show list of unreadable sectors
        /*
        if(results.UnreadableSectors.Count > 0)
            foreach(ulong bad in results.UnreadableSectors)
                string.Format("Sector {0} could not be read", bad);
*/

        // TODO: Show results
        /*

        if(results.SeekTotal != 0 || results.SeekMin != double.MaxValue || results.SeekMax != double.MinValue)

            string.Format("Testing {0} seeks, longest seek took {1:F3} ms, fastest one took {2:F3} ms. ({3:F3} ms average)",
                                 results.SeekTimes, results.SeekMax, results.SeekMin, results.SeekTotal / 1000);
                                 */

        Statistics.AddCommand("media-scan");

        await WorkFinished();
    }

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void ScanSpeed(ulong sector, double currentSpeed) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        /*  TODO: Abandoned project need to find replacement
        if(ChartPoints.Count == 0)
            ChartPoints.Add(new DataPoint(0, currentSpeed));

        ChartPoints.Add(new DataPoint(sector, currentSpeed));
        */

        if(currentSpeed > MaxY) MaxY = currentSpeed + currentSpeed / 10d;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void InitBlockMap(ulong blocks, ulong blockSize, ulong blocksToRead, ushort currentProfile) =>
        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            Blocks        = blocks / blocksToRead;
            _blocksToRead = blocksToRead;

            MinX = 0;
            MinY = 0;

            switch(currentProfile)
            {
                case 0x0005: // CD and DDCD
                case 0x0008:
                case 0x0009:
                case 0x000A:
                case 0x0020:
                case 0x0021:
                case 0x0022:
                    MaxX = blocks switch
                           {
                               <= 360000 => 360000,
                               <= 405000 => 405000,
                               <= 445500 => 445500,
                               _         => blocks
                           };

                    StepsX = MaxX   / 10;
                    StepsY = 150    * 4;
                    MaxY   = StepsY * 12.5;

                    break;
                case 0x0010: // DVD SL
                case 0x0011:
                case 0x0012:
                case 0x0013:
                case 0x0014:
                case 0x0018:
                case 0x001A:
                case 0x001B:
                    MaxX   = 2298496;
                    StepsX = MaxX / 10;
                    StepsY = 1352.5;
                    MaxY   = StepsY * 18;

                    break;
                case 0x0015: // DVD DL
                case 0x0016:
                case 0x0017:
                case 0x002A:
                case 0x002B:
                    MaxX   = 4173824;
                    StepsX = MaxX / 10;
                    StepsY = 1352.5;
                    MaxY   = StepsY * 18;

                    break;
                case 0x0041:
                case 0x0042:
                case 0x0043:
                case 0x0040: // BD
                    MaxX = blocks switch
                           {
                               <= 12219392 => 12219392,
                               <= 24438784 => 24438784,
                               <= 48878592 => 48878592,
                               <= 62500864 => 62500864,
                               _           => blocks
                           };

                    StepsX = MaxX / 10;
                    StepsY = 4394.5;
                    MaxY   = StepsY * 18;

                    break;
                case 0x0050: // HD DVD
                case 0x0051:
                case 0x0052:
                case 0x0053:
                case 0x0058:
                case 0x005A:
                    MaxX = blocks switch
                           {
                               <= 7361599  => 7361599,
                               <= 16305407 => 16305407,
                               _           => blocks
                           };

                    StepsX = MaxX / 10;
                    StepsY = 4394.5;
                    MaxY   = StepsY * 8;

                    break;
                default:
                    MaxX   = blocks;
                    StepsX = MaxX / 10;
                    StepsY = 625;
                    MaxY   = StepsY;

                    break;
            }
        });

    async Task WorkFinished() => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        StopVisible     = false;
        StartVisible    = true;
        CloseVisible    = true;
        ProgressVisible = false;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void EndProgress() => await Dispatcher.UIThread.InvokeAsync(() => { Progress1Visible = false; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void UpdateProgress(string text, long current, long maximum) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        ProgressText          = text;
        ProgressIndeterminate = false;

        ProgressMaxValue = maximum;
        ProgressValue    = current;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void InitProgress() => await Dispatcher.UIThread.InvokeAsync(() => { Progress1Visible = true; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void PulseProgress(string text) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        ProgressText          = text;
        ProgressIndeterminate = true;
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]

    // ReSharper disable once AsyncVoidLambda
    async void StoppingErrorMessage(string text) => await Dispatcher.UIThread.InvokeAsync(async () =>
    {
        ProgressText = text;

        await MessageBoxManager.GetMessageBoxStandard(UI.Title_Error, $"{text}", ButtonEnum.Ok, Icon.Error)
                               .ShowWindowDialogAsync(_view);

        await WorkFinished();
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void UpdateStatus(string text) => await Dispatcher.UIThread.InvokeAsync(() => { ProgressText = text; });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void OnScanUnreadable(ulong sector) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        _localResults.Errored += _blocksToRead;
        UnreadableSectors     =  string.Format(Localization.Core._0_sectors_could_not_be_read, _localResults.Errored);
        BlockMapList.Add((sector / _blocksToRead, double.NaN));
    });

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    async void OnScanTime(ulong sector, double duration) => await Dispatcher.UIThread.InvokeAsync(() =>
    {
        BlockMapList.Add((sector / _blocksToRead, duration));

        switch(duration)
        {
            case < 3:
                _localResults.A += _blocksToRead;

                break;
            case >= 3 and < 10:
                _localResults.B += _blocksToRead;

                break;
            case >= 10 and < 50:
                _localResults.C += _blocksToRead;

                break;
            case >= 50 and < 150:
                _localResults.D += _blocksToRead;

                break;
            case >= 150 and < 500:
                _localResults.E += _blocksToRead;

                break;
            case >= 500:
                _localResults.F += _blocksToRead;

                break;
        }

        A = string.Format(Localization.Core._0_sectors_took_less_than_3_ms,                        _localResults.A);
        B = string.Format(Localization.Core._0_sectors_took_less_than_10_ms_but_more_than_3_ms,    _localResults.B);
        C = string.Format(Localization.Core._0_sectors_took_less_than_50_ms_but_more_than_10_ms,   _localResults.C);
        D = string.Format(Localization.Core._0_sectors_took_less_than_150_ms_but_more_than_50_ms,  _localResults.D);
        E = string.Format(Localization.Core._0_sectors_took_less_than_500_ms_but_more_than_150_ms, _localResults.E);
        F = string.Format(Localization.Core._0_sectors_took_more_than_500_ms,                      _localResults.F);
    });
}