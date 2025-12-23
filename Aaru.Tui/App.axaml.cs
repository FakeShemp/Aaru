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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using Aaru.Tui.ViewModels.Windows;
using Aaru.Tui.Views.Windows;
using Avalonia;
using Avalonia.Markup.Xaml;
using Prism.DryIoc;

namespace Aaru.Tui;

public class App : PrismApplication
{
    /// <inheritdoc />
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        base.Initialize();
    }

    /// <inheritdoc />
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // Register your Services, Views, Dialogs, etc. here

        // Views - Region Navigation
        containerRegistry.RegisterForNavigation<FileView, FileViewViewModel>();
        containerRegistry.RegisterForNavigation<HexViewWindow, HexViewWindowViewModel>();
        containerRegistry.RegisterForNavigation<ImageWindow, ImageWindowViewModel>();
    }

    /// <inheritdoc />
    protected override AvaloniaObject CreateShell() => Container.Resolve<MainWindow>();


    /// <summary>Called after Initialize.</summary>
    protected override void OnInitialized()
    {
        // Register Views to the Region it will appear in. Don't register them in the ViewModel.
        IRegionManager regionManager = Container.Resolve<IRegionManager>();

        // WARNING: Prism v11.0.0
        // - DataTemplates MUST define a DataType or else an XAML error will be thrown
        // - Error: DataTemplate inside of DataTemplates must have a DataType set
        regionManager.RegisterViewWithRegion("ContentRegion", typeof(FileView));
    }
}

/*public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if(ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
        {
            var mainWindow = new MainWindow();
            var vm         = new MainWindowViewModel(mainWindow);
            mainWindow.DataContext     = vm;
            desktopLifetime.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }
}*/