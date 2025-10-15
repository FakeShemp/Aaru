using Avalonia.Media;

namespace Aaru.Tui.Models;

public class FileModel
{
    public string Path            { get; set; }
    public string Filename        { get; set; }
    public IBrush ForegroundBrush { get; set; } // Add this property for ListBox Foreground binding
}