// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ArchiveSubdirectoryModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI data models.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains information about a virtual subdirectory inside an archive.
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
using Avalonia.Media.Imaging;

namespace Aaru.Gui.Models;

public sealed class ArchiveSubdirectoryModel
{
    public string                                         Name           { get; set; }
    public string                                         Path           { get; set; }
    public IArchive                                       Archive        { get; set; }
    public ObservableCollection<ArchiveSubdirectoryModel> Subdirectories { get; set; } = [];
    public ObservableCollection<ArchiveFileModel>         Entries        { get; set; } = [];
    public Bitmap                                         Icon           { get; set; }
}