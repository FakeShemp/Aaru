// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Inode.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Xia filesystem plugin.
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

// Information from the Linux kernel
/// <inheritdoc />
public sealed partial class Xia
{
    /// <summary>Reads an inode from disk</summary>
    /// <param name="inodeNumber">The inode number to read</param>
    /// <param name="inode">The read inode structure</param>
    /// <returns>Error code indicating success or failure</returns>
    ErrorNumber ReadInode(uint inodeNumber, out Inode inode)
    {
        inode = default(Inode);

        if(inodeNumber == 0 || inodeNumber > _superblock.s_ninodes)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inode number: {0}", inodeNumber);

            return ErrorNumber.InvalidArgument;
        }

        // Calculate the zone containing this inode
        // Inode table starts after: 1 (superblock) + imap_zones + zmap_zones
        // From Linux kernel: zone = 1 + imap_zones + zmap_zones + (ino-1) / INODES_PER_ZONE
        uint inodesPerZone = _superblock.s_zone_size / 64; // sizeof(Inode) = 64 bytes
        uint inodeZone = 1 + _superblock.s_imap_zones + _superblock.s_zmap_zones + (inodeNumber - 1) / inodesPerZone;

        AaruLogging.Debug(MODULE_NAME,
                          "Reading inode {0} from zone {1} (inodes per zone: {2})",
                          inodeNumber,
                          inodeZone,
                          inodesPerZone);

        // Read the zone containing the inode
        ErrorNumber errno = ReadZone(inodeZone, out byte[] zoneData);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading inode zone: {0}", errno);

            return errno;
        }

        // Calculate offset within the zone
        uint inodeOffset = (inodeNumber - 1) % inodesPerZone * 64;

        AaruLogging.Debug(MODULE_NAME, "Inode offset within zone: {0}", inodeOffset);

        var inodeData = new byte[64];
        Array.Copy(zoneData, inodeOffset, inodeData, 0, 64);

        inode = Marshal.ByteArrayToStructureLittleEndian<Inode>(inodeData);

        AaruLogging.Debug(MODULE_NAME,
                          "Inode {0}: mode=0x{1:X4}, size={2}, nlinks={3}",
                          inodeNumber,
                          inode.i_mode,
                          inode.i_size,
                          inode.i_nlinks);

        return ErrorNumber.NoError;
    }
}