using System.Windows.Input;
using Aaru.CommonTypes.Interfaces;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.Input;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class ImageWindowViewModel : ViewModelBase
{
    readonly string     _filename;
    readonly IBaseImage _imageFormat;
    readonly Window     _view;

    public ImageWindowViewModel(Window view, IBaseImage imageFormat, string filename)
    {
        _imageFormat = imageFormat;
        _filename    = filename;
        _view        = view;

        ExitCommand = new RelayCommand(Exit);
        BackCommand = new RelayCommand(Back);
    }

    public ICommand BackCommand { get; }
    public ICommand ExitCommand { get; }

    void Back()
    {
        _view.Close();
    }

    void Exit()
    {
        var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime!.Shutdown();
    }
}