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
    readonly IMediaImage _imageFormat;
    readonly Window      _parent;
    readonly Window      _view;
    [ObservableProperty]
    public string _filePath;
    [ObservableProperty]
    bool _isStatusVisible;
    [ObservableProperty]
    ObservableCollection<FileSystemModelNode> _nodes;
    [ObservableProperty]
    string? _status;

    public ImageWindowViewModel(Window parent, Window view, IMediaImage imageFormat, string filePath)
    {
        _imageFormat = imageFormat;
        FilePath     = filePath;
        _view        = view;
        _parent      = parent;

        ExitCommand = new RelayCommand(Exit);
        BackCommand = new RelayCommand(Back);
    }

    public ICommand BackCommand { get; }
    public ICommand ExitCommand { get; }

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

    public void LoadComplete()
    {
        _ = Task.Run(Worker);
    }

    void Worker()
    {
        IsStatusVisible = true;
        Status          = "Loading partitions...";

        Nodes = [];

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

        Status = "Loading filesystems...";

        PluginRegister plugins = PluginRegister.Singleton;

        foreach(Partition partition in partitionsList)
        {
            var node = new FileSystemModelNode(partition.Name ?? $"Partition {sequence}")
            {
                Partition = partition
            };

            Core.Filesystems.Identify(_imageFormat, out List<string>? idPlugins, partition);

            if(idPlugins.Count > 0)
            {
                var subNodes = new ObservableCollection<FileSystemModelNode>();

                foreach(string pluginName in idPlugins)
                {
                    if(!plugins.Filesystems.TryGetValue(pluginName, out IFilesystem? fs)) continue;
                    if(fs is null) continue;

                    var fsNode = new FileSystemModelNode(fs.Name)
                    {
                        Filesystem = fs
                    };

                    subNodes.Add(fsNode);
                }

                node.SubNodes = subNodes;
            }

            Nodes.Add(node);
            sequence++;
        }

        Status          = "Done.";
        IsStatusVisible = false;
    }
}