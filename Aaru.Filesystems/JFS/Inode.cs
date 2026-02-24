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

using System;
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

    /// <summary>Reads a fileset inode by its inode number using the FILESYSTEM_I xtree and IAGs</summary>
    /// <param name="inodeNumber">The fileset inode number</param>
    /// <param name="inode">The read inode</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadFilesetInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        // Determine the IAG number for this inode
        var iagno = (int)(inodeNumber / INOSPERIAG);

        // IAG pages are at logical block (iagno + 1) << l2nbperpage in the FILESYSTEM_I xtree
        long iagLogicalBlock = (long)(iagno + 1) << _l2nbperpage;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadFilesetInode: inode={0}, iagno={1}, iagLogicalBlock={2}",
                          inodeNumber,
                          iagno,
                          iagLogicalBlock);

        ErrorNumber errno = XTreeLookup(_fsInode.di_u, false, iagLogicalBlock, out long iagPhysicalBlock);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFilesetInode: Error looking up IAG {0}: {1}", iagno, errno);

            return errno;
        }

        // Read the IAG page
        errno = ReadFsBlock(iagPhysicalBlock, out byte[] iagData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFilesetInode: Error reading IAG page: {0}", errno);

            return errno;
        }

        InodeAllocationGroup iag = Marshal.ByteArrayToStructureLittleEndian<InodeAllocationGroup>(iagData);

        // Find the inode extent containing our inode
        var inoInIag = (int)(inodeNumber & INOSPERIAG - 1);
        int extno    = inoInIag >> L2INOSPEREXT;

        Extent inodeExtent = iag.inoext[extno];

        ulong extAddr = ExtentAddress(inodeExtent);
        uint  extLen  = ExtentLength(inodeExtent);

        if(extAddr == 0 || extLen == 0)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "ReadFilesetInode: inode extent not backed (addr={0}, len={1})",
                              extAddr,
                              extLen);

            return ErrorNumber.NoSuchFile;
        }

        // Calculate the block number and offset within the page
        int  pageInExtent = (inoInIag & INOSPEREXT - 1) >> L2INOSPERPAGE;
        long blkno        = (long)extAddr + (pageInExtent << _l2nbperpage);
        int  relInode     = inoInIag & INOSPERPAGE - 1;

        AaruLogging.Debug(MODULE_NAME,
                          "ReadFilesetInode: extAddr={0}, pageInExtent={1}, blkno={2}, relInode={3}",
                          extAddr,
                          pageInExtent,
                          blkno,
                          relInode);

        // Read the 4K page containing the inode
        errno = ReadBytes(blkno * _superblock.s_bsize, PSIZE, out byte[] inodePage);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFilesetInode: Error reading inode page: {0}", errno);

            return errno;
        }

        // Extract the inode from the page
        int inodeOffset = relInode * DISIZE;

        if(inodeOffset + DISIZE > inodePage.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "ReadFilesetInode: inode offset {0} exceeds page", inodeOffset);

            return ErrorNumber.InvalidArgument;
        }

        var inodeData = new byte[DISIZE];
        Array.Copy(inodePage, inodeOffset, inodeData, 0, DISIZE);

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

        AaruLogging.Debug(MODULE_NAME,
                          "ReadFilesetInode: read inode {0}: di_number={1}, di_mode=0x{2:X8}, di_size={3}",
                          inodeNumber,
                          inode.di_number,
                          inode.di_mode,
                          inode.di_size);

        return ErrorNumber.NoError;
    }
}