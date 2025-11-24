// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : List.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'list' command.
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

using System.ComponentModel;
using System.Linq;
using System.Threading;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Devices;
using Aaru.Localization;
using Aaru.Logging;
using JetBrains.Annotations;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Device;

sealed class ListDevicesCommand : Command<ListDevicesCommand.Settings>
{
    const string MODULE_NAME = "List-Devices command";

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)

    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("list-devices");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);

        AaruLogging.Information(UI.List_devices_command);

        DeviceInfo[] devices = Devices.Device.ListDevices(out bool isRemote,
                                                          out string serverApplication,
                                                          out string serverVersion,
                                                          out string serverOperatingSystem,
                                                          out string serverOperatingSystemVersion,
                                                          out string serverArchitecture,
                                                          settings.AaruRemoteHost);

        if(isRemote)
        {
            Statistics.AddRemote(serverApplication,
                                 serverVersion,
                                 serverOperatingSystem,
                                 serverOperatingSystemVersion,
                                 serverArchitecture);
        }

        if(devices == null || devices.Length == 0)
        {
            AaruLogging.WriteLine(UI.No_known_devices_attached);
            AaruLogging.Information(UI.No_known_devices_attached);
        }
        else
        {
            Table table = new();
            table.AddColumn(UI.Path);
            table.AddColumn(UI.Title_Vendor);
            table.AddColumn(UI.Title_Model);
            table.AddColumn(UI.Serial);
            table.AddColumn(UI.Title_Bus);
            table.AddColumn(UI.Supported_Question);
            table.Border(TableBorder.Rounded);
            table.BorderColor(Color.Yellow);

            foreach(DeviceInfo dev in devices.OrderBy(static d => d.Path))
            {
                table.AddRow($"[italic][olive]{Markup.Escape(dev.Path        ?? "")}[/][/]",
                             $"[italic][slateblue1]{Markup.Escape(dev.Vendor ?? "")}[/][/]",
                             $"[italic][purple]{Markup.Escape(dev.Model      ?? "")}[/][/]",
                             $"[italic][aqua]{Markup.Escape(dev.Serial       ?? "")}[/][/]",
                             $"[italic][rosybrown]{Markup.Escape(dev.Bus     ?? "")}[/][/]",
                             $"[italic]{(dev.Supported ? "[green]✓[/]" : "[red]✗[/]")}[/]");

                AaruLogging
                   .Information("Path: {Path}, Vendor: {Vendor}, Model: {Model}, Serial: {Serial}, Bus: {Bus}, Supported: {Supported}",
                                dev.Path,
                                dev.Vendor,
                                dev.Model,
                                dev.Serial,
                                dev.Bus,
                                dev.Supported);
            }

            AnsiConsole.Write(table);
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : DeviceFamily
    {
        [CanBeNull]
        [LocalizedDescription(nameof(UI.aaruremote_host))]
        [CommandArgument(0, "[aaru-remote-host]")]
        [DefaultValue(null)]
        public string AaruRemoteHost { get; init; }
    }

#endregion
}