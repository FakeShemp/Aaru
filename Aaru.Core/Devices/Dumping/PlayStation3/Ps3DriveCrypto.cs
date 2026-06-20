// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ps3DriveCrypto.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : PS3 optical drive authentication cryptography.
//
// --[ Description ] ----------------------------------------------------------
//
//     AES-128-CBC and 2-key 3DES-EDE-CBC helpers for Sony PS-SYSTEM drive auth.
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
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Security.Cryptography;

namespace Aaru.Core.Devices.Dumping.PlayStation3;

/// <summary>AES-128-CBC and 2-key 3DES-EDE-CBC helpers for PS3 drive authentication.</summary>
static class Ps3DriveCrypto
{
    /// <summary>AES-128-CBC encrypt without padding.</summary>
    public static byte[] Aes128CbcEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> input)
    {
        using Aes aes = Aes.Create();
        aes.Key     = key.ToArray();
        aes.IV      = iv.ToArray();
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = aes.CreateEncryptor();

        return encryptor.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }

    /// <summary>AES-128-CBC decrypt without padding.</summary>
    public static byte[] Aes128CbcDecrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> input)
    {
        using Aes aes = Aes.Create();
        aes.Key     = key.ToArray();
        aes.IV      = iv.ToArray();
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform decryptor = aes.CreateDecryptor();

        return decryptor.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }

    /// <summary>2-key 3DES-EDE-CBC encrypt without padding.</summary>
    public static byte[] TripleDesEdeCbcEncrypt(ReadOnlySpan<byte> key, ReadOnlySpan<byte> iv, ReadOnlySpan<byte> input)
    {
        using TripleDES tdes = TripleDES.Create();
        tdes.Key     = key.ToArray();
        tdes.IV      = iv.ToArray();
        tdes.Mode    = CipherMode.CBC;
        tdes.Padding = PaddingMode.None;

        using ICryptoTransform encryptor = tdes.CreateEncryptor();

        return encryptor.TransformFinalBlock(input.ToArray(), 0, input.Length);
    }
}
