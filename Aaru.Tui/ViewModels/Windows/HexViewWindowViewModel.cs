using System.Collections.ObjectModel;
using System.Windows.Input;
using Aaru.CommonTypes.Interfaces;
using Aaru.Tui.ViewModels.Dialogs;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GoToSectorDialog = Aaru.Tui.Views.Dialogs.GoToSectorDialog;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class HexViewWindowViewModel : ViewModelBase
{
    private const int         BYTES_PER_LINE = 16;
    readonly      IMediaImage _imageFormat;
    readonly      Window      _parent;
    readonly      Window      _view;
    [ObservableProperty]
    ulong _currentSector;
    [ObservableProperty]
    string _filePath;
    [ObservableProperty]
    long _fileSize;
    [ObservableProperty]
    ObservableCollection<HexViewLine> _lines = [];

    internal HexViewWindowViewModel(Window parent, Window view, IMediaImage imageFormat, string filePath,
                                    ulong  currentSector = 0)
    {
        ExitCommand           = new RelayCommand(Exit);
        BackCommand           = new RelayCommand(Back);
        NextSectorCommand     = new RelayCommand(NextSector);
        PreviousSectorCommand = new RelayCommand(PreviousSector);
        GoToCommand           = new AsyncRelayCommand(GoToAsync);

        _parent        = parent;
        _view          = view;
        _imageFormat   = imageFormat;
        _currentSector = currentSector;
        FilePath       = filePath;
        LoadSector();
    }

    public ICommand BackCommand           { get; }
    public ICommand ExitCommand           { get; }
    public ICommand NextSectorCommand     { get; }
    public ICommand PreviousSectorCommand { get; }
    public ICommand GoToCommand           { get; }

    async Task GoToAsync()
    {
        var dialog = new GoToSectorDialog
        {
            DataContext = new GoToSectorDialogViewModel(null!, _imageFormat.Info.Sectors - 1)
        };

        // Set the dialog reference after creation
        ((GoToSectorDialogViewModel)dialog.DataContext!)._dialog = dialog;

        bool? result = await dialog.ShowDialog<bool?>(_view);

        if(result == true)
        {
            var viewModel = (GoToSectorDialogViewModel)dialog.DataContext;

            if(viewModel.Result.HasValue)
            {
                CurrentSector = viewModel.Result.Value;
                LoadSector();
            }
        }
    }

    void PreviousSector()
    {
        if(CurrentSector <= 0) return;

        CurrentSector--;
        LoadSector();
    }

    void NextSector()
    {
        if(CurrentSector >= _imageFormat.Info.Sectors - 1) return;

        CurrentSector++;

        LoadSector();
    }


    void Back()
    {
        _parent.Show();
        _view.Close();
    }

    void Exit()
    {
        var lifetime = Application.Current!.ApplicationLifetime as IControlledApplicationLifetime;
        lifetime!.Shutdown();
    }

    void LoadSector()
    {
        Lines.Clear();

        _imageFormat.ReadSector(CurrentSector, out byte[]? sector);

        using var stream = new MemoryStream(sector);
        var       buffer = new byte[BYTES_PER_LINE];
        long      offset = 0;

        const int maxLines = 1000;

        for(var lineCount = 0; stream.Position < stream.Length && lineCount < maxLines; lineCount++)
        {
            int bytesRead = stream.Read(buffer, 0, BYTES_PER_LINE);

            if(bytesRead == 0) break;

            var line = new HexViewLine
            {
                Offset = offset,
                Bytes  = buffer.Take(bytesRead).ToArray()
            };

            Lines.Add(line);
            offset += bytesRead;
        }
    }

    public void LoadComplete() {}
}

public sealed class HexViewLine
{
    internal long   Offset { get; init; }
    internal byte[] Bytes  { get; init; }

    public string OffsetString => $"{Offset:X8}";

    public string HexString
    {
        get
        {
            var hex = string.Join(" ", Bytes.Select(b => $"{b:X2}"));

            // Pad to 16 bytes worth of hex (16 * 3 - 1 = 47 chars)
            return hex.PadRight(47);
        }
    }

    public string AsciiString
    {
        get
        {
            var ascii = new char[Bytes.Length];

            for(var i = 0; i < Bytes.Length; i++) ascii[i] = Bytes[i] >= 32 && Bytes[i] < 127 ? (char)Bytes[i] : '.';

            return new string(ascii).PadRight(16);
        }
    }
}