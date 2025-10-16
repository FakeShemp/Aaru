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