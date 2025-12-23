// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Update.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'update' command.
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
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Database;
using Aaru.Localization;
using Aaru.Logging;
using Microsoft.EntityFrameworkCore;
using Spectre.Console.Cli;

namespace Aaru.Commands.Database;

sealed class UpdateCommand : AsyncCommand<UpdateCommand.Settings>
{
    const    string MODULE_NAME = "Update command";
    readonly bool   _mainDbUpdate;

    public override async Task<int> ExecuteAsync(CommandContext    context, Settings settings,
                                                 CancellationToken cancellationToken)
    {
        MainClass.PrintCopyright();

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);

        if(settings.ClearAll)
        {
            try
            {
                File.Delete(Aaru.Settings.Settings.LocalDbPath);

                var ctx = AaruContext.Create(Aaru.Settings.Settings.LocalDbPath);
                await ctx.Database.MigrateAsync(cancellationToken);
                await ctx.SaveChangesAsync(cancellationToken);
            }
            catch(Exception) when(!Debugger.IsAttached)
            {
                AaruLogging.Error(UI.Could_not_remove_local_database);

                return (int)ErrorNumber.CannotRemoveDatabase;
            }
        }

        if(settings.Clear || settings.ClearAll)
        {
            try
            {
                File.Delete(Aaru.Settings.Settings.MainDbPath);
            }
            catch(Exception) when(!Debugger.IsAttached)
            {
                AaruLogging.Error(UI.Could_not_remove_main_database);

                return (int)ErrorNumber.CannotRemoveDatabase;
            }
        }

        await DoUpdateAsync(settings.Clear || settings.ClearAll);

        return (int)ErrorNumber.NoError;
    }

    internal static async Task DoUpdateAsync(bool create)
    {
        await Remote.UpdateMainDatabaseAsync(create);
        Statistics.AddCommand("update");
    }

#region Nested type: Settings

    public class Settings : DatabaseFamily
    {
        [CommandOption("--clear")]
        [LocalizedDescription(nameof(UI.Clear_existing_main_database))]
        [DefaultValue(false)]
        public bool Clear { get; init; }
        [CommandOption("--clear-all")]
        [LocalizedDescription(nameof(UI.Clear_existing_main_and_local_database))]
        [DefaultValue(false)]
        public bool ClearAll { get; init; }
    }

#endregion
}