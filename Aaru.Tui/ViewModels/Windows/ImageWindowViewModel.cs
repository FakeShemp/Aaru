// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Text User Interface.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.Tui.Models;
using Aaru.Tui.Views.Windows;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Humanizer;
using Humanizer.Bytes;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Tui.ViewModels.Windows;

public sealed partial class ImageWindowViewModel : ViewModelBase
{
    readonly IMediaImage _imageFormat;
    readonly Window      _parent;
    readonly Window      _view;
    [ObservableProperty]
    public string _filePath;
    [ObservableProperty]
    string _filesystemInformation;
    [ObservableProperty]
    bool _isFilesystemInformationVisible;
    [ObservableProperty]
    bool _isPartitionInformationVisible;
    [ObservableProperty]
    bool _isStatusVisible;
    [ObservableProperty]
    ObservableCollection<FileSystemModelNode> _nodes;
    [ObservableProperty]
    string _partitionDescription;
    [ObservableProperty]
    string _partitionLength;
    [ObservableProperty]
    string _partitionName;
    [ObservableProperty]
    string _partitionOffset;
    [ObservableProperty]
    string _partitionScheme;
    [ObservableProperty]
    string _partitionSequence;
    [ObservableProperty]
    string _partitionSize;
    [ObservableProperty]
    string _partitionStart;
    [ObservableProperty]
    string _partitionType;
    FileSystemModelNode? _selectedNode;
    [ObservableProperty]
    string? _status;

    public ImageWindowViewModel(Window parent, Window view, IMediaImage imageFormat, string filePath)
    {
        _imageFormat = imageFormat;
        FilePath     = filePath;
        _view        = view;
        _parent      = parent;

        ExitCommand       = new RelayCommand(Exit);
        BackCommand       = new RelayCommand(Back);
        SectorViewCommand = new RelayCommand(SectorView);
    }

    public FileSystemModelNode? SelectedNode
    {
        get => _selectedNode;
        set
        {
            SetProperty(ref _selectedNode, value);

            if(_selectedNode is null) return;

            if(_selectedNode.Partition is not null && _selectedNode.Filesystem is null)
            {
                IsPartitionInformationVisible = true;

                PartitionSequence    = _selectedNode.Partition.Value.Sequence.ToString();
                PartitionName        = _selectedNode.Partition.Value.Name;
                PartitionType        = _selectedNode.Partition.Value.Type;
                PartitionStart       = _selectedNode.Partition.Value.Start.ToString();
                PartitionOffset      = ByteSize.FromBytes(_selectedNode.Partition.Value.Offset).Humanize();
                PartitionLength      = $"{_selectedNode.Partition.Value.Length} sectors";
                PartitionSize        = ByteSize.FromBytes(_selectedNode.Partition.Value.Size).Humanize();
                PartitionScheme      = _selectedNode.Partition.Value.Scheme;
                PartitionDescription = _selectedNode.Partition.Value.Description;

                OnPropertyChanged(nameof(PartitionSequence));
                OnPropertyChanged(nameof(PartitionName));
                OnPropertyChanged(nameof(PartitionType));
                OnPropertyChanged(nameof(PartitionStart));
                OnPropertyChanged(nameof(PartitionOffset));
                OnPropertyChanged(nameof(PartitionLength));
                OnPropertyChanged(nameof(PartitionSize));
                OnPropertyChanged(nameof(PartitionScheme));
                OnPropertyChanged(nameof(PartitionDescription));
            }
            else
                IsPartitionInformationVisible = false;

            if(_selectedNode.Filesystem is not null)
            {
                IsFilesystemInformationVisible = true;
                FilesystemInformation          = _selectedNode.FilesystemInformation ?? "";

                OnPropertyChanged(nameof(FilesystemInformation));
            }
            else
                IsFilesystemInformationVisible = false;

            OnPropertyChanged(nameof(IsPartitionInformationVisible));
            OnPropertyChanged(nameof(IsFilesystemInformationVisible));
        }
    }

    public ICommand BackCommand       { get; }
    public ICommand ExitCommand       { get; }
    public ICommand SectorViewCommand { get; }

    void SectorView()
    {
        if(SelectedNode?.Partition is null) return;

        var view = new HexViewWindow();

        var vm = new HexViewWindowViewModel(_view, view, _imageFormat, FilePath, SelectedNode.Partition.Value.Start);
        view.DataContext = vm;
        view.Show();
        _view.Hide();
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
                        Partition  = partition,
                        Filesystem = fs
                    };

                    try
                    {
                        fs.GetInformation(_imageFormat, partition, Encoding.ASCII, out string? information, out _);

                        fsNode.FilesystemInformation = information;
                    }
                    catch(Exception ex)
                    {
                        SentrySdk.CaptureException(ex);
                    }

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