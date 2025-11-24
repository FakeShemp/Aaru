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
//     Contains constants for cdrdao cuesheets (toc/bin).
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

public sealed partial class Cdrdao
{
    /// <summary>Audio track, 2352 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_AUDIO = "AUDIO";
    /// <summary>Mode 1 track, cooked, 2048 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE1 = "MODE1";
    /// <summary>Mode 1 track, raw, 2352 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE1_RAW = "MODE1_RAW";
    /// <summary>Mode 2 mixed formless, cooked, 2336 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE2 = "MODE2";
    /// <summary>Mode 2 form 1 track, cooked, 2048 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE2_FORM1 = "MODE2_FORM1";
    /// <summary>Mode 2 form 2 track, cooked, 2324 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE2_FORM2 = "MODE2_FORM2";
    /// <summary>Mode 2 mixed forms track, cooked, 2336 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE2_MIX = "MODE2_FORM_MIX";
    /// <summary>Mode 2 track, raw, 2352 bytes/sector</summary>
    const string CDRDAO_TRACK_TYPE_MODE2_RAW = "MODE2_RAW";

    const string REGEX_COMMENT  = @"^\s*\/{2}(?<comment>.+)$";
    const string REGEX_COPY     = @"^\s*(?<no>NO)?\s*COPY";
    const string REGEX_DISCTYPE = @"^\s*(?<type>(CD_DA|CD_ROM_XA|CD_ROM|CD_I))";
    const string REGEX_EMPHASIS = @"^\s*(?<no>NO)?\s*PRE_EMPHASIS";
    const string REGEX_FILE_AUDIO =
        """^\s*(AUDIO)?FILE\s*"(?<filename>.+)"\s*(#(?<base_offset>\d+))?\s*((?<start>[\d]+:[\d]+:[\d]+)|(?<start_num>\d+))\s*(?<length>[\d]+:[\d]+:[\d]+)?""";
    const string REGEX_FILE_DATA =
        """^\s*DATAFILE\s*"(?<filename>.+)"\s*(#(?<base_offset>\d+))?\s*(?<length>[\d]+:[\d]+:[\d]+)?""";
    const string REGEX_INDEX = @"^\s*INDEX\s*(?<address>\d+:\d+:\d+)";
    const string REGEX_ISRC = """
                              ^\s*ISRC\s*"(?<isrc>[A-Z0-9]{5,5}[0-9]{7,7})"
                              """;
    const string REGEX_MCN = """
                             ^\s*CATALOG\s*"(?<catalog>[\x21-\x7F]{13,13})"
                             """;
    const string REGEX_PREGAP = @"^\s*START\s*(?<address>\d+:\d+:\d+)?";
    const string REGEX_STEREO = @"^\s*(?<num>(TWO|FOUR))_CHANNEL_AUDIO";
    const string REGEX_TRACK =
        @"^\s*TRACK\s*(?<type>(AUDIO|MODE1_RAW|MODE1|MODE2_FORM1|MODE2_FORM2|MODE2_FORM_MIX|MODE2_RAW|MODE2))\s*(?<subchan>(RW_RAW|RW))?";
    const string REGEX_ZERO_AUDIO  = @"^\s*SILENCE\s*(?<length>\d+:\d+:\d+)";
    const string REGEX_ZERO_DATA   = @"^\s*ZERO\s*(?<length>\d+:\d+:\d+)";
    const string REGEX_ZERO_PREGAP = @"^\s*PREGAP\s*(?<length>\d+:\d+:\d+)";

    // CD-Text
    const string REGEX_ARRANGER = """
                                  ^\s*ARRANGER\s*"(?<arranger>.+)"
                                  """;
    const string REGEX_COMPOSER = """
                                  ^\s*COMPOSER\s*"(?<composer>.+)"
                                  """;
    const string REGEX_DISC_ID = """
                                 ^\s*DISC_ID\s*"(?<discid>.+)"
                                 """;
    const string REGEX_MESSAGE = """
                                 ^\s*MESSAGE\s*"(?<message>.+)"
                                 """;
    const string REGEX_PERFORMER = """
                                   ^\s*PERFORMER\s*"(?<performer>.+)"
                                   """;
    const string REGEX_SONGWRITER = """
                                    ^\s*SONGWRITER\s*"(?<songwriter>.+)"
                                    """;
    const string REGEX_TITLE = """
                               ^\s*TITLE\s*"(?<title>.+)"
                               """;
    const string REGEX_UPC = """
                             ^\s*UPC_EAN\s*"(?<catalog>[\d]{13,13})"
                             """;

    // Unused
    const string REGEX_CD_TEXT          = @"^\s*CD_TEXT\s*\{";
    const string REGEX_CLOSURE          = @"^\s*\}";
    const string REGEX_LANGUAGE         = @"^\s*LANGUAGE\s*(?<code>\d+)\s*\{";
    const string REGEX_LANGUAGE_MAP     = @"^\s*LANGUAGE_MAP\s*\{";
    const string REGEX_LANGUAGE_MAPPING = @"^\s*(?<code>\d+)\s?\:\s?(?<language>\d+|\w+)";

    [GeneratedRegex(REGEX_COMMENT)]
    private static partial Regex CommentRegex();

    [GeneratedRegex(REGEX_DISCTYPE)]
    private static partial Regex DiscTypeRegex();

    [GeneratedRegex(REGEX_MCN)]
    private static partial Regex McnRegex();

    [GeneratedRegex(REGEX_TRACK)]
    private static partial Regex TrackRegex();

    [GeneratedRegex(REGEX_COPY)]
    private static partial Regex CopyRegex();

    [GeneratedRegex(REGEX_EMPHASIS)]
    private static partial Regex EmphasisRegex();

    [GeneratedRegex(REGEX_STEREO)]
    private static partial Regex StereoRegex();

    [GeneratedRegex(REGEX_ISRC)]
    private static partial Regex IsrcRegex();

    [GeneratedRegex(REGEX_INDEX)]
    private static partial Regex IndexRegex();

    [GeneratedRegex(REGEX_PREGAP)]
    private static partial Regex PregapRegex();

    [GeneratedRegex(REGEX_ZERO_PREGAP)]
    private static partial Regex ZeroPregapRegex();

    [GeneratedRegex(REGEX_ZERO_DATA)]
    private static partial Regex ZeroDataRegex();

    [GeneratedRegex(REGEX_ZERO_AUDIO)]
    private static partial Regex ZeroAudioRegex();

    [GeneratedRegex(REGEX_FILE_AUDIO)]
    private static partial Regex FileAudioRegex();

    [GeneratedRegex(REGEX_FILE_DATA)]
    private static partial Regex FileDataRegex();

    [GeneratedRegex(REGEX_TITLE)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(REGEX_PERFORMER)]
    private static partial Regex PerformerRegex();

    [GeneratedRegex(REGEX_SONGWRITER)]
    private static partial Regex SongwriterRegex();

    [GeneratedRegex(REGEX_COMPOSER)]
    private static partial Regex ComposerRegex();

    [GeneratedRegex(REGEX_ARRANGER)]
    private static partial Regex ArrangerRegex();

    [GeneratedRegex(REGEX_MESSAGE)]
    private static partial Regex MessageRegex();

    [GeneratedRegex(REGEX_DISC_ID)]
    private static partial Regex DiscIdRegex();

    [GeneratedRegex(REGEX_UPC)]
    private static partial Regex UpcRegex();

    [GeneratedRegex(REGEX_CD_TEXT)]
    private static partial Regex CdTextRegex();

    [GeneratedRegex(REGEX_LANGUAGE)]
    private static partial Regex LanguageRegex();

    [GeneratedRegex(REGEX_CLOSURE)]
    private static partial Regex ClosureRegex();

    [GeneratedRegex(REGEX_LANGUAGE_MAP)]
    private static partial Regex LanguageMapRegex();

    [GeneratedRegex(REGEX_LANGUAGE_MAPPING)]
    private static partial Regex LanguageMappingRegex();
}