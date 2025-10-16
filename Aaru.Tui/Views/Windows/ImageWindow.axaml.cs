using Aaru.Tui.ViewModels.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aaru.Tui.Views.Windows;

public partial class ImageWindow : Window
{
    public ImageWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        (DataContext as ImageWindowViewModel)?.LoadComplete();
    }
}