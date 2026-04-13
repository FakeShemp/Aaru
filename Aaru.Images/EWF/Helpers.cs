// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helper methods for Expert Witness Format disk images.
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
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Structs;
using Track = Aaru.CommonTypes.Structs.Track;

namespace Aaru.Images;

public sealed partial class Ewf
{
    /// <summary>
    ///     Generates the next segment filename following EWF naming conventions.
    ///     E01→E02→...→E99→EAA→EAB→...→EZZ→FAA→...→ZZZ for v1 EnCase.
    ///     Ex01→Ex02→...→Ex99→ExAA→...→ExZZ for v2.
    ///     s01→s02→...→s99→saa→...→szz for SMART.
    /// </summary>
    static string GetNextSegmentFilename(string currentPath)
    {
        string dir  = Path.GetDirectoryName(currentPath) ?? "";
        string name = Path.GetFileNameWithoutExtension(currentPath);
        string ext  = Path.GetExtension(currentPath);

        if(string.IsNullOrEmpty(ext) || ext.Length < 2) return null;

        // Determine if we're in v2 mode (.Ex01 has 5-char extension)
        bool isV2    = ext.Length == 5; // .Ex01
        bool isLower = char.IsLower(ext[1]);

        if(isV2)
        {
            // .Ex01 → .Ex02 → ... → .Ex99 → .ExAA → ... → .ExZZ
            string numPart = ext.Substring(3); // "01" from ".Ex01"
            char   prefix1 = ext[1];           // 'E'
            char   prefix2 = ext[2];           // 'x'

            if(int.TryParse(numPart, out int num) && num < 99)
                return Path.Combine(dir, name + ext.Substring(0, 3) + (num + 1).ToString("D2"));

            if(int.TryParse(numPart, out _))
            {
                // 99 → AA
                char a = isLower ? 'a' : 'A';

                return Path.Combine(dir, name + $".{prefix1}{prefix2}{a}{a}");
            }

            // AA → AB → ... → AZ → BA → ... → ZZ
            char c1 = ext[3];
            char c2 = ext[4];

            char baseChar = isLower ? 'a' : 'A';
            char maxChar  = isLower ? 'z' : 'Z';

            if(c2 < maxChar) return Path.Combine(dir, name + $".{prefix1}{prefix2}{c1}{(char)(c2 + 1)}");

            if(c1 < maxChar) return Path.Combine(dir, name + $".{prefix1}{prefix2}{(char)(c1 + 1)}{baseChar}");

            return null; // ZZ reached
        }

        // v1: .E01 → .E02 → ... → .E99 → .EAA → ... → .EZZ → .FAA → ... → .ZZZ
        // SMART: .s01 → .s02 → ... → .s99 → .saa → ... → .szz
        char   firstChar = ext[1];           // 'E' or 's'
        string suffix    = ext.Substring(2); // "01" from ".E01"

        if(int.TryParse(suffix, out int segNum) && segNum < 99)
            return Path.Combine(dir, name + $".{firstChar}{segNum + 1:D2}");

        if(int.TryParse(suffix, out _))
        {
            // 99 → AA (or aa for lowercase)
            char a = isLower ? 'a' : 'A';

            return Path.Combine(dir, name + $".{firstChar}{a}{a}");
        }

        // Letter suffixes: EAA → EAB → ... → EAZ → EBA → ... → EZZ → FAA → ... → ZZZ
        char ch1 = ext[1]; // first letter (E, F, ..., Z) or (s for SMART - but SMART only goes to szz)
        char ch2 = ext[2]; // second letter
        char ch3 = ext[3]; // third letter

        char lBase = isLower ? 'a' : 'A';
        char lMax  = isLower ? 'z' : 'Z';

        if(ch3 < lMax) return Path.Combine(dir, name + $".{ch1}{ch2}{(char)(ch3 + 1)}");

        if(ch2 < lMax) return Path.Combine(dir, name + $".{ch1}{(char)(ch2 + 1)}{lBase}");

        if(ch1 < lMax) return Path.Combine(dir, name + $".{(char)(ch1 + 1)}{lBase}{lBase}");

        return null; // ZZZ reached
    }

    /// <summary>
    ///     Parses EWF header text from a raw (already decompressed) byte array.
    ///     header sections use ASCII; header2 sections use UTF-16 LE.
    /// </summary>
    static Dictionary<string, string> ParseHeaderText(byte[] data, bool isHeader2)
    {
        string text;

        if(isHeader2)
        {
            // UTF-16 LE, may have BOM
            if(data.Length >= 2 && data[0] == 0xFF && data[1] == 0xFE)
                text = Encoding.Unicode.GetString(data, 2, data.Length - 2);
            else
                text = Encoding.Unicode.GetString(data);
        }
        else
            text = Encoding.ASCII.GetString(data);

        var result = new Dictionary<string, string>();

        // Header text format is category-based with newline-delimited sections.
        // Most common format:
        // Line 1: category number (e.g., "1" or "3")
        // Line 2: "main" or empty
        // Line 3: tab-separated field identifiers (e.g., "c\tn\ta\te\tt\tav\tov\tm\tu\tp\tr")
        // Line 4: tab-separated field values
        // The remaining lines may repeat the pattern or be empty.

        string[] lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        if(lines.Length < 4) return result;

        // Try to find the field identifiers and values lines
        // Look for the line containing tab-separated single-letter or short identifiers
        for(var i = 0; i < lines.Length - 1; i++)
        {
            string line = lines[i].Trim();

            if(!line.Contains('\t')) continue;

            string[] keys = line.Split('\t');

            // Verify this looks like an identifier line (short keys)
            var isKeyLine = true;

            foreach(string key in keys)
            {
                if(key.Length > 4)
                {
                    isKeyLine = false;

                    break;
                }
            }

            if(!isKeyLine) continue;

            // Next line should contain the values
            if(i + 1 >= lines.Length) break;

            string[] values = lines[i + 1].Trim().Split('\t');

            for(var j = 0; j < keys.Length && j < values.Length; j++)
            {
                string key   = keys[j].Trim();
                string value = values[j].Trim();

                if(!string.IsNullOrEmpty(key) && !result.ContainsKey(key)) result[key] = value;
            }

            break; // Only process the first key/value pair set
        }

        return result;
    }

    /// <summary>
    ///     Decompresses zlib (RFC 1950) compressed data by stripping the 2-byte header
    ///     and using DeflateStream.
    /// </summary>
    static byte[] DecompressZlib(byte[] compressedData, int uncompressedSize)
    {
        if(compressedData == null || compressedData.Length < 3 || uncompressedSize <= 0)
            return new byte[Math.Max(uncompressedSize, 0)];

        var decompressed = new byte[uncompressedSize];

        // Skip 2-byte zlib header (CMF + FLG)
        using var ms        = new MemoryStream(compressedData, 2, compressedData.Length - 2);
        using var deflate   = new DeflateStream(ms, CompressionMode.Decompress);
        var       totalRead = 0;

        while(totalRead < uncompressedSize)
        {
            int read = deflate.Read(decompressed, totalRead, uncompressedSize - totalRead);

            if(read == 0) break;

            totalRead += read;
        }

        return decompressed;
    }

    /// <summary>
    ///     Builds session and track lists from EWF session entries using the libewf reconstruction algorithm.
    /// </summary>
    static void BuildSessionsAndTracks(List<(ulong startSector, uint flags)> sessionEntries, ulong totalSectors,
                                       uint bytesPerSector, out List<Session> sessions, out List<Track> tracks)
    {
        sessions = [];
        tracks   = [];

        if(sessionEntries is not { Count: > 0 })
        {
            // No session entries — create a single session with one data track
            sessions.Add(new Session
            {
                Sequence    = 1,
                StartSector = 0,
                EndSector   = totalSectors - 1,
                StartTrack  = 1,
                EndTrack    = 1
            });

            tracks.Add(new Track
            {
                Sequence          = 1,
                Session           = 1,
                StartSector       = 0,
                EndSector         = totalSectors - 1,
                Type              = TrackType.Data,
                BytesPerSector    = (int)bytesPerSector,
                RawBytesPerSector = (int)bytesPerSector,
                SubchannelType    = TrackSubchannelType.None
            });

            return;
        }

        // Build tracks from session entries
        // Each entry defines the start of a region; its type is determined by the audio flag.
        ushort sessionSequence   = 1;
        uint   trackSequence     = 1;
        ulong  sessionStart      = 0;
        uint   sessionStartTrack = 1;

        for(var i = 0; i < sessionEntries.Count; i++)
        {
            (ulong startSector, uint flags) = sessionEntries[i];
            bool  isAudio   = (flags & SESSION_ENTRY_FLAG_IS_AUDIO) != 0;
            ulong endSector = i + 1 < sessionEntries.Count ? sessionEntries[i + 1].startSector - 1 : totalSectors - 1;

            // If this is a data entry and there was a previous audio entry, we're starting a new session
            if(!isAudio && i > 0 && (sessionEntries[i - 1].flags & SESSION_ENTRY_FLAG_IS_AUDIO) != 0)
            {
                // Close previous session
                sessions.Add(new Session
                {
                    Sequence    = sessionSequence,
                    StartSector = sessionStart,
                    EndSector   = startSector - 1,
                    StartTrack  = sessionStartTrack,
                    EndTrack    = trackSequence - 1
                });

                sessionSequence++;
                sessionStart      = startSector;
                sessionStartTrack = trackSequence;
            }
            else if(!isAudio && i > 0 && (sessionEntries[i - 1].flags & SESSION_ENTRY_FLAG_IS_AUDIO) == 0)
            {
                // Data followed by data — new session boundary
                sessions.Add(new Session
                {
                    Sequence    = sessionSequence,
                    StartSector = sessionStart,
                    EndSector   = startSector - 1,
                    StartTrack  = sessionStartTrack,
                    EndTrack    = trackSequence - 1
                });

                sessionSequence++;
                sessionStart      = startSector;
                sessionStartTrack = trackSequence;
            }

            tracks.Add(new Track
            {
                Sequence          = trackSequence,
                Session           = sessionSequence,
                StartSector       = startSector,
                EndSector         = endSector,
                Type              = isAudio ? TrackType.Audio : TrackType.CdMode1,
                BytesPerSector    = isAudio ? 2352 : (int)bytesPerSector,
                RawBytesPerSector = isAudio ? 2352 : (int)bytesPerSector,
                SubchannelType    = TrackSubchannelType.None
            });

            trackSequence++;
        }

        // Close last session
        sessions.Add(new Session
        {
            Sequence    = sessionSequence,
            StartSector = sessionStart,
            EndSector   = totalSectors - 1,
            StartTrack  = sessionStartTrack,
            EndTrack    = trackSequence - 1
        });
    }
}