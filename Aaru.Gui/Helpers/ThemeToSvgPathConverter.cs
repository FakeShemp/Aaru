// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : ThemeToSvgPathConverter.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : GUI helpers.
//
// --[ Description ] ----------------------------------------------------------
//
//     Converter to provide theme-aware SVG asset paths.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General public License for more details.
//
//     You should have received a copy of the GNU General public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

#nullable enable

using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace Aaru.Gui.Helpers;

/// <summary>
///     Converter that returns different SVG asset paths based on the current theme (light or dark).
/// </summary>
/// <inheritdoc />
public class ThemeToSvgPathConverter : IValueConverter
{
    /// <inheritdoc />
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if(value is not ThemeVariant themeVariant || parameter is not string fullPath) return null;

        // Check if it's dark theme
        bool isDark = themeVariant == ThemeVariant.Dark;

        if(!isDark) return fullPath;

        // For dark theme, insert "Dark/" before the filename
        int lastSlashIndex = fullPath.LastIndexOf('/');

        if(lastSlashIndex == -1)
        {
            // No path separator, just filename
            return $"Dark/{fullPath}";
        }

        // Split path and filename, insert Dark/ before filename
        string directory = fullPath[..(lastSlashIndex + 1)];
        string filename  = fullPath[(lastSlashIndex   + 1)..];

        return $"{directory}Dark/{filename}";
    }

    /// <inheritdoc />
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotImplementedException();
}