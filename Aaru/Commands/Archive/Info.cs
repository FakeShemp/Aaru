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
// Copyright © 2021-2026 Michael Drüing
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.ComponentModel;
using System.Text;
using System.Threading;
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

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("archive-info");

        AaruLogging.Debug(MODULE_NAME, "debug={0}",    settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "input={0}",    Markup.Escape(settings.Path ?? ""));
        AaruLogging.Debug(MODULE_NAME, "verbose={0}",  settings.Verbose);
        AaruLogging.Debug(MODULE_NAME, "encoding={0}", Markup.Escape(settings.Encoding ?? ""));

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
            IArchive archiveFormat = null;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Identifying_archive_format).IsIndeterminate();
                archiveFormat = ArchiveFormat.Detect(inputFilter);
            });

            if(archiveFormat == null)
            {
                AaruLogging.WriteLine(UI.Archive_format_not_identified);

                return (int)ErrorNumber.UnrecognizedFormat;
            }

            AaruLogging.WriteLine(UI.Archive_format_identified_by_0_1, archiveFormat.Name, archiveFormat.Id);
            AaruLogging.WriteLine();

            try
            {
                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Obtaining_archive_information).IsIndeterminate();
                    ArchiveInfo.PrintArchiveInfo(archiveFormat, inputFilter, encodingClass);
                });

                Statistics.AddArchiveFormat(archiveFormat.Name);
                Statistics.AddFilter(inputFilter.Name);
            }
            catch(Exception ex)
            {
                AaruLogging.Error(UI.Unable_to_get_information_about_archive);
                AaruLogging.Error(Localization.Core.Error_0, ex.Message);
                AaruLogging.Exception(ex, ex.Message);

                return (int)ErrorNumber.CannotOpenFormat;
            }
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
        [LocalizedDescription(nameof(UI.Name_of_character_encoding_to_use))]
        [DefaultValue(null)]
        public string Encoding { get; init; }

        [LocalizedDescription(nameof(UI.Archive_file_path))]
        [CommandArgument(0, "<path>")]
        public string Path { get; init; }
    }

#endregion
}