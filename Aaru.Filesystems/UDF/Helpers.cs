// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Universal Disk Format plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;
using Aaru.Helpers;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Universal Disk Format filesystem</summary>
[SuppressMessage("ReSharper", "UnusedMember.Local")]
public sealed partial class UDF
{
    /// <summary>
    ///     Converts an ECMA-167 Timestamp structure to a .NET DateTime.
    ///     Returns DateTime.MinValue if the timestamp contains invalid values.
    /// </summary>
    /// <param name="timestamp">The ECMA-167 timestamp to convert</param>
    /// <returns>The equivalent DateTime value, or DateTime.MinValue if invalid</returns>
    static DateTime EcmaToDateTime(Timestamp timestamp)
    {
        try
        {
            // Validate basic timestamp fields before conversion
            if(timestamp is { year: 0, month: 0, day: 0 }) return DateTime.MinValue;

            if(timestamp.month is < 1 or > 12 || timestamp.day is < 1 or > 31) return DateTime.MinValue;

            return DateHandlers.EcmaToDateTime(timestamp.typeAndZone,
                                               timestamp.year,
                                               timestamp.month,
                                               timestamp.day,
                                               timestamp.hour,
                                               timestamp.minute,
                                               timestamp.second,
                                               timestamp.centiseconds,
                                               timestamp.hundredsMicroseconds,
                                               timestamp.microseconds);
        }
        catch(ArgumentOutOfRangeException)
        {
            // Invalid timestamp values
            return DateTime.MinValue;
        }
    }
}