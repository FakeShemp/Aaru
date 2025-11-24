// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Constants.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains constants for CloneCD disc images.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System.Text.RegularExpressions;

namespace Aaru.Images;

public sealed partial class CloneCd
{
    const string CCD_IDENTIFIER     = @"^\s*\[CloneCD\]";
    const string DISC_IDENTIFIER    = @"^\s*\[Disc\]";
    const string SESSION_IDENTIFIER = @"^\s*\[Session\s*(?<number>\d+)\]";
    const string ENTRY_IDENTIFIER   = @"^\s*\[Entry\s*(?<number>\d+)\]";
    const string TRACK_IDENTIFIER   = @"^\s*\[TRACK\s*(?<number>\d+)\]";
    const string CDTEXT_IDENTIFIER  = @"^\s*\[CDText\]";
    const string CCD_VERSION        = @"^\s*Version\s*=\s*(?<value>\d+)";
    const string DISC_ENTRIES       = @"^\s*TocEntries\s*=\s*(?<value>\d+)";
    const string DISC_SESSIONS      = @"^\s*Sessions\s*=\s*(?<value>\d+)";
    const string DISC_SCRAMBLED     = @"^\s*DataTracksScrambled\s*=\s*(?<value>\d+)";
    const string CDTEXT_LENGTH      = @"^\s*CDTextLength\s*=\s*(?<value>\d+)";
    const string DISC_CATALOG       = @"^\s*CATALOG\s*=\s*(?<value>[\x21-\x7F]{13})";
    const string SESSION_PREGAP     = @"^\s*PreGapMode\s*=\s*(?<value>\d+)";
    const string SESSION_SUBCHANNEL = @"^\s*PreGapSubC\s*=\s*(?<value>\d+)";
    const string ENTRY_SESSION      = @"^\s*Session\s*=\s*(?<value>\d+)";
    const string ENTRY_POINT        = @"^\s*Point\s*=\s*(?<value>[\w+]+)";
    const string ENTRY_ADR          = @"^\s*ADR\s*=\s*(?<value>\w+)";
    const string ENTRY_CONTROL      = @"^\s*Control\s*=\s*(?<value>\w+)";
    const string ENTRY_TRACKNO      = @"^\s*TrackNo\s*=\s*(?<value>\d+)";
    const string ENTRY_AMIN         = @"^\s*AMin\s*=\s*(?<value>\d+)";
    const string ENTRY_ASEC         = @"^\s*ASec\s*=\s*(?<value>\d+)";
    const string ENTRY_AFRAME       = @"^\s*AFrame\s*=\s*(?<value>\d+)";
    const string ENTRY_ALBA         = @"^\s*ALBA\s*=\s*(?<value>-?\d+)";
    const string ENTRY_ZERO         = @"^\s*Zero\s*=\s*(?<value>\d+)";
    const string ENTRY_PMIN         = @"^\s*PMin\s*=\s*(?<value>\d+)";
    const string ENTRY_PSEC         = @"^\s*PSec\s*=\s*(?<value>\d+)";
    const string ENTRY_PFRAME       = @"^\s*PFrame\s*=\s*(?<value>\d+)";
    const string ENTRY_PLBA         = @"^\s*PLBA\s*=\s*(?<value>\d+)";
    const string CDTEXT_ENTRIES     = @"^\s*Entries\s*=\s*(?<value>\d+)";
    const string CDTEXT_ENTRY       = @"^\s*Entry\s*(?<number>\d+)\s*=\s*(?<value>([0-9a-fA-F]+\s*)+)";
    const string TRACK_MODE         = @"^\s*MODE\s*=\s*(?<value>\d+)";
    const string TRACK_INDEX        = @"^\s*INDEX\s*(?<index>\d+)\s*=\s*(?<lba>\d+)";

    [GeneratedRegex(CCD_IDENTIFIER)]
    private static partial Regex CcdIdentifierRegex();

    [GeneratedRegex(DISC_IDENTIFIER)]
    private static partial Regex DiscIdentifierRegex();

    [GeneratedRegex(SESSION_IDENTIFIER)]
    private static partial Regex SessionIdentifierRegex();

    [GeneratedRegex(ENTRY_IDENTIFIER)]
    private static partial Regex EntryIdentifierRegex();

    [GeneratedRegex(TRACK_IDENTIFIER)]
    private static partial Regex TrackIdentifierRegex();

    [GeneratedRegex(CDTEXT_IDENTIFIER)]
    private static partial Regex CdtextIdentifierRegex();

    [GeneratedRegex(CCD_VERSION)]
    private static partial Regex CcdVersionRegex();

    [GeneratedRegex(DISC_ENTRIES)]
    private static partial Regex DiscEntriesRegex();

    [GeneratedRegex(DISC_SESSIONS)]
    private static partial Regex DiscSessionsRegex();

    [GeneratedRegex(DISC_SCRAMBLED)]
    private static partial Regex DiscScrambledRegex();

    [GeneratedRegex(CDTEXT_LENGTH)]
    private static partial Regex CdtextLengthRegex();

    [GeneratedRegex(DISC_CATALOG)]
    private static partial Regex DiscCatalogRegex();

    [GeneratedRegex(SESSION_PREGAP)]
    private static partial Regex SesionPregapRegex();

    [GeneratedRegex(SESSION_SUBCHANNEL)]
    private static partial Regex SessionSubchannelRegex();

    [GeneratedRegex(ENTRY_SESSION)]
    private static partial Regex EntrySessionRegex();

    [GeneratedRegex(ENTRY_POINT)]
    private static partial Regex EntryPointRegex();

    [GeneratedRegex(ENTRY_ADR)]
    private static partial Regex EntryAdrRegex();

    [GeneratedRegex(ENTRY_CONTROL)]
    private static partial Regex EntryControlRegex();

    [GeneratedRegex(ENTRY_TRACKNO)]
    private static partial Regex EntryTracknoRegex();

    [GeneratedRegex(ENTRY_AMIN)]
    private static partial Regex EntryAminRegex();

    [GeneratedRegex(ENTRY_ASEC)]
    private static partial Regex EntryAsecRegex();

    [GeneratedRegex(ENTRY_AFRAME)]
    private static partial Regex EntryAframeRegex();

    [GeneratedRegex(ENTRY_ALBA)]
    private static partial Regex EntryAlbaRegex();

    [GeneratedRegex(ENTRY_ZERO)]
    private static partial Regex EntryZeroRegex();

    [GeneratedRegex(ENTRY_PMIN)]
    private static partial Regex EntryPminRegex();

    [GeneratedRegex(ENTRY_PSEC)]
    private static partial Regex EntryPsecRegex();

    [GeneratedRegex(ENTRY_PFRAME)]
    private static partial Regex EntryPframeRegex();

    [GeneratedRegex(ENTRY_PLBA)]
    private static partial Regex EntryPlbaRegex();

    [GeneratedRegex(CDTEXT_ENTRIES)]
    private static partial Regex CdtextEntriesRegex();

    [GeneratedRegex(CDTEXT_ENTRY)]
    private static partial Regex CdtextEntryRegex();

    [GeneratedRegex(TRACK_MODE)]
    private static partial Regex TrackModeRegex();

    [GeneratedRegex(TRACK_INDEX)]
    private static partial Regex TrackIndexRegex();
}