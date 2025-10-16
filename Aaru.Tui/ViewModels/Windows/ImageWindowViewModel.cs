using System.Collections.ObjectModel;
using System.Windows.Input;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.Tui.Models;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class ImageWindowViewModel : ViewModelBase
{
    readonly string      _filename;
    readonly IMediaImage _imageFormat;
    readonly Window      _view;
    [ObservableProperty]
    bool _isStatusVisible;
    [ObservableProperty]
    ObservableCollection<FileSystemModelNode> _nodes;
    [ObservableProperty]
    string? _status;

    public ImageWindowViewModel(Window view, IMediaImage imageFormat, string filename)
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

    public void LoadComplete()
    {
        _ = Task.Run(Worker);
    }

    void Worker()
    {
        IsStatusVisible = true;
        Status          = "Loading partitions...";

        Nodes = new ObservableCollection<FileSystemModelNode>();

        List<Partition>? partitionsList = Core.Partitions.GetAll(_imageFormat);

        if(partitionsList.Count == 0)
        {
            partitionsList.Add(new Partition
            {
                Name   = Localization.Core.Whole_device,
                Length = _imageFormat.Info.Sectors,
                Size   = _imageFormat.Info.Sectors * _imageFormat.Info.SectorSize
            });
        }

        var sequence = 0;

        foreach(Partition partition in partitionsList)
        {
            var node = new FileSystemModelNode(partition.Name ?? $"Partition {sequence}")
            {
                Partition = partition
            };

            Nodes.Add(node);
            sequence++;
        }

        Status          = "Done.";
        IsStatusVisible = false;
    }
}