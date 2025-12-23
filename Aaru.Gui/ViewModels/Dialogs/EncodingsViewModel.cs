// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : EncodingsViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the encodings list dialog.
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Aaru.Gui.Models;
using Aaru.Gui.Views.Dialogs;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;

namespace Aaru.Gui.ViewModels.Dialogs;

public sealed class EncodingsViewModel : ViewModelBase
{
    readonly Encodings _view;

    public EncodingsViewModel(Encodings view)
    {
        _view        = view;
        Encodings    = [];
        CloseCommand = new RelayCommand(Close);

        _ = Task.Run(() =>
        {
            var encodings = Encoding.GetEncodings()
                                    .Select(static info => new EncodingModel
                                     {
                                         Name        = info.Name,
                                         DisplayName = info.GetEncoding().EncodingName
                                     })
                                    .ToList();

            encodings.AddRange(Claunia.Encoding.Encoding.GetEncodings()
                                      .Select(static info => new EncodingModel
                                       {
                                           Name        = info.Name,
                                           DisplayName = info.DisplayName
                                       }));

            Dispatcher.UIThread.Invoke(() =>
            {
                foreach(EncodingModel encoding in encodings.OrderBy(static t => t.DisplayName)) Encodings.Add(encoding);
            });
        });
    }

    public ICommand                            CloseCommand { get; }
    public ObservableCollection<EncodingModel> Encodings    { get; }

    void Close() => _view.Close();
}