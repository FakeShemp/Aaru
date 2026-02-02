// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : BeOS filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of the Be (new) filesystem</summary>
public sealed partial class BeFS
{
    /// <summary>Reads an i-node from disk by its block_run address</summary>
    /// <remarks>
    ///     Locates and reads an i-node from disk given its allocation group and block address.
    ///     Validates the i-node magic number to ensure correctness.
    /// </remarks>
    /// <param name="inodeAddr">The block_run address of the i-node to read</param>
    /// <param name="inode">Output buffer containing the parsed i-node</param>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber ReadInode(block_run inodeAddr, out bfs_inode inode)
    {
        inode = default(bfs_inode);

        // Calculate absolute block address using ag_shift
        long blockAddress = ((long)inodeAddr.allocation_group << _superblock.ag_shift) + inodeAddr.start;

        long byteAddressInFS     = blockAddress * _superblock.block_size;
        uint sectorSize          = _imagePlugin.Info.SectorSize;
        long partitionByteOffset = (long)_partition.Start * sectorSize;

        long absoluteByteAddress = byteAddressInFS + partitionByteOffset;
        long sectorAddress       = absoluteByteAddress / sectorSize;

        var sectorsToRead = (uint)((_superblock.inode_size + sectorSize - 1) / sectorSize);

        AaruLogging.Debug(MODULE_NAME,
                          "ReadInode: AG={0}, start={1}, blockAddress={2}, byte_addr_fs=0x{3:X8}, part_offset=0x{4:X8}, absolute_byte=0x{5:X8}, sector={6}, sectors_to_read={7}",
                          inodeAddr.allocation_group,
                          inodeAddr.start,
                          blockAddress,
                          byteAddressInFS,
                          partitionByteOffset,
                          absoluteByteAddress,
                          sectorAddress,
                          sectorsToRead);

        // Read the i-node block
        ErrorNumber errno = _imagePlugin.ReadSectors((ulong)sectorAddress,
                                                     false,
                                                     sectorsToRead,
                                                     out byte[] inodeData,
                                                     out SectorStatus[] _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading i-node: {0}", errno);

            return errno;
        }

        // Marshal the i-node
        inode = _littleEndian
                    ? Marshal.ByteArrayToStructureLittleEndian<bfs_inode>(inodeData)
                    : Marshal.ByteArrayToStructureBigEndian<bfs_inode>(inodeData);

        // Validate i-node magic
        if(inode.magic1 != INODE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid i-node magic! Expected 0x{0:X8}, got 0x{1:X8}",
                              INODE_MAGIC,
                              inode.magic1);

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Finds a child entry in a directory and returns its i-node address</summary>
    /// <remarks>
    ///     Searches the B+tree of a directory for a specific filename and returns
    ///     the i-node address associated with that entry.
    /// </remarks>
    /// <param name="dirInode">The i-node of the directory to search</param>
    /// <param name="filename">The filename to search for</param>
    /// <param name="childInodeAddr">Output i-node address of the child entry</param>
    /// <returns>Error code indicating success or failure</returns>
    private ErrorNumber FindDirectoryEntry(bfs_inode dirInode, string filename, out long childInodeAddr)
    {
        childInodeAddr = 0;

        AaruLogging.Debug(MODULE_NAME, "Searching for entry '{0}' in directory", filename);

        // Read the B+tree header
        ErrorNumber errno = ReadFromDataStream(dirInode.data, 0, 32, out byte[] headerData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading B+tree header from directory: {0}", errno);

            return errno;
        }

        bt_header btHeader = _littleEndian
                                 ? Marshal.ByteArrayToStructureLittleEndian<bt_header>(headerData)
                                 : Marshal.ByteArrayToStructureBigEndian<bt_header>(headerData);

        if(btHeader.magic != BTREE_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid B+tree magic in directory");

            return ErrorNumber.InvalidArgument;
        }

        // Search the B+tree for the filename
        const long BEFS_BT_INVAL = unchecked((long)0xFFFFFFFFFFFFFFFF);
        long       nodeOffset    = btHeader.node_root_pointer;
        var        found         = false;

        while(!found)
        {
            errno = ReadFromDataStream(dirInode.data, nodeOffset, btHeader.node_size, out byte[] nodeData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading B+tree node: {0}", errno);

                return errno;
            }

            bt_node_hdr nodeHeader = _littleEndian
                                         ? Marshal.ByteArrayToStructureLittleEndian<bt_node_hdr>(nodeData)
                                         : Marshal.ByteArrayToStructureBigEndian<bt_node_hdr>(nodeData);

            bool isLeafNode = nodeHeader.overflow_link == BEFS_BT_INVAL;

            if(isLeafNode)
            {
                // Parse leaf node to find the entry
                const int headerSize          = 28;
                int       keyDataStart        = headerSize;
                int       keyDataEnd          = keyDataStart + nodeHeader.keys_length;
                int       keyLengthIndexStart = keyDataEnd + 3 & ~3;
                int       valueDataStart      = keyLengthIndexStart + nodeHeader.node_keys * 2;

                var keyOffset = 0;

                for(var i = 0; i < nodeHeader.node_keys; i++)
                {
                    int offsetIndex = keyLengthIndexStart + i * 2;

                    ushort keyEndOffset = _littleEndian
                                              ? BitConverter.ToUInt16(nodeData, offsetIndex)
                                              : BigEndianBitConverter.ToUInt16(nodeData, offsetIndex);

                    int keyStart  = keyDataStart + keyOffset;
                    int keyLength = keyEndOffset - keyOffset;

                    int valueIndex = valueDataStart + i * 8;

                    long inodeNumber = _littleEndian
                                           ? BitConverter.ToInt64(nodeData, valueIndex)
                                           : BigEndianBitConverter.ToInt64(nodeData, valueIndex);

                    string keyName = _encoding.GetString(nodeData, keyStart, keyLength);

                    if(keyName == filename)
                    {
                        childInodeAddr = inodeNumber;
                        found          = true;

                        AaruLogging.Debug(MODULE_NAME,
                                          "Found entry '{0}' with i-node address {1}",
                                          filename,
                                          inodeNumber);

                        break;
                    }

                    keyOffset = keyEndOffset;
                }

                if(!found)
                {
                    // Check right sibling
                    if(nodeHeader.right_link != 0 && nodeHeader.right_link != BEFS_BT_INVAL)
                        nodeOffset = nodeHeader.right_link;
                    else
                    {
                        AaruLogging.Debug(MODULE_NAME, "Entry '{0}' not found in directory", filename);

                        return ErrorNumber.NoSuchFile;
                    }
                }
            }
            else
            {
                // Interior node - navigate to appropriate child
                // For simplicity, start with root and follow overflow link
                if(nodeHeader.overflow_link != 0 && nodeHeader.overflow_link != BEFS_BT_INVAL)
                    nodeOffset = nodeHeader.overflow_link;
                else
                {
                    AaruLogging.Debug(MODULE_NAME, "Interior node has no overflow link");

                    return ErrorNumber.InvalidArgument;
                }
            }
        }

        return ErrorNumber.NoError;
    }
}