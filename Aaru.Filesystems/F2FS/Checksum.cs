// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Checksum.cs
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

namespace Aaru.Filesystems;

public sealed partial class F2FS
{
    /// <summary>
    ///     Computes the F2FS checkpoint checksum, matching f2fs_checkpoint_chksum() in checkpoint.c.
    ///     CRCs bytes [0, chksumOfs), skips the 4-byte checksum field, then if chksumOfs is not at
    ///     the very end of the block (CP_CHKSUM_OFFSET = blockSize - 4), continues CRC'ing
    ///     bytes [chksumOfs+4, blockSize).
    /// </summary>
    static uint F2fsCheckpointChksum(byte[] block, uint chksumOfs, uint blockSize)
    {
        // First part: CRC bytes [0, chksumOfs) with seed = F2FS_SUPER_MAGIC
        uint chksum = Crc32Le(F2FS_MAGIC, block, 0, chksumOfs);

        // CP_CHKSUM_OFFSET = F2FS_BLKSIZE - sizeof(__le32)
        uint cpChksumOffset = blockSize - 4;

        if(chksumOfs < cpChksumOffset)
        {
            // Second part: skip the 4-byte checksum, continue CRC'ing the rest
            uint afterChksum = chksumOfs + 4;
            chksum = Crc32Le(chksum, block, afterChksum, blockSize - afterChksum);
        }

        return chksum;
    }

    /// <summary>
    ///     Standard CRC32 (polynomial 0xEDB88320, reflected / little-endian) matching
    ///     the Linux kernel's crc32_le(). The initial CRC value is used directly as the
    ///     running state — there is NO pre-inversion or post-inversion.
    /// </summary>
    static uint Crc32Le(uint crc, byte[] data, uint offset, uint length)
    {
        for(uint i = 0; i < length; i++)
        {
            crc ^= data[offset + i];

            for(var j = 0; j < 8; j++)
            {
                if((crc & 1) != 0)
                    crc = crc >> 1 ^ 0xEDB88320;
                else
                    crc >>= 1;
            }
        }

        return crc;
    }
}