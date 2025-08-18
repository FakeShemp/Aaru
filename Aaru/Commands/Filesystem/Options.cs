// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Options.cs
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

using System;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using JetBrains.Annotations;
using Serilog;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Filesystem;

sealed class ListOptionsCommand : Command<ListOptionsCommand.Settings>
{
    const string MODULE_NAME = "List-Options command";

    public override int Execute(CommandContext context, Settings settings)

    {
        MainClass.PrintCopyright();

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);
        Statistics.AddCommand("list-options");

        PluginRegister plugins = PluginRegister.Singleton;

        AaruLogging.WriteLine(UI.Read_only_filesystems_options);
        AaruLogging.Information(UI.Read_only_filesystems_options);

        foreach(IReadOnlyFilesystem fs in plugins.ReadOnlyFilesystems.Values)
        {
            if(fs is null) continue;

            var options = fs.SupportedOptions.ToList();

            if(options.Count == 0) continue;

            var table = new Table
            {
                Title = new TableTitle(string.Format(UI.Options_for_0,
                                                     fs.Name))
            };

            table.AddColumn(new TableColumn(new Markup(UI.Title_Name).Centered()));
            table.AddColumn(new TableColumn(new Markup(UI.Title_Type).Centered()));
            table.AddColumn(new TableColumn(new Markup(UI.Title_Description).Centered()));
            table.Border(TableBorder.Rounded);
            table.BorderColor(Color.Yellow);

            foreach((string name, Type type, string description) option in options.OrderBy(t => t.name))
            {
                table.AddRow($"[purple]{Markup.Escape(option.name)}[/]",
                             $"[italic][olive]{TypeToString(option.type)}[/][/]",
                             $"[slateblue1]{Markup.Escape(option.description)}[/]");

                AaruLogging.Information("({Name}) - {Type} - {Description}",
                                option.name,
                                TypeToString(option.type),
                                option.description);
            }

            AnsiConsole.Write(table);
            AaruLogging.WriteLine();
        }

        return (int)ErrorNumber.NoError;
    }

    [NotNull]
    static string TypeToString([NotNull] Type type)
    {
        if(type == typeof(bool)) return UI.TypeToString_boolean;

        if(type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long))
            return UI.TypeToString_signed_number;

        if(type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong))
            return UI.TypeToString_number;

        if(type == typeof(float) || type == typeof(double)) return UI.TypeToString_float_number;

        if(type == typeof(Guid)) return UI.TypeToString_uuid;

        return type == typeof(string) ? UI.TypeToString_string : type.ToString();
    }

#region Nested type: Settings

    public class Settings : FilesystemFamily {}

#endregion
}