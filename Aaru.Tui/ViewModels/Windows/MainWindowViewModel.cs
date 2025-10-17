// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Text User Interface.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using System.Windows.Input;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Helpers;
using Aaru.Tui.Models;
using Aaru.Tui.ViewModels.Dialogs;
using Aaru.Tui.Views.Dialogs;
using Aaru.Tui.Views.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Humanizer.Bytes;
using Color = Avalonia.Media.Color;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    readonly Window _view;
    [ObservableProperty]
    string _copyright;
    [ObservableProperty]
    string _currentPath;
    [ObservableProperty]
    ObservableCollection<FileModel> _files = [];
    [ObservableProperty]
    string _informationalVersion;
    [ObservableProperty]
    bool _isStatusVisible;
    FileModel? _selectedFile;
    [ObservableProperty]
    string _status;

    public MainWindowViewModel(Window view)
    {
        _view                   = view;
        ExitCommand             = new RelayCommand(Exit);
        SectorViewCommand       = new RelayCommand(SectorView);
        GoToPathCommand         = new AsyncRelayCommand(GoToPathAsync);
        HelpCommand             = new AsyncRelayCommand(HelpAsync);
        OpenSelectedFileCommand = new RelayCommand(OpenSelectedFile, CanOpenSelectedFile);

        InformationalVersion =
            Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion ??
            "?.?.?";

        Copyright = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
        Status    = "Loading...";
    }

    public FileModel? SelectedFile
    {
        get => _selectedFile;
        set
        {
            SetProperty(ref _selectedFile, value);
            OnPropertyChanged(nameof(IsFileInfoAvailable));
            OnPropertyChanged(nameof(SelectedFileIsNotDirectory));
            OnPropertyChanged(nameof(SelectedFileLength));
            OnPropertyChanged(nameof(SelectedFileCreationTime));
            OnPropertyChanged(nameof(SelectedFileLastWriteTime));
            OnPropertyChanged(nameof(SelectedFileAttributes));
            OnPropertyChanged(nameof(SelectedFileUnixMode));
            OnPropertyChanged(nameof(SelectedFileHasInformation));
            OnPropertyChanged(nameof(SelectedFileInformation));
        }
    }

    public ICommand  OpenSelectedFileCommand { get; }
    public ICommand  ExitCommand { get; }
    public ICommand  SectorViewCommand { get; }
    public ICommand  GoToPathCommand { get; }
    public ICommand  HelpCommand { get; }
    public bool      IsFileInfoAvailable => SelectedFile?.FileInfo != null;
    public bool      SelectedFileIsNotDirectory => SelectedFile?.IsDirectory == false;
    public long?     SelectedFileLength => SelectedFile?.IsDirectory == false ? SelectedFile?.FileInfo?.Length : 0;
    public DateTime? SelectedFileCreationTime => SelectedFile?.FileInfo?.CreationTime;
    public DateTime? SelectedFileLastWriteTime => SelectedFile?.FileInfo?.LastWriteTime;
    public string?   SelectedFileAttributes => SelectedFile?.FileInfo?.Attributes.ToString();
    public string?   SelectedFileUnixMode => SelectedFile?.FileInfo?.UnixFileMode.ToString();
    public bool      SelectedFileHasInformation => SelectedFile?.Information != null;

    public string? SelectedFileInformation => SelectedFile?.Information;

    Task HelpAsync()
    {
        var dialog = new MainHelpDialog
        {
            DataContext = new MainHelpDialogViewModel(null!)
        };

        // Set the dialog reference after creation
        ((MainHelpDialogViewModel)dialog.DataContext!)._dialog = dialog;

        return dialog.ShowDialog(_view);
    }

    async Task GoToPathAsync()
    {
        var dialog = new GoToPathDialog
        {
            DataContext = new GoToPathDialogViewModel(null!)
        };

        // Set the dialog reference after creation
        ((GoToPathDialogViewModel)dialog.DataContext!)._dialog = dialog;

        bool? result = await dialog.ShowDialog<bool?>(_view);

        if(result == true)
        {
            var viewModel = (GoToPathDialogViewModel)dialog.DataContext;

            if(viewModel.Path is not null && Directory.Exists(viewModel.Path))
            {
                Environment.CurrentDirectory = viewModel.Path;
                LoadFiles();
            }
        }
    }

    void SectorView()
    {
        if(SelectedFile?.ImageFormat is null) return;

        var view = new HexViewWindow();

        var vm = new HexViewWindowViewModel(_view, view, SelectedFile.ImageFormat, SelectedFile.Path);
        view.DataContext = vm;
        view.Show();
        _view.Hide();
    }

    void Exit()
    {
        var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime!.Shutdown();
    }

    public void LoadComplete()
    {
        LoadFiles();
    }

    public void LoadFiles()
    {
        IsStatusVisible = true;
        Status          = "Loading...";
        CurrentPath     = Directory.GetCurrentDirectory();
        Files.Clear();

        var parentDirectory = new FileModel
        {
            Filename = "..",
            Path     = Path.GetRelativePath(CurrentPath, ".."),
            ForegroundBrush =
                new SolidColorBrush(Color.Parse(DirColorsParser.Instance.DirectoryColor ??
                                                DirColorsParser.Instance.NormalColor)),
            IsDirectory = true
        };

        Files.Add(parentDirectory);

        foreach(FileModel model in Directory.GetDirectories(CurrentPath, "*", SearchOption.TopDirectoryOnly)
                                            .Select(directory => new FileModel
                                             {
                                                 Path     = directory,
                                                 Filename = Path.GetFileName(directory),
                                                 ForegroundBrush =
                                                     new SolidColorBrush(Color.Parse(DirColorsParser.Instance
                                                                                .DirectoryColor ??
                                                                             DirColorsParser.Instance.NormalColor)),
                                                 IsDirectory = true
                                             }))
            Files.Add(model);

        foreach(string file in Directory.GetFiles(CurrentPath, "*", SearchOption.TopDirectoryOnly))
        {
            var model = new FileModel
            {
                Path     = file,
                Filename = Path.GetFileName(file)
            };

            string extension = Path.GetExtension(file);

            model.ForegroundBrush =
                new SolidColorBrush(Color.Parse(DirColorsParser.Instance.ExtensionColors.TryGetValue(extension,
                                                    out string? hex)
                                                    ? hex
                                                    : DirColorsParser.Instance.NormalColor));

            Files.Add(model);
        }

        _ = Task.Run(Worker);
    }

    void Worker()
    {
        IsStatusVisible = true;
        Status          = "Loading file information...";

        foreach(FileModel file in Files)
        {
            try
            {
                file.FileInfo = new FileInfo(file.Path);

                IFilter inputFilter = PluginRegister.Singleton.GetFilter(file.Path);

                if(inputFilter is null) continue;

                IBaseImage imageFormat = ImageFormat.Detect(inputFilter);

                if(imageFormat is null) continue;

                ErrorNumber opened = imageFormat.Open(inputFilter);

                if(opened != ErrorNumber.NoError) continue;

                StringBuilder sb = new();

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.Version))
                    sb.AppendLine($"[bold][#875fff]Format:[/][/] [italic][#af8787]{imageFormat.Format}[/][/] [#875fff]version[/] [#af8787]{imageFormat.Info.Version}[/]");
                else
                    sb.AppendLine($"[bold][#875fff]Format:[/][/] [italic][#af8787]{imageFormat.Format}[/][/]");

                switch(string.IsNullOrWhiteSpace(imageFormat.Info.Application))
                {
                    case false when !string.IsNullOrWhiteSpace(imageFormat.Info.ApplicationVersion):
                        sb.AppendLine($"[#875fff]Was created with[/] [italic][#af8787]{imageFormat.Info.Application}[/][/] [#875fff]version[/] [italic][#af8787]{imageFormat.Info.ApplicationVersion}[/][/]");

                        break;
                    case false:
                        sb.AppendLine($"[#875fff]Was created with[/] [italic][#af8787]{imageFormat.Info.Application}[/][/]");

                        break;
                }

                sb.AppendLine($"[#875fff]Image without headers is[/] [lime]{imageFormat.Info.ImageSize}[/] [#875fff]bytes long[/]");

                sb.AppendLine($"[#875fff]Contains a media of[/] [lime]{imageFormat.Info.Sectors}[/] [#875fff]sectors[/]");
                sb.AppendLine($"[#875fff]Maximum sector size of[/] [teal]{imageFormat.Info.SectorSize}[/] [#875fff]bytes[/]");
                sb.AppendLine($"[#875fff]Would be[/] [aqua]{ByteSize.FromBytes(imageFormat.Info.Sectors * imageFormat.Info.SectorSize).Humanize()}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.Creator))
                    sb.AppendLine($"[bold][#875fff]Created by:[/][/] [green]{imageFormat.Info.Creator}[/]");

                if(imageFormat.Info.CreationTime != DateTime.MinValue)
                    sb.AppendLine($"[#875fff]Created on[/] [#afd700]{imageFormat.Info.CreationTime}[/]");

                if(imageFormat.Info.LastModificationTime != DateTime.MinValue)
                    sb.AppendLine($"[#875fff]Last modified on[/] [#afd700]{imageFormat.Info.LastModificationTime}[/]");

                sb.AppendLine($"[#875fff]Contains a media of type[/] [italic][fuchsia]{imageFormat.Info.MediaType}[/][/]");
                sb.AppendLine($"[#875fff]XML type:[/] [italic][#af8787]{imageFormat.Info.MetadataMediaType}[/][/]");

                sb.AppendLine(imageFormat.Info.HasPartitions
                                  ? "[green]Has partitions[/]"
                                  : "[red]Doesn\'t have partitions[/]");

                sb.AppendLine(imageFormat.Info.HasSessions
                                  ? "[green]Has sessions[/]"
                                  : "[red]Doesn\'t have sessions[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.Comments))
                    sb.AppendLine($"[bold][#875fff]Comments:[/][/] {imageFormat.Info.Comments}");

                if(imageFormat.Info.MediaSequence != 0 && imageFormat.Info.LastMediaSequence != 0)
                {
                    sb.AppendLine($"[#875fff]Media is number[/] [teal]{imageFormat.Info.MediaSequence}[/]" +
                                  "\n"                                                                     +
                                  $" [#875fff]on a set of[/] [teal]{imageFormat.Info.LastMediaSequence}[/] [#875fff]medias[/]");
                }

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaTitle))
                    sb.AppendLine($"[bold][#875fff]Media title:[/][/] [italic]{imageFormat.Info.MediaTitle}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaManufacturer))
                    sb.AppendLine($"[bold][#875fff]Media manufacturer:[/][/] [italic]{imageFormat.Info.MediaManufacturer}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaModel))
                    sb.AppendLine($"[bold][#875fff]Media model:[/][/] [italic]{imageFormat.Info.MediaModel}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaSerialNumber))
                    sb.AppendLine($"[bold][#875fff]Media serial number:[/][/] [italic]{imageFormat.Info.MediaSerialNumber}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaBarcode))
                    sb.AppendLine($"[bold][#875fff]Media barcode:[/][/] [italic]{imageFormat.Info.MediaBarcode}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaPartNumber))
                    sb.AppendLine($"[bold][#875fff]Media part number:[/][/] [italic]{imageFormat.Info.MediaPartNumber}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveManufacturer))
                    sb.AppendLine($"[bold][#875fff]Drive manufacturer:[/][/] [italic]{imageFormat.Info.DriveManufacturer}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveModel))
                    sb.AppendLine($"[bold][#875fff]Drive model:[/][/] [italic]{imageFormat.Info.DriveModel}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveSerialNumber))
                    sb.AppendLine($"[bold][#875fff]Drive serial number:[/][/] [italic]{imageFormat.Info.DriveSerialNumber}[/]");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveFirmwareRevision))
                    sb.AppendLine($"[bold][#875fff]Drive firmware info:[/][/] [italic]{imageFormat.Info.DriveFirmwareRevision}[/]");

                if(imageFormat.Info.Cylinders > 0                                      &&
                   imageFormat.Info is { Heads: > 0, SectorsPerTrack: > 0 }            &&
                   imageFormat.Info.MetadataMediaType != MetadataMediaType.OpticalDisc &&
                   imageFormat is not ITapeImage { IsTape: true })
                    sb.AppendLine($"[bold][#875fff]Media geometry:[/][/] [italic][teal]{imageFormat.Info.Cylinders}[/] [#875fff]cylinders,[/] [teal]{imageFormat.Info.Heads}[/] [#875fff]heads,[/] [teal]{imageFormat.Info.SectorsPerTrack}[/] [#875fff]sectors per track[/][/]");

                if(imageFormat.Info.ReadableMediaTags is { Count: > 0 })
                {
                    sb.AppendLine($"[bold][blue]Contains[/] [teal]{imageFormat.Info.ReadableMediaTags.Count}[/] [blue]readable media tags:[/][/]");

                    foreach(MediaTagType tag in imageFormat.Info.ReadableMediaTags.Order())
                        sb.Append($"[italic][#af8787]{tag}[/][/] ");

                    sb.AppendLine();
                }

                if(imageFormat.Info.ReadableSectorTags is { Count: > 0 })
                {
                    sb.AppendLine($"[bold][blue]Contains [teal]{imageFormat.Info.ReadableSectorTags.Count}[/] [blue]readable sector tags:[/][/]");

                    foreach(SectorTagType tag in imageFormat.Info.ReadableSectorTags.Order())
                        sb.Append($"[italic][#af8787]{tag}[/][/] ");

                    sb.AppendLine();
                }

                file.Information = sb.ToString();

                file.ImageFormat = imageFormat as IMediaImage;
            }
            catch(Exception ex)
            {
                SentrySdk.CaptureException(ex);
            }
        }

        Status          = "Done.";
        IsStatusVisible = false;
    }

    void OpenSelectedFile()
    {
        if(SelectedFile.IsDirectory)
        {
            CurrentPath                  = SelectedFile.Path;
            Environment.CurrentDirectory = CurrentPath;
            LoadFiles();

            return;
        }

        if(SelectedFile.ImageFormat is null) return;

        var imageWindow = new ImageWindow();

        var imageViewModel = new ImageWindowViewModel(_view, imageWindow, SelectedFile.ImageFormat, SelectedFile.Path);

        imageWindow.DataContext = imageViewModel;
        imageWindow.Show();
        _view.Hide();
    }

    bool CanOpenSelectedFile() => SelectedFile != null;
}