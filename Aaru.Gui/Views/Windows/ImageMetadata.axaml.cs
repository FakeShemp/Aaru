using Aaru.Gui.ViewModels.Windows;
using Avalonia.Controls;

namespace Aaru.Gui.Views.Windows;

public partial class ImageMetadata : Window
{
    public ImageMetadata()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        (DataContext as ImageMetadataViewModel)?.CloseImage();
        base.OnClosing(e);
    }
}