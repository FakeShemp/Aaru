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
    public string _copyright;
    [ObservableProperty]
    public string _currentPath;
    [ObservableProperty]
    ObservableCollection<FileModel> _files = [];
    [ObservableProperty]
    public string _informationalVersion;
    [ObservableProperty]
    public FileModel _selectedFile;

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

    public ICommand OpenSelectedFileCommand { get; }

    public ICommand ExitCommand { get; }

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
    }

    void OpenSelectedFile()
    {
        if(!SelectedFile.IsDirectory) return;

        CurrentPath                  = SelectedFile.Path;
        Environment.CurrentDirectory = CurrentPath;
        LoadFiles();
    }

    bool CanOpenSelectedFile() => SelectedFile != null;
}