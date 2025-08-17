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
//     Implements the 'info' command.
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
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Image;

sealed class ImageInfoCommand : Command<ImageInfoCommand.Settings>
{
    const string MODULE_NAME = "Image-info command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("image-info");

        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--input={0}",   Markup.Escape(settings.ImagePath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}", settings.Verbose);

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
                AaruConsole.WriteLine(UI.Image_format_not_identified);

                return (int)ErrorNumber.UnrecognizedFormat;
            }

            AaruConsole.WriteLine(UI.Image_format_identified_by_0_1, imageFormat.Name, imageFormat.Id);
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

                ImageInfo.PrintImageInfo(imageFormat);

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
        }
        catch(Exception ex)
        {
            AaruConsole.ErrorWriteLine(string.Format(UI.Error_reading_file_0, Markup.Escape(ex.Message)));
            AaruConsole.WriteException(ex);

            return (int)ErrorNumber.UnexpectedException;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : ImageFamily
    {
        [Description("Media image path")]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }

#endregion
}