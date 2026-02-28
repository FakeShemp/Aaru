// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Veritas File System plugin.
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <summary>Reads an inode from the inode list</summary>
    /// <param name="inode">Inode number to read</param>
    /// <param name="diskInode">The read inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInode(uint inode, out DiskInode diskInode)
    {
        diskInode = default(DiskInode);

        int blockSize = _superblock.vs_bsize;

        // Calculate which block contains this inode and the offset within the block
        // From Linux: block = extent + ((ino * VXFS_ISIZE) / blocksize)
        //             offset = ((ino % (blocksize / VXFS_ISIZE)) * VXFS_ISIZE)
        uint inodeBlock  = _ilistExtent + (uint)(inode * VXFS_ISIZE / blockSize);
        var  inodeOffset = (int)(inode                              % (blockSize / VXFS_ISIZE) * VXFS_ISIZE);

        // Convert filesystem block to sector
        long  blockByteOffset = inodeBlock             * blockSize;
        ulong sectorOff       = (ulong)blockByteOffset / _imagePlugin.Info.SectorSize;
        var   byteOff         = (uint)((ulong)blockByteOffset % _imagePlugin.Info.SectorSize);

        int  inodeSize       = System.Runtime.InteropServices.Marshal.SizeOf<DiskInode>();
        uint totalByteOffset = byteOff + (uint)inodeOffset;

        var sectorsToRead = (uint)((totalByteOffset + inodeSize + _imagePlugin.Info.SectorSize - 1) /
                                   _imagePlugin.Info.SectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sectorOff,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] blockData,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(totalByteOffset + inodeSize > blockData.Length) return ErrorNumber.InvalidArgument;

        var inodeBytes = new byte[inodeSize];
        Array.Copy(blockData, totalByteOffset, inodeBytes, 0, inodeSize);

        diskInode = _bigEndian
                        ? Marshal.ByteArrayToStructureBigEndian<DiskInode>(inodeBytes)
                        : Marshal.ByteArrayToStructureLittleEndian<DiskInode>(inodeBytes);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads an inode from a parent inode's data blocks (e.g., structural ilist inode)</summary>
    /// <param name="parentInode">The parent inode whose data blocks contain the inode table</param>
    /// <param name="inode">Inode number to read</param>
    /// <param name="diskInode">The read inode</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadInodeFromInode(DiskInode parentInode, uint inode, out DiskInode diskInode)
    {
        diskInode = default(DiskInode);

        int blockSize = _superblock.vs_bsize;

        // Calculate the byte offset of the inode within the parent's data
        long inodeByteOffset = (long)inode * VXFS_ISIZE;
        int  inodeSize       = System.Runtime.InteropServices.Marshal.SizeOf<DiskInode>();

        // Read the parent inode's data to find the target inode
        // We need to read starting at the block containing our inode
        var blockNumber   = (int)(inodeByteOffset / blockSize);
        var offsetInBlock = (int)(inodeByteOffset % blockSize);

        // Read the specific block from the parent inode's extents
        byte[] blockData = ReadInodeBlock(parentInode, (uint)blockNumber);

        if(blockData == null)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Could not read block {0} from parent inode for inode {1}",
                              blockNumber,
                              inode);

            return ErrorNumber.InvalidArgument;
        }

        if(offsetInBlock + inodeSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Inode {0} offset {1} + size {2} exceeds block data length {3}",
                              inode,
                              offsetInBlock,
                              inodeSize,
                              blockData.Length);

            return ErrorNumber.InvalidArgument;
        }

        var inodeBytes = new byte[inodeSize];
        Array.Copy(blockData, offsetInBlock, inodeBytes, 0, inodeSize);

        diskInode = _bigEndian
                        ? Marshal.ByteArrayToStructureBigEndian<DiskInode>(inodeBytes)
                        : Marshal.ByteArrayToStructureLittleEndian<DiskInode>(inodeBytes);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a specific logical block from an inode's data extents</summary>
    /// <param name="inode">The inode whose data to read</param>
    /// <param name="logicalBlock">The logical block number within the inode's data</param>
    /// <returns>The block data, or null on error</returns>
    byte[] ReadInodeBlock(DiskInode inode, uint logicalBlock)
    {
        int blockSize = _superblock.vs_bsize;

        switch((InodeOrgType)inode.vdi_orgtype)
        {
            case InodeOrgType.Immed:
            {
                // Immediate data - only valid if logicalBlock is 0 and data fits
                if(logicalBlock != 0 || inode.vdi_org == null) return null;

                return inode.vdi_org;
            }

            case InodeOrgType.Ext4:
            {
                if(inode.vdi_org == null || inode.vdi_org.Length < 96) return null;

                Ext4 ext4 = _bigEndian
                                ? Marshal.ByteArrayToStructureBigEndian<Ext4>(inode.vdi_org)
                                : Marshal.ByteArrayToStructureLittleEndian<Ext4>(inode.vdi_org);

                if(ext4.ve4_direct == null) return null;

                int  directExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<DirectExtent>();
                uint currentBlock     = 0;

                for(var i = 0; i < VXFS_NDADDR; i++)
                {
                    int offset = i * directExtentSize;

                    if(offset + directExtentSize > ext4.ve4_direct.Length) break;

                    var extBytes = new byte[directExtentSize];
                    Array.Copy(ext4.ve4_direct, offset, extBytes, 0, directExtentSize);

                    DirectExtent extent = _bigEndian
                                              ? Marshal.ByteArrayToStructureBigEndian<DirectExtent>(extBytes)
                                              : Marshal.ByteArrayToStructureLittleEndian<DirectExtent>(extBytes);

                    if(extent.size == 0) continue;

                    if(logicalBlock >= currentBlock && logicalBlock < currentBlock + extent.size)
                    {
                        uint physBlock = extent.extent + (logicalBlock - currentBlock);

                        return ReadBlocks(physBlock, 1);
                    }

                    currentBlock += extent.size;
                }

                return null;
            }

            case InodeOrgType.Typed:
            {
                if(inode.vdi_org == null || inode.vdi_org.Length < 96) return null;

                int  typedExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<TypedExtent>();
                uint currentBlock    = 0;

                for(var i = 0; i < VXFS_NTYPED; i++)
                {
                    int offset = i * typedExtentSize;

                    if(offset + typedExtentSize > inode.vdi_org.Length) break;

                    var extBytes = new byte[typedExtentSize];
                    Array.Copy(inode.vdi_org, offset, extBytes, 0, typedExtentSize);

                    TypedExtent extent = _bigEndian
                                             ? Marshal.ByteArrayToStructureBigEndian<TypedExtent>(extBytes)
                                             : Marshal.ByteArrayToStructureLittleEndian<TypedExtent>(extBytes);

                    var extType = (byte)((extent.vt_hdr & VXFS_TYPED_TYPEMASK) >> VXFS_TYPED_TYPESHIFT);

                    if(extType != (byte)TypedExtentType.Data) continue;

                    if(extent.vt_size == 0) continue;

                    if(logicalBlock >= currentBlock && logicalBlock < currentBlock + extent.vt_size)
                    {
                        uint physBlock = extent.vt_block + (logicalBlock - currentBlock);

                        return ReadBlocks(physBlock, 1);
                    }

                    currentBlock += extent.vt_size;
                }

                return null;
            }

            default:
                return null;
        }
    }

    /// <summary>Reads filesystem blocks for a given inode's data</summary>
    /// <param name="inode">The disk inode</param>
    /// <returns>The raw data bytes, or null on error</returns>
    byte[] ReadInodeData(DiskInode inode)
    {
        int blockSize = _superblock.vs_bsize;

        switch((InodeOrgType)inode.vdi_orgtype)
        {
            case InodeOrgType.Immed:
            {
                // Data stored directly in inode (max 96 bytes)
                if(inode.vdi_org == null) return null;

                var dataLen = (int)Math.Min((long)inode.vdi_size, VXFS_NIMMED);
                var data    = new byte[dataLen];
                Array.Copy(inode.vdi_org, 0, data, 0, dataLen);

                return data;
            }

            case InodeOrgType.Ext4:
            {
                // Ext4 organisation: direct and indirect extents in vdi_org
                if(inode.vdi_org == null || inode.vdi_org.Length < 96) return null;

                // Parse the Ext4 structure from vdi_org
                Ext4 ext4 = _bigEndian
                                ? Marshal.ByteArrayToStructureBigEndian<Ext4>(inode.vdi_org)
                                : Marshal.ByteArrayToStructureLittleEndian<Ext4>(inode.vdi_org);

                // Read direct extents (stored as raw bytes within ve4_direct)
                if(ext4.ve4_direct == null) return null;

                List<byte> result           = new();
                int        directExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<DirectExtent>();
                var        remaining        = (long)inode.vdi_size;

                for(var i = 0; i < VXFS_NDADDR && remaining > 0; i++)
                {
                    int offset = i * directExtentSize;

                    if(offset + directExtentSize > ext4.ve4_direct.Length) break;

                    var extBytes = new byte[directExtentSize];
                    Array.Copy(ext4.ve4_direct, offset, extBytes, 0, directExtentSize);

                    DirectExtent extent = _bigEndian
                                              ? Marshal.ByteArrayToStructureBigEndian<DirectExtent>(extBytes)
                                              : Marshal.ByteArrayToStructureLittleEndian<DirectExtent>(extBytes);

                    if(extent.size == 0) continue;

                    byte[] extentData = ReadBlocks(extent.extent, extent.size);

                    if(extentData == null) return null;

                    var toTake  = (int)Math.Min(remaining, extentData.Length);
                    var trimmed = new byte[toTake];
                    Array.Copy(extentData, 0, trimmed, 0, toTake);
                    result.AddRange(trimmed);
                    remaining -= toTake;
                }

                return result.ToArray();
            }

            case InodeOrgType.Typed:
            {
                // Typed extents
                if(inode.vdi_org == null || inode.vdi_org.Length < 96) return null;

                int typedExtentSize = System.Runtime.InteropServices.Marshal.SizeOf<TypedExtent>();

                List<byte> result    = new();
                var        remaining = (long)inode.vdi_size;

                for(var i = 0; i < VXFS_NTYPED && remaining > 0; i++)
                {
                    int offset = i * typedExtentSize;

                    if(offset + typedExtentSize > inode.vdi_org.Length) break;

                    var extBytes = new byte[typedExtentSize];
                    Array.Copy(inode.vdi_org, offset, extBytes, 0, typedExtentSize);

                    TypedExtent extent = _bigEndian
                                             ? Marshal.ByteArrayToStructureBigEndian<TypedExtent>(extBytes)
                                             : Marshal.ByteArrayToStructureLittleEndian<TypedExtent>(extBytes);

                    var extType = (byte)((extent.vt_hdr & VXFS_TYPED_TYPEMASK) >> VXFS_TYPED_TYPESHIFT);

                    if(extType != (byte)TypedExtentType.Data) continue;

                    if(extent.vt_size == 0) continue;

                    byte[] extentData = ReadBlocks(extent.vt_block, extent.vt_size);

                    if(extentData == null) return null;

                    var toTake  = (int)Math.Min(remaining, extentData.Length);
                    var trimmed = new byte[toTake];
                    Array.Copy(extentData, 0, trimmed, 0, toTake);
                    result.AddRange(trimmed);
                    remaining -= toTake;
                }

                return result.ToArray();
            }

            default:
                AaruLogging.Debug(MODULE_NAME, "Unknown inode org type: {0}", inode.vdi_orgtype);

                return null;
        }
    }
}