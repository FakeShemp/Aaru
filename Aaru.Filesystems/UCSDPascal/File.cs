// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : U.C.S.D. Pascal filesystem plugin.
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
using System.IO;
using System.Linq;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using FileAttributes = Aaru.CommonTypes.Structs.FileAttributes;

namespace Aaru.Filesystems;

// Information from Call-A.P.P.L.E. Pascal Disk Directory Structure
public sealed partial class PascalPlugin
{
    /// <summary>DLE character used for space compression in UCSD Pascal text files</summary>
    const byte DLE = 0x10;

    /// <summary>Size of the optional text editor header in UCSD Pascal text files</summary>
    const int TEXT_EDITOR_HEADER_SIZE = 1024;

    ErrorNumber GetFileEntry(string path, out PascalFileEntry entry)
    {
        entry = new PascalFileEntry();

        foreach(PascalFileEntry ent in _fileEntries.Where(ent => string.Equals(path,
                                                                               StringHandlers
                                                                                  .PascalToString(ent.Filename,
                                                                                       _encoding),
                                                                               StringComparison
                                                                                  .InvariantCultureIgnoreCase)))
        {
            entry = ent;

            return ErrorNumber.NoError;
        }

        return ErrorNumber.NoSuchFile;
    }

    /// <summary>
    ///     Calculates the accurate file size for a UCSD Pascal file entry.
    ///     Size = (LastBlock - FirstBlock - 1) * BlockSize + LastBytes
    ///     LastBytes ranges from 1-512; a value of 0 is treated as 512 (full block).
    /// </summary>
    /// <param name="entry">The file entry</param>
    /// <returns>File size in bytes</returns>
    long GetFileSize(PascalFileEntry entry)
    {
        // Handle edge case: if FirstBlock >= LastBlock, file is empty
        if(entry.FirstBlock >= entry.LastBlock) return 0;

        long blockSize = _device.Info.SectorSize * _multiplier;

        // LastBytes should be 1-512; 0 is invalid but treated as 512 (full block used)
        int lastBytes = entry.LastBytes;

        if(lastBytes <= 0 || lastBytes > 512) lastBytes = 512;

        return (entry.LastBlock - entry.FirstBlock - 1) * blockSize + lastBytes;
    }

    /// <summary>
    ///     Decodes UCSD Pascal text format to standard text format.
    ///     UCSD Pascal text files use:
    ///     - An optional 1KB header used by the text editor (skipped if present)
    ///     - DLE (0x10) followed by a byte for space compression (value - 32 = spaces to column)
    ///     - CR (0x0D) as line terminator, converted to LF (0x0A)
    ///     - NUL characters as padding (discarded)
    /// </summary>
    /// <param name="rawData">Raw UCSD Pascal text file data</param>
    /// <returns>Decoded text as byte array</returns>
    static byte[] DecodeUcsdText(byte[] rawData)
    {
        if(rawData == null || rawData.Length == 0) return rawData;

        using var output = new MemoryStream();
        var       offset = 0;

        // Check if the file starts with a 1KB text editor header
        // The header is binary data, so we detect it by checking if the first bytes are not text characters
        if(rawData.Length >= TEXT_EDITOR_HEADER_SIZE && !IsTextBuffer(rawData, 0, Math.Min(16, rawData.Length)))
            offset = TEXT_EDITOR_HEADER_SIZE;

        var column   = 0;
        var dleSeen  = false;
        var nonWhite = false;

        while(offset < rawData.Length)
        {
            byte c = rawData[offset++];

            if(dleSeen)
            {
                dleSeen = false;

                if(c < 32)
                {
                    // Invalid DLE sequence, treat DLE as normal character
                    offset--;
                    c = DLE;
                }
                else
                {
                    // DLE compression: value - 32 = target column position
                    int targetColumn = c - 32;

                    if(nonWhite)
                    {
                        // Insert spaces to reach target column
                        for(int i = column; i < column + targetColumn; i++) output.WriteByte((byte)' ');
                    }

                    column += targetColumn;

                    continue;
                }
            }

            switch(c)
            {
                case 0x00:
                    // NUL - padding, ignore
                    break;

                case 0x0D: // CR
                case 0x0A: // LF
                    output.WriteByte((byte)'\n');
                    column   = 0;
                    nonWhite = false;

                    break;

                case DLE:
                    dleSeen = true;

                    break;

                default:
                    if(!nonWhite)
                    {
                        // Output leading spaces
                        for(var i = 0; i < column; i++) output.WriteByte((byte)' ');

                        nonWhite = true;
                    }

                    output.WriteByte(c);
                    column++;

                    break;
            }
        }

        return output.ToArray();
    }

    /// <summary>Checks if a buffer contains text characters (printable ASCII, whitespace, or DLE)</summary>
    /// <param name="buffer">Buffer to check</param>
    /// <param name="offset">Offset in buffer</param>
    /// <param name="length">Number of bytes to check</param>
    /// <returns>True if buffer appears to be text</returns>
    static bool IsTextBuffer(byte[] buffer, int offset, int length)
    {
        // Skip trailing NUL padding
        while(length > 0 && buffer[offset + length - 1] == 0) length--;

        for(var i = 0; i < length; i++)
        {
            byte c = buffer[offset + i];

            // Allow printable ASCII, whitespace, and DLE
            if(c is not (>= 0x20 and <= 0x7E or 0x09 or 0x0A or 0x0D or DLE or 0x00)) return false;
        }

        return true;
    }

#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public ErrorNumber OpenFile(string path, out IFileNode node)
    {
        node = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        byte[] file;
        var    isTextFile = false;

        if(_debug &&
           (string.Equals(path, "$",     StringComparison.InvariantCulture) ||
            string.Equals(path, "$Boot", StringComparison.InvariantCulture)))
            file = string.Equals(path, "$", StringComparison.InvariantCulture) ? _catalogBlocks : _bootBlocks;
        else
        {
            ErrorNumber error = GetFileEntry(path, out PascalFileEntry entry);

            if(error != ErrorNumber.NoError) return error;

            isTextFile = entry.EntryType == PascalFileKind.Text;

            error = _device.ReadSectors((ulong)entry.FirstBlock * _multiplier,
                                        false,
                                        (uint)(entry.LastBlock - entry.FirstBlock) * _multiplier,
                                        out byte[] tmp,
                                        out _);

            if(error != ErrorNumber.NoError) return error;

            long fileSize = GetFileSize(entry);
            file = new byte[fileSize];

            Array.Copy(tmp, 0, file, 0, file.Length);

            // Decode text file if option is enabled and file is a text file
            if(_decodeText && isTextFile) file = DecodeUcsdText(file);
        }

        node = new PascalFileNode
        {
            Path   = path,
            Length = file.Length,
            Offset = 0,
            Cache  = file
        };

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber CloseFile(IFileNode node)
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        if(node is not PascalFileNode mynode) return ErrorNumber.InvalidArgument;

        mynode.Cache = null;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber ReadFile(IFileNode node, long length, byte[] buffer, out long read)
    {
        read = 0;

        if(!_mounted) return ErrorNumber.AccessDenied;

        if(buffer is null || buffer.Length < length) return ErrorNumber.InvalidArgument;

        if(node is not PascalFileNode mynode) return ErrorNumber.InvalidArgument;

        read = length;

        if(length + mynode.Offset >= mynode.Length) read = mynode.Length - mynode.Offset;

        Array.Copy(mynode.Cache, mynode.Offset, buffer, 0, read);
        mynode.Offset += read;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Stat(string path, out FileEntryInfo stat)
    {
        stat = null;

        string[] pathElements = path.Split(['/'], StringSplitOptions.RemoveEmptyEntries);

        if(pathElements.Length != 1) return ErrorNumber.NotSupported;

        if(_debug)
        {
            if(string.Equals(path, "$",     StringComparison.InvariantCulture) ||
               string.Equals(path, "$Boot", StringComparison.InvariantCulture))
            {
                stat = new FileEntryInfo
                {
                    Attributes = FileAttributes.System,
                    BlockSize  = _device.Info.SectorSize * _multiplier,
                    Links      = 1
                };

                if(string.Equals(path, "$", StringComparison.InvariantCulture))
                {
                    stat.Blocks = _catalogBlocks.Length / stat.BlockSize + _catalogBlocks.Length % stat.BlockSize;

                    stat.Length = _catalogBlocks.Length;
                }
                else
                {
                    stat.Blocks = _bootBlocks.Length / stat.BlockSize + _catalogBlocks.Length % stat.BlockSize;
                    stat.Length = _bootBlocks.Length;
                }

                return ErrorNumber.NoError;
            }
        }

        ErrorNumber error = GetFileEntry(path, out PascalFileEntry entry);

        if(error != ErrorNumber.NoError) return error;

        stat = new FileEntryInfo
        {
            Attributes       = FileAttributes.File,
            Blocks           = entry.LastBlock - entry.FirstBlock,
            BlockSize        = _device.Info.SectorSize * _multiplier,
            LastWriteTimeUtc = DateHandlers.UcsdPascalToDateTime(entry.ModificationTime),
            Length           = GetFileSize(entry),
            Links            = 1
        };

        return ErrorNumber.NoError;
    }

#endregion
}