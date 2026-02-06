// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : File.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : RT-11 file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Identifies the RT-11 file system and shows information.
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
using System.Runtime.InteropServices;
using Aaru.CommonTypes.Enums;

namespace Aaru.Filesystems;

// Information from http://www.trailing-edge.com/~shoppa/rt11fs/
/// <inheritdoc />
public sealed partial class RT11
{
    /// <summary>Gets the file length from cached directory information</summary>
    /// <param name="filename">Filename</param>
    /// <param name="fileLength">Output file length in blocks</param>
    /// <returns>Error code</returns>
    ErrorNumber GetFileLengthFromCache(string filename, out uint fileLength)
    {
        fileLength = 0;

        if(!_rootDirectoryCache.ContainsKey(filename)) return ErrorNumber.NoSuchFile;

        // Read the first directory segment to get file length
        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + _firstDirectoryBlock,
                                                     false,
                                                     2,
                                                     out byte[] dirSegmentData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse directory segment header
        DirectorySegmentHeader segmentHeader =
            Marshal.PtrToStructure<DirectorySegmentHeader>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, 0));

        // Directory entries start after the 5-word header (10 bytes)
        var offset    = 10;
        int entrySize = DIRECTORY_ENTRY_WORDS * 2 + segmentHeader.extraBytesPerEntry;

        while(offset + entrySize <= dirSegmentData.Length)
        {
            DirectoryEntry entry =
                Marshal.PtrToStructure<DirectoryEntry>(Marshal.UnsafeAddrOfPinnedArrayElement(dirSegmentData, offset));

            // Check for end-of-segment marker
            if((entry.status & 0xFF00) == E_EOS) break;

            // Process permanent files only
            if((entry.status & 0xFF00) == E_PERM)
            {
                string entryFilename = DecodeRadix50Filename(entry.filename1, entry.filename2, entry.filetype);

                if(string.Equals(entryFilename, filename, StringComparison.OrdinalIgnoreCase))
                {
                    fileLength = entry.length;

                    return ErrorNumber.NoError;
                }
            }

            offset += entrySize;
        }

        return ErrorNumber.NoSuchFile;
    }
}