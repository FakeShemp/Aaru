// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : MetadataSchema.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'metadata-schema' command.
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

using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Threading;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Spectre.Console.Cli;
using IOFile = System.IO.File;

namespace Aaru.Commands;

sealed class MetadataSchemaCommand : Command<MetadataSchemaCommand.Settings>
{
    const string MODULE_NAME = "Metadata Schema command";

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("metadata-schema");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",   settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);
        AaruLogging.Debug(MODULE_NAME, "--output={0}",  settings.Output);

        if(string.IsNullOrWhiteSpace(settings.Output))
        {
            AaruLogging.Error(UI.Output_is_required);

            return (int)ErrorNumber.InvalidArgument;
        }

        AaruLogging.WriteLine(UI.Generating_JSON_schema_for_metadata);

        // Create options with the MetadataJsonContext as TypeInfoResolver
        var options = new JsonSerializerOptions
        {
            WriteIndented    = true,
            TypeInfoResolver = new MetadataJsonContext()
        };

        JsonNode schema = options.GetJsonSchemaAsNode(typeof(MetadataJson));

        string jsonString = schema.ToJsonString(options);

        IOFile.WriteAllText(settings.Output, jsonString);

        AaruLogging.WriteLine(UI.Schema_successfully_written_to_0, settings.Output);

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : BaseSettings
    {
        [CommandArgument(0, "<output>")]
        [LocalizedDescription(nameof(UI.Output_file_for_the_JSON_schema))]
        public string Output { get; set; }
    }

#endregion
}