// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LicenseViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the license dialog.
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

using System.IO;
using System.Reflection;
using System.Windows.Input;
using Aaru.Gui.Views.Dialogs;
using Aaru.Localization;
using CommunityToolkit.Mvvm.Input;
using JetBrains.Annotations;

namespace Aaru.Gui.ViewModels.Dialogs;

public sealed class LicenseViewModel : ViewModelBase
{
    readonly LicenseDialog _view;
    string                 _versionText;

    public LicenseViewModel(LicenseDialog view)
    {
        _view        = view;
        CloseCommand = new RelayCommand(Close);

        // TODO: Localize
        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Aaru.Gui.LICENSE");

        if(stream == null) return;

        using var reader = new StreamReader(stream);

        LicenseText = reader.ReadToEnd();
    }

    [NotNull]
    public string Title => UI.Title_Aaru_license;

    [NotNull]
    public string CloseLabel => UI.ButtonLabel_Close;

    public string   LicenseText  { get; }
    public ICommand CloseCommand { get; }

    void Close() => _view.Close();
}