// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : LibreDriveAacsReadBuffer.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     LibreDrive-patched drives: READ BUFFER probe and Volume Identifier read
//     when microcode was loaded externally (e.g. MakeMKV). BD-only offset for v1.
//
// --[ License ] --------------------------------------------------------------
//
//     Permission is hereby granted, free of charge, to any person obtaining a
//     copy of this software and associated documentation files (the
//     "Software"), to deal in the Software without restriction, including
//     without limitation the rights to use, copy, modify, merge, publish,
//     distribute, sublicense, and/or sell copies of the Software, and to
//     permit persons to whom the Software is furnished to do so, subject to
//     the following conditions:
//
//     The above copyright notice and this permission notice shall be included
//     in all copies or substantial portions of the Software.
//
//     THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
//     OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
//     MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
//     IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
//     CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
//     TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
//     SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// ----------------------------------------------------------------------------
// Copyright © 2026 Rebecca Wallander
// ****************************************************************************/

using System;
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Decryption.Aacs;

/// <summary>READ BUFFER–based Volume Identifier access on externally unlocked LibreDrive firmware.</summary>
public static class LibreDriveAacsReadBuffer
{
    const string MODULE_NAME = "LibreDrive AACS READ BUFFER";

    /// <summary>SCSI READ BUFFER mode byte used for LibreDrive probe and VID reads (vendor buffer).</summary>
    public const byte ReadBufferMode = 0x02;

    /// <summary>SCSI READ BUFFER buffer ID used for LibreDrive probe and VID reads.</summary>
    public const byte ReadBufferId = 0x77;

    const uint ProbeBufferOffset  = 0;
    const uint ProbeTransferLen   = 64;
    const uint BdRomVidBufferOffset = 0x105B78;
    const uint VidTransferLen     = 36;

    static ReadOnlySpan<byte> MmkvAscii => "MMkv"u8;
    static ReadOnlySpan<byte> LbDrAscii => "LbDr"u8;

    /// <summary>
    ///     Parses a 36-byte READ BUFFER response: 4-byte prefix then 16-byte Volume Identifier (bytes 4–19).
    /// </summary>
    internal static bool TryParseLibreDriveReadBufferVid(ReadOnlySpan<byte> response, out byte[]? volumeIdentifier)
    {
        volumeIdentifier = null;

        if(response.Length < 20) return false;

        volumeIdentifier = new byte[16];
        response.Slice(4, 16).CopyTo(volumeIdentifier);

        return true;
    }

    /// <summary>Returns true if the 64-byte probe buffer contains LibreDrive fingerprint substrings.</summary>
    internal static bool ResponseLooksLikeLibreDrivePatchedProbe(ReadOnlySpan<byte> probeBuffer)
    {
        if(probeBuffer.Length < 8) return false;

        if(!BufferContains(probeBuffer, MmkvAscii)) return false;

        return BufferContains(probeBuffer, LbDrAscii);
    }

    static bool BufferContains(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        if(needle.Length == 0 || haystack.Length < needle.Length) return false;

        for(int i = 0; i <= haystack.Length - needle.Length; i++)
        {
            if(haystack.Slice(i, needle.Length).SequenceEqual(needle)) return true;
        }

        return false;
    }

    /// <summary>
    ///     Issues READ BUFFER (mode 0x02, buffer ID 0x77, offset 0, length 64) and checks for the LibreDrive
    ///     patched fingerprint.
    /// </summary>
    public static bool TryProbeLibreDrivePatched(Device dev, uint timeout)
    {
        bool sense = dev.ScsiReadBuffer(out byte[] buffer, out _, ProbeBufferOffset, ProbeTransferLen, timeout,
                                        out double _, ReadBufferMode, ReadBufferId);

        if(sense || buffer.Length < 8)
        {
            AaruLogging.Debug(MODULE_NAME, "LibreDrive READ BUFFER probe: command failed or short buffer.");

            return false;
        }

        bool ok = ResponseLooksLikeLibreDrivePatchedProbe(buffer);

        if(!ok)
            AaruLogging.Debug(MODULE_NAME, "LibreDrive READ BUFFER probe: fingerprint not found.");

        return ok;
    }

    /// <summary>
    ///     Reads the Volume Identifier via READ BUFFER at BD-ROM LibreDrive offset <c>0x105B78</c> (v1 constant).
    ///     Skips <see cref="AacsMediaKind.HdDvd" /> until a verified offset exists.
    /// </summary>
    public static bool TryReadVolumeIdentifierViaLibreDriveReadBuffer(Device dev, AacsMediaKind kind, uint timeout,
                                                                      out byte[]? volumeIdentifier)
    {
        volumeIdentifier = null;

        if(kind != AacsMediaKind.BluRay)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "LibreDrive READ BUFFER VID offset is BD-ROM-only in this iteration; skipping HD DVD.");

            return false;
        }

        bool sense = dev.ScsiReadBuffer(out byte[] buffer, out _, BdRomVidBufferOffset, VidTransferLen, timeout,
                                        out double _, ReadBufferMode, ReadBufferId);

        if(sense || buffer.Length < 20)
        {
            AaruLogging.Debug(MODULE_NAME, "LibreDrive READ BUFFER VID read failed or buffer too short.");

            return false;
        }

        if(!TryParseLibreDriveReadBufferVid(buffer, out volumeIdentifier) || volumeIdentifier is null) return false;

        bool allZero = true;

        foreach(byte b in volumeIdentifier)
        {
            if(b != 0)
            {
                allZero = false;

                break;
            }
        }

        if(allZero)
        {
            volumeIdentifier = null;

            return false;
        }

        return true;
    }
}