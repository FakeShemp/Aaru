// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dir.cs
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

using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
public sealed partial class PFS
{
    /// <summary>Reads directory blocks following an anode chain and caches entries</summary>
    /// <param name="startAnode">The starting anode</param>
    /// <param name="cache">The cache dictionary to populate</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadDirectoryBlocks(Anode startAnode, Dictionary<string, DirEntryCacheItem> cache)
    {
        Anode currentAnode = startAnode;

        while(true)
        {
            // Read blocks in this anode's cluster
            for(uint i = 0; i < currentAnode.clustersize; i++)
            {
                uint blockNr = currentAnode.blocknr + i;

                ErrorNumber errno = ReadBlock(blockNr, out byte[] blockData);

                if(errno != ErrorNumber.NoError) return errno;

                // Parse directory block
                var blockId = BigEndianBitConverter.ToUInt16(blockData, 0);

                if(blockId != DBLKID)
                {
                    AaruLogging.Debug(MODULE_NAME,
                                      "Invalid directory block ID: 0x{0:X4} at block {1}",
                                      blockId,
                                      blockNr);

                    continue;
                }

                // Parse directory entries
                ParseDirectoryBlock(blockData, cache);
            }

            // Move to next anode in chain
            if(currentAnode.next == ANODE_EOF) break;

            ErrorNumber err = GetAnode(currentAnode.next, out currentAnode);

            if(err != ErrorNumber.NoError) return err;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a directory block and adds entries to the cache</summary>
    /// <param name="blockData">The directory block data</param>
    /// <param name="cache">The cache dictionary to populate</param>
    void ParseDirectoryBlock(byte[] blockData, Dictionary<string, DirEntryCacheItem> cache)
    {
        // Directory block header: id(2) + notused(2) + datestamp(4) + notused2(4) + anodenr(4) + parent(4) = 20 bytes
        var offset = 20;

        while(offset < blockData.Length)
        {
            // Check if we've reached the end of entries
            if(blockData[offset] == 0) break;

            byte entrySize = blockData[offset];

            if(entrySize < 22 || offset + entrySize > blockData.Length) break;

            // Parse entry
            var  type           = (EntryType)(sbyte)blockData[offset + 1];
            var  anode          = BigEndianBitConverter.ToUInt32(blockData, offset + 2);
            var  fsize          = BigEndianBitConverter.ToUInt32(blockData, offset + 6);
            var  creationday    = BigEndianBitConverter.ToUInt16(blockData, offset + 10);
            var  creationminute = BigEndianBitConverter.ToUInt16(blockData, offset + 12);
            var  creationtick   = BigEndianBitConverter.ToUInt16(blockData, offset + 14);
            var  protection     = (ProtectionBits)blockData[offset + 16];
            byte nlength        = blockData[offset + 17];

            if(nlength == 0 || offset + 18 + nlength > blockData.Length)
            {
                offset += entrySize;

                continue;
            }

            string filename = _encoding.GetString(blockData, offset + 18, nlength);

            // Create cache item
            var item = new DirEntryCacheItem
            {
                Anode          = anode,
                Type           = type,
                Size           = fsize,
                Protection     = protection,
                CreationDay    = creationday,
                CreationMinute = creationminute,
                CreationTick   = creationtick
            };

            // Check for extra fields (comment follows filename)
            int commentOffset = offset + 18 + nlength;

            if(commentOffset < offset + entrySize && blockData[commentOffset] > 0)
            {
                int commentLength = blockData[commentOffset];

                if(commentOffset + 1 + commentLength <= offset + entrySize)
                    item.Comment = _encoding.GetString(blockData, commentOffset + 1, commentLength);
            }

            // Check for extended file size in dir extension mode
            if(_modeFlags.HasFlag(ModeFlags.LargeFile) && _modeFlags.HasFlag(ModeFlags.DirExtension))
            {
                // ExtraFields may follow the comment - we'd need to parse them for fsizex
                // For now, just use the 32-bit size
            }

            if(!string.IsNullOrEmpty(filename) && !cache.ContainsKey(filename)) cache[filename] = item;

            offset += entrySize;
        }
    }
}