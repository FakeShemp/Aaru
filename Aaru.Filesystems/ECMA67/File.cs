// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : ECMA-67 plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     File operations for the ECMA-67 file system.
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
using System.Globalization;
using System.IO;
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using Aaru.Logging;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the filesystem described in ECMA-67</summary>
public sealed partial class ECMA67
{
    /// <summary>Parses a 5-digit ECMA-67 extent address (CCSSN) into an absolute LBA</summary>
    /// <param name="address">5-byte ASCII extent address: CC=cylinder, S=side, NN=sector</param>
    /// <param name="lba">Resulting absolute LBA</param>
    /// <returns><c>true</c> if the address was valid and parsed successfully</returns>
    bool TryParseExtentAddress(byte[] address, out ulong lba)
    {
        lba = 0;

        if(address is null || address.Length < 5) return false;

        string addrStr = Encoding.ASCII.GetString(address).Trim();

        if(addrStr.Length < 5) return false;

        if(!int.TryParse(addrStr[..2],  NumberStyles.Integer, CultureInfo.InvariantCulture, out int cylinder) ||
           !int.TryParse(addrStr[2..3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int side)     ||
           !int.TryParse(addrStr[3..5], NumberStyles.Integer, CultureInfo.InvariantCulture, out int sector))
            return false;

        const int sectorsPerTrack    = 16;
        int       sectorsPerCylinder = _doubleSided ? sectorsPerTrack * 2 : sectorsPerTrack;

        lba = (ulong)(cylinder * sectorsPerCylinder + side * sectorsPerTrack + (sector - 1));

        return true;
    }

    /// <summary>Parses an ASCII digit field into an integer</summary>
    /// <param name="field">ASCII digit bytes</param>
    /// <param name="defaultValue">Value to return if the field is all spaces</param>
    /// <returns>Parsed integer, or <paramref name="defaultValue" /> if the field is blank</returns>
    static int ParseAsciiNumber(byte[] field, int defaultValue)
    {
        if(field is null) return defaultValue;

        string str = Encoding.ASCII.GetString(field).Trim();

        if(string.IsNullOrEmpty(str)) return defaultValue;

        return int.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value)
                   ? value
                   : defaultValue;
    }

    /// <summary>Computes the logical file length from the extent and End of Data fields</summary>
    /// <param name="label">File label</param>
    /// <param name="startLba">First LBA of the extent</param>
    /// <param name="endLba">Last LBA of the extent</param>
    /// <param name="blockLength">Block length from the file label</param>
    /// <returns>File length in bytes</returns>
    long ComputeFileLength(in FileLabel label, ulong startLba, ulong endLba, int blockLength)
    {
        // If there is a valid End of Data address, it points to the first unused block after data.
        // The data fills from startLba up to (but not including) that address.
        if(!TryParseExtentAddress(label.endOfData, out ulong eodLba) || eodLba <= startLba)
            return (long)(endLba - startLba + 1) * PHYSICAL_RECORD_LENGTH_DATA;

        var sectors = (long)(eodLba - startLba);

        int offsetToNext = ParseAsciiNumber(label.offsetToNextRecordSpace, 0);

        // Each sector holds blockLength bytes of data (rest is NUL padding)
        // The last block may have unused positions indicated by offsetToNextRecordSpace
        long fullBlocks        = sectors * PHYSICAL_RECORD_LENGTH_DATA;
        long dataInLastBlock   = blockLength - offsetToNext;
        long dataInEarlier     = (sectors - 1) * blockLength;
        long dataFromBlockCalc = dataInEarlier + dataInLastBlock;

        // Use the smaller of the two calculations as a sanity bound
        return Math.Min(fullBlocks, dataFromBlockCalc > 0 ? dataFromBlockCalc : fullBlocks);
    }

    /// <summary>Parses a 6-byte ECMA-67 date field (YYMMDD) into a DateTime</summary>
    /// <param name="date">6-byte ASCII date field</param>
    /// <returns>Parsed DateTime in UTC, or <c>null</c> if the field is blank or invalid</returns>
    static DateTime? ParseEcma67Date(byte[] date)
    {
        if(date is null || date.Length < 6) return null;

        string str = Encoding.ASCII.GetString(date).Trim();

        if(string.IsNullOrEmpty(str)) return null;

        if(str.Length < 6) return null;

        if(!int.TryParse(str[..2],  NumberStyles.Integer, CultureInfo.InvariantCulture, out int year)  ||
           !int.TryParse(str[2..4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int month) ||
           !int.TryParse(str[4..6], NumberStyles.Integer, CultureInfo.InvariantCulture, out int day))
            return null;

        if(month is < 1 or > 12 || day is < 1 or > 31) return null;

        // Two-digit year: 00-99. Assume 1900s for this 1981-era standard.
        year += 1900;

        try
        {
            return new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc);
        }
        catch(ArgumentOutOfRangeException)
        {
            return null;
        }
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        // Root directory
        if(pathElements.Length == 0   ||
           string.IsNullOrEmpty(path) ||
           string.Equals(path, "/", StringComparison.OrdinalIgnoreCase))
        {
            stat = new FileEntryInfo
            {
                Attributes = FileAttributes.Directory,
                BlockSize  = PHYSICAL_RECORD_LENGTH_DATA,
                Links      = 1
            };

            return ErrorNumber.NoError;
        }

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        string fileName = pathElements[0];

        if(!_fileLabels.TryGetValue(fileName, out FileLabel label)) return ErrorNumber.NoSuchFile;

        if(!TryParseExtentAddress(label.beginOfExtent, out ulong startLba) ||
           !TryParseExtentAddress(label.endOfExtent,   out ulong endLba))
            return ErrorNumber.InvalidArgument;

        int  blockLength = ParseAsciiNumber(label.blockLength, PHYSICAL_RECORD_LENGTH_DATA);
        long fileLength  = ComputeFileLength(label, startLba, endLba, blockLength);
        long blocks      = (fileLength + PHYSICAL_RECORD_LENGTH_DATA - 1) / PHYSICAL_RECORD_LENGTH_DATA;

        FileAttributes attributes = FileAttributes.File;

        if(label.writeProtect == WRITE_PROTECT_YES) attributes |= FileAttributes.ReadOnly;

        stat = new FileEntryInfo
        {
            Attributes = attributes,
            BlockSize  = PHYSICAL_RECORD_LENGTH_DATA,
            Length     = fileLength,
            Blocks     = blocks,
            Links      = 1
        };

        // Parse creation date (YYMMDD) if present
        DateTime? creationDate = ParseEcma67Date(label.creationDate);

        if(creationDate.HasValue) stat.CreationTimeUtc = creationDate.Value;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        string fileName = pathElements[0];

        if(!_fileLabels.TryGetValue(fileName, out FileLabel label)) return ErrorNumber.NoSuchFile;

        // Parse extent addresses from ASCII digit fields
        if(!TryParseExtentAddress(label.beginOfExtent, out ulong startLba) ||
           !TryParseExtentAddress(label.endOfExtent,   out ulong endLba))
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid extent address for file \"{0}\"", fileName);

            return ErrorNumber.InvalidArgument;
        }

        // Parse block length from the 5-digit ASCII field
        int blockLength = ParseAsciiNumber(label.blockLength, PHYSICAL_RECORD_LENGTH_DATA);

        // Compute file length from extent size and End of Data field
        long fileLength = ComputeFileLength(label, startLba, endLba, blockLength);

        node = new Ecma67FileNode
        {
            Path     = path,
            Length   = fileLength,
            Offset   = 0,
            Label    = label,
            StartLba = startLba,
            EndLba   = endLba
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        return node is not Ecma67FileNode ? ErrorNumber.InvalidArgument : ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        if(node is not Ecma67FileNode myNode) return ErrorNumber.InvalidArgument;

        long toRead = length;

        if(myNode.Offset + toRead > myNode.Length) toRead = myNode.Length - myNode.Offset;

        if(toRead <= 0) return ErrorNumber.NoError;

        // Compute which sectors to read
        long firstSector    = myNode.Offset                                               / PHYSICAL_RECORD_LENGTH_DATA;
        long offsetInSector = myNode.Offset                                               % PHYSICAL_RECORD_LENGTH_DATA;
        long sectorsNeeded  = (toRead + offsetInSector + PHYSICAL_RECORD_LENGTH_DATA - 1) / PHYSICAL_RECORD_LENGTH_DATA;

        var ms = new MemoryStream();

        for(long i = 0; i < sectorsNeeded; i++)
        {
            ulong lba = myNode.StartLba + (ulong)(firstSector + i);

            if(lba > myNode.EndLba) break;

            ErrorNumber errno = _imagePlugin.ReadSector(lba, false, out byte[] sectorData, out _);

            if(errno != ErrorNumber.NoError) return errno;

            ms.Write(sectorData, 0, sectorData.Length);
        }

        ms.Position = offsetInSector;
        ms.EnsureRead(buffer, 0, (int)toRead);

        read          =  toRead;
        myNode.Offset += toRead;

        return ErrorNumber.NoError;
    }

#endregion
}