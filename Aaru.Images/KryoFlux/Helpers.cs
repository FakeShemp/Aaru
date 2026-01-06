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
//     Contains helpers for KryoFlux STREAM images.
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
using System.Diagnostics.CodeAnalysis;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;

namespace Aaru.Images;

[SuppressMessage("ReSharper", "UnusedType.Global")]
public sealed partial class KryoFlux
{
    /// <summary>
    ///     Decodes KryoFlux stream format into cell values (flux transition timings).
    ///     Per KryoFlux spec: The stream uses encoding markers to efficiently encode flux transition timings.
    /// </summary>
    /// <param name="streamData">The raw stream data to decode</param>
    /// <param name="streamLength">Length of the stream data</param>
    /// <param name="cellValues">Output list of decoded cell values (flux transition timings in clock cycles)</param>
    /// <returns>Error number indicating success or failure</returns>
    static ErrorNumber DecodeKryoFluxStream(byte[] streamData, int streamLength, out List<uint> cellValues)
    {
        cellValues = new List<uint>();

        if(streamData == null || streamLength <= 0) return ErrorNumber.InvalidArgument;

        int streamPosition = 0;

        while(streamPosition < streamLength)
        {
            byte encodingMarker = streamData[streamPosition++];

            if(encodingMarker <= 0x07)
            {
                // Value: 16-bit value where upper 8 bits are the marker, lower 8 bits are next byte
                if(streamPosition >= streamLength) return ErrorNumber.InvalidArgument;

                byte lowerByte = streamData[streamPosition++];
                uint cellValue = (uint)((encodingMarker << 8) | lowerByte);

                cellValues.Add(cellValue);
            }
            else if(encodingMarker == (byte)BlockIds.Nop1)
            {
                // Nop1: Skip 1 byte
                streamPosition++;
            }
            else if(encodingMarker == (byte)BlockIds.Nop2)
            {
                // Nop2: Skip 2 bytes
                streamPosition += 2;
            }
            else if(encodingMarker == (byte)BlockIds.Nop3)
            {
                // Nop3: Skip 3 bytes
                streamPosition += 3;
            }
            else if(encodingMarker == (byte)BlockIds.Ovl16)
            {
                // Overflow16: Next cell value is increased by 0x10000
                // Continue decoding at next stream position
                // The actual value will be determined by the next encoding marker
                if(streamPosition >= streamLength) return ErrorNumber.InvalidArgument;

                byte nextMarker = streamData[streamPosition++];
                uint cellValue = 0x10000;

                if(nextMarker <= 0x07)
                {
                    // Value16 after overflow
                    if(streamPosition >= streamLength) return ErrorNumber.InvalidArgument;

                    byte lowerByte = streamData[streamPosition++];
                    cellValue += (uint)((nextMarker << 8) | lowerByte);
                }
                else if(nextMarker == (byte)BlockIds.Flux3)
                {
                    // Value16: 16-bit value from next 2 bytes
                    if(streamPosition + 1 >= streamLength) return ErrorNumber.InvalidArgument;

                    byte byte1 = streamData[streamPosition++];
                    byte byte2 = streamData[streamPosition++];
                    cellValue += (uint)((byte2 << 8) | byte1);
                }
                else if(nextMarker >= 0x0E)
                {
                    // Sample: direct value
                    cellValue += (uint)(nextMarker - 0x0D);
                }
                else
                {
                    // Invalid encoding after overflow
                    return ErrorNumber.InvalidArgument;
                }

                cellValues.Add(cellValue);
            }
            else if(encodingMarker == (byte)BlockIds.Flux3)
            {
                // Value16: 16-bit value from next 2 bytes
                if(streamPosition + 1 >= streamLength) return ErrorNumber.InvalidArgument;

                byte byte1 = streamData[streamPosition++];
                byte byte2 = streamData[streamPosition++];
                uint cellValue = (uint)((byte2 << 8) | byte1);

                cellValues.Add(cellValue);
            }
            else if(encodingMarker == (byte)BlockIds.Oob)
            {
                // OOB header: This should be handled by the caller, but if we encounter it here,
                // we need to skip past the OOB block. The OOB block structure is:
                // byte 0: 0x0D (already read)
                // byte 1: OOB type
                // bytes 2-3: length (little-endian)
                // bytes 4+: data
                if(streamPosition + 2 >= streamLength) return ErrorNumber.InvalidArgument;

                byte oobType = streamData[streamPosition++];
                ushort oobLength = (ushort)(streamData[streamPosition] | (streamData[streamPosition + 1] << 8));
                streamPosition += 2;

                // Skip OOB data
                streamPosition += oobLength;
            }
            else if(encodingMarker >= 0x0E)
            {
                // Sample: direct value (marker - 0x0D)
                uint cellValue = (uint)(encodingMarker - 0x0D);
                cellValues.Add(cellValue);
            }
            else
            {
                // Flux2 variants (0x00-0x07 already handled above)
                // These are legacy encodings, treat as regular values
                // Actually, 0x00-0x07 are already handled, so this shouldn't happen
                // But handle Flux2_1 through Flux2_7 just in case
                if(encodingMarker >= (byte)BlockIds.Flux2_1 && encodingMarker <= (byte)BlockIds.Flux2_7)
                {
                    // These are 2-byte values where the marker indicates the high byte
                    if(streamPosition >= streamLength) return ErrorNumber.InvalidArgument;

                    byte lowerByte = streamData[streamPosition++];
                    uint cellValue = (uint)((encodingMarker << 8) | lowerByte);
                    cellValues.Add(cellValue);
                }
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Converts a uint32 cell value to Aaru's flux representation format.
    ///     Format: byte array where 255 = overflow, remainder = value
    /// </summary>
    /// <param name="ticks">The cell value in clock cycles</param>
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
    ///     Calculates resolution in picoseconds from sample clock frequency.
    ///     Resolution = (1 / sck) * 1e12 picoseconds
    /// </summary>
    /// <param name="sck">Sample clock frequency in Hz</param>
    /// <returns>Resolution in picoseconds</returns>
    static ulong CalculateResolution(double sck)
    {
        if(sck <= 0) return 0;

        double periodSeconds = 1.0 / sck;
        double periodPicoseconds = periodSeconds * 1e12;

        return (ulong)periodPicoseconds;
    }
}

