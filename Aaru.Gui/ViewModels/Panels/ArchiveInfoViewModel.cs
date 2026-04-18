// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ArchiveInfoViewModel.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI view models.
//
// --[ Description ] ----------------------------------------------------------
//
//     View model and code for the opened archive information panel.
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

using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;
using Humanizer;

namespace Aaru.Gui.ViewModels.Panels;

public sealed class ArchiveInfoViewModel
{
    public ArchiveInfoViewModel(string path, IFilter filter, IArchive archive)
    {
        ArchivePathText     = string.Format($"[blue]{path}[/]");
        FilterText          = string.Format(UI.Filter_0,                    filter.Name);
        PluginText          = string.Format(UI.Archive_plugin_0,            archive.Name);
        PluginIdText        = string.Format(UI.Archive_plugin_id_0,         archive.Id);
        NumberOfEntriesText = string.Format(UI.Archive_number_of_entries_0, archive.NumberOfEntries);
        FeaturesText        = string.Format(UI.Archive_features_0,          archive.ArchiveFeatures.Humanize());

        archive.GetInformation(filter, null, out string information);
        InformationText = information ?? "";
    }

    public string ArchivePathText     { get; }
    public string FilterText          { get; }
    public string PluginText          { get; }
    public string PluginIdText        { get; }
    public string NumberOfEntriesText { get; }
    public string FeaturesText        { get; }
    public string InformationText     { get; }
}