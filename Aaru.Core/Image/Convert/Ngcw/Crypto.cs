// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Crypto.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Image conversion.
//
// --[ Description ] ----------------------------------------------------------
//
//     Nintendo Wii disc encryption: common keys, title key decryption,
//     group encrypt/decrypt.
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
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program. If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2019-2026 Natalia Portillo
// ****************************************************************************/

using System;
using System.Security.Cryptography;

namespace Aaru.Core.Image.Ngcw;

/// <summary>Wii disc encryption helpers.</summary>
static class Crypto
{
    /// <summary>Wii physical group size (32 KiB).</summary>
    public const int GROUP_SIZE = 0x8000;

    /// <summary>Hash block size within a Wii group (1 KiB).</summary>
    public const int GROUP_HASH_SIZE = 0x0400;

    /// <summary>User data size within a Wii group (31 KiB).</summary>
    public const int GROUP_DATA_SIZE = 0x7C00;

    /// <summary>Number of 2048-byte logical sectors per group.</summary>
    public const int LOGICAL_PER_GROUP = 16;

    /// <summary>Logical sector size in bytes.</summary>
    public const int SECTOR_SIZE = 2048;

    /// <summary>Maximum number of partitions supported.</summary>
    public const int MAX_PARTITIONS = 32;
    /// <summary>Wii standard common key.</summary>
    public static readonly byte[] WII_COMMON_KEY =
    [
        0xEB, 0xE4, 0x2A, 0x22, 0x5E, 0x85, 0x93, 0xE4, 0x48, 0xD9, 0xC5, 0x45, 0x73, 0x81, 0xAA, 0xF7
    ];

    /// <summary>Wii Korean common key.</summary>
    public static readonly byte[] WII_KOREAN_KEY =
    [
        0x63, 0xB8, 0x2B, 0xB4, 0xF4, 0x61, 0x4E, 0x2E, 0x13, 0xF2, 0xFE, 0xFB, 0xBA, 0x4C, 0x9B, 0x7E
    ];

    /// <summary>
    ///     Decrypt a Wii title key from a ticket using the appropriate common key.
    /// </summary>
    /// <param name="ticket">Raw ticket data (0x2A4 bytes).</param>
    /// <returns>16-byte decrypted title key.</returns>
    public static byte[] DecryptTitleKey(byte[] ticket)
    {
        byte commonKeyIndex = ticket[0x1F1];

        byte[] commonKey = commonKeyIndex == 1 ? WII_KOREAN_KEY : WII_COMMON_KEY;

        // IV = title_id (8 bytes at ticket + 0x1DC) + 8 zero bytes
        var iv = new byte[16];
        Array.Copy(ticket, 0x1DC, iv, 0, 8);

        // Encrypted title key at ticket + 0x1BF (16 bytes)
        var encryptedKey = new byte[16];
        Array.Copy(ticket, 0x1BF, encryptedKey, 0, 16);

        using var aes = Aes.Create();
        aes.Key     = commonKey;
        aes.IV      = iv;
        aes.Mode    = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        using ICryptoTransform decryptor = aes.CreateDecryptor();

        return decryptor.TransformFinalBlock(encryptedKey, 0, 16);
    }

    /// <summary>
    ///     Decrypt a Wii group (0x8000 bytes) into separate hash block and user data.
    /// </summary>
    /// <param name="key">16-byte AES-128 partition key.</param>
    /// <param name="encryptedGroup">0x8000-byte encrypted input.</param>
    /// <param name="hashBlock">0x400-byte output for hash block.</param>
    /// <param name="dataOut">0x7C00-byte output for user data.</param>
    public static void DecryptGroup(byte[] key, byte[] encryptedGroup, byte[] hashBlock, byte[] dataOut)
    {
        // Hash block: first 0x400 bytes, IV = all zeros
        using var aesHash = Aes.Create();
        aesHash.Key     = key;
        aesHash.IV      = new byte[16];
        aesHash.Mode    = CipherMode.CBC;
        aesHash.Padding = PaddingMode.None;

        using ICryptoTransform hashDecryptor = aesHash.CreateDecryptor();
        byte[]                 decryptedHash = hashDecryptor.TransformFinalBlock(encryptedGroup, 0, GROUP_HASH_SIZE);
        Array.Copy(decryptedHash, 0, hashBlock, 0, GROUP_HASH_SIZE);

        // Data block: next 0x7C00 bytes
        // IV = bytes 0x3D0..0x3DF of the ENCRYPTED input (not the decrypted hash block)
        var dataIv = new byte[16];
        Array.Copy(encryptedGroup, 0x3D0, dataIv, 0, 16);

        using var aesData = Aes.Create();
        aesData.Key     = key;
        aesData.IV      = dataIv;
        aesData.Mode    = CipherMode.CBC;
        aesData.Padding = PaddingMode.None;

        using ICryptoTransform dataDecryptor = aesData.CreateDecryptor();
        byte[] decryptedData = dataDecryptor.TransformFinalBlock(encryptedGroup, GROUP_HASH_SIZE, GROUP_DATA_SIZE);
        Array.Copy(decryptedData, 0, dataOut, 0, GROUP_DATA_SIZE);
    }
}