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
using Aaru.Localization;
using Aaru.Logging;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;
using MsBox.Avalonia;
using MsBox.Avalonia.Base;
using MsBox.Avalonia.Enums;
using Spectre.Console;
using Console = Aaru.Gui.Views.Dialogs.Console;
using FileSystem = Aaru.CommonTypes.AaruMetadata.FileSystem;
using ImageInfo = Aaru.Gui.Views.Panels.ImageInfo;
using Partition = Aaru.CommonTypes.Partition;
using PlatformID = Aaru.CommonTypes.Interop.PlatformID;

namespace Aaru.Gui.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    const    string MODULE_NAME = "Main Window ViewModel";
    readonly Bitmap _genericFolderIcon;
    readonly Bitmap _genericHddIcon;
    readonly Bitmap _genericOpticalIcon;
    readonly Bitmap _genericTapeIcon;
    readonly Window _view;
    Console         _console;
    [ObservableProperty]
    [CanBeNull]
    object _contentPanel;
    [ObservableProperty]
    bool _devicesSupported;
    [ObservableProperty]
    string _title;
    [ObservableProperty]
    ObservableCollection<RootModel> _treeRoot;
    object _treeViewSelectedItem;


    public MainWindowViewModel(Window view)
    {
        AboutCommand      = new AsyncRelayCommand(AboutAsync);
        EncodingsCommand  = new AsyncRelayCommand(EncodingsAsync);
        PluginsCommand    = new AsyncRelayCommand(PluginsAsync);
        StatisticsCommand = new AsyncRelayCommand(StatisticsAsync);
        ExitCommand       = new RelayCommand(Exit);
        SettingsCommand   = new AsyncRelayCommand(SettingsAsync);
        ConsoleCommand    = new RelayCommand(Console);
        OpenCommand       = new AsyncRelayCommand(OpenAsync);

        _genericHddIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-harddisk.png")));

        _genericOpticalIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/drive-optical.png")));

        _genericTapeIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/media-tape.png")));

        _genericFolderIcon =
            new Bitmap(AssetLoader.Open(new Uri("avares://Aaru.Gui/Assets/Icons/oxygen/32x32/inode-directory.png")));


        switch(DetectOS.GetRealPlatformID())
        {
            case PlatformID.Win32NT:
            case PlatformID.Linux:
            case PlatformID.FreeBSD:
                DevicesSupported = true;

                break;
        }

        TreeRoot =
        [
            new RootModel
            {
                Name = "Nothing opened."
            }
        ];

        _view = view;
        Title = "Aaru";
    }

    public ICommand OpenCommand       { get; }
    public ICommand SettingsCommand   { get; }
    public ICommand ExitCommand       { get; }
    public ICommand ConsoleCommand    { get; }
    public ICommand EncodingsCommand  { get; }
    public ICommand PluginsCommand    { get; }
    public ICommand StatisticsCommand { get; }
    public ICommand AboutCommand      { get; }

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
        get => _treeViewSelectedItem;
        set
        {
            if(value == _treeViewSelectedItem) return;

            SetProperty(ref _treeViewSelectedItem, value);

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
                    ContentPanel = new Aaru.Gui.Views.Panels.Partition
                    {
                        DataContext = partitionModel.ViewModel
                    };

                    break;
                case FileSystemModel fileSystemModel:
                    ContentPanel = new Aaru.Gui.Views.Panels.FileSystem
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

                // Create image model with appropriate icon based on media type
                var mediaResource = new Uri($"avares://Aaru.Gui/Assets/Logos/Media/{imageFormat.Info.MediaType}.png");

                var imageModel = new ImageModel
                {
                    Path = result[0].Path.LocalPath,
                    Icon = AssetLoader.Exists(mediaResource)
                               ? new Bitmap(AssetLoader.Open(mediaResource))
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
                        Name   = Localization.Core.Whole_device,
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
                Title = $"Aaru - {imageModel.FileName}";
            }
            catch(Exception ex)
            {
                IMsBox<ButtonResult> msbox = MessageBoxManager.GetMessageBoxStandard(UI.Title_Error,
                    UI.Unable_to_open_image_format,
                    ButtonEnum.Ok,
                    Icon.Error);

                await msbox.ShowAsync();

                AaruLogging.Error(UI.Unable_to_open_image_format);
                AaruLogging.Error(Localization.Core.Error_0, ex.Message);
                AaruLogging.Exception(ex, Localization.Core.Error_0, ex.Message);
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

    Task SettingsAsync()
    {
        var dialog = new SettingsDialog();
        dialog.DataContext = new SettingsViewModel(dialog, false);

        return dialog.ShowDialog(_view);
    }

    void Exit() => (Application.Current?.ApplicationLifetime as ClassicDesktopStyleApplicationLifetime)?.Shutdown();

    void Console()
    {
        if(_console is null)
        {
            _console             = new Console();
            _console.DataContext = new ConsoleViewModel(_console);
        }

        _console.Show();
    }
}