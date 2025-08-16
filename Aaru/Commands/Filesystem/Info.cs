// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'fs-info' command.
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
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Core;
using Aaru.Localization;
using Spectre.Console;
using Spectre.Console.Cli;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Commands.Filesystem;

sealed class FilesystemInfoCommand : Command<FilesystemInfoCommand.Settings>
{
    const string MODULE_NAME = "Fs-info command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        if(settings.Debug)
        {
            IAnsiConsole stderrConsole = AnsiConsole.Create(new AnsiConsoleSettings
            {
                Out = new AnsiConsoleOutput(System.Console.Error)
            });

            AaruConsole.DebugWriteLineEvent += (format, objects) =>
            {
                if(objects is null)
                    stderrConsole.MarkupLine(format);
                else
                    stderrConsole.MarkupLine(format, objects);
            };

            AaruConsole.WriteExceptionEvent += ex => { stderrConsole.WriteException(ex); };
        }

        if(settings.Verbose)
        {
            AaruConsole.WriteEvent += (format, objects) =>
            {
                if(objects is null)
                    AnsiConsole.Markup(format);
                else
                    AnsiConsole.Markup(format, objects);
            };
        }

        Statistics.AddCommand("fs-info");

        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}",       settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--encoding={0}",    Markup.Escape(settings.Encoding ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--filesystems={0}", settings.Filesystems);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--input={0}",       Markup.Escape(settings.ImagePath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--partitions={0}",  settings.Partitions);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}",     settings.Verbose);

        IFilter inputFilter = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_file_filter).IsIndeterminate();
            inputFilter = PluginRegister.Singleton.GetFilter(settings.ImagePath);
        });

        if(inputFilter == null)
        {
            AaruConsole.ErrorWriteLine(UI.Cannot_open_specified_file);

            return (int)ErrorNumber.CannotOpenFile;
        }

        Encoding encodingClass = null;

        if(settings.Encoding != null)
        {
            try
            {
                encodingClass = Claunia.Encoding.Encoding.GetEncoding(settings.Encoding);

                if(settings.Verbose) AaruConsole.VerboseWriteLine(UI.encoding_for_0, encodingClass.EncodingName);
            }
            catch(ArgumentException)
            {
                AaruConsole.ErrorWriteLine(UI.Specified_encoding_is_not_supported);

                return (int)ErrorNumber.EncodingUnknown;
            }
        }

        PluginRegister plugins = PluginRegister.Singleton;

        bool checkRaw = false;

        try
        {
            IMediaImage imageFormat = null;
            IBaseImage  baseImage   = null;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Identifying_image_format).IsIndeterminate();
                baseImage   = ImageFormat.Detect(inputFilter);
                imageFormat = baseImage as IMediaImage;
            });

            if(baseImage == null)
            {
                AaruConsole.WriteLine(UI.Image_format_not_identified_not_proceeding_with_analysis);

                return (int)ErrorNumber.UnrecognizedFormat;
            }

            if(imageFormat == null)
            {
                AaruConsole.WriteLine(UI.Command_not_supported_for_this_image_type);

                return (int)ErrorNumber.InvalidArgument;
            }

            if(settings.Verbose)
                AaruConsole.VerboseWriteLine(UI.Image_format_identified_by_0_1, imageFormat.Name, imageFormat.Id);
            else
                AaruConsole.WriteLine(UI.Image_format_identified_by_0, imageFormat.Name);

            AaruConsole.WriteLine();

            try
            {
                ErrorNumber opened = ErrorNumber.NoData;

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Invoke_Opening_image_file).IsIndeterminate();
                    opened = imageFormat.Open(inputFilter);
                });

                if(opened != ErrorNumber.NoError)
                {
                    AaruConsole.WriteLine(UI.Unable_to_open_image_format);
                    AaruConsole.WriteLine(Localization.Core.Error_0, opened);

                    return (int)opened;
                }

                if(settings.Verbose)
                {
                    ImageInfo.PrintImageInfo(imageFormat);
                    AaruConsole.WriteLine();
                }

                Statistics.AddMediaFormat(imageFormat.Format);
                Statistics.AddMedia(imageFormat.Info.MediaType, false);
                Statistics.AddFilter(inputFilter.Name);
            }
            catch(Exception ex)
            {
                AaruConsole.ErrorWriteLine(UI.Unable_to_open_image_format);
                AaruConsole.ErrorWriteLine(Localization.Core.Error_0, ex.Message);
                AaruConsole.WriteException(ex);

                return (int)ErrorNumber.CannotOpenFormat;
            }

            List<string> idPlugins = null;
            IFilesystem  fs;
            string       information;

            if(settings.Partitions)
            {
                List<Partition> partitionsList = null;

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Enumerating_partitions).IsIndeterminate();
                    partitionsList = Core.Partitions.GetAll(imageFormat);
                });

                Core.Partitions.AddSchemesToStats(partitionsList);

                if(partitionsList.Count == 0)
                {
                    AaruConsole.DebugWriteLine(MODULE_NAME, UI.No_partitions_found);

                    if(!settings.Filesystems)
                    {
                        AaruConsole.WriteLine(UI.No_partitions_found_not_searching_for_filesystems);

                        return (int)ErrorNumber.NothingFound;
                    }

                    checkRaw = true;
                }
                else
                {
                    AaruConsole.WriteLine(UI._0_partitions_found, partitionsList.Count);

                    for(int i = 0; i < partitionsList.Count; i++)
                    {
                        Table table = new()
                        {
                            Title = new TableTitle(string.Format(UI.Partition_0, partitionsList[i].Sequence))
                        };

                        table.AddColumn("");
                        table.AddColumn("");
                        table.HideHeaders();

                        table.AddRow(UI.Title_Name, Markup.Escape(partitionsList[i].Name ?? ""));
                        table.AddRow(UI.Title_Type, Markup.Escape(partitionsList[i].Type ?? ""));

                        table.AddRow(Localization.Core.Title_Start,
                                     string.Format(UI.sector_0_byte_1,
                                                   partitionsList[i].Start,
                                                   partitionsList[i].Offset));

                        table.AddRow(UI.Title_Length,
                                     string.Format(UI._0_sectors_1_bytes,
                                                   partitionsList[i].Length,
                                                   partitionsList[i].Size));

                        table.AddRow(UI.Title_Scheme,      Markup.Escape(partitionsList[i].Scheme      ?? ""));
                        table.AddRow(UI.Title_Description, Markup.Escape(partitionsList[i].Description ?? ""));

                        AnsiConsole.Write(table);

                        if(!settings.Filesystems) continue;

                        Core.Spectre.ProgressSingleSpinner(ctx =>
                        {
                            ctx.AddTask(UI.Identifying_filesystems_on_partition).IsIndeterminate();
                            Core.Filesystems.Identify(imageFormat, out idPlugins, partitionsList[i]);
                        });

                        switch(idPlugins.Count)
                        {
                            case 0:
                                AaruConsole.WriteLine($"[bold]{UI.Filesystem_not_identified}[/]");

                                break;
                            case > 1:
                            {
                                AaruConsole.WriteLine($"[italic]{string.Format(UI.Identified_by_0_plugins,
                                                                               idPlugins.Count)}[/]");

                                foreach(string pluginName in idPlugins)
                                {
                                    if(!plugins.Filesystems.TryGetValue(pluginName, out fs)) continue;
                                    if(fs is null) continue;

                                    AaruConsole.WriteLine($"[bold]{string.Format(UI.As_identified_by_0, fs.Name)
                                    }[/]");

                                    fs.GetInformation(imageFormat,
                                                      partitionsList[i],
                                                      encodingClass,
                                                      out information,
                                                      out FileSystem fsMetadata);

                                    AaruConsole.Write(information);
                                    Statistics.AddFilesystem(fsMetadata.Type);
                                }

                                break;
                            }
                            default:
                            {
                                plugins.Filesystems.TryGetValue(idPlugins[0], out fs);

                                if(fs is null) continue;

                                AaruConsole.WriteLine($"[bold]{string.Format(UI.Identified_by_0, fs.Name)}[/]");

                                fs.GetInformation(imageFormat,
                                                  partitionsList[i],
                                                  encodingClass,
                                                  out information,
                                                  out FileSystem fsMetadata);

                                AaruConsole.Write("{0}", information);
                                Statistics.AddFilesystem(fsMetadata.Type);

                                break;
                            }
                        }

                        AaruConsole.WriteLine();
                    }
                }
            }

            if(checkRaw)
            {
                var wholePart = new Partition
                {
                    Name   = Localization.Core.Whole_device,
                    Length = imageFormat.Info.Sectors,
                    Size   = imageFormat.Info.Sectors * imageFormat.Info.SectorSize
                };

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Identifying_filesystems).IsIndeterminate();
                    Core.Filesystems.Identify(imageFormat, out idPlugins, wholePart);
                });

                switch(idPlugins.Count)
                {
                    case 0:
                        AaruConsole.WriteLine($"[bold]{UI.Filesystem_not_identified}[/]");

                        break;
                    case > 1:
                    {
                        AaruConsole.WriteLine($"[italic]{string.Format(UI.Identified_by_0_plugins, idPlugins.Count)
                        }[/]");

                        foreach(string pluginName in idPlugins)
                        {
                            if(!plugins.Filesystems.TryGetValue(pluginName, out fs)) continue;
                            if(fs is null) continue;

                            AaruConsole.WriteLine($"[bold]{string.Format(UI.As_identified_by_0, fs.Name)}[/]");

                            fs.GetInformation(imageFormat,
                                              wholePart,
                                              encodingClass,
                                              out information,
                                              out FileSystem fsMetadata);

                            AaruConsole.Write(information);
                            Statistics.AddFilesystem(fsMetadata.Type);
                        }

                        break;
                    }
                    default:
                    {
                        plugins.Filesystems.TryGetValue(idPlugins[0], out fs);

                        if(fs is null) break;

                        AaruConsole.WriteLine($"[bold]{string.Format(UI.Identified_by_0, fs.Name)}[/]");

                        fs.GetInformation(imageFormat,
                                          wholePart,
                                          encodingClass,
                                          out information,
                                          out FileSystem fsMetadata);

                        AaruConsole.Write(information);
                        Statistics.AddFilesystem(fsMetadata.Type);

                        break;
                    }
                }
            }
        }
        catch(Exception ex)
        {
            AaruConsole.ErrorWriteLine(Markup.Escape(string.Format(UI.Error_reading_file_0, ex.Message)));
            AaruConsole.WriteException(ex);

            return (int)ErrorNumber.UnexpectedException;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : FilesystemFamily
    {
        [Description("Name of character encoding to use.")]
        [DefaultValue(null)]
        [CommandOption("-e|--encoding")]
        public string Encoding { get; init; }
        [Description("Searches and prints information about filesystems.")]
        [DefaultValue(true)]
        [CommandOption("-p|--partitions")]
        public bool Partitions { get; init; }
        [Description("Searches and interprets partitions.")]
        [CommandOption("-f|--filesystems")]
        [DefaultValue(true)]
        public bool Filesystems { get; init; }
        [Description("Media image path")]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }

#endregion
}