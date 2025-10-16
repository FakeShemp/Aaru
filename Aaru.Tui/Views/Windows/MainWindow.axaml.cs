using Aaru.Tui.ViewModels.Windows;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Aaru.Tui.Views.Windows;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(object? dataContext) : this() => DataContext = dataContext;

    private void ListBox_OnKeyDown(object sender, KeyEventArgs e)
    {
        if(e.Key == Key.Enter)
        {
            if(DataContext is MainWindowViewModel vm && vm.OpenSelectedFileCommand.CanExecute(null))
            {
                vm.OpenSelectedFileCommand.Execute(null);
                e.Handled = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        (DataContext as MainWindowViewModel)?.LoadComplete();
    }
}