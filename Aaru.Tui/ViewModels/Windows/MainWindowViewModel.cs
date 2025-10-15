using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using Aaru.Helpers;
using Aaru.Tui.Models;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Color = Avalonia.Media.Color;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    string _copyright;
    [ObservableProperty]
    string _currentPath;
    [ObservableProperty]
    ObservableCollection<FileModel> _files = [];
    [ObservableProperty]
    string _informationalVersion;
    FileModel? _selectedFile;

    public MainWindowViewModel()
    {
        ExitCommand             = new RelayCommand(Exit);
        OpenSelectedFileCommand = new RelayCommand(OpenSelectedFile, CanOpenSelectedFile);

        InformationalVersion =
            Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion ??
            "?.?.?";

        Copyright = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyCopyrightAttribute>()?.Copyright ?? "";
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
        CurrentPath = Directory.GetCurrentDirectory();
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
        foreach(FileModel file in Files) file.FileInfo = new FileInfo(file.Path);
    }

    void OpenSelectedFile()
    {
        if(SelectedFile?.IsDirectory != true) return;

        CurrentPath                  = SelectedFile.Path;
        Environment.CurrentDirectory = CurrentPath;
        LoadFiles();
    }

    bool CanOpenSelectedFile() => SelectedFile != null;
}