using System.Collections.ObjectModel;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;

namespace Aaru.Tui.Models;

public class FileSystemModelNode
{
    public FileSystemModelNode(string title) => Title = title;

    public FileSystemModelNode(string title, ObservableCollection<FileSystemModelNode> subNodes)
    {
        Title    = title;
        SubNodes = subNodes;
    }

    public ObservableCollection<FileSystemModelNode>? SubNodes   { get; set; }
    public string                                     Title      { get; }
    public Partition?                                 Partition  { get; set; }
    public IFilesystem?                               Filesystem { get; set; }
}