using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Iciclecreek.Avalonia.WindowManager;

namespace Aaru.Tui.ViewModels.Dialogs;

public sealed class ImageHelpDialogViewModel : ViewModelBase
{
    internal ManagedWindow _dialog = null!;


    public ImageHelpDialogViewModel(ManagedWindow dialog)
    {
        _dialog   = dialog;
        OkCommand = new RelayCommand(Ok);
    }

    public ICommand OkCommand { get; }

    void Ok()
    {
        _dialog.Close(true);
    }
}