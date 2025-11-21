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
using System.Threading;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Sentry;
using Spectre.Console;
using Spectre.Console.Cli;
using Remote = Aaru.Devices.Remote.Remote;

namespace Aaru.Commands;

sealed class RemoteCommand : Command<RemoteCommand.Settings>
{
    const string MODULE_NAME = "Remote command";

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
                Title = new TableTitle(UI.Title_Server_information)
            };

            AaruLogging.Information(UI.Title_Server_information);

            table.AddColumn("");
            table.AddColumn("");
            table.Columns[0].RightAligned();
            table.Border(TableBorder.Rounded);
            table.BorderColor(Color.Yellow);
            table.HideHeaders();

            table.AddRow(UI.Server_application, $"[green]{remote.ServerApplication}[/] [red]{remote.ServerVersion}[/]");

            table.AddRow(UI.Server_operating_system,
                         $"[fuchsia]{remote.ServerOperatingSystem}[/] [lime]{remote.ServerOperatingSystemVersion}[/] [slateblue1]([gold3]{
                             remote.ServerArchitecture}[/])[/]");

            table.AddRow(UI.Server_maximum_protocol, $"[teal]{remote.ServerProtocolVersion}[/]");

            AaruLogging.Information($"{UI.Server_application}: {remote.ServerApplication} {remote.ServerVersion}");
            AaruLogging.Information($"{UI.Server_operating_system}: {remote.ServerOperatingSystem} {remote.ServerOperatingSystemVersion} ({remote.ServerArchitecture})");
            AaruLogging.Information($"{UI.Server_maximum_protocol}: {remote.ServerProtocolVersion}");

            AnsiConsole.Write(table);
            remote.Disconnect();
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);
            AaruLogging.Error(UI.Error_connecting_to_host);

            return (int)ErrorNumber.CannotOpenDevice;
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : BaseSettings
    {
        [CommandArgument(0, "<host>")]
        [LocalizedDescription(nameof(UI.aaruremote_host))]
        public string Host { get; init; }
    }

#endregion
}