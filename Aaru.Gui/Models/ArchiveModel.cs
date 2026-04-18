// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ArchiveModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about an opened archive.
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
using Aaru.CommonTypes.Interfaces;
using Aaru.Gui.ViewModels.Panels;
using Avalonia.Media;

namespace Aaru.Gui.Models;

public sealed class ArchiveModel : RootModel
{
    public string                                         Path      { get; set; }
    public string                                         FileName  { get; set; }
    public IImage                                         Icon      { get; set; }
    public IArchive                                       Archive   { get; set; }
    public IFilter                                        Filter    { get; set; }
    public ArchiveInfoViewModel                           ViewModel { get; set; }
    public ObservableCollection<ArchiveSubdirectoryModel> Roots     { get; set; } = [];
}