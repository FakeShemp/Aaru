// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Michael Drüing <michael@drueing.de>
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
// Copyright © 2021-2025 Michael Drüing
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.ComponentModel;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Archive;

sealed class ArchiveInfoCommand : Command<ArchiveInfoCommand.Settings>
{
    const string MODULE_NAME = "Analyze command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("archive-info");

        AaruConsole.DebugWriteLine(MODULE_NAME, "debug={0}",    settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "input={0}",    Markup.Escape(settings.Path ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "verbose={0}",  settings.Verbose);
        AaruConsole.DebugWriteLine(MODULE_NAME, "encoding={0}", Markup.Escape(settings.Encoding ?? ""));

        IFilter inputFilter = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_file_filter).IsIndeterminate();
            inputFilter = PluginRegister.Singleton.GetFilter(settings.Path);
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

        try
        {
            IArchive archiveFormat = null;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Identifying_archive_format).IsIndeterminate();
                archiveFormat = ArchiveFormat.Detect(inputFilter);
            });

            if(archiveFormat == null)
            {
                AaruConsole.WriteLine(UI.Archive_format_not_identified);

                return (int)ErrorNumber.UnrecognizedFormat;
            }

            AaruConsole.WriteLine(UI.Archive_format_identified_by_0_1, archiveFormat.Name, archiveFormat.Id);
            AaruConsole.WriteLine();

            try
            {
                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Obtaining_archive_information).IsIndeterminate();
                    ArchiveInfo.PrintArchiveInfo(archiveFormat, inputFilter, encodingClass);
                });

                // TODO: Implement
                //Statistics.AddArchiveFormat(archiveFormat.Format);
                Statistics.AddFilter(inputFilter.Name);
            }
            catch(Exception ex)
            {
                AaruConsole.ErrorWriteLine(UI.Unable_to_get_information_about_archive);
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

    public class Settings : ArchiveFamily
    {
        [CommandOption("-e|--encoding")]
        [Description("Name of character encoding to use.")]
        [DefaultValue(null)]
        public string Encoding { get; init; }

        [Description("Archive file path")]
        [CommandArgument(0, "<path>")]
        public string Path { get; init; }
    }

#endregion
}