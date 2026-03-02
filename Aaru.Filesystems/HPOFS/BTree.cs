// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : BTree.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : High Performance Optical File System plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     B-tree directory node parsing for the High Performance Optical File System.
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
using Aaru.Helpers;
using Aaru.Logging;

namespace Aaru.Filesystems;

public sealed partial class HPOFS
{
    /// <summary>Parses separator keys and child pointers from an INDX (interior) B-tree node</summary>
    /// <remarks>
    ///     INDX node entry area layout (starts at offset 0x24):
    ///     [4] leftChild (uint32 big-endian) — first child node ID
    ///     Then alternating entries and child pointers:
    ///     entry: [2] kl (uint16 BE) + [2] rd (uint16 BE, always 0) + [kl] key bytes
    ///     [4] childPtr (uint32 big-endian) — child node ID for keys >= this entry's key
    ///     End marker: kl = 0
    ///     Key format: [4] timestamp (uint32 BE) + [kl-4] name (null-padded)
    /// </remarks>
    (List<uint> children, List<(string name, uint timestamp)> keys) ParseIndxEntries(byte[] nodeData)
    {
        List<uint>                          children = new();
        List<(string name, uint timestamp)> keys     = new();

        if(nodeData.Length < 0x28) return (children, keys);

        // Read leftChild pointer at +0x24
        var leftChild = BigEndianBitConverter.ToUInt32(nodeData, 0x24);
        children.Add(leftChild);

        var offset = 0x28;

        while(offset + 4 <= nodeData.Length)
        {
            var kl = BigEndianBitConverter.ToUInt16(nodeData, offset);

            // End marker
            if(kl == 0) break;

            var rd = BigEndianBitConverter.ToUInt16(nodeData, offset + 2);

            // Key starts at offset + 4, length = kl bytes
            if(offset + 4 + kl > nodeData.Length) break;

            // Parse key: timestamp(u32) + name(null-padded)
            if(kl >= 4)
            {
                var timestamp   = BigEndianBitConverter.ToUInt32(nodeData, offset + 4);
                int nameAreaLen = kl - 4;
                var name        = "";

                if(nameAreaLen > 0 && nameAreaLen <= MAX_FILENAME_LENGTH)
                    name = _encoding.GetString(nodeData, offset + 8, nameAreaLen).TrimEnd('\0');

                keys.Add((name, timestamp));
            }

            // Skip past entry: kl(2) + rd(2) + key[kl]
            offset += 4 + kl;

            // Read embedded child pointer after this entry
            if(offset + 4 > nodeData.Length) break;

            var childPtr = BigEndianBitConverter.ToUInt32(nodeData, offset);
            children.Add(childPtr);
            offset += 4;
        }

        return (children, keys);
    }

    /// <summary>Parses directory entries from a DATA (leaf) B-tree node</summary>
    /// <remarks>
    ///     DATA node entry area layout (starts at offset 0x24):
    ///     Each entry:
    ///     [2] kl (uint16 BE, key length)
    ///     [2] rd (uint16 BE, record descriptor / record length)
    ///     [kl] key bytes: [4] timestamp (uint32 BE) + [kl-4] name (null-padded)
    ///     [rd] record bytes
    ///     End marker: kl = 0
    ///     Record format (rd = 0xDC = 220 bytes for directory entries):
    ///     +0x00: sectorAddress (uint32 BE)
    ///     +0x04: sectorAddressDup (uint32 BE, same value)
    ///     +0x08: extentCount (uint32 BE)
    ///     +0x0C: entryType (uint8: 0x20=file/dir, 0x40=attribute, 0x60=system)
    ///     +0x0E: dosAttributes (uint8: 0x01=rdonly, 0x02=hidden, 0x04=system, 0x10=dir, 0x20=archive)
    ///     +0x1C: timestamp1 (uint32 BE, creation time, Unix epoch)
    ///     +0x20: timestamp2 (uint32 BE, modification time, Unix epoch)
    ///     +0x40: dataSectorCount (uint16 BE, sector count for first inline data extent)
    ///     +0x44: dataStartLba (uint32 BE, start LBA for first inline data extent)
    ///     +0x4C: subfSector (uint32 BE, SUBF sector for additional extents; 0xFFFFFFFF = none)
    ///     +0x58: fileSize (uint32 BE)
    /// </remarks>
    List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint creationTimestamp, uint
            modificationTimestamp, uint fileSize, ushort dataSectorCount, uint dataStartLba, uint subfSector)>
        ParseDataEntries(byte[] nodeData)
    {
        List<(string fullPath, uint timestamp, byte attributes, uint sectorAddress, uint creationTimestamp, uint
            modificationTimestamp, uint fileSize, ushort dataSectorCount, uint dataStartLba, uint subfSector)> entries =
            new();

        var offset = 0x24;

        while(offset + 4 <= nodeData.Length)
        {
            var kl = BigEndianBitConverter.ToUInt16(nodeData, offset);

            // End marker
            if(kl == 0) break;

            var rd = BigEndianBitConverter.ToUInt16(nodeData, offset + 2);

            // Key must hold at least a timestamp
            if(kl < 4) break;

            int stride = 4 + kl + rd;

            // Bounds check
            if(offset + stride > nodeData.Length) break;

            // Parse key: timestamp(u32) + name(null-padded)
            var timestamp   = BigEndianBitConverter.ToUInt32(nodeData, offset + 4);
            int nameAreaLen = kl - 4;
            var name        = "";

            if(nameAreaLen > 0 && nameAreaLen <= MAX_FILENAME_LENGTH)
                name = _encoding.GetString(nodeData, offset + 8, nameAreaLen).TrimEnd('\0');

            // Parse record data
            int    recordStart           = offset + 4 + kl;
            uint   sectorAddress         = 0;
            byte   attributes            = 0;
            uint   creationTimestamp     = 0;
            uint   modificationTimestamp = 0;
            uint   fileSize              = 0;
            ushort dataSectorCount       = 0;
            uint   dataStartLba          = 0;
            var    subfSector            = 0xFFFFFFFF;

            if(rd >= 4) sectorAddress = BigEndianBitConverter.ToUInt32(nodeData, recordStart);

            if(rd >= 0x0F) attributes = nodeData[recordStart + 0x0E];

            if(rd >= 0x20) creationTimestamp = BigEndianBitConverter.ToUInt32(nodeData, recordStart + 0x1C);

            if(rd >= 0x24) modificationTimestamp = BigEndianBitConverter.ToUInt32(nodeData, recordStart + 0x20);

            if(rd >= 0x5C && (attributes & 0x10) == 0)
                fileSize = BigEndianBitConverter.ToUInt32(nodeData, recordStart + 0x58);

            if(rd >= 0x48 && (attributes & 0x10) == 0)
            {
                dataSectorCount = BigEndianBitConverter.ToUInt16(nodeData, recordStart + 0x40);
                dataStartLba    = BigEndianBitConverter.ToUInt32(nodeData, recordStart + 0x44);
            }

            if(rd >= 0x50 && (attributes & 0x10) == 0)
                subfSector = BigEndianBitConverter.ToUInt32(nodeData, recordStart + 0x4C);

            if(!string.IsNullOrWhiteSpace(name))
            {
                AaruLogging.Debug(MODULE_NAME,
                                  "DATA entry: name='{0}', ts=0x{1:X8}, attrs=0x{2:X2}, sector=0x{3:X8}",
                                  name,
                                  timestamp,
                                  attributes,
                                  sectorAddress);

                entries.Add((name, timestamp, attributes, sectorAddress, creationTimestamp, modificationTimestamp,
                             fileSize, dataSectorCount, dataStartLba, subfSector));
            }

            offset += stride;
        }

        return entries;
    }
}