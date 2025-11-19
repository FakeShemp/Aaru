// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceInfoViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the device information panel.
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
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Decoders.SCSI.SSC;
using Aaru.Devices;
using Aaru.Gui.ViewModels.Tabs;
using Aaru.Gui.Views.Tabs;
using Aaru.Localization;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Humanizer.Localisation;
using DeviceInfo = Aaru.Core.Devices.Info.DeviceInfo;

namespace Aaru.Gui.ViewModels.Panels;

public sealed partial class DeviceInfoViewModel : ViewModelBase
{
    readonly DeviceInfo _devInfo;
    readonly Window     _view;
    [ObservableProperty]
    AtaInfo _ataInfo;
    [ObservableProperty]
    string _blockLimits;
    [ObservableProperty]
    string _blockSizeGranularity;
    [ObservableProperty]
    string _cid;
    [ObservableProperty]
    string _csd;
    [ObservableProperty]
    string _densities;
    [ObservableProperty]
    string _deviceType;
    [ObservableProperty]
    string _extendedCsd;
    [ObservableProperty]
    string _firewireGuid;
    [ObservableProperty]
    string _firewireManufacturer;
    [ObservableProperty]
    string _firewireModel;
    [ObservableProperty]
    string _firewireModelId;
    [ObservableProperty]
    string _firewireVendorId;
    [ObservableProperty]
    bool _firewireVisible;
    [ObservableProperty]
    bool _kreon;
    [ObservableProperty]
    bool _kreonChallengeResponse;
    [ObservableProperty]
    bool _kreonChallengeResponse360;
    [ObservableProperty]
    bool _kreonDecryptSs;
    [ObservableProperty]
    bool _kreonDecryptSs360;
    [ObservableProperty]
    bool _kreonErrorSkipping;
    [ObservableProperty]
    bool _kreonLock;
    [ObservableProperty]
    bool _kreonWxripperUnlock;
    [ObservableProperty]
    bool _kreonWxripperUnlock360;
    [ObservableProperty]
    bool _kreonXtremeUnlock;
    [ObservableProperty]
    bool _kreonXtremeUnlock360;
    [ObservableProperty]
    string _manufacturer;
    [ObservableProperty]
    string _maxBlockSize;
    [ObservableProperty]
    string _mediumDensity;
    [ObservableProperty]
    string _mediumTypes;
    [ObservableProperty]
    string _minBlockSize;
    [ObservableProperty]
    string _model;
    [ObservableProperty]
    string _ocr;
    [ObservableProperty]
    PcmciaInfo _pcmciaInfo;
    [ObservableProperty]
    bool _plextorBitSetting;
    [ObservableProperty]
    bool _plextorBitSettingDl;
    [ObservableProperty]
    string _plextorCdReadTime;
    [ObservableProperty]
    string _plextorCdWriteTime;
    [ObservableProperty]
    string _plextorDiscs;
    [ObservableProperty]
    string _plextorDvd;
    [ObservableProperty]
    bool _plextorDvdPlusWriteTest;
    [ObservableProperty]
    string _plextorDvdReadTime;
    [ObservableProperty]
    bool _plextorDvdTimesVisible;
    [ObservableProperty]
    string _plextorDvdWriteTime;
    [ObservableProperty]
    bool _plextorEepromVisible;
    [ObservableProperty]
    bool _plextorGigaRec;
    [ObservableProperty]
    bool _plextorHidesRecordables;
    [ObservableProperty]
    bool _plextorHidesSessions;
    [ObservableProperty]
    bool _plextorHiding;
    [ObservableProperty]
    bool _plextorPoweRec;
    [ObservableProperty]
    bool _plextorPoweRecEnabled;
    [ObservableProperty]
    string _plextorPoweRecLast;
    [ObservableProperty]
    bool _plextorPoweRecLastVisible;
    [ObservableProperty]
    string _plextorPoweRecMax;
    [ObservableProperty]
    bool _plextorPoweRecMaxVisible;
    [ObservableProperty]
    string _plextorPoweRecRecommended;
    [ObservableProperty]
    bool _plextorPoweRecRecommendedVisible;
    [ObservableProperty]
    string _plextorPoweRecSelected;
    [ObservableProperty]
    bool _plextorPoweRecSelectedVisible;
    [ObservableProperty]
    bool _plextorSecuRec;
    [ObservableProperty]
    bool _plextorSilentMode;
    [ObservableProperty]
    string _plextorSilentModeAccessTime;
    [ObservableProperty]
    string _plextorSilentModeCdReadSpeedLimit;
    [ObservableProperty]
    string _plextorSilentModeCdWriteSpeedLimit;
    [ObservableProperty]
    string _plextorSilentModeDvdReadSpeedLimit;
    [ObservableProperty]
    bool _plextorSilentModeDvdReadSpeedLimitVisible;
    [ObservableProperty]
    bool _plextorSilentModeEnabled;
    [ObservableProperty]
    bool _plextorSpeedEnabled;
    [ObservableProperty]
    bool _plextorSpeedRead;
    [ObservableProperty]
    bool _plextorVariRec;
    [ObservableProperty]
    bool _plextorVariRecDvd;
    [ObservableProperty]
    bool _plextorVisible;
    [ObservableProperty]
    bool _removable;
    [ObservableProperty]
    bool _removableChecked;
    [ObservableProperty]
    string _revision;
    [ObservableProperty]
    bool _saveUsbDescriptorsEnabled;
    [ObservableProperty]
    string _scr;
    [ObservableProperty]
    ScsiInfo _scsiInfo;
    [ObservableProperty]
    string _scsiType;
    [ObservableProperty]
    string _sdMm;
    [ObservableProperty]
    SdMmcInfo _sdMmcInfo;
    [ObservableProperty]
    string _sdMmcText;
    [ObservableProperty]
    string _secureDigital;
    [ObservableProperty]
    string _serial;
    [ObservableProperty]
    bool _ssc;
    [ObservableProperty]
    string _usbConnected;
    [ObservableProperty]
    string _usbManufacturer;
    [ObservableProperty]
    string _usbProduct;
    [ObservableProperty]
    string _usbProductId;
    [ObservableProperty]
    string _usbSerial;
    [ObservableProperty]
    string _usbVendorId;
    [ObservableProperty]
    bool _usbVisible;

    public DeviceInfoViewModel(DeviceInfo devInfo, Window view)
    {
        SaveUsbDescriptorsCommand = new AsyncRelayCommand(SaveUsbDescriptorsAsync);
        _view                     = view;
        _devInfo                  = devInfo;

        DeviceType   = devInfo.Type.ToString();
        Manufacturer = devInfo.Manufacturer;
        Model        = devInfo.Model;
        Revision     = devInfo.FirmwareRevision;
        Serial       = devInfo.Serial;
        ScsiType     = devInfo.ScsiType.ToString();
        Removable    = devInfo.IsRemovable;
        UsbVisible   = devInfo.IsUsb;

        if(devInfo.IsUsb)
        {
            UsbVisible                = true;
            SaveUsbDescriptorsEnabled = devInfo.UsbDescriptors != null;
            UsbVendorId               = $"{devInfo.UsbVendorId:X4}";
            UsbProductId              = $"{devInfo.UsbProductId:X4}";
            UsbManufacturer           = devInfo.UsbManufacturerString;
            UsbProduct                = devInfo.UsbProductString;
            UsbSerial                 = devInfo.UsbSerialString;
        }

        if(devInfo.IsFireWire)
        {
            FirewireVisible      = true;
            FirewireVendorId     = $"{devInfo.FireWireVendor:X4}";
            FirewireModelId      = $"{devInfo.FireWireModel:X4}";
            FirewireManufacturer = devInfo.FireWireVendorName;
            FirewireModel        = devInfo.FireWireModelName;
            FirewireGuid         = $"{devInfo.FireWireGuid:X16}";
        }

        if(devInfo.IsPcmcia)
        {
            PcmciaInfo = new PcmciaInfo
            {
                DataContext = new PcmciaInfoViewModel(devInfo.Cis, _view)
            };
        }

        if(devInfo.AtaIdentify != null || devInfo.AtapiIdentify != null)
        {
            AtaInfo = new AtaInfo
            {
                DataContext =
                    new AtaInfoViewModel(devInfo.AtaIdentify, devInfo.AtapiIdentify, devInfo.AtaMcptError, _view)
            };
        }

        if(devInfo.ScsiInquiryData != null)
        {
            ScsiInfo = new ScsiInfo
            {
                DataContext = new ScsiInfoViewModel(devInfo.ScsiInquiryData,
                                                    devInfo.ScsiInquiry,
                                                    devInfo.ScsiEvpdPages,
                                                    devInfo.ScsiMode,
                                                    devInfo.ScsiType,
                                                    devInfo.ScsiModeSense6,
                                                    devInfo.ScsiModeSense10,
                                                    devInfo.MmcConfiguration,
                                                    _view)
            };

            if(devInfo.PlextorFeatures != null)
            {
                PlextorVisible = true;

                if(devInfo.PlextorFeatures.Eeprom != null)
                {
                    PlextorEepromVisible = true;
                    PlextorDiscs = $"{devInfo.PlextorFeatures.Discs}";
                    PlextorCdReadTime = devInfo.PlextorFeatures.CdReadTime.Seconds().Humanize(minUnit: TimeUnit.Second);

                    PlextorCdWriteTime =
                        devInfo.PlextorFeatures.CdWriteTime.Seconds().Humanize(minUnit: TimeUnit.Second);

                    if(devInfo.PlextorFeatures.IsDvd)
                    {
                        PlextorDvdTimesVisible = true;

                        PlextorDvdReadTime = devInfo.PlextorFeatures.DvdReadTime.Seconds()
                                                    .Humanize(minUnit: TimeUnit.Second);

                        PlextorDvdWriteTime = devInfo.PlextorFeatures.DvdWriteTime.Seconds()
                                                     .Humanize(minUnit: TimeUnit.Second);
                    }
                }

                PlextorPoweRec = devInfo.PlextorFeatures.PoweRec;

                if(devInfo.PlextorFeatures.PoweRec)
                {
                    PlextorPoweRecEnabled = devInfo.PlextorFeatures.PoweRecEnabled;

                    if(devInfo.PlextorFeatures.PoweRecEnabled)
                    {
                        PlextorPoweRecEnabled = true;

                        if(devInfo.PlextorFeatures.PoweRecRecommendedSpeed > 0)
                        {
                            PlextorPoweRecRecommendedVisible = true;

                            PlextorPoweRecRecommended =
                                string.Format(UI._0_Kb_sec, devInfo.PlextorFeatures.PoweRecRecommendedSpeed);
                        }

                        if(devInfo.PlextorFeatures.PoweRecSelected > 0)
                        {
                            PlextorPoweRecSelectedVisible = true;

                            PlextorPoweRecSelected =
                                string.Format(UI._0_Kb_sec, devInfo.PlextorFeatures.PoweRecSelected);
                        }

                        if(devInfo.PlextorFeatures.PoweRecMax > 0)
                        {
                            PlextorPoweRecMaxVisible = true;
                            PlextorPoweRecMax        = string.Format(UI._0_Kb_sec, devInfo.PlextorFeatures.PoweRecMax);
                        }

                        if(devInfo.PlextorFeatures.PoweRecLast > 0)
                        {
                            PlextorPoweRecLastVisible = true;
                            PlextorPoweRecLast = string.Format(UI._0_Kb_sec, devInfo.PlextorFeatures.PoweRecLast);
                        }
                    }
                }

                PlextorSilentMode = devInfo.PlextorFeatures.SilentMode;

                if(devInfo.PlextorFeatures.SilentMode)
                {
                    PlextorSilentModeEnabled = devInfo.PlextorFeatures.SilentModeEnabled;

                    if(devInfo.PlextorFeatures.SilentModeEnabled)
                    {
                        PlextorSilentModeAccessTime = devInfo.PlextorFeatures.AccessTimeLimit == 2
                                                          ? Localization.Core.Access_time_is_slow
                                                          : Localization.Core.Access_time_is_fast;

                        PlextorSilentModeCdReadSpeedLimit =
                            devInfo.PlextorFeatures.CdReadSpeedLimit > 0
                                ? $"{devInfo.PlextorFeatures.CdReadSpeedLimit}x"
                                : UI.unlimited_as_in_speed;

                        PlextorSilentModeCdWriteSpeedLimit =
                            devInfo.PlextorFeatures.CdWriteSpeedLimit > 0
                                ? $"{devInfo.PlextorFeatures.CdReadSpeedLimit}x"
                                : UI.unlimited_as_in_speed;

                        if(devInfo.PlextorFeatures.IsDvd)
                        {
                            PlextorSilentModeDvdReadSpeedLimitVisible = true;

                            PlextorSilentModeDvdReadSpeedLimit =
                                devInfo.PlextorFeatures.DvdReadSpeedLimit > 0
                                    ? $"{devInfo.PlextorFeatures.DvdReadSpeedLimit}x"
                                    : UI.unlimited_as_in_speed;
                        }
                    }
                }

                PlextorGigaRec   = devInfo.PlextorFeatures.GigaRec;
                PlextorSecuRec   = devInfo.PlextorFeatures.SecuRec;
                PlextorSpeedRead = devInfo.PlextorFeatures.SpeedRead;

                if(devInfo.PlextorFeatures.SpeedRead) PlextorSpeedEnabled = devInfo.PlextorFeatures.SpeedReadEnabled;

                PlextorHiding = devInfo.PlextorFeatures.Hiding;

                if(devInfo.PlextorFeatures.Hiding)
                {
                    PlextorHidesRecordables = devInfo.PlextorFeatures.HidesRecordables;
                    PlextorHidesSessions    = devInfo.PlextorFeatures.HidesSessions;
                }

                PlextorVariRec = devInfo.PlextorFeatures.VariRec;

                if(devInfo.PlextorFeatures.IsDvd)
                {
                    PlextorVariRecDvd       = devInfo.PlextorFeatures.VariRecDvd;
                    PlextorBitSetting       = devInfo.PlextorFeatures.BitSetting;
                    PlextorBitSettingDl     = devInfo.PlextorFeatures.BitSettingDl;
                    PlextorDvdPlusWriteTest = devInfo.PlextorFeatures.DvdPlusWriteTest;
                }
            }

            if(devInfo.ScsiInquiry?.KreonPresent == true)
            {
                Kreon                  = true;
                KreonChallengeResponse = devInfo.KreonFeatures.HasFlag(KreonFeatures.ChallengeResponse);
                KreonDecryptSs         = devInfo.KreonFeatures.HasFlag(KreonFeatures.DecryptSs);
                KreonXtremeUnlock      = devInfo.KreonFeatures.HasFlag(KreonFeatures.XtremeUnlock);
                KreonWxripperUnlock    = devInfo.KreonFeatures.HasFlag(KreonFeatures.WxripperUnlock);

                KreonChallengeResponse360 = devInfo.KreonFeatures.HasFlag(KreonFeatures.ChallengeResponse360);

                KreonDecryptSs360      = devInfo.KreonFeatures.HasFlag(KreonFeatures.DecryptSs360);
                KreonXtremeUnlock360   = devInfo.KreonFeatures.HasFlag(KreonFeatures.XtremeUnlock360);
                KreonWxripperUnlock360 = devInfo.KreonFeatures.HasFlag(KreonFeatures.WxripperUnlock360);
                KreonLock              = devInfo.KreonFeatures.HasFlag(KreonFeatures.Lock);
                KreonErrorSkipping     = devInfo.KreonFeatures.HasFlag(KreonFeatures.ErrorSkipping);
            }

            if(devInfo.BlockLimits != null)
            {
                BlockLimits.BlockLimitsData? blockLimits = Decoders.SCSI.SSC.BlockLimits.Decode(devInfo.BlockLimits);

                if(blockLimits.HasValue)
                {
                    Ssc = true;

                    if(blockLimits.Value.minBlockLen == blockLimits.Value.maxBlockLen)
                    {
                        MinBlockSize = string.Format(Localization.Core.Device_block_size_is_fixed_at_0_bytes,
                                                     blockLimits.Value.minBlockLen);
                    }
                    else
                    {
                        MaxBlockSize = blockLimits.Value.maxBlockLen > 0
                                           ? string.Format(Localization.Core.Device_maximum_block_size_is_0_bytes,
                                                           blockLimits.Value.maxBlockLen)
                                           : Localization.Core.Device_does_not_specify_a_maximum_block_size;

                        MinBlockSize = string.Format(Localization.Core.Device_minimum_block_size_is_0_bytes,
                                                     blockLimits.Value.minBlockLen);

                        if(blockLimits.Value.granularity > 0)
                        {
                            BlockSizeGranularity =
                                string.Format(Localization.Core.Device_needs_a_block_size_granularity_of_pow_0_1_bytes,
                                              blockLimits.Value.granularity,
                                              Math.Pow(2, blockLimits.Value.granularity));
                        }
                    }
                }
            }

            if(devInfo.DensitySupport != null)
            {
                if(devInfo.DensitySupportHeader.HasValue)
                    Densities = DensitySupport.PrettifyDensity(devInfo.DensitySupportHeader);
            }

            if(devInfo.MediumDensitySupport != null)
            {
                if(devInfo.MediaTypeSupportHeader.HasValue)
                    MediumTypes = DensitySupport.PrettifyMediumType(devInfo.MediaTypeSupportHeader);

                MediumDensity = DensitySupport.PrettifyMediumType(devInfo.MediumDensitySupport);
            }
        }

        SdMmcInfo = new SdMmcInfo
        {
            DataContext = new SdMmcInfoViewModel(devInfo.Type,
                                                 devInfo.CID,
                                                 devInfo.CSD,
                                                 devInfo.OCR,
                                                 devInfo.ExtendedCSD,
                                                 devInfo.SCR)
        };
    }

    public ICommand SaveUsbDescriptorsCommand { get; }

    async Task SaveUsbDescriptorsAsync()
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
        saveFs.Write(_devInfo.UsbDescriptors, 0, _devInfo.UsbDescriptors.Length);

        saveFs.Close();
    }
}