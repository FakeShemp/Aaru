// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Olt.cs
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
using Aaru.CommonTypes.Enums;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <summary>Reads the Object Location Table and extracts inode list extent information</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadOlt()
    {
        if(_superblock.vs_oltext == null || _superblock.vs_oltext[0] == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No OLT extent defined in superblock");

            return ErrorNumber.InvalidArgument;
        }

        // OLT extent is stored as a filesystem block number
        long  oltByteOffset  = (long)_superblock.vs_oltext[0] * _superblock.vs_bsize;
        ulong oltSectorOff   = (ulong)oltByteOffset           / _imagePlugin.Info.SectorSize;
        var   oltOff         = (uint)((ulong)oltByteOffset   % _imagePlugin.Info.SectorSize);
        var   oltSizeInBytes = (uint)(_superblock.vs_oltsize * _superblock.vs_bsize);

        var oltSizeInSectors = (oltOff + oltSizeInBytes + _imagePlugin.Info.SectorSize - 1) /
                               _imagePlugin.Info.SectorSize;

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + oltSectorOff,
                                                     false,
                                                     oltSizeInSectors,
                                                     out byte[] oltData,
                                                     out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading OLT data: {0}", errno);

            return errno;
        }

        int oltHeaderSize = System.Runtime.InteropServices.Marshal.SizeOf<OltHeader>();

        if(oltOff + oltHeaderSize > oltData.Length) return ErrorNumber.InvalidArgument;

        var oltHeaderBytes = new byte[oltHeaderSize];
        Array.Copy(oltData, oltOff, oltHeaderBytes, 0, oltHeaderSize);

        OltHeader oltHeader = _bigEndian
                                  ? Marshal.ByteArrayToStructureBigEndian<OltHeader>(oltHeaderBytes)
                                  : Marshal.ByteArrayToStructureLittleEndian<OltHeader>(oltHeaderBytes);

        if(oltHeader.olt_magic != VXFS_OLT_MAGIC)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Invalid OLT magic: 0x{0:X8}, expected 0x{1:X8}",
                              oltHeader.olt_magic,
                              VXFS_OLT_MAGIC);

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "OLT magic validated");
        AaruLogging.Debug(MODULE_NAME, "OLT header size: {0}", oltHeader.olt_size);

        // Walk OLT entries starting after the header
        var  foundIlist    = false;
        int  oltCommonSize = System.Runtime.InteropServices.Marshal.SizeOf<OltCommon>();
        uint entryOffset   = oltOff + oltHeader.olt_size;
        uint oltEnd        = oltOff + oltSizeInBytes;

        while(entryOffset + oltCommonSize <= oltEnd && entryOffset + oltCommonSize <= oltData.Length)
        {
            var commonBytes = new byte[oltCommonSize];
            Array.Copy(oltData, entryOffset, commonBytes, 0, oltCommonSize);

            OltCommon common = _bigEndian
                                   ? Marshal.ByteArrayToStructureBigEndian<OltCommon>(commonBytes)
                                   : Marshal.ByteArrayToStructureLittleEndian<OltCommon>(commonBytes);

            if(common.olt_size == 0) break;

            AaruLogging.Debug(MODULE_NAME,
                              "OLT entry at offset {0}: type={1}, size={2}",
                              entryOffset,
                              common.olt_type,
                              common.olt_size);

            if((OltEntryType)common.olt_type == OltEntryType.Ilist)
            {
                int ilistSize = System.Runtime.InteropServices.Marshal.SizeOf<OltIlist>();

                if(entryOffset + ilistSize <= oltData.Length)
                {
                    var ilistBytes = new byte[ilistSize];
                    Array.Copy(oltData, entryOffset, ilistBytes, 0, ilistSize);

                    OltIlist ilist = _bigEndian
                                         ? Marshal.ByteArrayToStructureBigEndian<OltIlist>(ilistBytes)
                                         : Marshal.ByteArrayToStructureLittleEndian<OltIlist>(ilistBytes);

                    _ilistExtent = ilist.olt_iext[0];
                    foundIlist   = true;

                    AaruLogging.Debug(MODULE_NAME, "Found inode list extent at block {0}", _ilistExtent);
                }
            }

            if((OltEntryType)common.olt_type == OltEntryType.FsHead)
            {
                int fsHeadSize = System.Runtime.InteropServices.Marshal.SizeOf<OltFsHead>();

                if(entryOffset + fsHeadSize <= oltData.Length)
                {
                    var fsHeadBytes = new byte[fsHeadSize];
                    Array.Copy(oltData, entryOffset, fsHeadBytes, 0, fsHeadSize);

                    OltFsHead fsHead = _bigEndian
                                           ? Marshal.ByteArrayToStructureBigEndian<OltFsHead>(fsHeadBytes)
                                           : Marshal.ByteArrayToStructureLittleEndian<OltFsHead>(fsHeadBytes);

                    _fsHeadIno = fsHead.olt_fsino[0];

                    AaruLogging.Debug(MODULE_NAME, "Found fileset header inode {0}", _fsHeadIno);
                }
            }

            entryOffset += common.olt_size;
        }

        if(!foundIlist)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not find inode list extent in OLT");

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }
}