// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ls.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'ls' command.
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
using System.ComponentModel;
using System.IO;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using Aaru.Helpers;
using Aaru.Localization;
using Aaru.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Commands.Archive;

sealed class ArchiveListCommand : Command<ArchiveListCommand.Settings>
{
    const string MODULE_NAME = "Archive list command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",       settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--encoding={0}",    Markup.Escape(settings.Encoding ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--long-format={0}", settings.LongFormat);
        AaruLogging.Debug(MODULE_NAME, "--input={0}",       Markup.Escape(settings.Path ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}",     settings.Verbose);
        Statistics.AddCommand("archive-list");

        IFilter inputFilter = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_file_filter).IsIndeterminate();
            inputFilter = PluginRegister.Singleton.GetFilter(settings.Path);
        });

        if(inputFilter == null)
        {
            AaruLogging.Error(UI.Cannot_open_specified_file);

            return (int)ErrorNumber.CannotOpenFile;
        }

        Encoding encodingClass = null;

        if(settings.Encoding != null)
        {
            try
            {
                encodingClass = Claunia.Encoding.Encoding.GetEncoding(settings.Encoding);

                if(settings.Verbose) AaruLogging.Verbose(UI.encoding_for_0, encodingClass.EncodingName);
            }
            catch(ArgumentException)
            {
                AaruLogging.Error(UI.Specified_encoding_is_not_supported);

                return (int)ErrorNumber.EncodingUnknown;
            }
        }

        PluginRegister plugins = PluginRegister.Singleton;

        try
        {
            IArchive archive = null;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Identifying_archive_format).IsIndeterminate();
                archive = ArchiveFormat.Detect(inputFilter);
            });

            if(archive == null)
            {
                AaruLogging.WriteLine(UI.Archive_format_not_identified_not_proceeding_with_listing);

                return (int)ErrorNumber.UnrecognizedFormat;
            }

            if(settings.Verbose)
                AaruLogging.Verbose(UI.Archive_format_identified_by_0_1, archive.Name, archive.Id);
            else
                AaruLogging.WriteLine(UI.Archive_format_identified_by_0, archive.Name);

            try
            {
                ErrorNumber opened = ErrorNumber.NoData;

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Opening_archive).IsIndeterminate();
                    opened = archive.Open(inputFilter, encodingClass);
                });

                if(opened != ErrorNumber.NoError)
                {
                    AaruLogging.Error(UI.Unable_to_open_archive_format);
                    AaruLogging.Error(Localization.Core.Error_0, opened);

                    return (int)opened;
                }

                AaruLogging.Debug(MODULE_NAME, UI.Correctly_opened_archive_file);

                // TODO: Implement
                //Statistics.AddArchiveFormat(archive.Name);
                Statistics.AddFilter(inputFilter.Name);
            }
            catch(Exception ex)
            {
                AaruLogging.Error(UI.Unable_to_open_archive_format);
                AaruLogging.Error(Localization.Core.Error_0, ex.Message);
                AaruLogging.Exception(ex, ex.Message);

                return (int)ErrorNumber.CannotOpenFormat;
            }

            if(!settings.LongFormat)
            {
                for(int i = 0; i < archive.NumberOfEntries; i++)
                {
                    ErrorNumber errno = archive.GetFilename(i, out string fileName);

                    // Ignore that file
                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Error(UI.Error_0_getting_filename_for_archive_entry_1, errno, i);

                        continue;
                    }

                    AaruLogging.WriteLine(Markup.Escape(fileName));
                }

                return (int)ErrorNumber.NoError;
            }

            var  table             = new Table();
            int  files             = 0;
            int  folders           = 0;
            long totalSize         = 0;
            long totalUncompressed = 0;

            AnsiConsole.Live(table)
                       .Start(ctx =>
                        {
                            table.HideFooters();
                            table.Border(TableBorder.Rounded);
                            table.BorderColor(Color.Yellow);

                            table.AddColumn(new TableColumn(UI.Title_Date)
                            {
                                NoWrap    = true,
                                Alignment = Justify.Center
                            });

                            ctx.Refresh();

                            table.AddColumn(new TableColumn(UI.Title_Time)
                            {
                                NoWrap    = true,
                                Alignment = Justify.Center
                            });

                            ctx.Refresh();

                            table.AddColumn(new TableColumn(UI.Title_Attributes_ABBREVIATED)
                            {
                                NoWrap    = true,
                                Alignment = Justify.Right
                            });

                            ctx.Refresh();

                            table.AddColumn(new TableColumn(UI.Title_Size)
                            {
                                NoWrap    = true,
                                Alignment = Justify.Right
                            });

                            ctx.Refresh();

                            if(archive.ArchiveFeatures.HasFlag(ArchiveSupportedFeature.SupportsCompression))
                            {
                                table.AddColumn(new TableColumn(UI.Title_Compressed)
                                {
                                    NoWrap    = true,
                                    Alignment = Justify.Right
                                });
                            }

                            ctx.Refresh();

                            table.AddColumn(new TableColumn(UI.Title_Name)
                            {
                                Alignment = Justify.Left
                            });

                            ctx.Refresh();

                            for(int i = 0; i < archive.NumberOfEntries; i++)
                            {
                                ErrorNumber errno = archive.GetFilename(i, out string fileName);

                                if(errno != ErrorNumber.NoError)
                                {
                                    AaruLogging.Error(UI.Error_0_getting_filename_for_archive_entry_1, errno, i);

                                    continue;
                                }

                                errno = archive.Stat(i, out FileEntryInfo stat);

                                if(errno != ErrorNumber.NoError)
                                {
                                    AaruLogging.Error(UI.Error_0_retrieving_stat_for_archive_entry_1, errno, i);

                                    continue;
                                }

                                char[] attr = new char[5];

                                if(stat.Attributes.HasFlag(FileAttributes.Directory))
                                {
                                    folders++;
                                    attr[0] = 'D';
                                }
                                else if(stat.Attributes.HasFlag(FileAttributes.File))
                                {
                                    files++;
                                    attr[0] = 'F';
                                }
                                else
                                {
                                    attr[0] = stat.Attributes.HasFlag(FileAttributes.Alias)   ||
                                              stat.Attributes.HasFlag(FileAttributes.Symlink) ||
                                              stat.Attributes.HasFlag(FileAttributes.Shadow)
                                                  ? 'L'
                                                  : stat.Attributes.HasFlag(FileAttributes.Device)
                                                      ? 'V'
                                                      : stat.Attributes.HasFlag(FileAttributes.Pipe)
                                                          ? 'P'
                                                          : '.';
                                }

                                attr[1] = stat.Attributes.HasFlag(FileAttributes.Archive) ? 'A' : '.';

                                attr[2] = stat.Attributes.HasFlag(FileAttributes.Immutable) ||
                                          stat.Attributes.HasFlag(FileAttributes.ReadOnly)
                                              ? 'R'
                                              : '.';

                                attr[3] = stat.Attributes.HasFlag(FileAttributes.System) ? 'S' : '.';
                                attr[4] = stat.Attributes.HasFlag(FileAttributes.Hidden) ? 'H' : '.';

                                errno = archive.GetCompressedSize(i, out long compressedSize);

                                if(errno != ErrorNumber.NoError)
                                {
                                    AaruLogging.Debug(MODULE_NAME,
                                                      UI.Error_0_getting_compressed_size_for_archive_entry_1,
                                                      errno,
                                                      i);

                                    continue;
                                }

                                errno = archive.GetUncompressedSize(i, out long uncompressedSize);

                                if(errno != ErrorNumber.NoError)
                                {
                                    AaruLogging.Debug(MODULE_NAME,
                                                      UI.Error_0_getting_uncompressed_size_for_archive_entry_1,
                                                      errno,
                                                      i);

                                    continue;
                                }

                                string color = DirColorsParser.Instance.NormalColor;

                                if(stat.Attributes.HasFlag(FileAttributes.Directory))
                                    color = DirColorsParser.Instance.DirectoryColor;
                                else if(!DirColorsParser.Instance.ExtensionColors
                                                        .TryGetValue(Path.GetExtension(fileName) ?? "",
                                                                     out color))
                                    color = DirColorsParser.Instance.NormalColor;

                                if(archive.ArchiveFeatures.HasFlag(ArchiveSupportedFeature.SupportsCompression))
                                {
                                    table.AddRow($"[blue]{stat.LastWriteTime?.ToShortDateString()       ?? ""}[/]",
                                                 $"[dodgerblue1]{stat.LastWriteTime?.ToLongTimeString() ?? ""}[/]",
                                                 $"[gold3]{new string(attr)}[/]",
                                                 $"[lime]{uncompressedSize}[/]",
                                                 $"[teal]{compressedSize}[/]",
                                                 $"[{color}]{Markup.Escape(fileName)}[/]");

                                    AaruLogging.Information($"Date: {stat.LastWriteTime?.ToShortDateString() ?? ""} "   +
                                                            $"Time: ({stat.LastWriteTime?.ToLongTimeString() ?? ""}), " +
                                                            $"Attributes: {new string(attr)}, "                        +
                                                            $"Uncompressed Size: {uncompressedSize}, "                 +
                                                            $"Compressed Size: {compressedSize}, "                     +
                                                            $"File Name: {fileName}");
                                }
                                else
                                {
                                    table.AddRow($"[blue]{stat.LastWriteTime?.ToShortDateString()       ?? ""}[/]",
                                                 $"[dodgerblue1]{stat.LastWriteTime?.ToLongTimeString() ?? ""}[/]",
                                                 $"[gold3]{new string(attr)}[/]",
                                                 $"[lime]{uncompressedSize}[/]",
                                                 $"[{color}]{Markup.Escape(fileName)}[/]");

                                    AaruLogging.Information($"Date: {stat.LastWriteTime?.ToShortDateString() ?? ""} "   +
                                                            $"Time: ({stat.LastWriteTime?.ToLongTimeString() ?? ""}), " +
                                                            $"Attributes: {new string(attr)}, "                        +
                                                            $"Uncompressed Size: {uncompressedSize}, "                 +
                                                            $"File Name: {fileName}");
                                }

                                totalSize         += compressedSize;
                                totalUncompressed += uncompressedSize;

                                ctx.Refresh();
                            }

                            table.ShowFooters();
                            table.Columns[0].Footer($"[blue]{inputFilter.LastWriteTime.ToShortDateString()}[/]");
                            table.Columns[1].Footer($"[dodgerblue1]{inputFilter.LastWriteTime.ToLongTimeString()}[/]");
                            table.Columns[3].Footer($"[lime]{totalUncompressed}[/]");

                            if(archive.ArchiveFeatures.HasFlag(ArchiveSupportedFeature.SupportsCompression))
                            {
                                table.Columns[4].Footer($"[teal]{totalSize}[/]");

                                table.Columns[5]
                                     .Footer(archive.ArchiveFeatures.HasFlag(ArchiveSupportedFeature
                                                                                .HasExplicitDirectories)
                                                 ? string.Format(UI._0_files_1_folders, files, folders)
                                                 : string.Format(UI._0_files,           files));
                            }
                            else
                            {
                                table.Columns[4]
                                     .Footer(archive.ArchiveFeatures.HasFlag(ArchiveSupportedFeature
                                                                                .HasExplicitDirectories)
                                                 ? string.Format(UI._0_files_1_folders, files, folders)
                                                 : string.Format(UI._0_files,           files));
                            }

                            table.ShowFooters();
                        });
        }
        catch(Exception ex)
        {
            AaruLogging.Error(string.Format(UI.Error_reading_file_0, Markup.Escape(ex.Message)));
            AaruLogging.Exception(ex, UI.Error_reading_file_0, ex.Message);

            return (int)ErrorNumber.UnexpectedException;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : ArchiveFamily
    {
        [CommandOption("-e|--encoding")]
        [Description("Name of character encoding to use.")]
        [DefaultValue(null)]
        public string Encoding { get; init; }

        [CommandOption("-l|--long-format")]
        [Description("Use long format.")]
        [DefaultValue(false)]
        public bool LongFormat { get; init; }

        [Description("Archive file path")]
        [CommandArgument(0, "<path>")]
        public string Path { get; init; }
    }

#endregion
}