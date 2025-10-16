using System.Windows.Input;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aaru.Tui.ViewModels.Dialogs;

public sealed partial class GoToSectorDialogViewModel : ViewModelBase
{
    readonly ulong  _maxSector;
    internal Window _dialog = null!;

    [ObservableProperty]
    string _errorMessage = string.Empty;

    [ObservableProperty]
    bool _hasError;

    [ObservableProperty]
    string _sectorNumber = string.Empty;

    public GoToSectorDialogViewModel(Window dialog, ulong maxSector)
    {
        _dialog       = dialog;
        _maxSector    = maxSector;
        OkCommand     = new RelayCommand(Ok);
        CancelCommand = new RelayCommand(Cancel);
    }

    public ulong? Result { get; private set; }

    public ICommand OkCommand     { get; }
    public ICommand CancelCommand { get; }

    void Ok()
    {
        if(!ulong.TryParse((string?)SectorNumber, out ulong sector))
        {
            ErrorMessage = "Please enter a valid number.";
            HasError     = true;

            return;
        }

        if(sector > _maxSector)
        {
            ErrorMessage = $"Sector number must be less than or equal to {_maxSector}.";
            HasError     = true;

            return;
        }

        Result = sector;
        _dialog.Close(true);
    }

    void Cancel()
    {
        Result = null;
        _dialog.Close(false);
    }
}