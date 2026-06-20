// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ps3DriveAuth.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : PS3 optical drive Data1/Data2 extraction.
//
// --[ Description ] ----------------------------------------------------------
//
//     Authenticates with a Sony PS-SYSTEM optical drive and retrieves PS3 disc
//     Data1/Data2 keys. Ported from DiscImageCreator ps3auth module.
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
using Aaru.Devices;
using Aaru.Logging;

namespace Aaru.Core.Devices.Dumping.PlayStation3;

/// <summary>Extracts PS3 disc Data1/Data2 keys from a Sony PS-SYSTEM optical drive.</summary>
static class Ps3DriveAuth
{
    const string MODULE_NAME = "PS3 drive auth";

    const int SESSION_BLOB_LENGTH   = 20;
    const int SESSION_RECEIVE_LENGTH = 36;
    const int VENDOR_FRAME_LENGTH    = 84;
    const int VENDOR_RESPONSE_LENGTH = 52;
    const int KEY_LENGTH             = 16;

    /// <summary>
    ///     Authenticates with the PS3 optical drive and retrieves encrypted Data1 and Data2 keys.
    /// </summary>
    /// <param name="dev">Sony PS-SYSTEM optical drive.</param>
    /// <param name="data1">Output: 16-byte Data1 key (redump data1 / enc_a).</param>
    /// <param name="data2">Output: 16-byte Data2 key (redump data2 / enc_b).</param>
    /// <returns><c>true</c> if keys were retrieved successfully.</returns>
    public static bool ExtractData1Data2(Device dev, out byte[] data1, out byte[] data2)
    {
        data1 = null;
        data2 = null;

        if(!dev.IsPlayStation3Drive())
        {
            AaruLogging.Debug(MODULE_NAME, "Device is not a Sony PS-SYSTEM optical drive");

            return false;
        }

        byte[] keyA;
        byte[] keyB;

        if(!EstablishSessionKeys(dev, out keyA, out keyB))
        {
            AaruLogging.Error(MODULE_NAME, "Session key establishment failed");

            return false;
        }

        if(!GetTwoBlobs(dev, keyA, keyB, out byte[] encA, out byte[] encB))
        {
            AaruLogging.Error(MODULE_NAME, "Failed to retrieve Data1/Data2 from drive");

            return false;
        }

        data1 = encA;
        data2 = encB;

        AaruLogging.Debug(MODULE_NAME, "Data1: {0}", Convert.ToHexString(data1));
        AaruLogging.Debug(MODULE_NAME, "Data2: {0}", Convert.ToHexString(data2));

        return true;
    }

    static bool EstablishSessionKeys(Device dev, out byte[] keyA, out byte[] keyB)
    {
        keyA = null;
        keyB = null;

        byte[] header = [0, 16, 0, 0];
        byte[] response = new byte[SESSION_BLOB_LENGTH];
        Array.Copy(header, response, 4);

        byte[] encryptedChallenge = Ps3DriveCrypto.Aes128CbcEncrypt(Ps3DriveTables.DefaultKey1,
                                                                   Ps3DriveTables.Iv1,
                                                                   Ps3DriveTables.SessionChallenge);

        Array.Copy(encryptedChallenge, 0, response, 4, KEY_LENGTH);

        if(dev.PlayStation3SendKey(response, 0, out _, dev.Timeout, out _)) return false;

        if(dev.PlayStation3ReportKey(out byte[] receive, 0, SESSION_RECEIVE_LENGTH, out _, dev.Timeout, out _))
            return false;

        byte[] establishOut1 = Ps3DriveCrypto.Aes128CbcDecrypt(Ps3DriveTables.DefaultKey2,
                                                               Ps3DriveTables.Iv1,
                                                               receive.AsSpan(4, KEY_LENGTH));

        byte[] establishOut2 = Ps3DriveCrypto.Aes128CbcDecrypt(Ps3DriveTables.DefaultKey2,
                                                               Ps3DriveTables.Iv1,
                                                               receive.AsSpan(20, KEY_LENGTH));

        for(int i = 0; i < KEY_LENGTH; i++)
        {
            if(Ps3DriveTables.SessionChallenge[i] != establishOut1[i]) return false;
        }

        byte[] tmp = new byte[KEY_LENGTH];
        Array.Copy(establishOut1, 0, tmp, 0, 8);
        Array.Copy(establishOut2, 8, tmp, 8, 8);
        keyA = Ps3DriveCrypto.Aes128CbcEncrypt(Ps3DriveTables.InputKey1, Ps3DriveTables.Iv1, tmp);

        Array.Copy(establishOut1, 8, tmp, 0, 8);
        Array.Copy(establishOut2, 0, tmp, 8, 8);
        keyB = Ps3DriveCrypto.Aes128CbcEncrypt(Ps3DriveTables.InputKey2, Ps3DriveTables.Iv1, tmp);

        Array.Copy(header, response, 4);

        byte[] confirmBlob = Ps3DriveCrypto.Aes128CbcEncrypt(Ps3DriveTables.DefaultKey1,
                                                             Ps3DriveTables.Iv1,
                                                             establishOut2);

        Array.Copy(confirmBlob, 0, response, 4, KEY_LENGTH);

        if(dev.PlayStation3SendKey(response, 2, out _, dev.Timeout, out _)) return false;

        return true;
    }

    static bool GetTwoBlobs(Device dev, byte[] keyA, byte[] keyB, out byte[] encA, out byte[] encB)
    {
        encA = null;
        encB = null;

        byte[] txFrame84 = new byte[VENDOR_FRAME_LENGTH];
        txFrame84[0] = 0;
        txFrame84[1] = 80;
        txFrame84[2] = 0;
        txFrame84[3] = 0;

        byte[] encryptedParamset = Ps3DriveCrypto.Aes128CbcEncrypt(keyA, Ps3DriveTables.IvAes, Ps3DriveTables.Paramset3);
        Array.Copy(encryptedParamset, 0, txFrame84, 4, 80);

        byte[] getOut = Ps3DriveCrypto.TripleDesEdeCbcEncrypt(keyA, Ps3DriveTables.Iv3Des, Ps3DriveTables.GetIn);

        if(dev.PlayStation3VendorE1(getOut, txFrame84, out _, dev.Timeout, out _)) return false;

        getOut = Ps3DriveCrypto.TripleDesEdeCbcEncrypt(keyA, Ps3DriveTables.Iv3Des, Ps3DriveTables.GetIn2);

        if(dev.PlayStation3VendorE0(out txFrame84, getOut, VENDOR_RESPONSE_LENGTH, out _, dev.Timeout, out _))
            return false;

        byte[] getOut2 = Ps3DriveCrypto.Aes128CbcDecrypt(keyA, Ps3DriveTables.IvAes, txFrame84.AsSpan(4, 48));

        encA = Ps3DriveCrypto.Aes128CbcDecrypt(keyB, Ps3DriveTables.IvAes, getOut2.AsSpan(3, KEY_LENGTH));
        encB = Ps3DriveCrypto.Aes128CbcDecrypt(keyB, Ps3DriveTables.IvAes, getOut2.AsSpan(19, KEY_LENGTH));

        return true;
    }
}
