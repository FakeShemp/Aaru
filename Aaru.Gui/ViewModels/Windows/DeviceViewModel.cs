// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model for device.
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
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Decoders.SCSI.SSC;
using Aaru.Devices;
using Aaru.Gui.ViewModels.Tabs;
using Aaru.Gui.Views.Tabs;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Aaru.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
using Humanizer.Localisation;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;

namespace Aaru.Gui.ViewModels.Windows;

public partial class DeviceViewModel : ViewModelBase
{
    readonly DeviceView _window;
    Device              _dev;
    [ObservableProperty]
    string _devicePath;
    [ObservableProperty]
    string _deviceType;
    [ObservableProperty]
    string _manufacturer;
    [ObservableProperty]
    string _model;
    [ObservableProperty]
    bool _removableChecked;
    [ObservableProperty]
    string _revision;
    [ObservableProperty]
    string _scsiType;
    [ObservableProperty]
    string _serial;
    [ObservableProperty]
    string _statusMessage;
    [ObservableProperty]
    bool _usbConnected;
    [ObservableProperty]
    bool       _usbVisible;
    [ObservableProperty]
    string     _usbVendorId;
    [ObservableProperty]
        string _usbProductId;
    [ObservableProperty]
    string     _usbManufacturer;
    [ObservableProperty]
        string _usbProduct;
    [ObservableProperty]
    string     _usbSerial;
    [ObservableProperty]
    bool _saveUsbDescriptorsEnabled;
    [ObservableProperty]
    bool       _firewireVisible;
    [ObservableProperty]
    string     _firewireVendorId;
    [ObservableProperty]
        string _firewireModelId;
    [ObservableProperty]
    string     _firewireManufacturer;
    [ObservableProperty]
        string _firewireModel;
    [ObservableProperty]
    string     _firewireGuid;
    [ObservableProperty]
    bool _plextorVisible;
    [ObservableProperty]
    bool _plextorEepromVisible;
    [ObservableProperty]
    bool _plextorDvdTimesVisible;
    [ObservableProperty]
    bool _plextorPoweRec;
    [ObservableProperty]
    bool _plextorPoweRecEnabled;
    [ObservableProperty]
    bool _plextorPoweRecRecommendedVisible;
    [ObservableProperty]
    bool _plextorPoweRecSelectedVisible;
    [ObservableProperty]
    bool _plextorPoweRecMaxVisible;
    [ObservableProperty]
    bool _plextorPoweRecLastVisible;
    [ObservableProperty]
    bool _plextorSilentMode;
    [ObservableProperty]
    bool _plextorSilentModeEnabled;
    [ObservableProperty]
    bool _plextorSilentModeDvdReadSpeedLimitVisible;
    [ObservableProperty]
    bool _plextorGigaRec;
    [ObservableProperty]
    bool _plextorSecuRec;
    [ObservableProperty]
    bool _plextorSpeedRead;
    [ObservableProperty]
    bool _plextorSpeedEnabled;
    [ObservableProperty]
    bool _plextorHiding;
    [ObservableProperty]
    bool _plextorHidesRecordables;
    [ObservableProperty]
    bool _plextorHidesSessions;
    [ObservableProperty]
    bool _plextorVariRec;
    [ObservableProperty]
    bool _plextorDvd;
    [ObservableProperty]
    bool _plextorVariRecDvd;
    [ObservableProperty]
    bool _plextorBitSetting;
    [ObservableProperty]
    bool _plextorBitSettingDl;
    [ObservableProperty]
    bool _plextorDvdPlusWriteTest;
    [ObservableProperty]
    string _plextorDiscs;
    [ObservableProperty]
    string _plextorCdReadTime;
    [ObservableProperty]
    string _plextorCdWriteTime;
    [ObservableProperty]
    string _plextorDvdReadTime;
    [ObservableProperty]
    string _plextorDvdWriteTime;
    [ObservableProperty]
    string _plextorPoweRecRecommended;
    [ObservableProperty]
    string _plextorPoweRecSelected;
    [ObservableProperty]
    string _plextorPoweRecMax;
    [ObservableProperty]
    string _plextorPoweRecLast;
    [ObservableProperty]
    string _plextorSilentModeAccessTime;
    [ObservableProperty]
    string _plextorSilentModeCdReadSpeedLimit;
    [ObservableProperty]
    string _plextorSilentModeCdWriteSpeedLimit;
    [ObservableProperty]
    string _plextorSilentModeDvdReadSpeedLimit;
    [ObservableProperty]
    bool _kreon;
    [ObservableProperty]
    bool _kreonChallengeResponse;
    [ObservableProperty]
    bool _kreonDecryptSs;
    [ObservableProperty]
    bool _kreonXtremeUnlock;
    [ObservableProperty]
    bool _kreonWxripperUnlock;
    [ObservableProperty]
    bool _kreonChallengeResponse360;
    [ObservableProperty]
    bool _kreonDecryptSs360;
    [ObservableProperty]
    bool _kreonXtremeUnlock360;
    [ObservableProperty]
    bool _kreonWxripperUnlock360;
    [ObservableProperty]
    bool _kreonLock;
    [ObservableProperty]
    bool _kreonErrorSkipping;
    [ObservableProperty]
    bool   _ssc;
    [ObservableProperty]
    bool   _blockLimits;
    [ObservableProperty]
    string _minBlockSize;
    [ObservableProperty]
    string _maxBlockSize;
    [ObservableProperty]
    string _blockSizeGranularity;
    [ObservableProperty]
    string _densities;
    [ObservableProperty]
    string _mediumTypes;
    [ObservableProperty]
    string _mediumDensity;
    [ObservableProperty]
    SdMmcInfo _sdMmcInfo;
    [ObservableProperty]
    ScsiInfo _scsiInfo;
    [ObservableProperty]
    PcmciaInfo _pcmciaInfo;
    [ObservableProperty]
    AtaInfo _ataInfo;

    public DeviceViewModel(DeviceView window, string devicePath)
    {
        _window    = window;
        DevicePath = devicePath;
    }

    public void LoadData()
    {
        _ = Task.Run(Worker);
    }

    public ICommand SaveUsbDescriptorsCommand { get; }

    void Worker()
    {
        Dispatcher.UIThread.Invoke(() => StatusMessage = "Opening device...");

        var dev = Device.Create(DevicePath, out ErrorNumber devErrno);

        if(dev is null)
        {
            AaruLogging.Error(string.Format(UI.Could_not_open_device_error_0, devErrno));

            Dispatcher.UIThread.Invoke(() =>
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    string.Format(UI.Could_not_open_device_error_0, devErrno),
                    ButtonEnum.Ok,
                    Icon.Error);

                _ = msbox.ShowAsync();

                _window.Close();
            });

            return;
        }

        if(dev.Error)
        {
            AaruLogging.Error(Error.Print(dev.LastError));

            Dispatcher.UIThread.Invoke(() =>
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    Error.Print(dev.LastError),
                    ButtonEnum.Ok,
                    Icon.Error);

                _ = msbox.ShowAsync();

                _window.Close();
            });

            return;
        }

        Dispatcher.UIThread.Invoke(() =>
        {
            DeviceType       = $"[rosybrown]{dev.Type.Humanize()}[/]";
            Manufacturer     = (dev.Manufacturer     != null ? $"[blue]{dev.Manufacturer}[/]" : null)!;
            Model            = (dev.Model            != null ? $"[purple]{dev.Model}[/]" : null)!;
            Revision         = (dev.FirmwareRevision != null ? $"[teal]{dev.FirmwareRevision}[/]" : null)!;
            Serial           = (dev.Serial           != null ? $"[fuchsia]{dev.Serial}[/]" : null)!;
            ScsiType         = $"[orange]{dev.ScsiType.Humanize()}[/]";
            RemovableChecked = dev.IsRemovable;
            UsbConnected     = dev.IsUsb;
            StatusMessage    = "Device opened successfully.";
        });

        _dev = dev;

        Statistics.AddDevice(dev);

        Dispatcher.UIThread.Invoke(() =>
        {
            StatusMessage    = "Querying device information...";
        });

        var devInfo = new Aaru.Core.Devices.Info.DeviceInfo(dev);

        Dispatcher.UIThread.Invoke(() =>
        {
            StatusMessage    = "Device information queryied successfully...";
        });

        if(devInfo.IsUsb)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                UsbVisible                = true;
                SaveUsbDescriptorsEnabled = devInfo.UsbDescriptors != null;
                UsbVendorId               = $"[cyan]{devInfo.UsbVendorId:X4}[/]";
                UsbProductId              = $"[cyan]{devInfo.UsbProductId:X4}[/]";
                UsbManufacturer           = $"[blue]{devInfo.UsbManufacturerString}[/]";
                UsbProduct                = $"[purple]{devInfo.UsbProductString}[/]";
                UsbSerial                 = $"[fuchsia]{devInfo.UsbSerialString}[/]";
            });
        }

        if(devInfo.IsFireWire)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                FirewireVisible      = true;
                FirewireVendorId     = $"[cyan]{devInfo.FireWireVendor:X4}[/]";
                FirewireModelId      = $"[cyan]{devInfo.FireWireModel:X4}[/]";
                FirewireManufacturer = $"[blue]{devInfo.FireWireVendorName}[/]";
                FirewireModel        = $"[purple]{devInfo.FireWireModelName}[/]";
                FirewireGuid         = $"[fuchsia]{devInfo.FireWireGuid:X16}[/]";
            });
        }

        if(devInfo.IsPcmcia)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                PcmciaInfo = new PcmciaInfo
                {
                    DataContext = new PcmciaInfoViewModel(devInfo.Cis, _window)
                };
            });
        }

        if(devInfo.AtaIdentify != null || devInfo.AtapiIdentify != null)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                AtaInfo = new AtaInfo
                {
                    DataContext = new AtaInfoViewModel(devInfo.AtaIdentify,
                                                       devInfo.AtapiIdentify,
                                                       devInfo.AtaMcptError,
                                                       _window)
                };
            });
        }

        if(devInfo.ScsiInquiryData != null)
        {
            Dispatcher.UIThread.Invoke(() =>
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
                                                        _window)
                };
            });

            if(devInfo.PlextorFeatures != null)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    PlextorVisible = true;

                    if(devInfo.PlextorFeatures.Eeprom != null)
                    {
                        PlextorEepromVisible = true;
                        PlextorDiscs         = $"{devInfo.PlextorFeatures.Discs}";

                        PlextorCdReadTime = devInfo.PlextorFeatures.CdReadTime.Seconds()
                                                   .Humanize(minUnit: TimeUnit.Second);

                        PlextorCdWriteTime = devInfo.PlextorFeatures.CdWriteTime.Seconds()
                                                    .Humanize(minUnit: TimeUnit.Second);

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
                                PlextorPoweRecMax = string.Format(UI._0_Kb_sec, devInfo.PlextorFeatures.PoweRecMax);
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

                    if(devInfo.PlextorFeatures.SpeedRead)
                        PlextorSpeedEnabled = devInfo.PlextorFeatures.SpeedReadEnabled;

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
                });
            }

            if(devInfo.ScsiInquiry?.KreonPresent == true)
            {
                Dispatcher.UIThread.Invoke(() =>
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
                });
            }

            if(devInfo.BlockLimits != null)
            {
                BlockLimits.BlockLimitsData? blockLimits = Decoders.SCSI.SSC.BlockLimits.Decode(devInfo.BlockLimits);

                if(blockLimits.HasValue)
                {
                    Dispatcher.UIThread.Invoke(() =>
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
                                    string.Format(Localization.Core
                                                              .Device_needs_a_block_size_granularity_of_pow_0_1_bytes,
                                                  blockLimits.Value.granularity,
                                                  Math.Pow(2, blockLimits.Value.granularity));
                            }
                        }
                    });
                }
            }

            if(devInfo.DensitySupport != null)
            {
                if(devInfo.DensitySupportHeader.HasValue)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        Densities = DensitySupport.PrettifyDensity(devInfo.DensitySupportHeader);
                    });
            }

            if(devInfo.MediumDensitySupport != null)
            {
                if(devInfo.MediaTypeSupportHeader.HasValue)
                    Dispatcher.UIThread.Invoke(() =>
                    {
                        MediumTypes = DensitySupport.PrettifyMediumType(devInfo.MediaTypeSupportHeader);
                    });

                Dispatcher.UIThread.Invoke(() =>
                {
                    MediumDensity = DensitySupport.PrettifyMediumType(devInfo.MediumDensitySupport);
                });
            }
        }

        if(devInfo.CID         != null ||
           devInfo.CSD         != null ||
           devInfo.OCR         != null ||
           devInfo.ExtendedCSD != null ||
           devInfo.SCR         != null)
            Dispatcher.UIThread.Invoke(() =>
            {
                SdMmcInfo = new SdMmcInfo
                {
                    DataContext = new SdMmcInfoViewModel(devInfo.Type,
                                                         devInfo.CID,
                                                         devInfo.CSD,
                                                         devInfo.OCR,
                                                         devInfo.ExtendedCSD,
                                                         devInfo.SCR)
                };
            });
    }

    public void Closed()
    {
        _dev?.Close();
    }
}