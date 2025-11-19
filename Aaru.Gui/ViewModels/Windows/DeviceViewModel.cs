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

using System.Threading.Tasks;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Devices;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Aaru.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using Humanizer;
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

    public DeviceViewModel(DeviceView window, string devicePath)
    {
        _window    = window;
        DevicePath = devicePath;
    }

    public void LoadData()
    {
        _ = Task.Run(Worker);
    }

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
    }

    public void Closed()
    {
        _dev?.Close();
    }
}