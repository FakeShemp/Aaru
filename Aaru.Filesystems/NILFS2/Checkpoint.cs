// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Checkpoint.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : NILFS2 filesystem plugin.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the New Implementation of a Log-structured File System v2</summary>
public sealed partial class NILFS2
{
    /// <summary>Reads the latest checkpoint from the checkpoint file</summary>
    /// <param name="cpfileInode">The checkpoint file inode from the super root</param>
    /// <param name="cno">Checkpoint number to read (from the segment summary that contained the super root)</param>
    /// <param name="checkpoint">Output checkpoint structure</param>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadLatestCheckpoint(in Inode cpfileInode, ulong cno, out Checkpoint checkpoint)
    {
        checkpoint = default(Checkpoint);

        AaruLogging.Debug(MODULE_NAME, "Reading checkpoint {0} from cpfile...", cno);

        // The cpfile is a simple metadata file (not palloc).
        // entries_per_block = block_size / checkpoint_size
        // first_entry_offset = ceil(sizeof(CpFileHeader) / checkpoint_size)
        // This accounts for the header taking up the first "slot(s)" in block 0.
        uint checkpointSize   = _superblock.checkpoint_size;
        uint entriesPerBlock  = _blockSize / checkpointSize;
        var  cpFileHeaderSize = (uint)Marshal.SizeOf<CpFileHeader>();
        uint firstEntryOffset = (cpFileHeaderSize + checkpointSize - 1) / checkpointSize;

        ulong tcno          = cno + firstEntryOffset - 1;
        ulong logicalBlock  = tcno / entriesPerBlock;
        var   offsetInBlock = (uint)(tcno % entriesPerBlock * checkpointSize);

        AaruLogging.Debug(MODULE_NAME,
                          "Checkpoint file: entriesPerBlock={0}, firstEntryOffset={1}, logicalBlock={2}, offset={3}",
                          entriesPerBlock,
                          firstEntryOffset,
                          logicalBlock,
                          offsetInBlock);

        // The cpfile bmap uses virtual block numbers (NILFS_BMAP_PTR_VS in kernel)
        // Only the DAT uses physical block numbers - cpfile needs DAT translation
        ErrorNumber errno = ReadLogicalBlock(cpfileInode, logicalBlock, false, out byte[] blockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading cpfile block: {0}", errno);

            return errno;
        }

        int cpStructSize = Marshal.SizeOf<Checkpoint>();

        if(offsetInBlock + cpStructSize > blockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Checkpoint extends past block boundary");

            return ErrorNumber.InvalidArgument;
        }

        checkpoint = Marshal.ByteArrayToStructureLittleEndian<Checkpoint>(blockData, (int)offsetInBlock, cpStructSize);

        if((checkpoint.flags & CheckpointFlags.Invalid) != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Checkpoint {0} is marked invalid", cno);

            return ErrorNumber.InvalidArgument;
        }

        if(checkpoint.cno != cno)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Checkpoint number mismatch: expected {0}, got {1} (data may be stale or corrupt)",
                              cno,
                              checkpoint.cno);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Checkpoint {0}: inodes={1}, blocks={2}",
                          checkpoint.cno,
                          checkpoint.inodes_count,
                          checkpoint.blocks_count);

        return ErrorNumber.NoError;
    }
}