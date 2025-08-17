// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ExtractFiles.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'extract' command.
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
using System.IO;
using System.Runtime.InteropServices;
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

namespace Aaru.Commands.Archive;

sealed class ArchiveExtractCommand : Command<ArchiveExtractCommand.Settings>
{
    const int    BUFFER_SIZE = 16777216;
    const string MODULE_NAME = "Extract-Files command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("archive-extract");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",    settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--encoding={0}", Markup.Escape(settings.Encoding  ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--input={0}",    Markup.Escape(settings.Path      ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--output={0}",   Markup.Escape(settings.OutputDir ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}",  settings.Verbose);
        AaruLogging.Debug(MODULE_NAME, "--xattrs={0}",   settings.XAttrs);

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
                AaruLogging.WriteLine(UI.Archive_format_not_identified_not_proceeding_with_extraction);

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
                AaruLogging.Exception(ex);

                return (int)ErrorNumber.CannotOpenFormat;
            }


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

                errno = archive.GetUncompressedSize(i, out long uncompressedSize);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Error(UI.Error_0_getting_uncompressed_size_for_archive_entry_1, errno, i);

                    continue;
                }

                errno = archive.GetEntry(i, out IFilter filter);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Error(UI.Error_0_getting_filter_for_archive_entry_1, errno, i);

                    continue;
                }

                if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    fileName = fileName.Replace('<', '\uFF1C')
                                       .Replace('>',  '\uFF1E')
                                       .Replace(':',  '\uFF1A')
                                       .Replace('\"', '\uFF02')
                                       .Replace('|',  '\uFF5C')
                                       .Replace('?',  '\uFF1F')
                                       .Replace('*',  '\uFF0A')
                                       .Replace('/',  '\\');
                }

                // Prevent absolute path attack
                fileName = fileName.TrimStart('\\').TrimStart('/');

                string outputPath     = Path.Combine(settings.OutputDir, fileName);
                string destinationDir = Path.GetDirectoryName(outputPath);

                if(File.Exists(destinationDir))
                {
                    AaruLogging.Error(UI.Cannot_write_file_0_output_exists, Markup.Escape(fileName));

                    continue;
                }

                if(destinationDir is not null) Directory.CreateDirectory(destinationDir);

                if(!File.Exists(outputPath) && !Directory.Exists(outputPath))
                {
                    AnsiConsole.Progress()
                               .AutoClear(true)
                               .HideCompleted(true)
                               .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                               .Start(ctx =>
                                {
                                    int position = 0;

                                    var outputFile =
                                        new FileStream(outputPath,
                                                       FileMode.CreateNew,
                                                       FileAccess.ReadWrite,
                                                       FileShare.None);

                                    ProgressTask task =
                                        ctx.AddTask(string.Format(UI.Reading_file_0, Markup.Escape(fileName)));

                                    task.MaxValue = uncompressedSize;
                                    byte[] outBuf    = new byte[BUFFER_SIZE];
                                    Stream inputFile = filter.GetDataForkStream();

                                    while(position < stat.Length)
                                    {
                                        int bytesToRead;

                                        if(stat.Length - position > BUFFER_SIZE)
                                            bytesToRead = BUFFER_SIZE;
                                        else
                                            bytesToRead = (int)(stat.Length - position);

                                        int bytesRead = inputFile.EnsureRead(outBuf, 0, bytesToRead);

                                        outputFile.Write(outBuf, 0, bytesRead);

                                        position += bytesToRead;
                                        task.Increment(bytesToRead);
                                    }

                                    inputFile.Close();
                                    outputFile.Close();
                                });

                    var fi = new FileInfo(outputPath);
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    try
                    {
                        if(stat.CreationTimeUtc.HasValue) fi.CreationTimeUtc = stat.CreationTimeUtc.Value;
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        if(stat.LastWriteTimeUtc.HasValue) fi.LastWriteTimeUtc = stat.LastWriteTimeUtc.Value;
                    }
                    catch
                    {
                        // ignored
                    }

                    try
                    {
                        if(stat.AccessTimeUtc.HasValue) fi.LastAccessTimeUtc = stat.AccessTimeUtc.Value;
                    }
                    catch
                    {
                        // ignored
                    }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                    AaruLogging.WriteLine(UI.Written_0_bytes_of_file_1_to_2,
                                          uncompressedSize,
                                          Markup.Escape(fileName),
                                          Markup.Escape(outputPath));
                }
                else
                    AaruLogging.Error(UI.Cannot_write_file_0_output_exists, Markup.Escape(fileName));

                if(!settings.XAttrs) continue;

                errno = archive.ListXAttr(i, out List<string> xattrNames);

                if(errno != ErrorNumber.NoError)
                {
                    AaruLogging.Error(UI.Error_0_listing_extended_attributes_for_archive_entry_1, errno, i);

                    continue;
                }

                foreach(string xattrName in xattrNames)
                {
                    byte[] xattrBuffer = [];

                    Core.Spectre.ProgressSingleSpinner(ctx =>
                    {
                        ctx.AddTask(UI.Reading_extended_attribute).IsIndeterminate();
                        errno = archive.GetXattr(i, xattrName, ref xattrBuffer);
                    });

                    if(errno != ErrorNumber.NoError)
                    {
                        AaruLogging.Debug(MODULE_NAME,
                                                   UI.Error_0_reading_extended_attribute_1_for_archive_entry_2,
                                                   errno,
                                                   xattrName,
                                                   i);

                        continue;
                    }

                    outputPath = Path.Combine(settings.OutputDir, ".xattrs", xattrName, fileName);

                    if(RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        outputPath = outputPath.Replace('<', '\uFF1C')
                                               .Replace('>',  '\uFF1E')
                                               .Replace(':',  '\uFF1A')
                                               .Replace('\"', '\uFF02')
                                               .Replace('|',  '\uFF5C')
                                               .Replace('?',  '\uFF1F')
                                               .Replace('*',  '\uFF0A')
                                               .Replace('/',  '\\');
                    }

                    destinationDir = Path.GetDirectoryName(outputPath);
                    if(destinationDir is not null) Directory.CreateDirectory(destinationDir);

                    if(!File.Exists(outputPath) && !Directory.Exists(outputPath))
                    {
                        Core.Spectre.ProgressSingleSpinner(ctx =>
                        {
                            ctx.AddTask(UI.Writing_extended_attribute).IsIndeterminate();

                            var outputFile =
                                new FileStream(outputPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);

                            outputFile.Write(xattrBuffer, 0, xattrBuffer.Length);

                            outputFile.Close();

                            var fi = new FileInfo(outputPath);
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                            try
                            {
                                if(stat.CreationTimeUtc.HasValue) fi.CreationTimeUtc = stat.CreationTimeUtc.Value;
                            }
                            catch
                            {
                                // ignored
                            }

                            try
                            {
                                if(stat.LastWriteTimeUtc.HasValue) fi.LastWriteTimeUtc = stat.LastWriteTimeUtc.Value;
                            }
                            catch
                            {
                                // ignored
                            }

                            try
                            {
                                if(stat.AccessTimeUtc.HasValue) fi.LastAccessTimeUtc = stat.AccessTimeUtc.Value;
                            }
                            catch
                            {
                                // ignored
                            }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
                            AaruLogging.WriteLine(UI.Written_0_bytes_of_file_1_to_2,
                                                  uncompressedSize,
                                                  Markup.Escape(fileName),
                                                  Markup.Escape(outputPath));
                        });
                    }
                    else
                        AaruLogging.Error(UI.Cannot_write_file_0_output_exists, Markup.Escape(fileName));
                }
            }
        }
        catch(Exception ex)
        {
            AaruLogging.Error(string.Format(UI.Error_reading_file_0, Markup.Escape(ex.Message)));
            AaruLogging.Exception(ex);

            return (int)ErrorNumber.UnexpectedException;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : ArchiveFamily
    {
        [Description("Name of character encoding to use.")]
        [DefaultValue(null)]
        [CommandOption("-e|--encoding")]
        public string Encoding { get; init; }

        [Description("Extract extended attributes if present.")]
        [DefaultValue(false)]
        [CommandOption("-x|--xattrs")]
        public bool XAttrs { get; init; }

        [Description("Archive file path")]
        [CommandArgument(0, "<path>")]
        public string Path { get; init; }

        [Description("Directory where extracted files will be created. Will abort if it exists")]
        [CommandArgument(1, "<output>")]
        public string OutputDir { get; init; }
    }

#endregion
}