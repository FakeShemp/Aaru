using Avalonia.Media;

namespace Aaru.Tui.Models;

public class FileModel
{
    public string    Path            { get; set; }
    public string    Filename        { get; set; }
    public IBrush    ForegroundBrush { get; set; }
    public bool      IsDirectory     { get; set; }
    public FileInfo? FileInfo        { get; set; }
    public string?   Information     { get; set; }
}