// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MediaInfoViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the media information panel.
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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Gui.ViewModels.Tabs;
using Aaru.Gui.ViewModels.Windows;
using Aaru.Gui.Views.Tabs;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer.Bytes;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using ScsiInfo = Aaru.Core.Media.Info.ScsiInfo;

namespace Aaru.Gui.ViewModels.Panels;

public sealed partial class MediaInfoViewModel : ViewModelBase
{
    readonly string   _devicePath;
    readonly ScsiInfo _scsiInfo;
    readonly Window   _view;
    [ObservableProperty]
    BlurayInfo _blurayInfo;
    [ObservableProperty]
    CompactDiscInfo _compactDiscInfo;
    [ObservableProperty]
    string _densitySupport;
    [ObservableProperty]
    DvdInfo _dvdInfo;
    [ObservableProperty]
    DvdWritableInfo _dvdWritableInfo;
    [ObservableProperty]
    string _generalVisible;
    [ObservableProperty]
    Bitmap _mediaLogo;
    [ObservableProperty]
    string _mediaSerial;
    [ObservableProperty]
    string _mediaSize;
    [ObservableProperty]
    string _mediaType;
    [ObservableProperty]
    string _mediumSupport;
    [ObservableProperty]
    bool _mmcVisible;
    [ObservableProperty]
    bool _saveDensitySupportVisible;
    [ObservableProperty]
    bool _saveGetConfigurationVisible;
    [ObservableProperty]
    bool _saveMediumSupportVisible;
    [ObservableProperty]
    bool _saveReadCapacity16Visible;
    [ObservableProperty]
    bool _saveReadCapacityVisible;
    [ObservableProperty]
    bool _saveReadMediaSerialVisible;
    [ObservableProperty]
    bool _saveRecognizedFormatLayersVisible;
    [ObservableProperty]
    bool _saveWriteProtectionStatusVisible;
    [ObservableProperty]
    bool _sscVisible;
    [ObservableProperty]
    XboxInfo _xboxInfo;

    public MediaInfoViewModel(ScsiInfo scsiInfo, string devicePath, Window view)
    {
        _view                             = view;
        SaveReadMediaSerialCommand        = new AsyncRelayCommand(SaveReadMediaSerial);
        SaveReadCapacityCommand           = new AsyncRelayCommand(SaveReadCapacity);
        SaveReadCapacity16Command         = new AsyncRelayCommand(SaveReadCapacity16);
        SaveGetConfigurationCommand       = new AsyncRelayCommand(SaveGetConfiguration);
        SaveRecognizedFormatLayersCommand = new AsyncRelayCommand(SaveRecognizedFormatLayers);
        SaveWriteProtectionStatusCommand  = new AsyncRelayCommand(SaveWriteProtectionStatus);
        SaveDensitySupportCommand         = new AsyncRelayCommand(SaveDensitySupport);
        SaveMediumSupportCommand          = new AsyncRelayCommand(SaveMediumSupport);
        DumpCommand                       = new AsyncRelayCommand(DumpAsync);
        ScanCommand                       = new AsyncRelayCommand(ScanAsync);
        _devicePath                       = devicePath;
        _scsiInfo                         = scsiInfo;

        var mediaResource = new Uri($"avares://Aaru.Gui/Assets/Logos/Media/{scsiInfo.MediaType}.png");

        MediaLogo = AssetLoader.Exists(mediaResource) ? new Bitmap(AssetLoader.Open(mediaResource)) : null;

        MediaType = scsiInfo.MediaType.ToString();

        if(scsiInfo.Blocks != 0 && scsiInfo.BlockSize != 0)
        {
            MediaSize = string.Format(Localization.Core.Media_has_0_blocks_of_1_bytes_each_for_a_total_of_2,
                                      scsiInfo.Blocks,
                                      scsiInfo.BlockSize,
                                      ByteSize.FromBytes(scsiInfo.Blocks * scsiInfo.BlockSize).ToString("0.000"));
        }

        if(scsiInfo.MediaSerialNumber != null)
        {
            var sbSerial = new StringBuilder();

            for(int i = 4; i < scsiInfo.MediaSerialNumber.Length; i++)
                sbSerial.Append($"{scsiInfo.MediaSerialNumber[i]:X2}");

            MediaSerial = sbSerial.ToString();
        }

        SaveReadMediaSerialVisible = scsiInfo.MediaSerialNumber != null;
        SaveReadCapacityVisible    = scsiInfo.ReadCapacity      != null;
        SaveReadCapacity16Visible  = scsiInfo.ReadCapacity16    != null;

        SaveGetConfigurationVisible       = scsiInfo.MmcConfiguration       != null;
        SaveRecognizedFormatLayersVisible = scsiInfo.RecognizedFormatLayers != null;
        SaveWriteProtectionStatusVisible  = scsiInfo.WriteProtectionStatus  != null;

        MmcVisible = SaveGetConfigurationVisible       ||
                     SaveRecognizedFormatLayersVisible ||
                     SaveWriteProtectionStatusVisible;

        if(scsiInfo.DensitySupportHeader.HasValue)
            DensitySupport = Decoders.SCSI.SSC.DensitySupport.PrettifyDensity(scsiInfo.DensitySupportHeader);

        if(scsiInfo.MediaTypeSupportHeader.HasValue)
            MediumSupport = Decoders.SCSI.SSC.DensitySupport.PrettifyMediumType(scsiInfo.MediaTypeSupportHeader);

        SaveDensitySupportVisible = scsiInfo.DensitySupport   != null;
        SaveMediumSupportVisible  = scsiInfo.MediaTypeSupport != null;

        SscVisible = SaveDensitySupportVisible || SaveMediumSupportVisible;

        CompactDiscInfo = new CompactDiscInfo
        {
            DataContext = new CompactDiscInfoViewModel(scsiInfo.Toc,
                                                       scsiInfo.Atip,
                                                       scsiInfo.DiscInformation,
                                                       scsiInfo.Session,
                                                       scsiInfo.RawToc,
                                                       scsiInfo.Pma,
                                                       scsiInfo.CdTextLeadIn,
                                                       scsiInfo.DecodedToc,
                                                       scsiInfo.DecodedAtip,
                                                       scsiInfo.DecodedSession,
                                                       scsiInfo.FullToc,
                                                       scsiInfo.DecodedCdTextLeadIn,
                                                       scsiInfo.DecodedDiscInformation,
                                                       scsiInfo.Mcn,
                                                       scsiInfo.Isrcs,
                                                       _view)
        };

        DvdInfo = new DvdInfo
        {
            DataContext = new DvdInfoViewModel(scsiInfo.DvdPfi,
                                               scsiInfo.DvdDmi,
                                               scsiInfo.DvdCmi,
                                               scsiInfo.HddvdCopyrightInformation,
                                               scsiInfo.DvdBca,
                                               scsiInfo.DvdAacs,
                                               scsiInfo.DecodedPfi,
                                               _view)
        };

        XboxInfo = new XboxInfo
        {
            DataContext = new XboxInfoViewModel(scsiInfo.XgdInfo,
                                                scsiInfo.DvdDmi,
                                                scsiInfo.XboxSecuritySector,
                                                scsiInfo.DecodedXboxSecuritySector,
                                                _view)
        };

        DvdWritableInfo = new DvdWritableInfo
        {
            DataContext = new DvdWritableInfoViewModel(scsiInfo.DvdRamDds,
                                                       scsiInfo.DvdRamCartridgeStatus,
                                                       scsiInfo.DvdRamSpareArea,
                                                       scsiInfo.LastBorderOutRmd,
                                                       scsiInfo.DvdPreRecordedInfo,
                                                       scsiInfo.DvdrMediaIdentifier,
                                                       scsiInfo.DvdrPhysicalInformation,
                                                       scsiInfo.HddvdrMediumStatus,
                                                       scsiInfo.HddvdrLastRmd,
                                                       scsiInfo.DvdrLayerCapacity,
                                                       scsiInfo.DvdrDlMiddleZoneStart,
                                                       scsiInfo.DvdrDlJumpIntervalSize,
                                                       scsiInfo.DvdrDlManualLayerJumpStartLba,
                                                       scsiInfo.DvdrDlRemapAnchorPoint,
                                                       scsiInfo.DvdPlusAdip,
                                                       scsiInfo.DvdPlusDcb,
                                                       _view)
        };

        BlurayInfo = new BlurayInfo
        {
            DataContext = new BlurayInfoViewModel(scsiInfo.BlurayDiscInformation,
                                                  scsiInfo.BlurayBurstCuttingArea,
                                                  scsiInfo.BlurayDds,
                                                  scsiInfo.BlurayCartridgeStatus,
                                                  scsiInfo.BluraySpareAreaInformation,
                                                  scsiInfo.BlurayPowResources,
                                                  scsiInfo.BlurayTrackResources,
                                                  scsiInfo.BlurayRawDfl,
                                                  scsiInfo.BlurayPac,
                                                  _view)
        };
    }

    public ICommand SaveReadMediaSerialCommand        { get; }
    public ICommand SaveReadCapacityCommand           { get; }
    public ICommand SaveReadCapacity16Command         { get; }
    public ICommand SaveGetConfigurationCommand       { get; }
    public ICommand SaveRecognizedFormatLayersCommand { get; }
    public ICommand SaveWriteProtectionStatusCommand  { get; }
    public ICommand SaveDensitySupportCommand         { get; }
    public ICommand SaveMediumSupportCommand          { get; }
    public ICommand DumpCommand                       { get; }
    public ICommand ScanCommand                       { get; }

    public string MediaInformationLabel           => UI.Title_Media_information;
    public string GeneralLabel                    => UI.Title_General;
    public string MediaTypeLabel                  => UI.Title_Media_type;
    public string MediaSerialNumberLabel          => UI.Title_Media_serial_number;
    public string SaveReadMediaSerialLabel        => UI.ButtonLabel_Save_READ_MEDIA_SERIAL_NUMBER_response;
    public string SaveReadCapacityLabel           => UI.ButtonLabel_Save_READ_CAPACITY_response;
    public string SaveReadCapacity16Label         => UI.ButtonLabel_Save_READ_CAPACITY_16_response;
    public string MMCLabel                        => Localization.Core.Title_MMC;
    public string SaveGetConfigurationLabel       => UI.ButtonLabel_Save_GET_CONFIGURATION_response;
    public string SaveRecognizedFormatLayersLabel => UI.ButtonLabel_Save_RECOGNIZED_FORMAT_LAYERS_response;
    public string SaveWriteProtectionStatusLabel  => UI.ButtonLabel_Save_WRITE_PROTECTION_STATUS_response;
    public string SscLabel                        => Localization.Core.Title_SSC;
    public string DensitySupportLabel             => UI.Densities_supported_by_currently_inserted_media;
    public string MediumSupportLabel              => UI.Medium_types_currently_inserted_in_device;
    public string SaveDensitySupportLabel         => UI.ButtonLabel_Save_REPORT_DENSITY_SUPPORT_MEDIA_response;
    public string SaveMediumSupportLabel          => UI.ButtonLabel_Save_REPORT_DENSITY_SUPPORT_MEDIUM_MEDIA_response;
    public string CompactDiscLabel                => Localization.Core.Title_CompactDisc;
    public string DvdLabel                        => Localization.Core.Title_DVD;
    public string Dvd_R_WLabel                    => Localization.Core.Title_DVD_Plus_Dash_R_W;
    public string XboxLabel                       => Localization.Core.Title_Xbox;
    public string BluRayLabel                     => Localization.Core.Title_Blu_ray;
    public string DumpLabel                       => UI.ButtonLabel_Dump_media_to_image;
    public string ScanLabel                       => UI.ButtonLabel_Scan_media_surface;

    async Task SaveElementAsync(byte[] data)
    {
        IStorageFile result = await _view.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            FileTypeChoices = new List<FilePickerFileType>
            {
                FilePickerFileTypes.Binary
            }
        });

        if(result is null) return;

        var saveFs = new FileStream(result.Path.AbsolutePath, FileMode.Create);
        saveFs.Write(data, 0, data.Length);

        saveFs.Close();
    }

    Task SaveReadMediaSerial() => SaveElementAsync(_scsiInfo.MediaSerialNumber);

    Task SaveReadCapacity() => SaveElementAsync(_scsiInfo.ReadCapacity);

    Task SaveReadCapacity16() => SaveElementAsync(_scsiInfo.ReadCapacity16);

    Task SaveGetConfiguration() => SaveElementAsync(_scsiInfo.MmcConfiguration);

    Task SaveRecognizedFormatLayers() => SaveElementAsync(_scsiInfo.RecognizedFormatLayers);

    Task SaveWriteProtectionStatus() => SaveElementAsync(_scsiInfo.WriteProtectionStatus);

    Task SaveDensitySupport() => SaveElementAsync(_scsiInfo.DensitySupport);

    Task SaveMediumSupport() => SaveElementAsync(_scsiInfo.MediaTypeSupport);

    async Task DumpAsync()
    {
        switch(_scsiInfo.MediaType)
        {
            case CommonTypes.MediaType.GDR or CommonTypes.MediaType.GDROM:
                await MessageBoxManager
                     .GetMessageBoxStandard(UI.Title_Error,
                                            Localization.Core.GD_ROM_dump_support_is_not_yet_implemented,
                                            ButtonEnum.Ok,
                                            Icon.Error)
                     .ShowWindowDialogAsync(_view);

                return;
            case CommonTypes.MediaType.XGD or CommonTypes.MediaType.XGD2 or CommonTypes.MediaType.XGD3
                when _scsiInfo.DeviceInfo.ScsiInquiry?.KreonPresent != true:
                await MessageBoxManager
                     .GetMessageBoxStandard(UI.Title_Error,
                                            Localization.Core
                                                        .Dumping_Xbox_Game_Discs_requires_a_drive_with_Kreon_firmware,
                                            ButtonEnum.Ok,
                                            Icon.Error)
                     .ShowWindowDialogAsync(_view);

                return;
        }

        var mediaDumpWindow = new MediaDump();

        mediaDumpWindow.DataContext =
            new MediaDumpViewModel(_devicePath, _scsiInfo.DeviceInfo, mediaDumpWindow, _scsiInfo);

        mediaDumpWindow.Show();
    }

    async Task ScanAsync()
    {
        switch(_scsiInfo.MediaType)
        {
            // TODO: GD-ROM
            case CommonTypes.MediaType.GDR:
            case CommonTypes.MediaType.GDROM:
                await MessageBoxManager
                     .GetMessageBoxStandard(UI.Title_Error,
                                            Localization.Core.GD_ROM_scan_support_is_not_yet_implemented,
                                            ButtonEnum.Ok,
                                            Icon.Error)
                     .ShowWindowDialogAsync(_view);

                return;

            // TODO: Xbox
            case CommonTypes.MediaType.XGD:
            case CommonTypes.MediaType.XGD2:
            case CommonTypes.MediaType.XGD3:
                await MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                              Localization.Core
                                                                          .Scanning_Xbox_discs_is_not_yet_supported,
                                                              ButtonEnum.Ok,
                                                              Icon.Error)
                                       .ShowWindowDialogAsync(_view);

                return;
        }

        var mediaScanWindow = new MediaScan();

        mediaScanWindow.DataContext = new MediaScanViewModel(_devicePath, mediaScanWindow);

        mediaScanWindow.Show();
    }
}