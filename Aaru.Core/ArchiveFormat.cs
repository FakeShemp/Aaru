// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ArchiveFormat.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Core algorithms.
//
// --[ Description ] ----------------------------------------------------------
//
//     Detects archive format.
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
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Sentry;

namespace Aaru.Core;

/// <summary>Core archive format operations</summary>
public static class ArchiveFormat
{
    const string MODULE_NAME = "Format detection";

    /// <summary>Detects the archive plugin that recognizes the data inside a filter</summary>
    /// <param name="archiveFilter">Filter</param>
    /// <returns>Detected archive plugin</returns>
    public static IArchive Detect(IFilter archiveFilter)
    {
        try
        {
            PluginRegister plugins = PluginRegister.Singleton;

            IArchive format = null;

            // Check all but RAW plugin
            foreach(IArchive plugin in plugins.Archives.Values)
            {
                if(plugin is null) continue;

                try
                {
                    AaruLogging.Debug(MODULE_NAME, Localization.Core.Trying_plugin_0, plugin.Name);

                    if(!plugin.Identify(archiveFilter)) continue;

                    format = plugin;

                    break;
                }
                catch(Exception ex)
                {
                    SentrySdk.CaptureException(ex);
                }
            }

            // Not recognized
            return format;
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            return null;
        }
    }
}