using System.Collections.ObjectModel;
using Aaru.CommonTypes;

namespace Aaru.Tui.Models;

public class FileSystemModelNode
{
    public FileSystemModelNode(string title) => Title = title;

    public FileSystemModelNode(string title, ObservableCollection<FileSystemModelNode> subNodes)
    {
        Title    = title;
        SubNodes = subNodes;
    }

    public ObservableCollection<FileSystemModelNode>? SubNodes  { get; }
    public string                                     Title     { get; }
    public Partition?                                 Partition { get; set; }
}