using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.CommonTypes.Interop;
using Aaru.Database;
using Aaru.Gui.Models;
using Aaru.Gui.ViewModels.Dialogs;
using Aaru.Gui.Views.Dialogs;
using Aaru.Localization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using Console = Aaru.Gui.Views.Dialogs.Console;
using PlatformID = Aaru.CommonTypes.Interop.PlatformID;

namespace Aaru.Gui.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    readonly Window _view;
    Console         _console;
    [ObservableProperty]
    object _contentPanel;
    [ObservableProperty]
    bool _devicesSupported;
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
        set => SetProperty(ref _treeViewSelectedItem, value);
    }

    Task OpenAsync() =>

        // TODO
        null;

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