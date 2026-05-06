// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsDeviceKeys.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads AACS device keys from libaacs KEYDB.cfg files (format 1.0),
//     using DK entries in labeled form:
//
//       | DK | DEVICE_KEY 0x... | DEVICE_NODE 0x... |
//            KEY_UV 0x... | KEY_U_MASK_SHIFT 0x...
//
//     All valid DK entries are loaded.
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Aaru.Decryption.Aacs;

/// <summary>
///     A single AACS device key with subset-difference metadata from a KEYDB.cfg DK entry.
/// </summary>
public readonly struct AacsDeviceKey
{
    /// <summary>Construct a device key.</summary>
    /// <param name="key">16-byte device key.</param>
    /// <param name="node">Optional device number / node.</param>
    /// <param name="uv">Optional UV value.</param>
    /// <param name="uMaskShift">Optional U-mask shift.</param>
    public AacsDeviceKey(byte[] key, uint? node, uint? uv, byte? uMaskShift)
    {
        Key        = key;
        Node       = node;
        Uv         = uv;
        UMaskShift = uMaskShift;
    }

    /// <summary>16-byte device key.</summary>
    public byte[] Key { get; }

    /// <summary>Device number / node, or <c>null</c> if not specified.</summary>
    public uint? Node { get; }

    /// <summary>UV, or <c>null</c> if not specified.</summary>
    public uint? Uv { get; }

    /// <summary>U-mask shift, or <c>null</c> if not specified.</summary>
    public byte? UMaskShift { get; }

    /// <summary><c>true</c> if all subset-difference metadata is present (pruned tree walk possible).</summary>
    public bool HasMetadata => Node.HasValue && Uv.HasValue && UMaskShift.HasValue;
}

/// <summary>Parses AACS device key files.</summary>
public static class AacsDeviceKeys
{
    /// <summary>Try to load device keys from <paramref name="path" />.</summary>
    /// <param name="path">Path to the device-keys file.</param>
    /// <param name="keys">On success, the parsed list of keys (never empty).</param>
    /// <param name="error">On failure, a human-readable error message.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryLoad(string path, out IReadOnlyList<AacsDeviceKey>? keys, out string? error)
    {
        keys  = null;
        error = null;

        if(string.IsNullOrWhiteSpace(path))
        {
            error = "AACS device keys file path is empty.";

            return false;
        }

        if(!File.Exists(path))
        {
            error = $"AACS device keys file '{path}' does not exist.";

            return false;
        }

        string[] rawLines;

        try
        {
            rawLines = File.ReadAllLines(path);
        }
        catch(IOException ex)
        {
            error = $"Cannot read AACS device keys file '{path}': {ex.Message}";

            return false;
        }

        List<AacsDeviceKey> parsed = [];

        for(int lineNumber = 1; lineNumber <= rawLines.Length; lineNumber++)
        {
            string line = StripComment(rawLines[lineNumber - 1]).Trim();

            if(line.Length == 0) continue;

            if(!TryParseKeydbLine(line, out bool isDkEntry, out AacsDeviceKey key, out string? lineErr))
                continue;

            if(lineErr is not null)
            {
                error = $"Invalid DK entry on line {lineNumber}: {lineErr}";

                return false;
            }

            if(!isDkEntry) continue;

            parsed.Add(key);
        }

        if(parsed.Count == 0)
        {
            error = $"AACS device keys file '{path}' contains no DK entries.";

            return false;
        }

        keys = parsed;

        return true;
    }

    static string StripComment(string line)
    {
        int semi = line.IndexOf(';');
        int hash = line.IndexOf('#');

        int cut = semi switch
                  {
                      < 0 when hash < 0 => -1,
                      < 0               => hash,
                      _ when hash < 0   => semi,
                      _                 => Math.Min(semi, hash)
                  };

        return cut < 0 ? line : line[..cut];
    }

    static bool TryParseKeydbLine(string line, out bool isDkEntry, out AacsDeviceKey key, out string? error)
    {
        isDkEntry = false;
        key   = default;
        error = null;

        string[] fields = line.Split('|');
        List<string> parts = [];

        foreach(string field in fields)
        {
            string trimmed = field.Trim();

            if(trimmed.Length == 0) continue;

            parts.Add(trimmed);
        }

        if(parts.Count == 0 || !parts[0].Equals("DK", StringComparison.OrdinalIgnoreCase))
            return false;

        isDkEntry = true;

        byte[]? deviceKey = null;
        uint?   node      = null;
        uint?   uv        = null;
        byte?   uMask     = null;

        for(int i = 1; i < parts.Count; i++)
        {
            string token = parts[i];
            int separator = token.IndexOfAny([' ', '\t']);

            if(separator <= 0)
            {
                error = $"invalid DK labeled field '{token}'.";

                return false;
            }

            string label = token[..separator].Trim();
            string value = token[(separator + 1)..].Trim();

            if(value.Length == 0)
            {
                error = $"DK labeled field '{label}' is missing a value.";

                return false;
            }

            switch(label.ToUpperInvariant())
            {
                case "DEVICE_KEY":
                    if(!TryParseHex(StripHexPrefix(value), 16, out byte[]? parsedKey, out string? keyErr))
                    {
                        error = $"invalid DEVICE_KEY field: {keyErr}";

                        return false;
                    }

                    deviceKey = parsedKey;

                    break;
                case "DEVICE_NODE":
                    if(!TryParseHexUInt32(StripHexPrefix(value), out uint parsedNode, out string? nodeErr))
                    {
                        error = $"invalid DEVICE_NODE field: {nodeErr}";

                        return false;
                    }

                    node = parsedNode;

                    break;
                case "KEY_UV":
                    if(!TryParseHexUInt32(StripHexPrefix(value), out uint parsedUv, out string? uvErr))
                    {
                        error = $"invalid KEY_UV field: {uvErr}";

                        return false;
                    }

                    uv = parsedUv;

                    break;
                case "KEY_U_MASK":
                case "KEY_U_MASK_SHIFT":
                    if(!TryParseHexUInt32(StripHexPrefix(value), out uint parsedShift, out string? shiftErr))
                    {
                        error = $"invalid {label} field: {shiftErr}";

                        return false;
                    }

                    if(parsedShift > 0xFF)
                    {
                        error = $"{label} does not fit in a byte.";

                        return false;
                    }

                    uMask = (byte)parsedShift;

                    break;
            }
        }

        if(deviceKey is null)
        {
            error = "DK labeled line is missing DEVICE_KEY.";

            return false;
        }

        if(node is null || uv is null || uMask is null)
        {
            error =
                "DK entry must include DEVICE_NODE, KEY_UV, and KEY_U_MASK or KEY_U_MASK_SHIFT after DEVICE_KEY.";

            return false;
        }

        key = new AacsDeviceKey(deviceKey, node, uv, uMask);

        return true;
    }

    static string StripHexPrefix(string s)
    {
        if(s.Length >= 2 && s[0] == '0' && s[1] is 'x' or 'X') return s[2..];

        return s;
    }

    static bool TryParseHex(string hex, int expectedBytes, out byte[]? data, out string? error)
    {
        data  = null;
        error = null;

        if(hex.Length != expectedBytes * 2)
        {
            error = $"expected {expectedBytes * 2} hex characters, got {hex.Length}.";

            return false;
        }

        byte[] bytes = new byte[expectedBytes];

        for(int i = 0; i < expectedBytes; i++)
        {
            if(!byte.TryParse(hex.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out byte b))
            {
                error = $"non-hex character at offset {i * 2}.";

                return false;
            }

            bytes[i] = b;
        }

        data = bytes;

        return true;
    }

    static bool TryParseHexUInt32(string hex, out uint value, out string? error)
    {
        value = 0;
        error = null;

        if(hex.Length == 0)
        {
            error = "empty value.";

            return false;
        }

        if(hex.Length > 8)
        {
            error = "value does not fit in 32 bits.";

            return false;
        }

        if(!uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value))
        {
            error = "not a hexadecimal integer.";

            return false;
        }

        return true;
    }
}