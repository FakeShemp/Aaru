// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : SettingsViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the settings dialog.
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

using System.Windows.Input;
using Aaru.Gui.Views.Dialogs;
using Aaru.Localization;
using Aaru.Settings;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace Aaru.Gui.ViewModels.Dialogs;

public sealed partial class SettingsViewModel : ViewModelBase
{
    readonly SettingsDialog _view;
    [ObservableProperty]
    bool _commandStatsChecked;
    [ObservableProperty]
    bool _deviceStatsChecked;
    [ObservableProperty]
    bool _filesystemStatsChecked;
    [ObservableProperty]
    bool _filterStatsChecked;
    [ObservableProperty]
    bool _gdprVisible;
    [ObservableProperty]
    bool _mediaImageStatsChecked;
    [ObservableProperty]
    bool _mediaScanStatsChecked;
    [ObservableProperty]
    bool _mediaStatsChecked;
    [ObservableProperty]
    bool _partitionStatsChecked;
    [ObservableProperty]
    bool _saveReportsGloballyChecked;
    [ObservableProperty]
    bool _saveStatsChecked;
    [ObservableProperty]
    bool _shareReportsChecked;
    [ObservableProperty]
    bool _shareStatsChecked;
    [ObservableProperty]
    int _tabControlSelectedIndex;
    [ObservableProperty]
    bool _verifyStatsChecked;

    public SettingsViewModel(SettingsDialog view, bool gdprChange)
    {
        _view                      = view;
        GdprVisible                = gdprChange;
        SaveReportsGloballyChecked = Settings.Settings.Current.SaveReportsGlobally;
        ShareReportsChecked        = Settings.Settings.Current.ShareReports;

        if(Settings.Settings.Current.Stats != null)
        {
            SaveStatsChecked       = true;
            ShareStatsChecked      = Settings.Settings.Current.Stats.ShareStats;
            CommandStatsChecked    = Settings.Settings.Current.Stats.CommandStats;
            DeviceStatsChecked     = Settings.Settings.Current.Stats.DeviceStats;
            FilesystemStatsChecked = Settings.Settings.Current.Stats.FilesystemStats;
            FilterStatsChecked     = Settings.Settings.Current.Stats.FilterStats;
            MediaImageStatsChecked = Settings.Settings.Current.Stats.MediaImageStats;
            MediaScanStatsChecked  = Settings.Settings.Current.Stats.MediaScanStats;
            PartitionStatsChecked  = Settings.Settings.Current.Stats.PartitionStats;
            MediaStatsChecked      = Settings.Settings.Current.Stats.MediaStats;
            VerifyStatsChecked     = Settings.Settings.Current.Stats.VerifyStats;
        }
        else
            SaveStatsChecked = false;

        CancelCommand = new RelayCommand(Cancel);
        SaveCommand   = new RelayCommand(Save);

        if(!_gdprVisible) _tabControlSelectedIndex = 1;
    }

    // TODO: Show Preferences in macOS
    [NotNull]
    public string Title => UI.Title_Settings;

    [NotNull]
    public string GdprLabel => UI.Title_GDPR;

    [NotNull]
    public string ReportsLabel => UI.Title_Reports;

    [NotNull]
    public string StatisticsLabel => UI.Title_Statistics;

    [NotNull]
    public string SaveLabel => UI.ButtonLabel_Save;

    [NotNull]
    public string CancelLabel => UI.ButtonLabel_Cancel;

    [NotNull]
    public string GdprText1 => UI.GDPR_Compliance;

    [NotNull]
    public string GdprText2 => UI.GDPR_Open_Source_Disclaimer;

    [NotNull]
    public string GdprText3 => UI.GDPR_Information_sharing;

    [NotNull]
    public string ReportsGloballyText => UI.Configure_Device_Report_information_disclaimer;

    [NotNull]
    public string SaveReportsGloballyText => UI.Save_device_reports_in_shared_folder_of_your_computer_Q;

    [NotNull]
    public string ReportsText => UI.Configure_share_report_disclaimer;

    [NotNull]
    public string ShareReportsText => UI.Share_your_device_reports_with_us_Q;

    [NotNull]
    public string StatisticsText => UI.Statistics_disclaimer;

    [NotNull]
    public string SaveStatsText => UI.Save_stats_about_your_Aaru_usage_Q;

    [NotNull]
    public string ShareStatsText => UI.Share_your_stats_anonymously_Q;

    [NotNull]
    public string CommandStatsText => UI.Gather_statistics_about_command_usage_Q;

    [NotNull]
    public string DeviceStatsText => UI.Gather_statistics_about_found_devices_Q;

    [NotNull]
    public string FilesystemStatsText => UI.Gather_statistics_about_found_filesystems_Q;

    [NotNull]
    public string FilterStatsText => UI.Gather_statistics_about_found_file_filters_Q;

    [NotNull]
    public string MediaImageStatsText => UI.Gather_statistics_about_found_media_image_formats_Q;

    [NotNull]
    public string MediaScanStatsText => UI.Gather_statistics_about_scanned_media_Q;

    [NotNull]
    public string PartitionStatsText => UI.Gather_statistics_about_found_partitioning_schemes_Q;

    [NotNull]
    public string MediaStatsText => UI.Gather_statistics_about_media_types_Q;

    [NotNull]
    public string VerifyStatsText => UI.Gather_statistics_about_media_image_verifications_Q;

    public ICommand CancelCommand { get; }
    public ICommand SaveCommand   { get; }

    void Save()
    {
        Settings.Settings.Current.SaveReportsGlobally = SaveReportsGloballyChecked;
        Settings.Settings.Current.ShareReports        = ShareReportsChecked;

        if(SaveStatsChecked)
        {
            Settings.Settings.Current.Stats = new StatsSettings
            {
                ShareStats      = ShareStatsChecked,
                CommandStats    = CommandStatsChecked,
                DeviceStats     = DeviceStatsChecked,
                FilesystemStats = FilesystemStatsChecked,
                FilterStats     = FilterStatsChecked,
                MediaImageStats = MediaImageStatsChecked,
                MediaScanStats  = MediaScanStatsChecked,
                PartitionStats  = PartitionStatsChecked,
                MediaStats      = MediaStatsChecked,
                VerifyStats     = VerifyStatsChecked
            };
        }
        else
            Settings.Settings.Current.Stats = null;

        Settings.Settings.Current.GdprCompliance = DicSettings.GDPR_LEVEL;
        Settings.Settings.SaveSettings();
        _view.Close();
    }

    void Cancel() => _view.Close();
}