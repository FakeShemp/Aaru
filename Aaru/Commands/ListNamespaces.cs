// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ListNamespaces.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Lists all options supported by read-only filesystems.
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

using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands;

sealed class ListNamespacesCommand : Command<ListNamespacesCommand.Settings>
{
    const string MODULE_NAME = "List-Namespaces command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}", settings.Verbose);
        Statistics.AddCommand("list-namespaces");

        Log.Information(UI.List_namespaces_command);

        PluginRegister plugins = PluginRegister.Singleton;

        foreach(IReadOnlyFilesystem fs in plugins.ReadOnlyFilesystems.Values)
        {
            if(fs?.Namespaces is null) continue;

            Log.Information(UI.Namespaces_for_0, fs.Name);

            Table table = new()
            {
                Title = new TableTitle(string.Format($"[bold][blue]{UI.Namespaces_for_0}[/][/]",
                                                     $"[teal]{Markup.Escape(fs.Name)}[/]"))
            };

            table.Border(TableBorder.Rounded);
            table.BorderColor(Color.Yellow);

            table.AddColumn(new TableColumn(new Markup($"[bold][darkgreen]{UI.Title_Namespace}[/][/]").Centered()));
            table.AddColumn(new TableColumn(new Markup($"[bold][slateblue1]{UI.Title_Description}[/][/]").Centered()));

            foreach(KeyValuePair<string, string> @namespace in fs.Namespaces.OrderBy(t => t.Key))
            {
                table.AddRow($"[italic][darkgreen]{Markup.Escape(@namespace.Key)}[/][/]",
                             $"[italic][slateblue1]{Markup.Escape(@namespace.Value)}[/][/]");

                Log.Information("({Namespace}) - {Description}", @namespace.Key, @namespace.Value);
            }

            AnsiConsole.Write(table);
            AaruConsole.WriteLine();
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : BaseSettings {}

#endregion
}