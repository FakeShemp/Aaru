using Aaru.Tui.ViewModels.Windows;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Aaru.Tui.Views.Windows;

public class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        (DataContext as MainWindowViewModel)?.LoadComplete();
    }
}