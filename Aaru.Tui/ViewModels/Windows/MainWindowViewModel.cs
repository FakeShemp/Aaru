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
    public bool      IsFileInfoAvailable => SelectedFile?.FileInfo != null;
    public bool      SelectedFileIsNotDirectory => SelectedFile?.IsDirectory == false;
    public long?     SelectedFileLength => SelectedFile?.IsDirectory == false ? SelectedFile?.FileInfo?.Length : 0;
    public DateTime? SelectedFileCreationTime => SelectedFile?.FileInfo?.CreationTime;
    public DateTime? SelectedFileLastWriteTime => SelectedFile?.FileInfo?.LastWriteTime;
    public string?   SelectedFileAttributes => SelectedFile?.FileInfo?.Attributes.ToString();
    public string?   SelectedFileUnixMode => SelectedFile?.FileInfo?.UnixFileMode.ToString();
    public bool      SelectedFileHasInformation => SelectedFile?.Information != null;

    public string? SelectedFileInformation => SelectedFile?.Information;

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
                    sb.AppendLine($"Format: {imageFormat.Format} version {imageFormat.Info.Version}");
                else
                    sb.AppendLine($"Format: {imageFormat.Format}");

                switch(string.IsNullOrWhiteSpace(imageFormat.Info.Application))
                {
                    case false when !string.IsNullOrWhiteSpace(imageFormat.Info.ApplicationVersion):
                        sb.AppendLine($"Was created with {imageFormat.Info.Application} version {imageFormat.Info.ApplicationVersion}");

                        break;
                    case false:
                        sb.AppendLine($"Was created with {imageFormat.Info.Application}");

                        break;
                }

                sb.AppendLine($"Image without headers is {imageFormat.Info.ImageSize} bytes long");

                sb.AppendLine($"Contains a media of {imageFormat.Info.Sectors} sectors");
                sb.AppendLine($"Maximum sector size of {imageFormat.Info.SectorSize} bytes");
                sb.AppendLine($"Would be {ByteSize.FromBytes(imageFormat.Info.Sectors * imageFormat.Info.SectorSize).Humanize()}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.Creator))
                    sb.AppendLine($"Created by: {imageFormat.Info.Creator}");

                if(imageFormat.Info.CreationTime != DateTime.MinValue)
                    sb.AppendLine($"Created on {imageFormat.Info.CreationTime}");

                if(imageFormat.Info.LastModificationTime != DateTime.MinValue)
                    sb.AppendLine($"Last modified on {imageFormat.Info.LastModificationTime}");

                sb.AppendLine($"Contains a media of type {imageFormat.Info.MediaType}");
                sb.AppendLine($"XML type: {imageFormat.Info.MetadataMediaType}");

                sb.AppendLine(imageFormat.Info.HasPartitions ? "Has partitions" : "Doesn\'t have partitions");

                sb.AppendLine(imageFormat.Info.HasSessions ? "Has sessions" : "Doesn\'t have sessions");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.Comments))
                    sb.AppendLine($"Comments: {imageFormat.Info.Comments}");

                if(imageFormat.Info.MediaSequence != 0 && imageFormat.Info.LastMediaSequence != 0)
                {
                    sb.AppendLine($"Media is number {imageFormat.Info.MediaSequence}" +
                                  "\n"                                                +
                                  $" on a set of {imageFormat.Info.LastMediaSequence} medias");
                }

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaTitle))
                    sb.AppendLine($"Media title: {imageFormat.Info.MediaTitle}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaManufacturer))
                    sb.AppendLine($"Media manufacturer: {imageFormat.Info.MediaManufacturer}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaModel))
                    sb.AppendLine($"Media model: {imageFormat.Info.MediaModel}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaSerialNumber))
                    sb.AppendLine($"Media serial number: {imageFormat.Info.MediaSerialNumber}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaBarcode))
                    sb.AppendLine($"Media barcode: {imageFormat.Info.MediaBarcode}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.MediaPartNumber))
                    sb.AppendLine($"Media part number: {imageFormat.Info.MediaPartNumber}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveManufacturer))
                    sb.AppendLine($"Drive manufacturer: {imageFormat.Info.DriveManufacturer}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveModel))
                    sb.AppendLine($"Drive model: {imageFormat.Info.DriveModel}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveSerialNumber))
                    sb.AppendLine($"Drive serial number: {imageFormat.Info.DriveSerialNumber}");

                if(!string.IsNullOrWhiteSpace(imageFormat.Info.DriveFirmwareRevision))
                    sb.AppendLine($"Drive firmware info: {imageFormat.Info.DriveFirmwareRevision}");

                if(imageFormat.Info.Cylinders > 0                                      &&
                   imageFormat.Info is { Heads: > 0, SectorsPerTrack: > 0 }            &&
                   imageFormat.Info.MetadataMediaType != MetadataMediaType.OpticalDisc &&
                   imageFormat is not ITapeImage { IsTape: true })
                    sb.AppendLine($"Media geometry: {imageFormat.Info.Cylinders} cylinders, {imageFormat.Info.Heads} heads, {imageFormat.Info.SectorsPerTrack} sectors per track");

                if(imageFormat.Info.ReadableMediaTags is { Count: > 0 })
                {
                    sb.AppendLine($"Contains {imageFormat.Info.ReadableMediaTags.Count} readable media tags:");

                    foreach(MediaTagType tag in imageFormat.Info.ReadableMediaTags.Order()) sb.Append($"{tag} ");

                    sb.AppendLine();
                }

                if(imageFormat.Info.ReadableSectorTags is { Count: > 0 })
                {
                    sb.AppendLine($"Contains {imageFormat.Info.ReadableSectorTags.Count} readable sector tags:");

                    foreach(SectorTagType tag in imageFormat.Info.ReadableSectorTags.Order()) sb.Append($"{tag} ");

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