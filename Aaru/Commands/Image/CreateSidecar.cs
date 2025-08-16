// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : CreateSidecar.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'create-sidecar' command.
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
using System.Linq;
using System.Text;
using System.Text.Json;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Core;
using Aaru.Localization;
using Spectre.Console;
using Spectre.Console.Cli;
using Directory = System.IO.Directory;
using File = System.IO.File;

namespace Aaru.Commands.Image;

sealed class CreateSidecarCommand : Command<CreateSidecarCommand.Settings>
{
    const  string       MODULE_NAME = "Create sidecar command";
    static ProgressTask _progressTask1;
    static ProgressTask _progressTask2;

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("create-sidecar");

        AaruConsole.DebugWriteLine(MODULE_NAME, "--block-size={0}", settings.BlockSize);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}",      settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--encoding={0}",   Markup.Escape(settings.Encoding  ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--input={0}",      Markup.Escape(settings.ImagePath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--tape={0}",       settings.Tape);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}",    settings.Verbose);

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

        if(File.Exists(settings.ImagePath))
        {
            if(settings.Tape)
            {
                AaruConsole.ErrorWriteLine(UI.You_cannot_use_tape_option_when_input_is_a_file);

                return (int)ErrorNumber.NotDirectory;
            }

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

            try
            {
                IBaseImage imageFormat = null;

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Identifying_image_format).IsIndeterminate();
                    imageFormat = ImageFormat.Detect(inputFilter);
                });

                if(imageFormat == null)
                {
                    AaruConsole.WriteLine(UI.Image_format_not_identified_not_proceeding_with_sidecar_creation);

                    return (int)ErrorNumber.UnrecognizedFormat;
                }

                if(settings.Verbose)
                    AaruConsole.VerboseWriteLine(UI.Image_format_identified_by_0_1, imageFormat.Name, imageFormat.Id);
                else
                    AaruConsole.WriteLine(UI.Image_format_identified_by_0, imageFormat.Name);

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

                    AaruConsole.DebugWriteLine(MODULE_NAME, UI.Correctly_opened_image_file);
                }
                catch(Exception ex)
                {
                    AaruConsole.ErrorWriteLine(UI.Unable_to_open_image_format);
                    AaruConsole.ErrorWriteLine(Localization.Core.Error_0, ex.Message);
                    AaruConsole.WriteException(ex);

                    return (int)ErrorNumber.CannotOpenFormat;
                }

                Statistics.AddMediaFormat(imageFormat.Format);
                Statistics.AddFilter(inputFilter.Name);

                var      sidecarClass = new Sidecar(imageFormat, settings.ImagePath, inputFilter.Id, encodingClass);
                Metadata sidecar      = new();

                AnsiConsole.Progress()
                           .AutoClear(true)
                           .HideCompleted(true)
                           .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                           .Start(ctx =>
                            {
                                sidecarClass.InitProgressEvent += () => { _progressTask1 = ctx.AddTask("Progress"); };

                                sidecarClass.InitProgressEvent2 += () => { _progressTask2 = ctx.AddTask("Progress"); };

                                sidecarClass.UpdateProgressEvent += (text, current, maximum) =>
                                {
                                    _progressTask1             ??= ctx.AddTask("Progress");
                                    _progressTask1.Description =   Markup.Escape(text);
                                    _progressTask1.Value       =   current;
                                    _progressTask1.MaxValue    =   maximum;
                                };

                                sidecarClass.UpdateProgressEvent2 += (text, current, maximum) =>
                                {
                                    _progressTask2             ??= ctx.AddTask("Progress");
                                    _progressTask2.Description =   Markup.Escape(text);
                                    _progressTask2.Value       =   current;
                                    _progressTask2.MaxValue    =   maximum;
                                };

                                sidecarClass.EndProgressEvent += () =>
                                {
                                    _progressTask1?.StopTask();
                                    _progressTask1 = null;
                                };

                                sidecarClass.EndProgressEvent2 += () =>
                                {
                                    _progressTask2?.StopTask();
                                    _progressTask2 = null;
                                };

                                sidecarClass.UpdateStatusEvent += text =>
                                    {
                                        AaruConsole.WriteLine(Markup.Escape(text));
                                    };

                                System.Console.CancelKeyPress += (_, e) =>
                                {
                                    e.Cancel = true;
                                    sidecarClass.Abort();
                                };

                                sidecar = sidecarClass.Create();
                            });

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(Localization.Core.Writing_metadata_sidecar).IsIndeterminate();

                    var jsonFs =
                        new FileStream(Path.Combine(Path.GetDirectoryName(settings.ImagePath) ??
                                                    throw new InvalidOperationException(),
                                                    Path.GetFileNameWithoutExtension(settings.ImagePath) +
                                                    ".metadata.json"),
                                       FileMode.Create);

                    JsonSerializer.Serialize(jsonFs,
                                             new MetadataJson
                                             {
                                                 AaruMetadata = sidecar
                                             },
                                             typeof(MetadataJson),
                                             MetadataJsonContext.Default);

                    jsonFs.Close();
                });
            }
            catch(Exception ex)
            {
                AaruConsole.ErrorWriteLine(string.Format(UI.Error_reading_file_0, Markup.Escape(ex.Message)));
                AaruConsole.WriteException(ex);

                return (int)ErrorNumber.UnexpectedException;
            }
        }
        else if(Directory.Exists(settings.ImagePath))
        {
            if(!settings.Tape)
            {
                AaruConsole.ErrorWriteLine(Localization.Core.Cannot_create_a_sidecar_from_a_directory);

                return (int)ErrorNumber.IsDirectory;
            }

            string[] contents = Directory.GetFiles(settings.ImagePath, "*", SearchOption.TopDirectoryOnly);
            var      files    = contents.Where(file => new FileInfo(file).Length % settings.BlockSize == 0).ToList();

            files.Sort(StringComparer.CurrentCultureIgnoreCase);

            var      sidecarClass = new Sidecar();
            Metadata sidecar      = new();

            AnsiConsole.Progress()
                       .AutoClear(true)
                       .HideCompleted(true)
                       .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                       .Start(ctx =>
                        {
                            sidecarClass.InitProgressEvent += () => { _progressTask1 = ctx.AddTask("Progress"); };

                            sidecarClass.InitProgressEvent2 += () => { _progressTask2 = ctx.AddTask("Progress"); };

                            sidecarClass.UpdateProgressEvent += (text, current, maximum) =>
                            {
                                _progressTask1             ??= ctx.AddTask("Progress");
                                _progressTask1.Description =   Markup.Escape(text);
                                _progressTask1.Value       =   current;
                                _progressTask1.MaxValue    =   maximum;
                            };

                            sidecarClass.UpdateProgressEvent2 += (text, current, maximum) =>
                            {
                                _progressTask2             ??= ctx.AddTask("Progress");
                                _progressTask2.Description =   Markup.Escape(text);
                                _progressTask2.Value       =   current;
                                _progressTask2.MaxValue    =   maximum;
                            };

                            sidecarClass.EndProgressEvent += () =>
                            {
                                _progressTask1?.StopTask();
                                _progressTask1 = null;
                            };

                            sidecarClass.EndProgressEvent2 += () =>
                            {
                                _progressTask2?.StopTask();
                                _progressTask2 = null;
                            };

                            sidecarClass.UpdateStatusEvent += text => { AaruConsole.WriteLine(Markup.Escape(text)); };

                            System.Console.CancelKeyPress += (_, e) =>
                            {
                                e.Cancel = true;
                                sidecarClass.Abort();
                            };

                            sidecar = sidecarClass.BlockTape(Path.GetFileName(settings.ImagePath),
                                                             files,
                                                             settings.BlockSize);
                        });

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(Localization.Core.Writing_metadata_sidecar).IsIndeterminate();

                var jsonFs =
                    new FileStream(Path.Combine(Path.GetDirectoryName(settings.ImagePath) ??
                                                throw new InvalidOperationException(),
                                                Path.GetFileNameWithoutExtension(settings.ImagePath) +
                                                ".metadata.json"),
                                   FileMode.Create);

                JsonSerializer.Serialize(jsonFs,
                                         new MetadataJson
                                         {
                                             AaruMetadata = sidecar
                                         },
                                         typeof(MetadataJson),
                                         MetadataJsonContext.Default);

                jsonFs.Close();
            });
        }
        else
            AaruConsole.ErrorWriteLine(UI.The_specified_input_file_cannot_be_found);

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : ImageFamily
    {
        [CommandOption("-b|--block-size")]
        [Description("Only used for tapes, indicates block size. Files in the folder whose size is not a multiple of this value will simply be ignored.")]
        [DefaultValue(512)]
        public uint BlockSize { get; init; }
        [CommandOption("-e|--encoding")]
        [Description("Name of character encoding to use.")]
        [DefaultValue(null)]
        public string Encoding { get; init; }
        [CommandOption("-t|--tape")]
        [Description("When used indicates that input is a folder containing alphabetically sorted files extracted from a linear block-based tape with fixed block size (e.g. a SCSI tape device).")]
        [DefaultValue(false)]
        public bool Tape { get; init; }
        [Description("Media image path")]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }

#endregion
}