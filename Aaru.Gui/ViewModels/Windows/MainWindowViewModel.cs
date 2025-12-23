// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MainWindowViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Main window view model.
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Interop;
using Aaru.Core;
using Aaru.Database;
using Aaru.Gui.Models;
using Aaru.Gui.ViewModels.Dialogs;
using Aaru.Gui.ViewModels.Panels;
using Aaru.Gui.Views.Dialogs;
using Aaru.Gui.Views.Panels;
using Aaru.Gui.Views.Windows;
using Aaru.Localization;
using Aaru.Logging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Svg;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Dto;
using MsBox.Avalonia.Enums;
using MsBox.Avalonia.Models;
using Spectre.Console;
using Console = Aaru.Gui.Views.Dialogs.Console;
using FileSystem = Aaru.CommonTypes.AaruMetadata.FileSystem;
using ImageInfo = Aaru.Gui.Views.Panels.ImageInfo;
using Partition = Aaru.CommonTypes.Partition;
using PlatformID = Aaru.CommonTypes.Interop.PlatformID;

namespace Aaru.Gui.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    const    string   MODULE_NAME = "Main Window ViewModel";
    readonly SvgImage _genericFolderIcon;
    readonly SvgImage _genericHddIcon;
    readonly SvgImage _genericOpticalIcon;
    readonly SvgImage _genericTapeIcon;
    readonly Window   _view;
    Console           _console;
    [ObservableProperty]
    [CanBeNull]
    object _contentPanel;
    [ObservableProperty]
    bool _devicesSupported;
    ImageModel _image;
    [ObservableProperty]
    bool _imageLoaded;
    [ObservableProperty]
    string _title;
    [ObservableProperty]
    ObservableCollection<RootModel> _treeRoot;

    public MainWindowViewModel(Window view)
    {
        AboutCommand                = new AsyncRelayCommand(AboutAsync);
        EncodingsCommand            = new AsyncRelayCommand(EncodingsAsync);
        PluginsCommand              = new AsyncRelayCommand(PluginsAsync);
        StatisticsCommand           = new AsyncRelayCommand(StatisticsAsync);
        ExitCommand                 = new RelayCommand(Exit);
        SettingsCommand             = new AsyncRelayCommand(SettingsAsync);
        ConsoleCommand              = new RelayCommand(Console);
        OpenCommand                 = new AsyncRelayCommand(OpenAsync);
        CalculateEntropyCommand     = new RelayCommand(CalculateEntropy);
        VerifyImageCommand          = new RelayCommand(VerifyImage);
        ChecksumImageCommand        = new RelayCommand(ChecksumImage);
        ConvertImageCommand         = new RelayCommand(ConvertImage);
        CreateSidecarCommand        = new RelayCommand(CreateSidecar);
        ViewImageSectorsCommand     = new RelayCommand(ViewImageSectors);
        DecodeImageMediaTagsCommand = new RelayCommand(DecodeImageMediaTags);
        OpenMhddLogCommand          = new AsyncRelayCommand(OpenMhddLogAsync);
        OpenIbgLogCommand           = new AsyncRelayCommand(OpenIbgLogAsync);
        ConnectToRemoteCommand      = new AsyncRelayCommand(ConnectToRemoteAsync);
        OpenDeviceCommand           = new RelayCommand(OpenDevice);
        ImageMetadataCommand        = new AsyncRelayCommand(ImageMetadataAsync);
        CreateMetadataCommand       = new AsyncRelayCommand(CreateMetadataAsync);
        EditMetadataCommand         = new AsyncRelayCommand(EditMetadataAsync);

        _genericHddIcon = new SvgImage
        {
            Source =
                SvgSource.Load(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/drive-harddisk.svg")))
        };

        _genericOpticalIcon = new SvgImage
        {
            Source = SvgSource.Load(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/media-tape.svg")))
        };

        _genericTapeIcon = new SvgImage
        {
            Source =
                SvgSource.Load(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/drive-optical.svg")))
        };

        _genericFolderIcon = new SvgImage
        {
            Source =
                SvgSource.Load(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/inode-directory.svg")))
        };

        switch(DetectOS.GetRealPlatformID())
        {
            case PlatformID.Win32NT:
            case PlatformID.Linux:
                DevicesSupported = true;

                break;
        }

        TreeRoot =
        [
            new RootModel
            {
                Name = UI.Nothing_opened
            }
        ];

        _view = view;
        Title = "Aaru";
    }

    public ICommand OpenCommand                 { get; }
    public ICommand SettingsCommand             { get; }
    public ICommand ExitCommand                 { get; }
    public ICommand ConsoleCommand              { get; }
    public ICommand EncodingsCommand            { get; }
    public ICommand PluginsCommand              { get; }
    public ICommand StatisticsCommand           { get; }
    public ICommand AboutCommand                { get; }
    public ICommand CalculateEntropyCommand     { get; }
    public ICommand VerifyImageCommand          { get; }
    public ICommand ChecksumImageCommand        { get; }
    public ICommand ConvertImageCommand         { get; }
    public ICommand CreateSidecarCommand        { get; }
    public ICommand ViewImageSectorsCommand     { get; }
    public ICommand DecodeImageMediaTagsCommand { get; }
    public ICommand OpenMhddLogCommand          { get; }
    public ICommand OpenIbgLogCommand           { get; }
    public ICommand ConnectToRemoteCommand      { get; }
    public ICommand OpenDeviceCommand           { get; }
    public ICommand ImageMetadataCommand        { get; }
    public ICommand CreateMetadataCommand       { get; }
    public ICommand EditMetadataCommand         { get; }

    public bool NativeMenuSupported
    {
        get
        {
            Window mainWindow = (Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)
              ?.MainWindow;

            return mainWindow is not null && NativeMenu.GetIsNativeMenuExported(mainWindow);
        }
    }

    public object TreeViewSelectedItem
    {
        get;
        set
        {
            if(value == field) return;

            SetProperty(ref field, value);

            ContentPanel = null;

            switch(value)
            {
                case ImageModel imageModel:
                    ContentPanel = new ImageInfo
                    {
                        DataContext = imageModel.ViewModel
                    };

                    break;
                case PartitionModel partitionModel:
                    ContentPanel = new Views.Panels.Partition
                    {
                        DataContext = partitionModel.ViewModel
                    };

                    break;
                case FileSystemModel fileSystemModel:
                    ContentPanel = new Views.Panels.FileSystem
                    {
                        DataContext = fileSystemModel.ViewModel
                    };

                    break;
                case SubdirectoryModel subdirectoryModel:
                    ContentPanel = new Subdirectory
                    {
                        DataContext = new SubdirectoryViewModel(subdirectoryModel, _view)
                    };

                    break;
            }
        }
    }

    Task ImageMetadataAsync()
    {
        var dialog = new ImageMetadata();
        dialog.DataContext = new ImageMetadataViewModel(dialog);

        return dialog.ShowDialog(_view);
    }

    async Task CreateMetadataAsync()
    {
        var dialog = new MetadataEditor();
        await dialog.ShowDialog(_view);
    }

    async Task EditMetadataAsync()
    {
        var    lifetime   = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        Window mainWindow = lifetime?.MainWindow;

        if(mainWindow == null) return;

        IReadOnlyList<IStorageFile> files =
            await mainWindow.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title         = "Open Metadata File",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("JSON")
                    {
                        Patterns = new[]
                        {
                            "*.json"
                        }
                    }
                }
            });

        if(files.Count == 0) return;

        string filePath = files[0].Path.LocalPath;
        var    dialog   = new MetadataEditor(filePath);
        await dialog.ShowDialog(_view);
    }

    void OpenDevice()
    {
        var deviceListWindow = new DeviceList();

        deviceListWindow.DataContext = new DeviceListViewModel(deviceListWindow);

        deviceListWindow.Show();
    }

    async Task ConnectToRemoteAsync()
    {
        IMsBox<string> msbox = MessageBoxManager.GetMessageBoxCustom(new MessageBoxCustomParams
        {
            ContentTitle = UI.Connect_to_AaruRemote,
            ButtonDefinitions =
            [
                new ButtonDefinition
                {
                    Name      = UI.ButtonLabel_Connect,
                    IsDefault = true
                },
                new ButtonDefinition
                {
                    Name     = UI.ButtonLabel_Cancel,
                    IsCancel = true
                }
            ],
            CanResize        = false,
            CloseOnClickAway = false,
            InputParams = new InputParams
            {
                Label        = UI.Address_IP_or_hostname,
                DefaultValue = "",
                Multiline    = false
            },
            ContentMessage = UI.Introduce_AaruRemote_server_address,
            MinWidth       = 400
        });

        string result = await msbox.ShowWindowDialogAsync(_view);

        if(result != UI.ButtonLabel_Connect) return;

        var deviceListWindow = new DeviceList
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen
        };

        deviceListWindow.DataContext = new DeviceListViewModel(deviceListWindow, msbox.InputValue);

        deviceListWindow.Show();
    }

    async Task OpenMhddLogAsync()
    {
        IReadOnlyList<IStorageFile> result = await _view.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = UI.Choose_MHDD_log_to_open,
            AllowMultiple  = false,
            FileTypeFilter = [FilePickerFileTypes.MhddLogFiles]
        });

        // Exit if user did not select exactly one file
        if(result.Count != 1) return;

        var mhddLogViewWindow = new MhddLogView();

        mhddLogViewWindow.DataContext = new MhddLogViewModel(mhddLogViewWindow, result[0].Path.LocalPath);

        mhddLogViewWindow.Show();
    }

    async Task OpenIbgLogAsync()
    {
        IReadOnlyList<IStorageFile> result = await _view.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = UI.Choose_IMGBurn_log_to_open,
            AllowMultiple  = false,
            FileTypeFilter = [FilePickerFileTypes.IbgLogFiles]
        });

        // Exit if user did not select exactly one file
        if(result.Count != 1) return;

        var ibgLogViewWindow = new IbgLogView();

        ibgLogViewWindow.DataContext = new IbgLogViewModel(ibgLogViewWindow, result[0].Path.LocalPath);

        ibgLogViewWindow.Show();
    }


    async Task OpenAsync()
    {
        // Open file picker dialog to allow user to select an image file
        // TODO: Extensions
        IReadOnlyList<IStorageFile> result = await _view.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title          = UI.Dialog_Choose_image_to_open,
            AllowMultiple  = false,
            FileTypeFilter = [FilePickerFileTypes.All]
        });

        // Exit if user did not select exactly one file
        if(result.Count != 1) return;

        // Get the appropriate filter plugin for the selected file
        IFilter inputFilter = PluginRegister.Singleton.GetFilter(result[0].Path.LocalPath);

        // Show error if no suitable filter plugin is found
        if(inputFilter == null)
        {
            IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                UI.Cannot_open_specified_file,
                ButtonEnum.Ok,
                Icon.Error);

            await msbox.ShowAsync();

            return;
        }

        try
        {
            // Detect the image format of the selected file
            if(ImageFormat.Detect(inputFilter) is not IMediaImage imageFormat)
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    UI.Image_format_not_identified,
                    ButtonEnum.Ok,
                    Icon.Error);

                await msbox.ShowAsync();

                return;
            }

            AaruLogging.WriteLine(UI.Image_format_identified_by_0_1, Markup.Escape(imageFormat.Name), imageFormat.Id);

            try
            {
                // Open the image file
                ErrorNumber opened = imageFormat.Open(inputFilter);

                if(opened != ErrorNumber.NoError)
                {
                    IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                        string.Format(UI.Error_0_opening_image_format, opened),
                        ButtonEnum.Ok,
                        Icon.Error);

                    await msbox.ShowAsync();

                    AaruLogging.Error(UI.Unable_to_open_image_format);
                    AaruLogging.Error(UI.No_error_given);

                    return;
                }

                // Check if we're using dark theme
                bool isDarkTheme = _view.ActualThemeVariant == ThemeVariant.Dark;

                // Build the appropriate SVG path based on theme
                string svgPath = isDarkTheme
                                     ? $"avares://Aaru.Gui/Assets/Logos/Media/Dark/{imageFormat.Info.MediaType}.svg"
                                     : $"avares://Aaru.Gui/Assets/Logos/Media/{imageFormat.Info.MediaType}.svg";

                // Create image model with appropriate icon based on media type
                var mediaResource = new Uri(svgPath);

                // Fallback to light theme version if dark version doesn't exist
                if(isDarkTheme && !AssetLoader.Exists(mediaResource))
                    mediaResource = new Uri($"avares://Aaru.Gui/Assets/Logos/Media/{imageFormat.Info.MediaType}.svg");

                var imageModel = new ImageModel
                {
                    Path = result[0].Path.LocalPath,
                    Icon = AssetLoader.Exists(mediaResource)
                               ? new SvgImage
                               {
                                   Source = SvgSource.Load(AssetLoader.Open(mediaResource))
                               }
                               : imageFormat.Info.MetadataMediaType == MetadataMediaType.BlockMedia
                                   ? _genericHddIcon
                                   : imageFormat.Info.MetadataMediaType == MetadataMediaType.OpticalDisc
                                       ? _genericOpticalIcon
                                       : _genericFolderIcon,
                    FileName  = Path.GetFileName(result[0].Path.LocalPath),
                    Image     = imageFormat,
                    ViewModel = new ImageInfoViewModel(result[0].Path.LocalPath, inputFilter, imageFormat, _view),
                    Filter    = inputFilter
                };

                // Extract all partitions from the image
                List<Partition> partitions = Core.Partitions.GetAll(imageFormat);
                Core.Partitions.AddSchemesToStats(partitions);

                var            checkRaw = false;
                List<string>   idPlugins;
                PluginRegister plugins = PluginRegister.Singleton;

                // Process partitions or raw device if no partitions found
                if(partitions.Count == 0)
                {
                    AaruLogging.Debug(MODULE_NAME, UI.No_partitions_found);

                    checkRaw = true;
                }
                else
                {
                    AaruLogging.WriteLine(UI._0_partitions_found, partitions.Count);

                    // Group partitions by scheme and process each one
                    foreach(string scheme in partitions.Select(static p => p.Scheme).Distinct().Order())
                    {
                        // TODO: Add icons to partition schemes
                        var schemeModel = new PartitionSchemeModel
                        {
                            Name = scheme
                        };

                        foreach(Partition partition in partitions.Where(p => p.Scheme == scheme)
                                                                 .OrderBy(static p => p.Start))
                        {
                            var partitionModel = new PartitionModel
                            {
                                // TODO: Add icons to partition types
                                Name      = $"{partition.Name} ({partition.Type})",
                                Partition = partition,
                                ViewModel = new PartitionViewModel(partition)
                            };

                            AaruLogging.WriteLine(UI.Identifying_filesystems_on_partition);

                            // Identify all filesystems on this partition
                            Core.Filesystems.Identify(imageFormat, out idPlugins, partition);

                            if(idPlugins.Count == 0)
                                AaruLogging.WriteLine(UI.Filesystem_not_identified);
                            else
                            {
                                AaruLogging.WriteLine(string.Format(UI.Identified_by_0_plugins, idPlugins.Count));

                                // Mount and create models for each identified filesystem
                                foreach(string pluginName in idPlugins)
                                {
                                    if(!plugins.Filesystems.TryGetValue(pluginName, out IFilesystem fs)) continue;
                                    if(fs is null) continue;

                                    fs.GetInformation(imageFormat,
                                                      partition,
                                                      null,
                                                      out string information,
                                                      out FileSystem fsMetadata);

                                    var rofs = fs as IReadOnlyFilesystem;

                                    if(rofs != null)
                                    {
                                        ErrorNumber error = rofs.Mount(imageFormat, partition, null, [], null);

                                        if(error != ErrorNumber.NoError) rofs = null;
                                    }

                                    var filesystemModel = new FileSystemModel
                                    {
                                        VolumeName = rofs?.Metadata.VolumeName is null
                                                         ? fsMetadata.VolumeName is null
                                                               ? fsMetadata.Type
                                                               : $"{fsMetadata.VolumeName} ({fsMetadata.Type})"
                                                         : $"{rofs.Metadata.VolumeName} ({rofs.Metadata.Type})",
                                        Filesystem = fs,
                                        ReadOnlyFilesystem = rofs,
                                        ViewModel = new FileSystemViewModel(rofs?.Metadata ?? fsMetadata, information)
                                    };

                                    // TODO: Trap expanding item
                                    if(rofs != null)
                                    {
                                        filesystemModel.Roots.Add(new SubdirectoryModel
                                        {
                                            Name   = "/",
                                            Path   = "",
                                            Plugin = rofs
                                        });

                                        Statistics.AddCommand("ls");
                                    }

                                    Statistics.AddFilesystem(rofs?.Metadata.Type ?? fsMetadata.Type);
                                    partitionModel.FileSystems.Add(filesystemModel);
                                }
                            }

                            schemeModel.Partitions.Add(partitionModel);
                        }

                        imageModel.PartitionSchemesOrFileSystems.Add(schemeModel);
                    }
                }

                // If no partitions were found, check the raw device
                if(checkRaw)
                {
                    var wholePart = new Partition
                    {
                        Name   = Aaru.Localization.Core.Whole_device,
                        Length = imageFormat.Info.Sectors,
                        Size   = imageFormat.Info.Sectors * imageFormat.Info.SectorSize
                    };

                    Core.Filesystems.Identify(imageFormat, out idPlugins, wholePart);

                    if(idPlugins.Count == 0)
                        AaruLogging.WriteLine(UI.Filesystem_not_identified);
                    else
                    {
                        AaruLogging.WriteLine(string.Format(UI.Identified_by_0_plugins, idPlugins.Count));

                        // Mount and create models for each identified filesystem on raw device
                        foreach(string pluginName in idPlugins)
                        {
                            if(!plugins.Filesystems.TryGetValue(pluginName, out IFilesystem fs)) continue;
                            if(fs is null) continue;

                            fs.GetInformation(imageFormat,
                                              wholePart,
                                              null,
                                              out string information,
                                              out FileSystem fsMetadata);

                            var rofs = fs as IReadOnlyFilesystem;

                            if(rofs != null)
                            {
                                ErrorNumber error = rofs.Mount(imageFormat, wholePart, null, [], null);

                                if(error != ErrorNumber.NoError) rofs = null;
                            }

                            var filesystemModel = new FileSystemModel
                            {
                                VolumeName = rofs?.Metadata.VolumeName is null
                                                 ? fsMetadata.VolumeName is null
                                                       ? fsMetadata.Type
                                                       : $"{fsMetadata.VolumeName} ({fsMetadata.Type})"
                                                 : $"{rofs.Metadata.VolumeName} ({rofs.Metadata.Type})",
                                Filesystem         = fs,
                                ReadOnlyFilesystem = rofs,
                                ViewModel          = new FileSystemViewModel(rofs?.Metadata ?? fsMetadata, information)
                            };

                            // TODO: Trap expanding item
                            if(rofs != null)
                            {
                                filesystemModel.Roots.Add(new SubdirectoryModel
                                {
                                    Name   = "/",
                                    Path   = "",
                                    Plugin = rofs
                                });

                                Statistics.AddCommand("ls");
                            }

                            Statistics.AddFilesystem(rofs?.Metadata.Type ?? fsMetadata.Type);
                            imageModel.PartitionSchemesOrFileSystems.Add(filesystemModel);
                        }
                    }
                }

                // Update statistics and populate the tree view with the opened image
                Statistics.AddMediaFormat(imageFormat.Format);
                Statistics.AddMedia(imageFormat.Info.MediaType, false);
                Statistics.AddFilter(inputFilter.Name);

                TreeRoot.Clear();
                TreeRoot.Add(imageModel);
                Title       = $"Aaru - {imageModel.FileName}";
                ImageLoaded = true;
                _image      = imageModel;
            }
            catch(Exception ex)
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    UI.Unable_to_open_image_format,
                    ButtonEnum.Ok,
                    Icon.Error);

                await msbox.ShowAsync();

                AaruLogging.Error(UI.Unable_to_open_image_format);
                AaruLogging.Error(Aaru.Localization.Core.Error_0, ex.Message);
                AaruLogging.Exception(ex, Aaru.Localization.Core.Error_0, ex.Message);
            }
        }
        catch(Exception ex)
        {
            IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                UI.Exception_reading_file,
                ButtonEnum.Ok,
                Icon.Error);

            await msbox.ShowAsync();

            AaruLogging.Error(string.Format(UI.Error_reading_file_0, ex.Message));
            AaruLogging.Exception(ex, UI.Error_reading_file_0, ex.Message);
        }

        Statistics.AddCommand("image-info");
    }

    Task AboutAsync()
    {
        var dialog = new About();
        dialog.DataContext = new AboutViewModel(dialog);

        return dialog.ShowDialog(_view);
    }

    Task EncodingsAsync()
    {
        var dialog = new Encodings();
        dialog.DataContext = new EncodingsViewModel(dialog);

        return dialog.ShowDialog(_view);
    }

    Task PluginsAsync()
    {
        var dialog = new PluginsDialog();
        dialog.DataContext = new PluginsViewModel(dialog);

        return dialog.ShowDialog(_view);
    }

    async Task StatisticsAsync()
    {
        await using var ctx = AaruContext.Create(Settings.Settings.LocalDbPath);

        if(!ctx.Commands.Any()     &&
           !ctx.Filesystems.Any()  &&
           !ctx.Filters.Any()      &&
           !ctx.MediaFormats.Any() &&
           !ctx.Medias.Any()       &&
           !ctx.Partitions.Any()   &&
           !ctx.SeenDevices.Any())
        {
            await MessageBoxManager.GetMessageBoxStandard(UI.Title_Warning, UI.There_are_no_statistics)
                                   .ShowWindowDialogAsync(_view);

            return;
        }

        var dialog = new StatisticsDialog();
        dialog.DataContext = new StatisticsViewModel(dialog);
        await dialog.ShowDialog(_view);
    }

    internal Task SettingsAsync()
    {
        var dialog = new SettingsDialog();
        dialog.DataContext = new SettingsViewModel(dialog, false);

        return dialog.ShowDialog(_view);
    }

    internal void Exit() =>
        (Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.Shutdown();

    void Console()
    {
        if(_console is null)
        {
            _console             = new Console();
            _console.DataContext = new ConsoleViewModel(_console);
        }

        _console.Show();
    }

    void CalculateEntropy()
    {
        if(_image is not {} imageModel) return;

        var imageEntropyWindow = new ImageEntropy();
        imageEntropyWindow.DataContext = new ImageEntropyViewModel(imageModel.Image, imageEntropyWindow);

        imageEntropyWindow.Closed += (_, _) => imageEntropyWindow = null;

        imageEntropyWindow.Show();
    }

    void VerifyImage()
    {
        if(_image is not {} imageModel) return;

        var imageVerifyWindow = new ImageVerify();
        imageVerifyWindow.DataContext = new ImageVerifyViewModel(imageModel.Image, imageVerifyWindow);

        imageVerifyWindow.Closed += (_, _) => imageVerifyWindow = null;

        imageVerifyWindow.Show();
    }

    void ChecksumImage()
    {
        if(_image is not {} imageModel) return;

        var imageChecksumWindow = new ImageChecksum();
        imageChecksumWindow.DataContext = new ImageChecksumViewModel(imageModel.Image, imageChecksumWindow);

        imageChecksumWindow.Closed += (_, _) => imageChecksumWindow = null;

        imageChecksumWindow.Show();
    }

    void ConvertImage()
    {
        if(_image is not {} imageModel) return;

        var imageConvertWindow = new ImageConvert();

        imageConvertWindow.DataContext =
            new ImageConvertViewModel(imageModel.Image, imageModel.Path, imageConvertWindow);

        imageConvertWindow.Closed += (_, _) => imageConvertWindow = null;

        imageConvertWindow.Show();
    }

    void CreateSidecar()
    {
        if(_image is not {} imageModel) return;

        var imageSidecarWindow = new ImageSidecar();

        // TODO: Pass thru chosen default encoding
        imageSidecarWindow.DataContext =
            new ImageSidecarViewModel(imageModel.Image,
                                      imageModel.Path,
                                      imageModel.Filter.Id,
                                      null,
                                      imageSidecarWindow);

        imageSidecarWindow.Show();
    }

    void ViewImageSectors()
    {
        if(_image is not {} imageModel) return;

        new ViewSector
        {
            DataContext = new ViewSectorViewModel(imageModel.Image)
        }.Show();
    }

    void DecodeImageMediaTags()
    {
        if(_image is not {} imageModel) return;

        new DecodeMediaTags
        {
            DataContext = new DecodeMediaTagsViewModel(imageModel.Image)
        }.Show();
    }
}