// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Print.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'print' command.
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
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Console;
using Aaru.Core;
using Aaru.Helpers;
using Aaru.Localization;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Image;

sealed class PrintHexCommand : Command<PrintHexCommand.Settings>
{
    const string MODULE_NAME = "PrintHex command";

    public override int Execute(CommandContext context, Settings settings)

    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("print-hex");

        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}",        settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--input={0}",        Markup.Escape(settings.ImagePath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--length={0}",       settings.Length);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--long-sectors={0}", settings.LongSectors);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--start={0}",        settings.Start);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}",      settings.Verbose);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--width={0}",        settings.Width);

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

        IBaseImage inputFormat = null;

        bool longSectors = settings.LongSectors;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_image_format).IsIndeterminate();
            inputFormat = ImageFormat.Detect(inputFilter);
        });

        if(inputFormat == null)
        {
            AaruConsole.ErrorWriteLine(UI.Unable_to_recognize_image_format_not_printing);

            return (int)ErrorNumber.UnrecognizedFormat;
        }

        ErrorNumber opened = ErrorNumber.NoData;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Invoke_Opening_image_file).IsIndeterminate();
            opened = inputFormat.Open(inputFilter);
        });

        if(opened != ErrorNumber.NoError)
        {
            AaruConsole.WriteLine(UI.Unable_to_open_image_format);
            AaruConsole.WriteLine(Localization.Core.Error_0, opened);

            return (int)opened;
        }

        if(inputFormat.Info.MetadataMediaType == MetadataMediaType.LinearMedia)
        {
            var byteAddressableImage = inputFormat as IByteAddressableImage;

            AaruConsole.WriteLine($"[bold][italic]{string.Format(UI.Start_0_as_in_sector_start, settings.Start)}[/][/]");

            byte[]      data      = new byte[settings.Length];
            ErrorNumber errno     = ErrorNumber.NoError;
            int         bytesRead = 0;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Reading_data).IsIndeterminate();

                errno = byteAddressableImage?.ReadBytesAt((long)settings.Start,
                                                          data,
                                                          0,
                                                          (int)settings.Length,
                                                          out bytesRead) ??
                        ErrorNumber.InvalidArgument;
            });

            // TODO: Span
            if(bytesRead != (int)settings.Length)
            {
                byte[] tmp = new byte[bytesRead];
                Array.Copy(data, 0, tmp, 0, bytesRead);
                data = tmp;
            }

            if(errno == ErrorNumber.NoError)
                AaruConsole.WriteLine(Markup.Escape(PrintHex.ByteArrayToHexArrayString(data, settings.Width, true)));
            else
                AaruConsole.ErrorWriteLine(string.Format(UI.Error_0_reading_data_from_1, errno, settings.Start));
        }
        else
        {
            for(ulong i = 0; i < settings.Length; i++)
            {
                if(inputFormat is not IMediaImage blockImage)
                {
                    AaruConsole.ErrorWriteLine(UI.Cannot_open_image_file_aborting);

                    break;
                }

                AaruConsole
                   .WriteLine($"[bold][italic]{string.Format(UI.Sector_0_as_in_sector_number, settings.Start)}[/][/]" +
                              i);

                if(blockImage.Info.ReadableSectorTags == null)
                {
                    AaruConsole.WriteLine(UI.Requested_sectors_tags_unsupported_by_image_format_printing_user_data);

                    longSectors = false;
                }
                else
                {
                    if(blockImage.Info.ReadableSectorTags.Count == 0)
                    {
                        AaruConsole.WriteLine(UI.Requested_sectors_tags_unsupported_by_image_format_printing_user_data);

                        longSectors = false;
                    }
                }

                byte[]      sector = [];
                ErrorNumber errno  = ErrorNumber.NoError;

                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Reading_sector).IsIndeterminate();

                    errno = longSectors
                                ? blockImage.ReadSectorLong(settings.Start + i, out sector)
                                : blockImage.ReadSector(settings.Start     + i, out sector);
                });

                if(errno == ErrorNumber.NoError)
                {
                    AaruConsole.WriteLine(Markup.Escape(PrintHex.ByteArrayToHexArrayString(sector,
                                                            settings.Width,
                                                            true)));
                }
                else
                    AaruConsole.ErrorWriteLine(string.Format(UI.Error_0_reading_sector_1, errno, settings.Start + i));
            }
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : ImageFamily
    {
        [Description("How many sectors to print.")]
        [DefaultValue(1)]
        [CommandOption("-l|--length")]
        public ulong Length { get; init; }
        [Description("Print sectors with tags included.")]
        [DefaultValue(false)]
        [CommandOption("-r|--long-sectors")]
        public bool LongSectors { get; init; }
        [Description("Starting sector.")]
        [DefaultValue(0)]
        [CommandOption("-s|--start")]
        public ulong Start { get; init; }
        [Description("How many bytes to print per line.")]
        [DefaultValue(32)]
        [CommandOption("-w|--width")]
        public ushort Width { get; init; }
        [Description("Media image path")]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }

#endregion
}