// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Formats.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'formats' command.
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
using System.Threading;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands;

sealed class FormatsCommand : Command<FormatsCommand.Settings>
{
    const string MODULE_NAME = "Formats command";

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)

    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("formats");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);

        PluginRegister plugins = PluginRegister.Singleton;

        Table table = new()
        {
            Title = new TableTitle(string.Format(UI.Supported_filters_0, PluginRegister.Singleton.Filters.Count))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Supported_filters_0, PluginRegister.Singleton.Filters.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Filter).Centered()));

        foreach(IFilter filter in PluginRegister.Singleton.Filters.Values)
        {
            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{filter.Id.ToString()}[/][/]",
                             $"[italic][purple]{Markup.Escape(filter.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", filter.Id, filter.Name);
            }
            else
            {
                table.AddRow($"[italic][purple]{Markup.Escape(filter.Name)}[/][/]");
                AaruLogging.Information("{Name}", filter.Name);
            }
        }

        AnsiConsole.Write(table);

        AaruLogging.WriteLine();

        table = new Table
        {
            Title = new TableTitle(string.Format(UI.Read_only_media_image_formats_0,
                                                 plugins.MediaImages.Count(t => !plugins.WritableImages
                                                                              .ContainsKey(t.Key))))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Read_only_media_image_formats_0,
                                plugins.MediaImages.Count(t => !plugins.WritableImages.ContainsKey(t.Key)));

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Media_image_format).Centered()));

        foreach(IMediaImage imagePlugin in
                plugins.MediaImages.Values.Where(t => !plugins.WritableImages.ContainsKey(t.Name)))
        {
            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{imagePlugin.Id.ToString()}[/][/]",
                             $"[italic][slateblue1]{Markup.Escape(imagePlugin.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", imagePlugin.Id, imagePlugin.Name);
            }
            else
            {
                table.AddRow($"[italic][slateblue1]{Markup.Escape(imagePlugin.Name)}[/][/]");
                AaruLogging.Information("{Name}", imagePlugin.Name);
            }
        }

        AnsiConsole.Write(table);

        AaruLogging.WriteLine();

        table = new Table
        {
            Title = new TableTitle(string.Format(UI.Read_write_media_image_formats_0, plugins.WritableImages.Count))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Read_write_media_image_formats_0, plugins.WritableImages.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Media_image_format).Centered()));

        foreach(IBaseWritableImage plugin in plugins.WritableImages.Values)
        {
            if(plugin is null) continue;

            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{plugin.Id.ToString()}[/][/]",
                             $"[italic][slateblue1]{Markup.Escape(plugin.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", plugin.Id, plugin.Name);
            }
            else
            {
                table.AddRow($"[italic][slateblue1]{Markup.Escape(plugin.Name)}[/][/]");
                AaruLogging.Information("{Name}", plugin.Name);
            }
        }

        AnsiConsole.Write(table);

        AnsiConsole.WriteLine();

        var idOnlyFilesystems = plugins.Filesystems.Where(t => !plugins.ReadOnlyFilesystems.ContainsKey(t.Key))
                                       .Select(static t => t.Value)
                                       .Where(static t => t is not null)
                                       .ToList();

        table = new Table
        {
            Title = new TableTitle(string.Format(UI.Supported_filesystems_for_identification_and_information_only_0,
                                                 idOnlyFilesystems.Count))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Supported_filesystems_for_identification_and_information_only_0,
                                idOnlyFilesystems.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Filesystem).Centered()));

        foreach(IFilesystem fs in idOnlyFilesystems)
        {
            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{fs.Id.ToString()}[/][/]",
                             $"[italic][purple]{Markup.Escape(fs.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", fs.Id, fs.Name);
            }
            else
            {
                table.AddRow($"[italic][purple]{Markup.Escape(fs.Name)}[/][/]");
                AaruLogging.Information("{Name}", fs.Name);
            }
        }

        AnsiConsole.Write(table);

        AaruLogging.WriteLine();

        table = new Table
        {
            Title = new TableTitle(string.Format(UI.Supported_filesystems_that_can_read_their_contents_0,
                                                 plugins.ReadOnlyFilesystems.Count))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Supported_filesystems_that_can_read_their_contents_0,
                                plugins.ReadOnlyFilesystems.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Filesystem).Centered()));

        foreach(IReadOnlyFilesystem fs in plugins.ReadOnlyFilesystems.Values)
        {
            if(fs is null) continue;

            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{fs.Id.ToString()}[/][/]",
                             $"[italic][purple]{Markup.Escape(fs.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", fs.Id, fs.Name);
            }
            else
            {
                table.AddRow($"[italic][purple]{Markup.Escape(fs.Name)}[/][/]");
                AaruLogging.Information("{Name}", fs.Name);
            }
        }

        AnsiConsole.Write(table);

        AaruLogging.WriteLine();

        table = new Table
        {
            Title = new TableTitle(string.Format(UI.Supported_partitioning_schemes_0,
                                                 PluginRegister.Singleton.Partitions.Count))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Supported_partitioning_schemes_0, plugins.Partitions.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Scheme).Centered()));

        foreach(IPartition plugin in plugins.Partitions.Values)
        {
            if(plugin is null) continue;

            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{plugin.Id.ToString()}[/][/]",
                             $"[italic][purple]{Markup.Escape(plugin.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", plugin.Id, plugin.Name);
            }
            else
            {
                table.AddRow($"[italic][purple]{Markup.Escape(plugin.Name)}[/][/]");
                AaruLogging.Information("{Name}", plugin.Name);
            }
        }

        AnsiConsole.Write(table);

        AaruLogging.WriteLine();

        table = new Table
        {
            Title = new TableTitle(string.Format($"[blue]{UI.Supported_archive_formats_0}[/]",
                                                 $"[green]{PluginRegister.Singleton.Archives.Count}[/]"))
        };

        table.Border(TableBorder.Rounded);
        table.BorderColor(Color.Yellow);

        AaruLogging.Information(UI.Supported_archive_formats_0, PluginRegister.Singleton.Archives.Count);

        if(settings.Verbose) table.AddColumn(new TableColumn(new Markup(UI.Title_GUID).Centered()));

        table.AddColumn(new TableColumn(new Markup(UI.Title_Archive_Format).Centered()));

        foreach(IArchive archive in plugins.Archives.Values)
        {
            if(archive is null) continue;

            if(settings.Verbose)
            {
                table.AddRow($"[italic][slateblue1]{archive.Id.ToString()}[/][/]",
                             $"[italic][red3]{Markup.Escape(archive.Name)}[/][/]");

                AaruLogging.Information("({Id}) {Name}", archive.Id, archive.Name);
            }
            else
            {
                table.AddRow($"[italic][red3]{Markup.Escape(archive.Name)}[/][/]");
                AaruLogging.Information("{Name}", archive.Name);
            }
        }

        AnsiConsole.Write(table);

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : BaseSettings {}

#endregion
}