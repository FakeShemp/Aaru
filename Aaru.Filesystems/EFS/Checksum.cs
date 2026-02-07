// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Checksum.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Extent File System plugin
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

using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class EFS
{
    /// <summary>
    ///     Computes the EFS superblock checksum for IRIX 3.3+ filesystems. This checksum rotates the accumulator to
    ///     preserve information from all fields.
    /// </summary>
    /// <param name="data">Raw superblock data (up to but not including the checksum field)</param>
    /// <returns>The computed checksum</returns>
    static int ComputeChecksum(byte[] data)
    {
        var checksum = 0;

        // Process data as big-endian 16-bit words up to (but not including) the checksum field
        // The checksum field is at offset 0x58 (88 bytes), so we process 44 16-bit words
        const int checksumOffset = 88; // Offset of sb_checksum in the structure

        for(var i = 0; i < checksumOffset && i + 1 < data.Length; i += 2)
        {
            // Big-endian: high byte first
            var word = (ushort)(data[i] << 8 | data[i + 1]);
            checksum ^= word;

            // Rotate left by 1 bit (preserves information)
            // (checksum < 0) is true if the sign bit is set, which gives us the carry bit
            checksum = checksum << 1 | (checksum < 0 ? 1 : 0);
        }

        return checksum;
    }

    /// <summary>
    ///     Computes the EFS superblock checksum for pre-IRIX 3.3 filesystems. This checksum shifts rather than rotates,
    ///     which discards information from fields more than 32 half-words before the checksum. Since a zero-filled spare
    ///     array lies just before the checksum, much of the computed sum tends to be 0.
    /// </summary>
    /// <param name="data">Raw superblock data (up to but not including the checksum field)</param>
    /// <returns>The computed checksum</returns>
    static int ComputeOldChecksum(byte[] data)
    {
        var checksum = 0;

        // Process data as big-endian 16-bit words up to (but not including) the checksum field
        const int checksumOffset = 88; // Offset of sb_checksum in the structure

        for(var i = 0; i < checksumOffset && i + 1 < data.Length; i += 2)
        {
            // Big-endian: high byte first
            var word = (ushort)(data[i] << 8 | data[i + 1]);
            checksum ^= word;

            // Shift left by 1 bit (discards the high bit, losing information)
            checksum <<= 1;
        }

        return checksum;
    }

    /// <summary>Validates the superblock checksum using the appropriate algorithm based on magic number</summary>
    /// <param name="data">Raw superblock data</param>
    /// <param name="superblock">Parsed superblock structure</param>
    /// <returns>True if checksum is valid, false otherwise</returns>
    static bool ValidateSuperblockChecksum(byte[] data, Superblock superblock)
    {
        var storedChecksum = (int)superblock.sb_checksum;

        if(superblock.sb_magic == EFS_MAGIC_NEW)
        {
            // Grown filesystem (EFS_NEWMAGIC) - definitely uses new checksum algorithm
            int calculatedChecksum = ComputeChecksum(data);

            AaruLogging.Debug(MODULE_NAME,
                              "Grown EFS checksum: stored=0x{0:X8}, calculated=0x{1:X8}",
                              storedChecksum,
                              calculatedChecksum);

            return storedChecksum == calculatedChecksum;
        }

        // EFS_MAGIC is used by both pre-3.3 and non-grown 3.3+ filesystems
        // Try the new algorithm first (more common), then fall back to old
        int newChecksum = ComputeChecksum(data);

        AaruLogging.Debug(MODULE_NAME,
                          "EFS checksum (new algorithm): stored=0x{0:X8}, calculated=0x{1:X8}",
                          storedChecksum,
                          newChecksum);

        if(storedChecksum == newChecksum) return true;

        // Try old algorithm for true pre-3.3 filesystems
        int oldChecksum = ComputeOldChecksum(data);

        AaruLogging.Debug(MODULE_NAME,
                          "EFS checksum (old algorithm): stored=0x{0:X8}, calculated=0x{1:X8}",
                          storedChecksum,
                          oldChecksum);

        return storedChecksum == oldChecksum;
    }

    /// <summary>
    ///     Validates the filesystem size parameterization based on the EFS version. Pre-3.3 and 3.3+ have different
    ///     definitions for fs_size. Note that EFS_MAGIC is used by both pre-3.3 and non-grown 3.3+ filesystems.
    /// </summary>
    /// <param name="superblock">The superblock to validate</param>
    /// <returns>True if size parameterization is valid, false otherwise</returns>
    static bool ValidateSizeParameterization(Superblock superblock)
    {
        // Calculate the data area end (first cylinder group + all cylinder groups)
        int dataAreaEnd = superblock.sb_firstcg + superblock.sb_cgfsize * superblock.sb_ncg;

        if(superblock.sb_magic == EFS_MAGIC_NEW)
        {
            // Grown filesystem: fs_size includes bitmap and replicated superblock at end
            // Data area should not exceed fs_size and should not exceed bitmap block location
            bool validSize    = dataAreaEnd <= superblock.sb_size;
            bool validBmblock = superblock.sb_bmblock == 0 || dataAreaEnd <= superblock.sb_bmblock;

            AaruLogging.Debug(MODULE_NAME,
                              "Grown EFS size validation: sb_size={0}, dataAreaEnd={1}, sb_bmblock={2}, validSize={3}, validBmblock={4}",
                              superblock.sb_size,
                              dataAreaEnd,
                              superblock.sb_bmblock,
                              validSize,
                              validBmblock);

            return validSize && validBmblock;
        }

        // EFS_MAGIC is used by both pre-3.3 and non-grown 3.3+ filesystems
        // Pre-3.3: fs_size should equal the data area end
        // Non-grown 3.3+: fs_size may include trailing space for replicated superblock

        // Try exact match first (pre-3.3 style)
        if(superblock.sb_size == dataAreaEnd)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "EFS size validation (exact match): sb_size={0}, dataAreaEnd={1}",
                              superblock.sb_size,
                              dataAreaEnd);

            return true;
        }

        // Allow fs_size >= dataAreaEnd for 3.3+ non-grown filesystems
        // which may have trailing space for replicated superblock
        bool valid = superblock.sb_size >= dataAreaEnd;

        AaruLogging.Debug(MODULE_NAME,
                          "EFS size validation (3.3+ style): sb_size={0}, dataAreaEnd={1}, valid={2}",
                          superblock.sb_size,
                          dataAreaEnd,
                          valid);

        return valid;
    }
}