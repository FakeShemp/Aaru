// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : IBM JFS filesystem plugin
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

using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's Journaled File System</summary>
public sealed partial class JFS
{
    /// <summary>Reads an aggregate inode from the fixed aggregate inode table</summary>
    /// <param name="inodeNumber">Aggregate inode number (0-31)</param>
    /// <param name="inode">The read inode</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadAggregateInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        // Aggregate inodes are at AITBL_OFF, 8 inodes per 4K page
        // Page = AITBL_OFF / PSIZE + inodeNumber / 8
        // Offset within page = (inodeNumber % 8) * DISIZE
        long pageByteOffset  = AITBL_OFF + inodeNumber / INOSPERPAGE * PSIZE;
        int  offsetInPage    = (int)(inodeNumber % INOSPERPAGE) * DISIZE;
        long inodeByteOffset = pageByteOffset + offsetInPage;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading aggregate inode {0} at byte offset 0x{1:X}",
                          inodeNumber,
                          inodeByteOffset);

        ErrorNumber errno = ReadBytes(inodeByteOffset, DISIZE, out byte[] inodeData);

        if(errno != ErrorNumber.NoError) return errno;

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

        return ErrorNumber.NoError;
    }
}