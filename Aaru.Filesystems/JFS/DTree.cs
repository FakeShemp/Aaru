// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : DTree.cs
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
using System.Collections.Generic;
using Aaru.CommonTypes.Enums;
using Aaru.Logging;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements detection of IBM's Journaled File System</summary>
public sealed partial class JFS
{
    /// <summary>Parses the dtree root in a directory inode's extension area to extract directory entries</summary>
    /// <param name="extensionData">The inode extension area (di_u, 384 bytes)</param>
    /// <param name="entries">Output dictionary of filename to inode number</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ParseDtreeRoot(byte[] extensionData, out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>(StringComparer.Ordinal);

        // dtroot starts at offset 96 in di_u (after dir_table_slot[12])
        const int dtOffset = 96;

        // Parse dtroot header (32 bytes = 1 slot)
        // DASD(16) + flag(1) + nextindex(1) + freecnt(1) + freelist(1) + idotdot(4) + stbl(8)
        byte dtrootFlag      = extensionData[dtOffset + 16];
        byte dtrootNextindex = extensionData[dtOffset + 17];

        AaruLogging.Debug(MODULE_NAME, "DTree root: flag=0x{0:X2}, nextindex={1}", dtrootFlag, dtrootNextindex);

        if(dtrootNextindex == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory is empty");

            return ErrorNumber.NoError;
        }

        // Check if this is a leaf root (should always be for inline dtree)
        if((dtrootFlag & BT_LEAF) == 0)
        {
            // The root dtree is internal - entries are in external pages
            AaruLogging.Debug(MODULE_NAME, "Root dtree is internal, reading external pages...");

            return ParseDtreeInternalRoot(extensionData, dtOffset, dtrootNextindex, out entries);
        }

        // Read the sorted entry index table
        // stbl is at dtOffset + 24 (after DASD(16) + flag(1) + nextindex(1) + freecnt(1) + freelist(1) + idotdot(4))
        int stblOffset = dtOffset + 24;

        bool hasIndex    = _superblock.s_flags.HasFlag(Flags.DirIndex);
        int  headDataLen = hasIndex ? DTLHDRDATALEN : DTLHDRDATALEN_LEGACY;

        for(var i = 0; i < dtrootNextindex && i < 8; i++)
        {
            int slotIdx = (sbyte)extensionData[stblOffset + i];

            if(slotIdx < 0 || slotIdx >= DTROOTMAXSLOT) continue;

            // The slot is at dtOffset + slotIdx * DTSLOTSIZE
            int slotOffset = dtOffset + slotIdx * DTSLOTSIZE;

            // Parse ldtentry: inumber(4) + next(1) + namlen(1) + name(22 or 26) [+ index(4)]
            var  inumber = BitConverter.ToUInt32(extensionData, slotOffset);
            var  next    = (sbyte)extensionData[slotOffset + 4];
            byte namlen  = extensionData[slotOffset + 5];

            // Read name characters from the first segment
            int charsInHead = Math.Min(namlen, headDataLen);
            var nameChars   = new char[namlen];

            for(var c = 0; c < charsInHead; c++)
                nameChars[c] = (char)BitConverter.ToUInt16(extensionData, slotOffset + 6 + c * 2);

            // Follow continuation slots for remaining characters
            int charsCopied = charsInHead;

            while(next >= 0 && charsCopied < namlen && next < DTROOTMAXSLOT)
            {
                int contSlotOffset = dtOffset + next * DTSLOTSIZE;

                // dtslot: next(1) + cnt(1) + name[15](30)
                var contNext = (sbyte)extensionData[contSlotOffset];
                int cnt      = Math.Min(namlen - charsCopied, 15);

                for(var c = 0; c < cnt && charsCopied < namlen; c++, charsCopied++)
                    nameChars[charsCopied] = (char)BitConverter.ToUInt16(extensionData, contSlotOffset + 2 + c * 2);

                next = contNext;
            }

            var name = new string(nameChars, 0, Math.Min(charsCopied, namlen));

            if(!string.IsNullOrEmpty(name) && inumber != 0) entries[name] = inumber;

            AaruLogging.Debug(MODULE_NAME, "DTree root entry: '{0}' -> inode {1}", name, inumber);
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses an internal dtree root by following child pages to find leaf entries</summary>
    ErrorNumber ParseDtreeInternalRoot(byte[]                       extensionData, int dtOffset, byte nextindex,
                                       out Dictionary<string, uint> entries)
    {
        entries = new Dictionary<string, uint>(StringComparer.Ordinal);

        int stblOffset = dtOffset + 24;

        // Each stbl entry in an internal root points to an idtentry which has a child extent
        for(var i = 0; i < nextindex && i < 8; i++)
        {
            int slotIdx = (sbyte)extensionData[stblOffset + i];

            if(slotIdx < 0 || slotIdx >= DTROOTMAXSLOT) continue;

            int slotOffset = dtOffset + slotIdx * DTSLOTSIZE;

            // idtentry: xd(8) + next(1) + namlen(1) + name(22)
            // The xd (pxd_t) at the start gives the child page address
            var   lenAddr   = BitConverter.ToUInt32(extensionData, slotOffset);
            var   addr2     = BitConverter.ToUInt32(extensionData, slotOffset + 4);
            ulong n         = lenAddr & ~0xFFFFFFu;
            var   childAddr = (long)((n << 8) + addr2);

            if(childAddr <= 0) continue;

            AaruLogging.Debug(MODULE_NAME, "DTree internal root: following child page at block {0}", childAddr);

            // Read the child dtree page
            ErrorNumber errno = ReadBytes(childAddr * _superblock.s_bsize, PSIZE, out byte[] childPage);

            if(errno != ErrorNumber.NoError) continue;

            // Parse child page recursively
            errno = ParseDtreePage(childPage, entries);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error parsing child dtree page at block {0}: {1}", childAddr, errno);
            }
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Parses a dtree page (leaf or internal) and collects directory entries</summary>
    ErrorNumber ParseDtreePage(byte[] pageData, Dictionary<string, uint> entries)
    {
        // dtpage header: next(8) + prev(8) + flag(1) + nextindex(1) + freecnt(1) + freelist(1)
        //                + maxslot(1) + stblindex(1) + rsrvd(2) + self(8) = 32 bytes
        byte flag      = pageData[16];
        byte nextindex = pageData[17];
        byte maxslot   = pageData[20];
        byte stblindex = pageData[21];

        AaruLogging.Debug(MODULE_NAME,
                          "DTree page: flag=0x{0:X2}, nextindex={1}, maxslot={2}, stblindex={3}",
                          flag,
                          nextindex,
                          maxslot,
                          stblindex);

        if((flag & BT_LEAF) != 0)
        {
            // Leaf page - extract entries
            // stbl is at slot[stblindex]
            int stblByteOffset = stblindex * DTSLOTSIZE;

            bool hasIndex    = _superblock.s_flags.HasFlag(Flags.DirIndex);
            int  headDataLen = hasIndex ? DTLHDRDATALEN : DTLHDRDATALEN_LEGACY;

            for(var i = 0; i < nextindex; i++)
            {
                if(stblByteOffset + i >= pageData.Length) break;

                int slotIdx = (sbyte)pageData[stblByteOffset + i];

                if(slotIdx < 0 || slotIdx >= maxslot) continue;

                int slotOffset = slotIdx * DTSLOTSIZE;

                if(slotOffset + 6 > pageData.Length) continue;

                var  inumber = BitConverter.ToUInt32(pageData, slotOffset);
                var  next    = (sbyte)pageData[slotOffset + 4];
                byte namlen  = pageData[slotOffset + 5];

                if(namlen == 0 || inumber == 0) continue;

                int charsInHead = Math.Min(namlen, headDataLen);
                var nameChars   = new char[namlen];

                for(var c = 0; c < charsInHead && slotOffset + 6 + c * 2 + 1 < pageData.Length; c++)
                    nameChars[c] = (char)BitConverter.ToUInt16(pageData, slotOffset + 6 + c * 2);

                int charsCopied = charsInHead;

                while(next >= 0 && charsCopied < namlen && next < maxslot)
                {
                    int contSlotOffset = next * DTSLOTSIZE;

                    if(contSlotOffset + 2 >= pageData.Length) break;

                    var contNext = (sbyte)pageData[contSlotOffset];
                    int cnt      = Math.Min(namlen - charsCopied, 15);

                    for(var c = 0; c < cnt && charsCopied < namlen; c++, charsCopied++)
                    {
                        if(contSlotOffset + 2 + c * 2 + 1 >= pageData.Length) break;

                        nameChars[charsCopied] = (char)BitConverter.ToUInt16(pageData, contSlotOffset + 2 + c * 2);
                    }

                    next = contNext;
                }

                var name = new string(nameChars, 0, Math.Min(charsCopied, namlen));

                if(!string.IsNullOrEmpty(name)) entries[name] = inumber;
            }

            // Follow next sibling page if present
            var nextPage = BitConverter.ToUInt64(pageData, 0);

            if(nextPage != 0)
            {
                ErrorNumber errno = ReadBytes((long)nextPage * _superblock.s_bsize, PSIZE, out byte[] nextPageData);

                if(errno == ErrorNumber.NoError) ParseDtreePage(nextPageData, entries);
            }

            return ErrorNumber.NoError;
        }

        if((flag & BT_INTERNAL) != 0)
        {
            // Internal page - follow child pointers
            int stblByteOffset = stblindex * DTSLOTSIZE;

            for(var i = 0; i < nextindex; i++)
            {
                if(stblByteOffset + i >= pageData.Length) break;

                int slotIdx = (sbyte)pageData[stblByteOffset + i];

                if(slotIdx < 0 || slotIdx >= maxslot) continue;

                int slotOffset = slotIdx * DTSLOTSIZE;

                if(slotOffset + 8 > pageData.Length) continue;

                // idtentry: xd(8) + ...
                var   lenAddr   = BitConverter.ToUInt32(pageData, slotOffset);
                var   addr2     = BitConverter.ToUInt32(pageData, slotOffset + 4);
                ulong addrN     = lenAddr & ~0xFFFFFFu;
                var   childAddr = (long)((addrN << 8) + addr2);

                if(childAddr <= 0) continue;

                ErrorNumber errno = ReadBytes(childAddr * _superblock.s_bsize, PSIZE, out byte[] childPage);

                if(errno == ErrorNumber.NoError) ParseDtreePage(childPage, entries);
            }

            return ErrorNumber.NoError;
        }

        AaruLogging.Debug(MODULE_NAME, "DTree page has unknown flag: 0x{0:X2}", flag);

        return ErrorNumber.InvalidArgument;
    }
}