// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Helpers.cs
// Author(s)      : Rebecca Wallander <sakcheen+github@gmail.com>
//
// Component      : Disk image plugins.
//
// --[ Description ] ----------------------------------------------------------
//
//     Contains helpers for HxC Stream flux images.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Aaru.Checksums;
using Aaru.CommonTypes.Structs;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class HxCStream
{
    /// <summary>
    ///     Decodes variable-length encoded pulses from HxCStream format.
    ///     Values &lt; 0x80: single byte value
    ///     0x80-0xBF: 2-byte value (6 bits + 8 bits)
    ///     0xC0-0xDF: 3-byte value (5 bits + 8 bits + 8 bits)
    ///     0xE0-0xEF: 4-byte value (4 bits + 24 bits)
    /// </summary>
    /// <param name="unpackedData">The unpacked data buffer</param>
    /// <param name="unpackedDataSize">Size of unpacked data</param>
    /// <param name="numberOfPulses">Number of pulses to decode (updated with actual count)</param>
    /// <returns>Array of decoded pulse values</returns>
    static uint[] DecodeVariableLengthPulses(byte[] unpackedData, uint unpackedDataSize, ref uint numberOfPulses)
    {
        if(numberOfPulses == 0) return [];

        var pulses = new List<uint>();
        uint k = 0;
        uint l = 0;

        while(l < numberOfPulses && k < unpackedDataSize)
        {
            byte c = unpackedData[k++];
            uint value = 0;

            if((c & 0x80) == 0)
            {
                // Single byte value
                if(c != 0) pulses.Add(c);
            }
            else if((c & 0xC0) == 0x80)
            {
                // 2-byte value
                if(k >= unpackedDataSize) break;
                value = (uint)((c & 0x3F) << 8) | unpackedData[k++];
                pulses.Add(value);
            }
            else if((c & 0xE0) == 0xC0)
            {
                // 3-byte value
                if(k + 1 >= unpackedDataSize) break;
                value = (uint)((c & 0x1F) << 16) | ((uint)unpackedData[k++] << 8) | unpackedData[k++];
                pulses.Add(value);
            }
            else if((c & 0xF0) == 0xE0)
            {
                // 4-byte value
                if(k + 2 >= unpackedDataSize) break;
                value = (uint)((c & 0x0F) << 24) | ((uint)unpackedData[k++] << 16) |
                        ((uint)unpackedData[k++] << 8) | unpackedData[k++];
                pulses.Add(value);
            }

            l++;
        }

        // Add dummy pulse (300 ticks)
        pulses.Add(300);
        numberOfPulses = (uint)pulses.Count;

        return pulses.ToArray();
    }

    /// <summary>
    ///     Converts a uint32 pulse value to Aaru's flux representation format.
    ///     Format: byte array where 255 = overflow, remainder = value
    /// </summary>
    /// <param name="ticks">The pulse value in ticks</param>
    /// <returns>Flux representation as byte array</returns>
    static byte[] UInt32ToFluxRepresentation(uint ticks)
    {
        uint over = ticks / 255;

        if(over == 0) return [(byte)ticks];

        var expanded = new byte[over + 1];
        Array.Fill(expanded, (byte)255, 0, (int)over);
        expanded[^1] = (byte)(ticks % 255);

        return expanded;
    }

    /// <summary>
    ///     Decodes a raw 16-bit IO stream value into a readable IoStreamState structure.
    ///     This provides named properties for known signals (index, write protect) and
    ///     all 16 IO channels, while preserving the raw value for future extensions.
    /// </summary>
    /// <param name="rawValue">The raw 16-bit IO stream value</param>
    /// <returns>Decoded IO stream state with named properties</returns>
    public static IoStreamState DecodeIoStreamValue(ushort rawValue) => IoStreamState.FromRawValue(rawValue);

    /// <summary>
    ///     Parses HxCStream metadata string and populates ImageInfo fields.
    ///     Metadata format: key-value pairs separated by newlines, values may be quoted strings.
    /// </summary>
    /// <param name="metadata">The metadata string to parse</param>
    /// <param name="imageInfo">ImageInfo structure to populate</param>
    static void ParseMetadata(string metadata, ImageInfo imageInfo)
    {
        if(string.IsNullOrEmpty(metadata)) return;

        var lines = metadata.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach(string line in lines)
        {
            string trimmed = line.Trim();

            if(string.IsNullOrEmpty(trimmed)) continue;

            // Find the first space to separate key from value
            int spaceIndex = trimmed.IndexOf(' ');

            if(spaceIndex <= 0) continue;

            string key = trimmed[..spaceIndex].Trim();
            string value = trimmed[(spaceIndex + 1)..].Trim();

            // Remove quotes if present
            if(value.Length >= 2 && value[0] == '"' && value[^1] == '"')
                value = value[1..^1];

            switch(key)
            {
                case "format_version":
                    if(string.IsNullOrEmpty(imageInfo.Version))
                        imageInfo.Version = value;

                    break;
                case "software_version":
                    // Extract version number (e.g., "v1.3.2.1 9 September 2021" -> "v1.3.2.1")
                    int versionEnd = value.IndexOf(' ', StringComparison.Ordinal);

                    if(versionEnd > 0)
                        imageInfo.ApplicationVersion = value[..versionEnd];
                    else
                        imageInfo.ApplicationVersion = value;

                    // Set application name if not already set
                    if(string.IsNullOrEmpty(imageInfo.Application))
                        imageInfo.Application = "HxC Floppy Emulator";

                    break;
                case "dump_name":
                    if(string.IsNullOrEmpty(imageInfo.MediaTitle))
                        imageInfo.MediaTitle = value;

                    break;
                case "dump_comment":
                    // Combine dump_comment and dump_comment2 if both exist
                    if(string.IsNullOrEmpty(imageInfo.Comments))
                        imageInfo.Comments = value;
                    else
                        imageInfo.Comments = $"{imageInfo.Comments}\n{value}";

                    break;
                case "dump_comment2":
                    if(!string.IsNullOrEmpty(value))
                    {
                        if(string.IsNullOrEmpty(imageInfo.Comments))
                            imageInfo.Comments = value;
                        else
                            imageInfo.Comments = $"{imageInfo.Comments}\n{value}";
                    }

                    break;
                case "operator":
                    if(string.IsNullOrEmpty(imageInfo.Creator))
                        imageInfo.Creator = value;

                    break;
                case "current_time":
                    // Parse date/time: "2025-11-13 16:42:29"
                    if(DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateTime))
                    {
                        if(imageInfo.CreationTime == default)
                            imageInfo.CreationTime = dateTime;

                        imageInfo.LastModificationTime = dateTime;
                    }

                    break;
                case "floppy_drive":
                    // Format: "1 \"5.25-inch Floppy drive\""
                    // Extract the quoted description
                    int quoteStart = value.IndexOf('"');

                    if(quoteStart >= 0)
                    {
                        int quoteEnd = value.LastIndexOf('"');

                        if(quoteEnd > quoteStart)
                        {
                            string driveDesc = value[(quoteStart + 1)..quoteEnd];

                            if(string.IsNullOrEmpty(imageInfo.DriveModel))
                                imageInfo.DriveModel = driveDesc;
                        }
                    }

                    break;
                case "drive_reference":
                    if(!string.IsNullOrEmpty(value) && string.IsNullOrEmpty(imageInfo.DriveSerialNumber))
                        imageInfo.DriveSerialNumber = value;

                    break;
            }
        }
    }

    static bool VerifyChunkCrc32(byte[] chunkData, uint storedCrc)
    {
        var crc32Context = new Crc32Context(0xEDB88320, 0x00000000);
        crc32Context.Update(chunkData, (uint)chunkData.Length);
        byte[] crc32Bytes = crc32Context.Final();
        // Final() returns big-endian, but stored CRC is little-endian
        Array.Reverse(crc32Bytes);
        uint crc32 = BitConverter.ToUInt32(crc32Bytes, 0);

        crc32 ^= 0xFFFFFFFF;
    
        return crc32 == storedCrc;
    }
}
