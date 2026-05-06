// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AacsHostCredentials.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// --[ Description ] ----------------------------------------------------------
//
//     Reads an AACS host private key and host certificate from a libaacs
//     KEYDB.cfg file (format 1.0), using HC entries:
//
//       | HC | HOST_PRIV_KEY 0x... | HOST_CERT 0x...
//
//     The first valid HC entry is selected. The host certificate is verified
//     against the AACS Licensing Authority root public key on load.
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
///     A parsed and verified pair of AACS host private key and host certificate, ready
///     for use in <see cref="AacsAuth" />.
/// </summary>
public sealed class AacsHostCredentials
{
    AacsHostCredentials(byte[] privateKey, byte[] certificate)
    {
        PrivateKey  = privateKey;
        Certificate = certificate;
    }

    /// <summary>20-byte big-endian host private scalar.</summary>
    public byte[] PrivateKey { get; }

    /// <summary>92-byte AACS host certificate.</summary>
    public byte[] Certificate { get; }

    /// <summary>
    ///     Try to load and verify host credentials from <paramref name="path" />.
    /// </summary>
    /// <param name="path">Path to the host key/certificate file.</param>
    /// <param name="credentials">On success, the parsed credentials.</param>
    /// <param name="errorMessage">On failure, a human-readable error message.</param>
    /// <returns><c>true</c> on success.</returns>
    public static bool TryLoad(string path, out AacsHostCredentials? credentials, out string? errorMessage)
    {
        credentials  = null;
        errorMessage = null;

        if(string.IsNullOrWhiteSpace(path))
        {
            errorMessage = "AACS host key file path is empty.";

            return false;
        }

        if(!File.Exists(path))
        {
            errorMessage = $"AACS host key file '{path}' does not exist.";

            return false;
        }

        string[] rawLines;

        try
        {
            rawLines = File.ReadAllLines(path);
        }
        catch(IOException ex)
        {
            errorMessage = $"Cannot read AACS host key file '{path}': {ex.Message}";

            return false;
        }

        List<string> lines = [];

        foreach(string line in rawLines)
        {
            string trimmed = line.Trim();

            if(trimmed.Length == 0) continue;

            if(trimmed.StartsWith(';') || trimmed.StartsWith('#')) continue;

            lines.Add(trimmed);
        }

        string? lastHcError = null;

        foreach(string line in lines)
        {
            if(!TryParseHcLine(line, out byte[]? priv, out byte[]? cert, out string? parseError))
                continue;

            if(parseError is not null)
            {
                lastHcError = parseError;

                continue;
            }

            if(cert![0] != 0x02)
            {
                lastHcError = $"Unsupported host certificate type 0x{cert[0]:X2} (only AACS 1.0 host certificates with type 0x02 are supported in Phase 1).";

                continue;
            }

            if(!AacsCrypto.VerifyAacsCert(cert))
            {
                lastHcError = "Host certificate signature verification failed (not signed by the AACS Licensing Authority).";

                continue;
            }

            credentials = new AacsHostCredentials(priv!, cert);

            return true;
        }

        errorMessage = lastHcError ?? $"AACS host key file '{path}' contains no HC entries.";

        return false;
    }

    static bool TryParseHcLine(string line, out byte[]? privateKey, out byte[]? certificate, out string? error)
    {
        privateKey  = null;
        certificate = null;
        error       = null;

        string content = StripComment(line).Trim();

        if(content.Length == 0) return false;

        string[] fields = content.Split('|');
        List<string> parts = [];

        foreach(string field in fields)
        {
            string trimmed = field.Trim();

            if(trimmed.Length == 0) continue;

            parts.Add(trimmed);
        }

        if(parts.Count == 0 || !parts[0].Equals("HC", StringComparison.OrdinalIgnoreCase))
            return false;

        for(int i = 1; i < parts.Count; i++)
        {
            string token = parts[i];
            int    split = token.IndexOfAny([' ', '\t']);

            if(split <= 0)
            {
                error = $"Invalid HC field '{token}'.";

                return true;
            }

            string label = token[..split].Trim();
            string value = token[(split + 1)..].Trim();

            if(value.Length == 0)
            {
                error = $"HC field '{label}' is missing a value.";

                return true;
            }

            switch(label.ToUpperInvariant())
            {
                case "HOST_PRIV_KEY":
                    if(!TryParseHex(StripHexPrefix(value), AacsCurve.SCALAR_SIZE, out privateKey, out string? pkErr))
                    {
                        error = $"Invalid HOST_PRIV_KEY field: {pkErr}";

                        return true;
                    }

                    break;
                case "HOST_CERT":
                    if(!TryParseHex(StripHexPrefix(value), AacsCurve.CERT_SIZE, out certificate, out string? certErr))
                    {
                        error = $"Invalid HOST_CERT field: {certErr}";

                        return true;
                    }

                    break;
            }
        }

        if(privateKey is null || certificate is null)
        {
            error = "HC entry is missing HOST_PRIV_KEY or HOST_CERT.";

            return true;
        }

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

    static string StripHexPrefix(string s)
    {
        if(s.Length >= 2 && s[0] == '0' && s[1] is 'x' or 'X')
            return s[2..];

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
}