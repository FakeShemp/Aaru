// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX System V filesystem plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class SysVfs
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting SysV volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        if(imagePlugin.Info.SectorSize < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Find and validate the superblock
        ErrorNumber errno = FindSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error finding superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Variant: {0}",         _variant);
        AaruLogging.Debug(MODULE_NAME, "Bytesex: {0}",         _bytesex);
        AaruLogging.Debug(MODULE_NAME, "Block size: {0}",      _blockSize);
        AaruLogging.Debug(MODULE_NAME, "First data zone: {0}", _firstDataZone);
        AaruLogging.Debug(MODULE_NAME, "Total zones: {0}",     _totalZones);

        _inodesPerBlock = _blockSize / INODE_SIZE;

        AaruLogging.Debug(MODULE_NAME, "Inodes per block: {0}", _inodesPerBlock);

        if(_inodesPerBlock <= 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid inodes per block");

            return ErrorNumber.InvalidArgument;
        }

        // Load root directory
        errno = LoadRootDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading root directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory loaded with {0} entries", _rootDirectoryCache.Count);

        // Build metadata
        string fsType = _variant switch
                        {
                            SysVVariant.Xenix     => FS_TYPE_XENIX,
                            SysVVariant.Xenix3    => FS_TYPE_XENIX3,
                            SysVVariant.SystemVR4 => FS_TYPE_SVR4,
                            SysVVariant.SystemVR2 => FS_TYPE_SVR2,
                            SysVVariant.ScoAfs    => FS_TYPE_AFS,
                            SysVVariant.Coherent  => FS_TYPE_COHERENT,
                            SysVVariant.UnixV7    => FS_TYPE_UNIX7,
                            SysVVariant.Eafs      => FS_TYPE_EAFS,
                            _                     => FS_TYPE_SVR4
                        };

        Metadata = new FileSystem
        {
            Type        = fsType,
            ClusterSize = (uint)_blockSize,
            Clusters    = (ulong)_totalZones
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Volume mounted successfully");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _rootDirectoryCache.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _encoding    = null;
        _blockSize   = 0;
        _freeBlocks  = 0;
        _freeInodes  = 0;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted");

        return ErrorNumber.NoError;
    }

    /// <summary>Finds and validates the superblock, setting variant, endianness, block size, etc.</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber FindSuperblock()
    {
        AaruLogging.Debug(MODULE_NAME, "Searching for superblock...");

        uint sectorSize = _imagePlugin.Info.SectorSize;

        byte sbSizeInSectors;

        if(sectorSize <= 0x400)
            sbSizeInSectors = (byte)(0x400 / sectorSize);
        else
            sbSizeInSectors = 1;

        // Sectors in a cylinder
        var spc = (int)(_imagePlugin.Info.Heads * _imagePlugin.Info.SectorsPerTrack);

        // Multiplier to convert 1024-byte block numbers to sector offsets
        var sectorsPerBlock                     = (int)(1024 / sectorSize);
        if(sectorsPerBlock < 1) sectorsPerBlock = 1;

        // Same locations as Identify/GetInformation
        int[] locations =
        [
            0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,

            // Odd SysV superblock locations (block numbers converted to sector offsets)
            9 * sectorsPerBlock, 15 * sectorsPerBlock, 18 * sectorsPerBlock,

            // Superblock can also skip one cylinder (for boot)
            spc
        ];

        foreach(int i in locations)
        {
            if((ulong)i + _partition.Start + sbSizeInSectors >= _imagePlugin.Info.Sectors) continue;

            ErrorNumber errno = _imagePlugin.ReadSectors((ulong)i + _partition.Start,
                                                         false,
                                                         sbSizeInSectors,
                                                         out byte[] sbSector,
                                                         out _);

            if(errno != ErrorNumber.NoError || sbSector.Length < 0x400) continue;

            // Check XENIX magic at 0x3F8
            var magic = BitConverter.ToUInt32(sbSector, 0x3F8);

            if(magic is XENIX_MAGIC or SYSV_MAGIC or EAFS_MAGIC)
            {
                if(magic is SYSV_MAGIC or EAFS_MAGIC)
                {
                    _superblockOffset = 0x200;

                    if(magic == EAFS_MAGIC)
                    {
                        _bytesex = Bytesex.LittleEndian;

                        ErrorNumber eafsErrno = DetectSysVVariant(i, sbSector);

                        _variant = SysVVariant.Eafs;

                        return eafsErrno;
                    }

                    return DetectSysVVariant(i, sbSector);
                }

                _bytesex         = Bytesex.LittleEndian;
                _superblockStart = i;

                return ReadXenixSuperblock(sbSector);
            }

            if(magic is XENIX_CIGAM or SYSV_CIGAM)
            {
                if(magic == SYSV_CIGAM)
                {
                    _bytesex          = Bytesex.BigEndian;
                    _superblockOffset = 0x200;

                    return DetectSysVVariant(i, sbSector);
                }

                _bytesex         = Bytesex.BigEndian;
                _superblockStart = i;

                return ReadXenixSuperblock(sbSector);
            }

            // Check XENIX 3 magic at 0x1F0
            magic = BitConverter.ToUInt32(sbSector, 0x1F0);

            if(magic == XENIX_MAGIC)
            {
                _bytesex         = Bytesex.LittleEndian;
                _superblockStart = i;

                return ReadXenix3Superblock(sbSector);
            }

            if(magic == XENIX_CIGAM)
            {
                _bytesex         = Bytesex.BigEndian;
                _superblockStart = i;

                return ReadXenix3Superblock(sbSector);
            }

            // Check SYSV/EAFS magic at 0x1F8
            magic = BitConverter.ToUInt32(sbSector, 0x1F8);

            if(magic is SYSV_MAGIC or EAFS_MAGIC)
            {
                _bytesex          = Bytesex.LittleEndian;
                _superblockOffset = 0;

                ErrorNumber sysVErrno = DetectSysVVariant(i, sbSector);

                if(magic == EAFS_MAGIC) _variant = SysVVariant.Eafs;

                return sysVErrno;
            }

            if(magic == SYSV_CIGAM)
            {
                _bytesex          = Bytesex.BigEndian;
                _superblockOffset = 0;

                return DetectSysVVariant(i, sbSector);
            }

            // Check Coherent
            var cohString = new byte[6];
            Array.Copy(sbSector, 0x1E4, cohString, 0, 6);
            string sFname = StringHandlers.CToString(cohString, _encoding);
            Array.Copy(sbSector, 0x1EA, cohString, 0, 6);
            string sFpack = StringHandlers.CToString(cohString, _encoding);

            if(sFname == COH_FNAME && sFpack == COH_FPACK ||
               sFname == COH_XXXXX && sFpack == COH_XXXXX ||
               sFname == COH_XXXXS && sFpack == COH_XXXXN)
            {
                _bytesex         = Bytesex.Pdp;
                _superblockStart = i;

                return ReadCoherentSuperblock(sbSector);
            }

            // Check V7
            var sFsize  = BitConverter.ToUInt32(sbSector, 0x002);
            var sNfree  = BitConverter.ToUInt16(sbSector, 0x006);
            var sNinode = BitConverter.ToUInt16(sbSector, 0x0D0);

            if(sFsize is <= 0 or >= 0xFFFFFFFF || sNfree is <= 0 or >= 0xFFFF || sNinode is <= 0 or >= 0xFFFF) continue;

            // Try PDP-endian first
            uint pdpFsize = (sFsize & 0xFFFF0000) >> 16 | (sFsize & 0x0000FFFF) << 16;

            if(pdpFsize                > 0          &&
               pdpFsize                < V7_MAXSIZE &&
               sNfree                  > 0          &&
               sNfree                  < V7_NICFREE &&
               sNinode                 > 0          &&
               sNinode                 < V7_NICINOD &&
               (pdpFsize & 0xFF000000) == 0x00      &&
               (pdpFsize * 512  == (_partition.End - _partition.Start) * sectorSize ||
                pdpFsize * 1024 == (_partition.End - _partition.Start) * sectorSize))
            {
                _bytesex         = Bytesex.Pdp;
                _superblockStart = i;

                return ReadV7Superblock(sbSector);
            }

            // Try LE
            if(sFsize                > 0          &&
               sFsize                < V7_MAXSIZE &&
               sNfree                > 0          &&
               sNfree                < V7_NICFREE &&
               sNinode               > 0          &&
               sNinode               < V7_NICINOD &&
               (sFsize & 0xFF000000) == 0x00      &&
               (sFsize * 512  == (_partition.End - _partition.Start) * sectorSize ||
                sFsize * 1024 == (_partition.End - _partition.Start) * sectorSize))
            {
                _bytesex         = Bytesex.LittleEndian;
                _superblockStart = i;

                return ReadV7Superblock(sbSector);
            }

            // Try BE
            uint beFsize = (sFsize & 0xFF)       << 24 |
                           (sFsize & 0xFF00)     << 8  |
                           (sFsize & 0xFF0000)   >> 8  |
                           (sFsize & 0xFF000000) >> 24;

            var beNfree  = (ushort)(sNfree  >> 8 | sNfree  << 8);
            var beNinode = (ushort)(sNinode >> 8 | sNinode << 8);

            if(beFsize                > 0          &&
               beFsize                < V7_MAXSIZE &&
               beNfree                > 0          &&
               beNfree                < V7_NICFREE &&
               beNinode               > 0          &&
               beNinode               < V7_NICINOD &&
               (beFsize & 0xFF000000) == 0x00      &&
               (beFsize * 512  == (_partition.End - _partition.Start) * sectorSize ||
                beFsize * 1024 == (_partition.End - _partition.Start) * sectorSize))
            {
                _bytesex         = Bytesex.BigEndian;
                _superblockStart = i;

                return ReadV7Superblock(sbSector);
            }
        }

        AaruLogging.Debug(MODULE_NAME, "No valid superblock found");

        return ErrorNumber.InvalidArgument;
    }

    /// <summary>Detects whether a SysV filesystem is R4, R2, or SCO AFS</summary>
    ErrorNumber DetectSysVVariant(int start, byte[] sbSector)
    {
        int offset = _superblockOffset;

        // Read s_type to determine block size
        var tempType = (FsType)BitConverter.ToUInt32(sbSector, 0x1FC + offset);

        if(_bytesex == Bytesex.BigEndian) tempType = (FsType)Swapping.Swap((uint)tempType);

        // Check for SCO AFS: s_nfree == 0xFFFF at offset 0x008 in R4 struct
        var tempNfree = BitConverter.ToUInt16(sbSector, 0x008 + offset);

        if(_bytesex == Bytesex.BigEndian) tempNfree = Swapping.Swap(tempNfree);

        bool scoAfs = tempNfree == SCO_NFREE;

        // ISC long filenames: s_type >= 0x10
        var  rawType = (uint)tempType;
        bool iscLong = rawType >= 0x10 && rawType <= 0x30;

        if(iscLong) tempType = (FsType)(rawType >> 4);

        _blockSize = tempType switch
                     {
                         FsType.Fs_512  => 512,
                         FsType.Fs_1024 => 1024,
                         FsType.Fs_2048 => 2048,
                         FsType.Fs_4096 => 4096,
                         FsType.Fs_8192 => 8192,
                         _              => 1024
                     };

        // Read s_time at R4 offset (0x1A4) to determine R4 vs R2
        var tempTime = BitConverter.ToInt32(sbSector, 0x1A4 + offset);

        if(_bytesex == Bytesex.BigEndian) tempTime = Swapping.Swap(tempTime);

        bool sysvR4 = tempTime >= JAN_1_1980;

        _superblockStart = start;

        if(scoAfs)
        {
            _variant = SysVVariant.ScoAfs;

            return ReadSysVR4Superblock(sbSector, offset);
        }

        if(sysvR4)
        {
            _variant = SysVVariant.SystemVR4;

            return ReadSysVR4Superblock(sbSector, offset);
        }

        _variant = SysVVariant.SystemVR2;

        return ReadSysVR2Superblock(sbSector, offset);
    }

    /// <summary>Reads a XENIX superblock (magic at 0x3F8)</summary>
    ErrorNumber ReadXenixSuperblock(byte[] sbSector)
    {
        XenixSuperBlock xnxSb = _bytesex == Bytesex.BigEndian
                                    ? Marshal.ByteArrayToStructureBigEndian<XenixSuperBlock>(sbSector)
                                    : Marshal.ByteArrayToStructureLittleEndian<XenixSuperBlock>(sbSector);

        _variant       = SysVVariant.Xenix;
        _firstDataZone = xnxSb.s_isize;
        _totalZones    = xnxSb.s_fsize;
        _freeBlocks    = xnxSb.s_tfree;
        _freeInodes    = xnxSb.s_tinode;

        _blockSize = xnxSb.s_type switch
                     {
                         FsType.Fs_512  => 512,
                         FsType.Fs_1024 => 1024,
                         FsType.Fs_2048 => 2048,
                         _              => 1024
                     };

        AaruLogging.Debug(MODULE_NAME, "XENIX superblock read, s_isize={0}, s_fsize={1}", _firstDataZone, _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a XENIX 3 superblock (magic at 0x1F0)</summary>
    ErrorNumber ReadXenix3Superblock(byte[] sbSector)
    {
        Xenix3SuperBlock xnx3Sb = _bytesex == Bytesex.BigEndian
                                      ? Marshal.ByteArrayToStructureBigEndian<Xenix3SuperBlock>(sbSector)
                                      : Marshal.ByteArrayToStructureLittleEndian<Xenix3SuperBlock>(sbSector);

        _variant       = SysVVariant.Xenix3;
        _firstDataZone = xnx3Sb.s_isize;
        _totalZones    = xnx3Sb.s_fsize;
        _freeBlocks    = xnx3Sb.s_tfree;
        _freeInodes    = xnx3Sb.s_tinode;

        _blockSize = xnx3Sb.s_type switch
                     {
                         FsType.Fs_512  => 512,
                         FsType.Fs_1024 => 1024,
                         FsType.Fs_2048 => 2048,
                         _              => 1024
                     };

        AaruLogging.Debug(MODULE_NAME,
                          "XENIX 3 superblock read, s_isize={0}, s_fsize={1}",
                          _firstDataZone,
                          _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a System V Release 4 superblock</summary>
    ErrorNumber ReadSysVR4Superblock(byte[] sbSector, int offset)
    {
        var offsetBuffer = new byte[sbSector.Length - offset];
        Array.Copy(sbSector, offset, offsetBuffer, 0, offsetBuffer.Length);

        SystemVRelease4SuperBlock sysvSb = _bytesex == Bytesex.BigEndian
                                               ? Marshal
                                                  .ByteArrayToStructureBigEndian<
                                                       SystemVRelease4SuperBlock>(offsetBuffer)
                                               : Marshal
                                                  .ByteArrayToStructureLittleEndian<
                                                       SystemVRelease4SuperBlock>(offsetBuffer);

        _firstDataZone = sysvSb.s_isize;
        _totalZones    = sysvSb.s_fsize;
        _freeBlocks    = sysvSb.s_tfree;
        _freeInodes    = sysvSb.s_tinode;

        AaruLogging.Debug(MODULE_NAME,
                          "SysV R4 superblock read, s_isize={0}, s_fsize={1}",
                          _firstDataZone,
                          _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a System V Release 2 superblock</summary>
    ErrorNumber ReadSysVR2Superblock(byte[] sbSector, int offset)
    {
        var offsetBuffer = new byte[sbSector.Length - offset];
        Array.Copy(sbSector, offset, offsetBuffer, 0, offsetBuffer.Length);

        SystemVRelease2SuperBlock sysvSb = _bytesex == Bytesex.BigEndian
                                               ? Marshal
                                                  .ByteArrayToStructureBigEndian<
                                                       SystemVRelease2SuperBlock>(offsetBuffer)
                                               : Marshal
                                                  .ByteArrayToStructureLittleEndian<
                                                       SystemVRelease2SuperBlock>(offsetBuffer);

        _firstDataZone = sysvSb.s_isize;
        _totalZones    = sysvSb.s_fsize;
        _freeBlocks    = sysvSb.s_tfree;
        _freeInodes    = sysvSb.s_tinode;

        AaruLogging.Debug(MODULE_NAME,
                          "SysV R2 superblock read, s_isize={0}, s_fsize={1}",
                          _firstDataZone,
                          _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a Coherent UNIX superblock</summary>
    ErrorNumber ReadCoherentSuperblock(byte[] sbSector)
    {
        CoherentSuperBlock cohSb = Marshal.ByteArrayToStructurePdpEndian<CoherentSuperBlock>(sbSector);

        _variant       = SysVVariant.Coherent;
        _blockSize     = 512;
        _firstDataZone = cohSb.s_isize;
        _totalZones    = cohSb.s_fsize;
        _freeBlocks    = cohSb.s_tfree;
        _freeInodes    = cohSb.s_tinode;

        AaruLogging.Debug(MODULE_NAME,
                          "Coherent superblock read, s_isize={0}, s_fsize={1}",
                          _firstDataZone,
                          _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Reads a UNIX 7th Edition superblock</summary>
    ErrorNumber ReadV7Superblock(byte[] sbSector)
    {
        UNIX7thEditionSuperBlock v7Sb;

        if(_bytesex == Bytesex.Pdp)
            v7Sb = Marshal.ByteArrayToStructurePdpEndian<UNIX7thEditionSuperBlock>(sbSector);
        else if(_bytesex == Bytesex.BigEndian)
            v7Sb = Marshal.ByteArrayToStructureBigEndian<UNIX7thEditionSuperBlock>(sbSector);
        else
            v7Sb = Marshal.ByteArrayToStructureLittleEndian<UNIX7thEditionSuperBlock>(sbSector);

        _variant       = SysVVariant.UnixV7;
        _blockSize     = 512;
        _firstDataZone = v7Sb.s_isize;
        _totalZones    = v7Sb.s_fsize;
        _freeBlocks    = v7Sb.s_tfree;
        _freeInodes    = v7Sb.s_tinode;

        AaruLogging.Debug(MODULE_NAME, "V7 superblock read, s_isize={0}, s_fsize={1}", _firstDataZone, _totalZones);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();

        // Read the root inode (inode 2)
        ErrorNumber errno = ReadInode(SYSV_ROOT_INO, out Inode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        if((rootInode.di_mode & S_IFMT) != S_IFDIR)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (mode=0x{0:X4})", rootInode.di_mode);

            return ErrorNumber.InvalidArgument;
        }

        int dirSize = rootInode.di_size;

        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", dirSize);

        if(dirSize <= 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Root directory is empty");

            return ErrorNumber.InvalidArgument;
        }

        // Read directory data from direct blocks
        var bytesRead = 0;
        int entrySize = 2 + DIRSIZE; // ushort d_ino + byte[14] d_name = 16

        for(var i = 0; i < 10 && bytesRead < dirSize; i++)
        {
            uint blockNumber = Read3ByteAddress(rootInode.di_addr, i);

            if(blockNumber == 0) continue;

            errno = ReadBlock(blockNumber, out byte[] blockData);

            if(errno != ErrorNumber.NoError)
            {
                AaruLogging.Debug(MODULE_NAME, "Error reading directory block {0}: {1}", blockNumber, errno);

                continue;
            }

            // Parse directory entries from this block
            var    blockOffset = 0;
            string pendingName = null;

            while(blockOffset + entrySize <= blockData.Length && bytesRead < dirSize)
            {
                var entryData = new byte[entrySize];
                Array.Copy(blockData, blockOffset, entryData, 0, entrySize);

                ushort dIno;

                switch(_bytesex)
                {
                    case Bytesex.BigEndian:
                        dIno = (ushort)(entryData[0] << 8 | entryData[1]);

                        break;
                    default:
                        dIno = (ushort)(entryData[0] | entryData[1] << 8);

                        break;
                }

                if(dIno == EAFS_LONG_NAME_INO && _variant == SysVVariant.Eafs)
                {
                    // EAFS long filename continuation entry
                    var nameBytes = new byte[DIRSIZE];
                    Array.Copy(entryData, 2, nameBytes, 0, DIRSIZE);
                    pendingName = (pendingName ?? "") + _encoding.GetString(nameBytes);
                }
                else if(dIno != 0)
                {
                    var nameBytes = new byte[DIRSIZE];
                    Array.Copy(entryData, 2, nameBytes, 0, DIRSIZE);
                    string namePart = StringHandlers.CToString(nameBytes, _encoding);

                    string name = pendingName != null ? pendingName + namePart : namePart;
                    pendingName = null;

                    if(!string.IsNullOrEmpty(name) && name is not ("." or ".."))
                    {
                        _rootDirectoryCache[name] = dIno;

                        AaruLogging.Debug(MODULE_NAME, "Cached entry: {0} -> inode {1}", name, dIno);
                    }
                }
                else
                    pendingName = null;

                blockOffset += entrySize;
                bytesRead   += entrySize;
            }
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}