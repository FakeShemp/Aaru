// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
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
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Aaru.Helpers;

public static partial class AnsiColorParser
{
    // Standard ANSI 16-color palette (indexes 0–15)
    private static readonly Color[] _basicPalette =
    [
        Color.FromArgb(0,   0,   0),   // 0: black
        Color.FromArgb(128, 0,   0),   // 1: red
        Color.FromArgb(0,   128, 0),   // 2: green
        Color.FromArgb(128, 128, 0),   // 3: yellow
        Color.FromArgb(0,   0,   128), // 4: blue
        Color.FromArgb(128, 0,   128), // 5: magenta
        Color.FromArgb(0,   128, 128), // 6: cyan
        Color.FromArgb(192, 192, 192), // 7: white
        Color.FromArgb(128, 128, 128), // 8: bright black (gray)
        Color.FromArgb(255, 0,   0),   // 9: bright red
        Color.FromArgb(0,   255, 0),   // 10: bright green
        Color.FromArgb(255, 255, 0),   // 11: bright yellow
        Color.FromArgb(0,   0,   255), // 12: bright blue
        Color.FromArgb(255, 0,   255), // 13: bright magenta
        Color.FromArgb(0,   255, 255), // 14: bright cyan
        Color.FromArgb(255, 255, 255)  // 15: bright white
    ];

    // Matches ESC [ params m
    private static readonly Regex _sequenceRegex = AnsiRegex();

    public static Color Parse(string input)
    {
        Match m = _sequenceRegex.Match(input);

        if(!m.Success) throw new ArgumentException("No ANSI SGR sequence found.", nameof(input));

        int[] parts = m.Groups["params"]
                       .Value.Split([';'], StringSplitOptions.RemoveEmptyEntries)
                       .Select(int.Parse)
                       .ToArray();

        bool isBold = parts.Contains(1);
        bool isDim  = parts.Contains(2);

        // True-color: ESC[38;2;<r>;<g>;<b>m
        int idx38 = Array.IndexOf(parts, 38);

        switch(idx38)
        {
            case >= 0 when parts.Length > idx38 + 4 && parts[idx38 + 1] == 2:
            {
                int r = parts[idx38 + 2], g = parts[idx38 + 3], b = parts[idx38 + 4];

                return Color.FromArgb(r, g, b);
            }

            // 256-color: ESC[38;5;<n>m
            case >= 0 when parts.Length > idx38 + 2 && parts[idx38 + 1] == 5:
                return ColorFrom256(parts[idx38                    + 2]);
        }

        // 30–37 and 90–97 color codes
        foreach(int code in parts)
        {
            switch(code)
            {
                // 30–37 => palette[0–7]
                case >= 30 and <= 37:
                {
                    int baseIndex = code - 30;

                    // Bold takes precedence
                    if(isBold) return _basicPalette[baseIndex + 8];

                    return isDim ? DimColor(_basicPalette[baseIndex]) : _basicPalette[baseIndex];
                }

                // 90–97 => palette[8–15]
                case >= 90 and <= 97:
                {
                    int brightIndex = code - 90 + 8;

                    return isDim ? DimColor(_basicPalette[brightIndex]) : _basicPalette[brightIndex];
                }
            }
        }

        // Fallback
        return Color.White;
    }

    private static Color ColorFrom256(int index)
    {
        switch(index)
        {
            case < 16:
                return _basicPalette[index];
            case <= 231:
            {
                // 6×6×6 color cube
                int ci = index - 16;
                int r  = ci / 36, g = ci / 6 % 6, b = ci % 6;

                int[] levels = [0, 95, 135, 175, 215, 255];

                return Color.FromArgb(levels[r], levels[g], levels[b]);
            }
            default:
            {
                // Grayscale 232–255
                int gray = 8 + (index - 232) * 10;

                return Color.FromArgb(gray, gray, gray);
            }
        }
    }

    private static Color DimColor(Color c) =>

        // Halve each channel to simulate faint intensity
        Color.FromArgb(c.R / 2, c.G / 2, c.B / 2);

    [GeneratedRegex(@"\e\[(?<params>[0-9;]*)m", RegexOptions.Compiled)]
    private static partial Regex AnsiRegex();
}