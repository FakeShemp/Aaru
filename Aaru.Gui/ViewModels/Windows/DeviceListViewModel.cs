// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DeviceListViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model for device list.
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
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;
using Aaru.Devices;
using Aaru.Devices.Remote;
using Aaru.Devices.Windows;
using Aaru.Gui.Models;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Aaru.Logging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Sentry;
using Spectre.Console;

namespace Aaru.Gui.ViewModels.Windows;

public partial class DeviceListViewModel : ViewModelBase
{
    readonly DeviceList _window;
    [ObservableProperty]
    ObservableCollection<DeviceModel> _devices;
    [ObservableProperty]
    string _remotePath;

    public DeviceListViewModel(DeviceList window) => _window = window;

    public DeviceListViewModel(DeviceList window, [NotNull] string remotePath)
    {
        _window    = window;
        RemotePath = remotePath;
    }

    public void LoadData()
    {
#pragma warning disable MVVMTK0034
        if(_remotePath != null)
#pragma warning restore MVVMTK0034
        {
            _ = Task.Run(LoadRemote);

            return;
        }

        DeviceInfo[] devices = null;

        if(OperatingSystem.IsWindows()) devices = ListDevices.GetList();

        if(OperatingSystem.IsLinux()) devices = ListDevices.GetList();

        if((OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) && devices != null)
        {
            Devices = [];

            foreach(DeviceInfo device in devices)
            {
                Devices.Add(new DeviceModel
                {
                    Bus       = device.Bus,
                    Model     = device.Model,
                    Path      = device.Path,
                    Serial    = device.Serial,
                    Supported = device.Supported,
                    Vendor    = device.Vendor
                });
            }

            return;
        }

        AaruLogging.Error(UI.Devices_are_not_supported_on_this_platform);

        IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                                                                             UI
                                                                                .Devices_are_not_supported_on_this_platform,
                                                                             ButtonEnum.Ok,
                                                                             Icon.Error);

        _ = msbox.ShowWindowDialogAsync(_window);
    }

    public void LoadRemote()
    {
        try
        {
            var aaruUri = new Uri(RemotePath);

            if(aaruUri.Scheme != "aaru" && aaruUri.Scheme != "dic")
            {
                AaruLogging.Error(UI.Invalid_remote_protocol);

                Dispatcher.UIThread.Invoke(() =>
                {
                    IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                        UI.Invalid_remote_protocol,
                        ButtonEnum.Ok,
                        Icon.Error);

                    _ = msbox.ShowAsync();

                    _window.Close();
                });
            }

            using var remote = new Remote(aaruUri);

            DeviceInfo[] devices = remote.ListDevices();

            Devices = [];

            Dispatcher.UIThread.Invoke(() =>
            {
                foreach(DeviceInfo device in devices)
                {
                    Devices.Add(new DeviceModel
                    {
                        Bus       = device.Bus,
                        Model     = device.Model,
                        Path      = device.Path,
                        Serial    = device.Serial,
                        Supported = device.Supported,
                        Vendor    = device.Vendor
                    });
                }
            });
        }
        catch(SocketException ex)
        {
            if(ex.SocketErrorCode == SocketError.HostNotFound)
            {
                Dispatcher.UIThread.Invoke(() =>
                {
                    IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                        UI.Host_not_found,
                        ButtonEnum.Ok,
                        Icon.Error);

                    _ = msbox.ShowAsync();

                    _window.Close();
                });
            }
            else
            {
                SentrySdk.CaptureException(ex);

                AaruLogging.Exception(ex, UI.Error_connecting_to_host);
                AaruLogging.Error(UI.Error_connecting_to_host);

                Dispatcher.UIThread.Invoke(() =>
                {
                    IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                        Markup.Remove(UI.Error_connecting_to_host),
                        ButtonEnum.Ok,
                        Icon.Error);

                    _ = msbox.ShowAsync();

                    _window.Close();
                });
            }
        }

        // ReSharper disable once UncatchableException
        catch(ArgumentException)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    UI.Server_sent_invalid_data,
                    ButtonEnum.Ok,
                    Icon.Error);

                _ = msbox.ShowAsync();

                _window.Close();
            });
        }
        catch(IOException)
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    UI.Unknown_network_error,
                    ButtonEnum.Ok,
                    Icon.Error);

                _ = msbox.ShowAsync();

                _window.Close();
            });
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            AaruLogging.Exception(ex, UI.Error_connecting_to_host);
            AaruLogging.Error(UI.Error_connecting_to_host);

            Dispatcher.UIThread.Invoke(() =>
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    Markup.Remove(UI.Error_connecting_to_host),
                    ButtonEnum.Ok,
                    Icon.Error);

                _ = msbox.ShowAsync();

                _window.Close();
            });
        }
    }
}