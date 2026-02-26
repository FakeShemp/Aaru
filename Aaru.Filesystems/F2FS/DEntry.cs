// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DEntry.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : F2FS filesystem plugin.
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
using System.Text;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <summary>Parses inline dentry from the inode's node block</summary>
    void ParseInlineDentry(Inode inode, byte[] nodeBlock)
    {
        // For inline dentry, the data is stored in the inode's i_addr area
        // The inline data starts after the extra attribute area (if present) + reserved word
        int inodeFixedSize = Marshal.SizeOf<Inode>() - DEF_ADDRS_PER_INODE * 4 - DEF_NIDS_PER_INODE * 4;

        var extraSize = 0;

        if((inode.i_inline & F2FS_EXTRA_ATTR) != 0 && inode.i_addr?.Length > 0)
        {
            // i_extra_isize is at the start of i_addr (first 2 bytes as LE16)
            extraSize = BitConverter.ToUInt16(nodeBlock, inodeFixedSize) / 4;
        }

        var inlineXattrAddrs = 0;

        if((inode.i_inline & F2FS_INLINE_XATTR) != 0 && (inode.i_inline & F2FS_EXTRA_ATTR) != 0)
        {
            // i_inline_xattr_size is at offset 2 within the extra area
            inlineXattrAddrs = BitConverter.ToUInt16(nodeBlock, inodeFixedSize + 2);
        }

        int totalAddrs     = DEF_ADDRS_PER_INODE - extraSize;
        int usableAddrs    = totalAddrs          - inlineXattrAddrs - 1; // -1 for DEF_INLINE_RESERVED_SIZE
        int inlineDataSize = usableAddrs * 4;

        // Inline data starts after the extra isize + reserved word
        int inlineDataOffset = inodeFixedSize + (extraSize + 1) * 4; // +1 for reserved

        AaruLogging.Debug(MODULE_NAME,
                          "Inline dentry: extraSize={0}, xattrAddrs={1}, dataSize={2}, offset={3}",
                          extraSize,
                          inlineXattrAddrs,
                          inlineDataSize,
                          inlineDataOffset);

        if(inlineDataOffset + inlineDataSize > nodeBlock.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Inline dentry data exceeds node block");

            return;
        }

        // Calculate inline dentry parameters
        // NR_INLINE_DENTRY = MAX_INLINE_DATA * 8 / ((SIZE_OF_DIR_ENTRY + F2FS_SLOT_LEN) * 8 + 1)
        // where SIZE_OF_DIR_ENTRY=11, F2FS_SLOT_LEN=8
        int nrInlineDentry = inlineDataSize * 8 / ((11 + 8) * 8 + 1);
        int bitmapSize     = (nrInlineDentry                    + 7) / 8;
        int reservedSize   = inlineDataSize - (11 + 8) * nrInlineDentry - bitmapSize;

        AaruLogging.Debug(MODULE_NAME,
                          "Inline: nrDentry={0}, bitmapSize={1}, reservedSize={2}",
                          nrInlineDentry,
                          bitmapSize,
                          reservedSize);

        int bitmapStart   = inlineDataOffset;
        int reservedEnd   = bitmapStart + bitmapSize + reservedSize;
        int dentryStart   = reservedEnd;
        int filenameStart = dentryStart + nrInlineDentry * 11;

        ParseDentryBitmapEntries(nodeBlock, bitmapStart, dentryStart, filenameStart, nrInlineDentry);
    }

    /// <summary>Parses regular (non-inline) dentry blocks from the inode's data blocks</summary>
    ErrorNumber ParseRegularDentry(Inode inode)
    {
        // Number of data blocks = ceil(i_size / blockSize)
        ulong nPages = (inode.i_size + _blockSize - 1) / _blockSize;

        AaruLogging.Debug(MODULE_NAME, "Reading {0} dentry blocks from root inode", nPages);

        // Get the extra isize in uint32 count
        var extraIsize = 0;

        if((inode.i_inline & F2FS_EXTRA_ATTR) != 0 && inode.i_addr?.Length > 0)
        {
            // i_extra_isize is stored as first u16 of i_addr, value is in bytes
            // We need the number of __le32 words it consumes
            extraIsize =  (int)(inode.i_addr[0] & 0xFFFF);
            extraIsize /= 4;
        }

        var xattrAddrs = 0;

        if((inode.i_inline & F2FS_INLINE_XATTR) != 0 &&
           (inode.i_inline & F2FS_EXTRA_ATTR)   != 0 &&
           inode.i_addr?.Length                 > 0)
            xattrAddrs = (int)(inode.i_addr[0] >> 16 & 0xFFFF);

        int addrsPerInode = DEF_ADDRS_PER_INODE - extraIsize - xattrAddrs;

        for(ulong n = 0; n < nPages; n++)
        {
            // Resolve the block address for this data page index
            ErrorNumber errno = ResolveDataBlock(inode, (uint)n, addrsPerInode, out uint dataBlockAddr);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error resolving data block {0}: {1}", n, errno);

                continue;
            }

            if(dataBlockAddr == 0) continue; // NULL_ADDR = hole

            // Read the dentry block
            errno = ReadBlock(dataBlockAddr, out byte[] dentryBlockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading dentry block at {0}: {1}", dataBlockAddr, errno);

                continue;
            }

            // Parse using the fixed block dentry layout
            ParseDentryBitmapEntries(dentryBlockData,
                                     0,
                                     SIZE_OF_DENTRY_BITMAP + SIZE_OF_RESERVED,
                                     SIZE_OF_DENTRY_BITMAP + SIZE_OF_RESERVED + NR_DENTRY_IN_BLOCK * 11,
                                     NR_DENTRY_IN_BLOCK);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Parses dentry entries from a bitmap/dentry/filename layout (used by both inline and regular dentry blocks)
    /// </summary>
    void ParseDentryBitmapEntries(byte[] data, int bitmapOffset, int dentryOffset, int filenameOffset, int maxDentries)
    {
        var bitPos = 0;

        while(bitPos < maxDentries)
        {
            // Find next set bit in bitmap
            bitPos = FindNextBitLE(data, bitmapOffset, maxDentries, bitPos);

            if(bitPos >= maxDentries) break;

            // Read the directory entry at this position
            int deOffset = dentryOffset + bitPos * 11; // SIZE_OF_DIR_ENTRY = 11

            if(deOffset + 11 > data.Length) break;

            var ino     = BitConverter.ToUInt32(data, deOffset + 4);
            var nameLen = BitConverter.ToUInt16(data, deOffset + 8);

            if(nameLen == 0 || nameLen > F2FS_NAME_LEN)
            {
                bitPos++;

                continue;
            }

            // Read filename
            int fnOffset = filenameOffset + bitPos * 8; // F2FS_SLOT_LEN = 8

            if(fnOffset + nameLen > data.Length)
            {
                bitPos++;

                continue;
            }

            string fileName = Encoding.UTF8.GetString(data, fnOffset, nameLen);

            if(!string.IsNullOrEmpty(fileName) &&
               fileName is not ("." or "..")   &&
               !_rootDirectoryCache.ContainsKey(fileName))
                _rootDirectoryCache[fileName] = ino;

            // Advance past the slots used by this entry
            int slots = (nameLen + 8 - 1) / 8; // GET_DENTRY_SLOTS

            bitPos += slots;
        }
    }
}