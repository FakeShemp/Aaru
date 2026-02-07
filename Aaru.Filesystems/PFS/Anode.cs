// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Anode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Professional File System plugin.
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
public sealed partial class PFS
{
    /// <summary>Gets an anode by number</summary>
    /// <param name="anodeNr">The anode number</param>
    /// <param name="anode">The anode data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber GetAnode(uint anodeNr, out Anode anode)
    {
        anode = default(Anode);

        // Calculate which anode block contains this anode
        ushort seqNr;
        ushort anodeOffset;

        if(_splitAnodeMode)
        {
            // In split mode, high 16 bits = seqnr, low 16 bits = offset
            seqNr       = (ushort)(anodeNr >> 16);
            anodeOffset = (ushort)(anodeNr & 0xFFFF);
        }
        else
        {
            // In normal mode, divide by anodes per block
            seqNr       = (ushort)(anodeNr / _anodesPerBlock);
            anodeOffset = (ushort)(anodeNr % _anodesPerBlock);
        }

        AaruLogging.Debug(MODULE_NAME,
                          "GetAnode: anodeNr={0}, seqNr={1}, anodeOffset={2}, splitMode={3}, anodesPerBlock={4}",
                          anodeNr,
                          seqNr,
                          anodeOffset,
                          _splitAnodeMode,
                          _anodesPerBlock);

        // Get the anode block
        ErrorNumber errno = GetAnodeBlock(seqNr, out byte[] anodeBlockData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "GetAnode: GetAnodeBlock failed with {0}", errno);

            return errno;
        }

        // Parse anode block header
        var blockId = BigEndianBitConverter.ToUInt16(anodeBlockData, 0);

        if(blockId != ABLKID)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid anode block ID: 0x{0:X4}", blockId);

            return ErrorNumber.InvalidArgument;
        }

        // Anodes start at offset 16 (after header)
        int anodeDataOffset = 16 + anodeOffset * 12; // Each anode is 12 bytes

        if(anodeDataOffset + 12 > anodeBlockData.Length)
        {
            AaruLogging.Debug(MODULE_NAME, "Anode offset out of bounds");

            return ErrorNumber.InvalidArgument;
        }

        anode.clustersize = BigEndianBitConverter.ToUInt32(anodeBlockData, anodeDataOffset);
        anode.blocknr     = BigEndianBitConverter.ToUInt32(anodeBlockData, anodeDataOffset + 4);
        anode.next        = BigEndianBitConverter.ToUInt32(anodeBlockData, anodeDataOffset + 8);

        return ErrorNumber.NoError;
    }

    /// <summary>Gets an anode block by sequence number</summary>
    /// <param name="seqNr">The sequence number</param>
    /// <param name="data">The anode block data</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber GetAnodeBlock(ushort seqNr, out byte[] data)
    {
        data = null;

        // Need to look up the anode block in the index
        // For small disks, indexblocks are in rootblock.idx.small.indexblocks
        // For large disks, we need to use bitmap index blocks

        // Calculate which index block contains this anode block reference
        int indexPerBlock = (_reservedBlockSize - 12) / 4; // 12 = IndexBlock header, 4 = sizeof(LONG)

        int indexBlockNr = seqNr / indexPerBlock;
        int indexOffset  = seqNr % indexPerBlock;

        AaruLogging.Debug(MODULE_NAME,
                          "GetAnodeBlock: seqNr={0}, indexPerBlock={1}, indexBlockNr={2}, indexOffset={3}",
                          seqNr,
                          indexPerBlock,
                          indexBlockNr,
                          indexOffset);

        // Get the index block number from rootblock
        uint indexBlockAddr;

        if(_modeFlags.HasFlag(ModeFlags.SuperIndex) && _hasExtension)
        {
            // Super index mode - need to go through superindex blocks
            int superIndexNr     = indexBlockNr / indexPerBlock;
            int superIndexOffset = indexBlockNr % indexPerBlock;

            AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: SuperIndex mode, superIndexNr={0}", superIndexNr);

            if(superIndexNr > MAXSUPER || _rootBlockExtension.superindex == null)
            {
                AaruLogging.Debug(MODULE_NAME, "Super index out of bounds: {0}", superIndexNr);

                return ErrorNumber.InvalidArgument;
            }

            uint superBlockAddr = _rootBlockExtension.superindex[superIndexNr];

            if(superBlockAddr == 0)
            {
                AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: superBlockAddr is 0");

                return ErrorNumber.InvalidArgument;
            }

            // Read super block
            ErrorNumber errno = ReadReservedBlock(superBlockAddr, out byte[] superBlockData);

            if(errno != ErrorNumber.NoError) return errno;

            // Get index block address from super block
            int superOffset = 12 + superIndexOffset * 4;
            indexBlockAddr = BigEndianBitConverter.ToUInt32(superBlockData, superOffset);
        }
        else
        {
            // Small disk mode - index blocks are in rootblock
            // Read the index array from after the fixed rootblock data
            // The index starts at offset 100 (size of fixed rootblock fields)
            // For small disk: 5 bitmap index + 99 anode index blocks

            AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: Small disk mode, indexBlockNr={0}", indexBlockNr);

            if(indexBlockNr > MAXSMALLINDEXNR)
            {
                AaruLogging.Debug(MODULE_NAME, "Index block number out of bounds: {0}", indexBlockNr);

                return ErrorNumber.InvalidArgument;
            }

            // Re-read rootblock to get the full index array
            uint sectorsPerReservedBlock = _reservedBlockSize / _imagePlugin.Info.SectorSize;

            ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + ROOTBLOCK,
                                                         false,
                                                         sectorsPerReservedBlock,
                                                         out byte[] fullRootBlock,
                                                         out _);

            if(errno != ErrorNumber.NoError) return errno;

            // Offset to small.indexblocks = 96 (fixed rootblock) + 20 (5 bitmapindex entries)
            // Fixed rootblock: diskType(4) + options(4) + datestamp(4) + creationday(2) + creationminute(2) +
            //                  creationtick(2) + protection(2) + diskname(32) + lastreserved(4) + firstreserved(4) +
            //                  reserved_free(4) + reserved_blksize(2) + rblkcluster(2) + blocksfree(4) + alwaysfree(4) +
            //                  roving_ptr(4) + deldir(4) + disksize(4) + extension(4) + not_used(4) = 96 bytes
            int indexArrayOffset = 96 + 20 + indexBlockNr * 4;

            AaruLogging.Debug(MODULE_NAME,
                              "GetAnodeBlock: indexArrayOffset={0}, fullRootBlock.Length={1}",
                              indexArrayOffset,
                              fullRootBlock.Length);

            indexBlockAddr = BigEndianBitConverter.ToUInt32(fullRootBlock, indexArrayOffset);

            AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: indexBlockAddr={0}", indexBlockAddr);
        }

        if(indexBlockAddr == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: indexBlockAddr is 0");

            return ErrorNumber.InvalidArgument;
        }

        // Read the index block
        ErrorNumber err = ReadReservedBlock(indexBlockAddr, out byte[] indexBlockData);

        if(err != ErrorNumber.NoError) return err;

        // Get anode block address from index block
        int offset         = 12 + indexOffset * 4; // 12 = IndexBlock header
        var anodeBlockAddr = BigEndianBitConverter.ToUInt32(indexBlockData, offset);

        AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: anodeBlockAddr={0} from offset {1}", anodeBlockAddr, offset);

        if(anodeBlockAddr == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "GetAnodeBlock: anodeBlockAddr is 0");

            return ErrorNumber.InvalidArgument;
        }

        // Read the anode block
        return ReadReservedBlock(anodeBlockAddr, out data);
    }
}