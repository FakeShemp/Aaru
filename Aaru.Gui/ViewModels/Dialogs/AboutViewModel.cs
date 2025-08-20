// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AboutViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the about dialog.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Gui.Models;
using Aaru.Gui.Views.Dialogs;
using Aaru.Localization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace Aaru.Gui.ViewModels.Dialogs;

public sealed partial class AboutViewModel : ViewModelBase
{
    readonly About _view;
    [ObservableProperty]
    string _versionText;

    public AboutViewModel(About view)
    {
        _view = view;

        VersionText =
            (Attribute.GetCustomAttribute(typeof(App).Assembly, typeof(AssemblyInformationalVersionAttribute)) as
                 AssemblyInformationalVersionAttribute)?.InformationalVersion;

        WebsiteCommand = new RelayCommand(OpenWebsite);
        LicenseCommand = new AsyncRelayCommand(LicenseAsync);
        CloseCommand   = new RelayCommand(Close);

        Assemblies = [];

        _ = Task.Run(() =>
        {
            foreach(Assembly assembly in AppDomain.CurrentDomain.GetAssemblies().OrderBy(a => a.FullName))
            {
                string name = assembly.GetName().Name;

                string version =
                    (Attribute.GetCustomAttribute(assembly, typeof(AssemblyInformationalVersionAttribute)) as
                         AssemblyInformationalVersionAttribute)?.InformationalVersion;

                if(name is null || version is null) continue;

                Assemblies.Add(new AssemblyModel
                {
                    Name    = name,
                    Version = version
                });
            }
        });
    }

    [NotNull]
    public string AboutLabel => UI.Label_About;

    [NotNull]
    public string LibrariesLabel => UI.Label_Libraries;

    [NotNull]
    public string AuthorsLabel => UI.Label_Authors;

    [NotNull]
    public string Title => UI.Title_About_Aaru;

    [NotNull]
    public string SoftwareName => "Aaru";

    [NotNull]
    public string SuiteName => "Aaru Data Preservation Suite";

    [NotNull]
    public string Copyright => "© 2011-2025 Natalia Portillo";

    [NotNull]
    public string Website => "https://aaru.app";

    [NotNull]
    public string License => UI.Label_License;

    [NotNull]
    public string CloseLabel => UI.ButtonLabel_Close;

    [NotNull]
    public string AssembliesLibraryText => UI.Title_Library;

    [NotNull]
    public string AssembliesVersionText => UI.Title_Version;

    [NotNull]
    public string Authors => UI.Text_Authors;

    public ICommand                            WebsiteCommand { get; }
    public ICommand                            LicenseCommand { get; }
    public ICommand                            CloseCommand   { get; }
    public ObservableCollection<AssemblyModel> Assemblies     { get; }

    static void OpenWebsite()
    {
        var process = new Process
        {
            StartInfo =
            {
                UseShellExecute = false,
                CreateNoWindow  = true,
                Arguments       = "https://aaru.app"
            }
        };

        if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            process.StartInfo.FileName  = "cmd";
            process.StartInfo.Arguments = $"/c start {process.StartInfo.Arguments.Replace("&", "^&")}";
        }
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            process.StartInfo.FileName = "xdg-open";
        else if(RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            process.StartInfo.FileName = "open";
        else
            return;

        process.Start();
    }

    Task LicenseAsync()
    {
        var dialog = new LicenseDialog();
        dialog.DataContext = new LicenseViewModel(dialog);

        return dialog.ShowDialog(_view);
    }

    void Close() => _view.Close();
}