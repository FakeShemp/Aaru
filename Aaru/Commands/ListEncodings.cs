// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ListEncodings.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     List all supported character encodings.
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

using System.Linq;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands;

sealed class ListEncodingsCommand : Command<ListEncodingsCommand.Settings>
{
    const string MODULE_NAME = "List-Encodings command";

    public override int Execute(CommandContext context, Settings settings)

    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("list-encodings");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);

        Log.Information(UI.List_encodings_command);

        var encodings = Encoding.GetEncodings()
                                .Select(info => new CommonEncodingInfo
                                 {
                                     Name        = info.Name,
                                     DisplayName = info.GetEncoding().EncodingName
                                 })
                                .ToList();

        encodings.AddRange(Claunia.Encoding.Encoding.GetEncodings()
                                  .Select(info => new CommonEncodingInfo
                                   {
                                       Name        = info.Name,
                                       DisplayName = info.DisplayName
                                   }));

        Table table = new();
        table.AddColumn(new TableColumn(new Markup($"[bold][darkgreen]{UI.Title_Name}[/][/]").Centered()));
        table.AddColumn(new TableColumn(new Markup($"[bold][slateblue1]{UI.Title_Description}[/][/]").Centered()));
        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        foreach(CommonEncodingInfo info in encodings.OrderBy(t => t.DisplayName))
        {
            table.AddRow($"[italic][darkgreen]{Markup.Escape(info.Name)}[/][/]",
                         $"[italic][slateblue1]{Markup.Escape(info.DisplayName)}[/][/]");

            Log.Information("({Name}) - {DisplayName}", info.Name, info.DisplayName);
        }

        AnsiConsole.Write(table);

        return (int)ErrorNumber.NoError;
    }

#region Nested type: CommonEncodingInfo

    struct CommonEncodingInfo
    {
        public string Name;
        public string DisplayName;
    }

#endregion

#region Nested type: Settings

    public class Settings : BaseSettings {}

#endregion
}