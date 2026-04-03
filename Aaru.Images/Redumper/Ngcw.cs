// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Ngcw.cs
// Author(s)      : Rebecca Wallander <sakcheen@gmail.com>
//
// Component      : Disc image plugins (Redumper Nintendo GOD/WOD).
//
// --[ Description ] ----------------------------------------------------------
//
//     Nintendo DVD descrambling for GameCube/Wii Redumper dumps. Produces
//     2064-byte long sectors (and 2048-byte user via ReadSector) matching a
//     raw dump after the Nintendo layer: Wii AES partition data remains
//     ciphertext in user sectors until conversion (see ConvertNgcwSectors).
//     Junk maps, partition key tags, and Wii decrypt are handled when
//     converting to AaruFormat, not when reading this plugin.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Rebecca Wallander
// ****************************************************************************/

using System;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.Decoders.Nintendo;

namespace Aaru.Images;

public sealed partial class Redumper
{
    const int NGCW_LONG_SECTOR_SIZE  = 2064;
    const int NGCW_SECTORS_PER_GROUP = 16;

    static bool IsNintendoMediaType(MediaType mt) => mt is MediaType.GOD or MediaType.WOD;

    /// <summary>
    ///     Derives the Nintendo disc key from LBA 0 so sectors 16+ can be descrambled.
    ///     Does not parse partitions, junk, or decrypt Wii groups — conversion does that.
    /// </summary>
    void TryInitializeNgcwAfterOpen()
    {
        _nintendoDerivedKey = null;

        if(!IsNintendoMediaType(_imageInfo.MediaType)) return;

        EnsureNintendoDerivedKeyFromLba0();
    }

    /// <summary>
    ///     Derives the Nintendo key from LBA 0 so sectors 16+ can be descrambled.
    /// </summary>
    /// <returns><c>True</c> if the Nintendo key was derived successfully, <c>False</c> if not</returns>
    bool EnsureNintendoDerivedKeyFromLba0()
    {
        ErrorNumber errno = ReadSectorLongForNgcw(0, false, out byte[] long0, out _);

        return errno == ErrorNumber.NoError && long0 != null && long0.Length >= NGCW_LONG_SECTOR_SIZE;
    }

    /// <summary>
    ///     Reads a sector long for Nintendo descrambling.
    /// </summary>
    /// <param name="sectorAddress">The sector address to read</param>
    /// <param name="negative">Whether the sector address is negative</param>
    /// <param name="buffer">The buffer to read the sector into</param>
    /// <param name="sectorStatus">The status of the sector</param>
    /// <returns>The error number</returns>
    /// <remarks>
    ///     This method is used to read a sector long for Nintendo descrambling.
    ///     It is used to read the sector long for LBA 0 to derive the Nintendo key.
    /// </remarks>
    ErrorNumber ReadSectorLongForNgcw(ulong sectorAddress, bool negative, out byte[] buffer, out SectorStatus sectorStatus)
    {
        buffer       = null;
        sectorStatus = SectorStatus.NotDumped;

        int lba = negative ? -(int)sectorAddress : (int)sectorAddress;

        long frameIndex = (long)lba - LBA_START;

        if(frameIndex < 0 || frameIndex >= _totalFrames) return ErrorNumber.OutOfRange;

        sectorStatus = MapState(_stateData[frameIndex]);

        byte[] dvdSector = ReadAndFlattenFrame(frameIndex);

        if(dvdSector is null) return ErrorNumber.InvalidArgument;

        if(sectorStatus != SectorStatus.Dumped)
        {
            buffer = dvdSector;

            return ErrorNumber.NoError;
        }

        if(!IsNintendoMediaType(_imageInfo.MediaType))
        {
            ErrorNumber error = _decoding.Scramble(dvdSector, out byte[] descrambled);

            buffer = error == ErrorNumber.NoError ? descrambled : dvdSector;

            return ErrorNumber.NoError;
        }

        if(!DescrambleNintendo2064InPlace(dvdSector, lba))
        {
            buffer = dvdSector;

            return ErrorNumber.NoError;
        }

        buffer = dvdSector;

        return ErrorNumber.NoError;
    }

    bool DescrambleNintendo2064InPlace(byte[] buffer, int lba)
    {
        byte[] one = new byte[NGCW_LONG_SECTOR_SIZE];
        Array.Copy(buffer, 0, one, 0, NGCW_LONG_SECTOR_SIZE);
        bool leadIn  = lba < 0;
        bool leadOut = _ngcwRegularDataSectors > 0 && lba >= (long)_ngcwRegularDataSectors;

        ErrorNumber error;
        byte[]      decoded;

        if(leadIn || leadOut)
            error = _decoding.Scramble(one, out decoded);
        else
        {
            byte key = lba < NGCW_SECTORS_PER_GROUP ? (byte)0 : (_nintendoDerivedKey ?? (byte)0);
            error = _nintendoDecoder.Scramble(one, key, out decoded);
        }

        if(error != ErrorNumber.NoError)
        {
            Array.Clear(buffer, 0, NGCW_LONG_SECTOR_SIZE);

            return false;
        }

        if(decoded != null) Array.Copy(decoded, 0, buffer, 0, NGCW_LONG_SECTOR_SIZE);

        if(lba == 0 && decoded != null)
        {
            byte[] keyMaterial = new byte[8];
            Array.Copy(decoded, Sector.NintendoMainDataOffset, keyMaterial, 0, 8);
            _nintendoDerivedKey = Sector.DeriveNintendoKey(keyMaterial);
        }

        return true;
    }
}
