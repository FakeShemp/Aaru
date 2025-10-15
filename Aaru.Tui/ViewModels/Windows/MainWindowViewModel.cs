using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aaru.Tui.ViewModels.Windows;

public sealed class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    ObservableCollection<string> _files = [];
    [ObservableProperty]
    public string _informationalVersion;

    public MainWindowViewModel()
    {
        ExitCommand = new RelayCommand(Exit);

        InformationalVersion =
            Assembly.GetExecutingAssembly()
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                   ?.InformationalVersion ??
            "?.?.?";
    }

    public ICommand ExitCommand { get; }

    void Exit()
    {
        var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime!.Shutdown();
    }

    public void LoadComplete()
    {
        Files.Add("..");

        foreach(string file in
                Directory.GetFiles(Directory.GetCurrentDirectory(), "*.*", SearchOption.TopDirectoryOnly))
            Files.Add(Path.GetFileName(file));
    }
}