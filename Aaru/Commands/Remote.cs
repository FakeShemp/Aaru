// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Remote.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'remote' command.
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

// TODO: Fix errors returned

using System;
using System.ComponentModel;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Logging;
using Spectre.Console;
using Spectre.Console.Cli;
using Remote = Aaru.Devices.Remote.Remote;

namespace Aaru.Commands;

sealed class RemoteCommand : Command<RemoteCommand.Settings>
{
    const string MODULE_NAME = "Remote command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("remote");

        AaruLogging.Debug(MODULE_NAME, "debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "host={0}",    Markup.Escape(settings.Host ?? ""));
        AaruLogging.Debug(MODULE_NAME, "verbose={0}", settings.Verbose);

        try
        {
            var remote = new Remote(new Uri(settings.Host));

            Statistics.AddRemote(remote.ServerApplication,
                                 remote.ServerVersion,
                                 remote.ServerOperatingSystem,
                                 remote.ServerOperatingSystemVersion,
                                 remote.ServerArchitecture);

            Table table = new()
            {
                Title = new TableTitle("Server information")
            };

            table.AddColumn("");
            table.AddColumn("");
            table.Columns[0].RightAligned();

            table.AddRow("Server application", $"{remote.ServerApplication} {remote.ServerVersion}");

            table.AddRow("Server operating system",
                         $"{remote.ServerOperatingSystem} {remote.ServerOperatingSystemVersion} ({
                             remote.ServerArchitecture})");

            table.AddRow("Server maximum protocol", $"{remote.ServerProtocolVersion}");

            AnsiConsole.Write(table);
            remote.Disconnect();
        }
        catch(Exception)
        {
            AaruLogging.Error("Error connecting to host.");

            return (int)ErrorNumber.CannotOpenDevice;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : BaseSettings
    {
        [CommandArgument(0, "<host>")]
        [Description("Aaru host")]
        public string Host { get; init; }
    }

#endregion
}