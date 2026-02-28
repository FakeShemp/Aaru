// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
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
using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

/// <inheritdoc />
/// <summary>Implements the Veritas filesystem</summary>
public sealed partial class VxFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting VxFS volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");

        if(imagePlugin.Info.SectorSize < 512)
        {
            AaruLogging.Debug(MODULE_NAME, "Sector size too small: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Read and validate the superblock
        ErrorNumber errno = ReadSuperblock();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading superblock: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Superblock read successfully");
        AaruLogging.Debug(MODULE_NAME, "Block size: {0} bytes",        _superblock.vs_bsize);
        AaruLogging.Debug(MODULE_NAME, "Version: {0}",                 _superblock.vs_version);
        AaruLogging.Debug(MODULE_NAME, "Total blocks: {0}",            _superblock.vs_size);
        AaruLogging.Debug(MODULE_NAME, "Free blocks: {0}",             _superblock.vs_free);
        AaruLogging.Debug(MODULE_NAME, "Free inodes: {0}",             _superblock.vs_ifree);
        AaruLogging.Debug(MODULE_NAME, "Inodes per block: {0}",        _superblock.vs_inopb);
        AaruLogging.Debug(MODULE_NAME, "First AU: {0}",                _superblock.vs_firstau);
        AaruLogging.Debug(MODULE_NAME, "Inode list offset in AU: {0}", _superblock.vs_istart);
        AaruLogging.Debug(MODULE_NAME, "OLT extent[0]: {0}",           _superblock.vs_oltext[0]);
        AaruLogging.Debug(MODULE_NAME, "OLT size: {0}",                _superblock.vs_oltsize);
        AaruLogging.Debug(MODULE_NAME, "Big-endian: {0}",              _bigEndian);

        // Read the OLT and find the initial inode list extent
        errno = ReadOlt();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading OLT: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "OLT read successfully, ilist extent at block {0}", _ilistExtent);

        // Read the fileset header and structural inode list
        errno = ReadFsHead();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading fileset header: {0}", errno);

            return errno;
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
        Metadata = new FileSystem
        {
            Type             = FS_TYPE,
            CreationDate     = DateHandlers.UnixUnsignedToDateTime(_superblock.vs_ctime, _superblock.vs_cutime),
            ModificationDate = DateHandlers.UnixUnsignedToDateTime(_superblock.vs_wtime, _superblock.vs_wutime),
            Clusters         = (ulong)_superblock.vs_size,
            ClusterSize      = (uint)_superblock.vs_bsize,
            Dirty            = _superblock.vs_clean != 0,
            FreeClusters     = (ulong)_superblock.vs_free,
            VolumeName       = StringHandlers.CToString(_superblock.vs_fname, _encoding)
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();
        _mounted = false;

        return ErrorNumber.NoError;
    }

    /// <summary>Reads and validates the VxFS superblock</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadSuperblock()
    {
        _bigEndian = false;

        ulong sbSectorOff = VXFS_BASE / _imagePlugin.Info.SectorSize;
        uint  sbOff       = VXFS_BASE % _imagePlugin.Info.SectorSize;

        int sbSizeInBytes = System.Runtime.InteropServices.Marshal.SizeOf<SuperBlock>();

        var sbSizeInSectors = (uint)((sbOff + sbSizeInBytes + _imagePlugin.Info.SectorSize - 1) /
                                     _imagePlugin.Info.SectorSize);

        ErrorNumber errno = _imagePlugin.ReadSectors(_partition.Start + sbSectorOff,
                                                     false,
                                                     sbSizeInSectors,
                                                     out byte[] sbSector,
                                                     out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sbOff + 4 > (uint)sbSector.Length) return ErrorNumber.InvalidArgument;

        var magic = BitConverter.ToUInt32(sbSector, (int)sbOff);

        if(magic != VXFS_MAGIC)
        {
            // Try HP-UX/parisc location (block 8, offset 0x2000, big-endian)
            sbSectorOff = VXFS_BASE_BE / _imagePlugin.Info.SectorSize;
            sbOff       = VXFS_BASE_BE % _imagePlugin.Info.SectorSize;

            sbSizeInSectors = (uint)((sbOff + sbSizeInBytes + _imagePlugin.Info.SectorSize - 1) /
                                     _imagePlugin.Info.SectorSize);

            errno = _imagePlugin.ReadSectors(_partition.Start + sbSectorOff,
                                             false,
                                             sbSizeInSectors,
                                             out sbSector,
                                             out _);

            if(errno != ErrorNumber.NoError) return errno;

            if(sbOff + 4 > (uint)sbSector.Length) return ErrorNumber.InvalidArgument;

            magic = BitConverter.ToUInt32(sbSector, (int)sbOff);

            if(magic != VXFS_MAGIC_BE) return ErrorNumber.InvalidArgument;

            _bigEndian = true;
        }

        if(sbOff + sbSizeInBytes > sbSector.Length) return ErrorNumber.InvalidArgument;

        var sb = new byte[sbSizeInBytes];
        Array.Copy(sbSector, sbOff, sb, 0, sbSizeInBytes);

        _superblock = _bigEndian
                          ? Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sb)
                          : Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sb);

        // Validate superblock
        if(_superblock.vs_version < 2 || _superblock.vs_version > 4)
        {
            AaruLogging.Debug(MODULE_NAME, "Unsupported VxFS version: {0}", _superblock.vs_version);

            return ErrorNumber.InvalidArgument;
        }

        if(_superblock.vs_bsize < 512 || (_superblock.vs_bsize & _superblock.vs_bsize - 1) != 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid block size: {0}", _superblock.vs_bsize);

            return ErrorNumber.InvalidArgument;
        }

        return ErrorNumber.NoError;
    }

    /// <summary>Reads the fileset headers and sets up structural and primary inode lists</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber ReadFsHead()
    {
        if(_fsHeadIno == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No fileset header inode found in OLT");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Reading fileset header inode {0} from initial ilist", _fsHeadIno);

        // Read the fileset header inode from the initial inode list
        ErrorNumber errno = ReadInode(_fsHeadIno, out DiskInode fsHeadInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading fileset header inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Fileset header inode: mode=0x{0:X8}, orgtype={1}, size={2}",
                          fsHeadInode.vdi_mode,
                          (InodeOrgType)fsHeadInode.vdi_orgtype,
                          fsHeadInode.vdi_size);

        // Read the fileset header data from the inode
        byte[] fsHeadData = ReadInodeData(fsHeadInode);

        if(fsHeadData == null || fsHeadData.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not read fileset header data");

            return ErrorNumber.InvalidArgument;
        }

        int fshSize = System.Runtime.InteropServices.Marshal.SizeOf<FilesetHeader>();
        int blockSz = _superblock.vs_bsize;

        // Structural fileset header is at block 0 of fship data
        if(fsHeadData.Length < fshSize)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Fileset header data too small: {0} bytes, need {1}",
                              fsHeadData.Length,
                              fshSize);

            return ErrorNumber.InvalidArgument;
        }

        var sfhBytes = new byte[fshSize];
        Array.Copy(fsHeadData, 0, sfhBytes, 0, fshSize);

        FilesetHeader sfh = _bigEndian
                                ? Marshal.ByteArrayToStructureBigEndian<FilesetHeader>(sfhBytes)
                                : Marshal.ByteArrayToStructureLittleEndian<FilesetHeader>(sfhBytes);

        AaruLogging.Debug(MODULE_NAME, "Structural fileset header version: {0}", sfh.fsh_version);
        AaruLogging.Debug(MODULE_NAME, "Structural fileset header ninodes: {0}", sfh.fsh_ninodes);
        AaruLogging.Debug(MODULE_NAME, "Structural fsh ilistino[0]: {0}",        sfh.fsh_ilistino?[0]);

        if(sfh.fsh_ilistino == null || sfh.fsh_ilistino[0] == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No structural ilist inode in fileset header");

            return ErrorNumber.InvalidArgument;
        }

        // Read the structural inode list inode from the initial ilist
        AaruLogging.Debug(MODULE_NAME, "Reading structural ilist inode {0} from initial ilist", sfh.fsh_ilistino[0]);

        errno = ReadInode(sfh.fsh_ilistino[0], out _stilistInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading structural ilist inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Structural ilist inode: mode=0x{0:X8}, orgtype={1}, size={2}, blocks={3}",
                          _stilistInode.vdi_mode,
                          (InodeOrgType)_stilistInode.vdi_orgtype,
                          _stilistInode.vdi_size,
                          _stilistInode.vdi_blocks);

        // Primary fileset header is at block 1 of fship data
        if(fsHeadData.Length < blockSz + fshSize)
        {
            AaruLogging.Debug(MODULE_NAME,
                              "Fileset header data too small for primary fsh: {0} bytes, need {1}",
                              fsHeadData.Length,
                              blockSz + fshSize);

            return ErrorNumber.InvalidArgument;
        }

        var pfhBytes = new byte[fshSize];
        Array.Copy(fsHeadData, blockSz, pfhBytes, 0, fshSize);

        FilesetHeader pfh = _bigEndian
                                ? Marshal.ByteArrayToStructureBigEndian<FilesetHeader>(pfhBytes)
                                : Marshal.ByteArrayToStructureLittleEndian<FilesetHeader>(pfhBytes);

        AaruLogging.Debug(MODULE_NAME, "Primary fileset header version: {0}",  pfh.fsh_version);
        AaruLogging.Debug(MODULE_NAME, "Primary fileset header ninodes: {0}",  pfh.fsh_ninodes);
        AaruLogging.Debug(MODULE_NAME, "Primary fileset header maxinode: {0}", pfh.fsh_maxinode);
        AaruLogging.Debug(MODULE_NAME, "Primary fsh ilistino[0]: {0}",         pfh.fsh_ilistino?[0]);

        if(pfh.fsh_ilistino == null || pfh.fsh_ilistino[0] == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "No primary ilist inode in fileset header");

            return ErrorNumber.InvalidArgument;
        }

        // Read the primary inode list inode from the STRUCTURAL ilist
        AaruLogging.Debug(MODULE_NAME, "Reading primary ilist inode {0} from structural ilist", pfh.fsh_ilistino[0]);

        errno = ReadInodeFromInode(_stilistInode, pfh.fsh_ilistino[0], out _ilistInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading primary ilist inode: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Primary ilist inode: mode=0x{0:X8}, orgtype={1}, size={2}, blocks={3}",
                          _ilistInode.vdi_mode,
                          (InodeOrgType)_ilistInode.vdi_orgtype,
                          _ilistInode.vdi_size,
                          _ilistInode.vdi_blocks);

        return ErrorNumber.NoError;
    }

    /// <summary>Loads the root directory and caches its contents</summary>
    /// <returns>Error number indicating success or failure</returns>
    ErrorNumber LoadRootDirectory()
    {
        AaruLogging.Debug(MODULE_NAME, "Loading root directory...");

        _rootDirectoryCache.Clear();
        _inodeCache.Clear();

        // Read the root inode from the primary inode list
        ErrorNumber errno = ReadInodeFromInode(_ilistInode, VXFS_ROOT_INO, out DiskInode rootInode);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading root inode: {0}", errno);

            return errno;
        }

        // Validate that root inode is a directory
        var fileType = (VxfsFileType)(rootInode.vdi_mode & VXFS_TYPE_MASK);

        if(fileType != VxfsFileType.Dir)
        {
            AaruLogging.Debug(MODULE_NAME, "Root inode is not a directory (mode=0x{0:X8})", rootInode.vdi_mode);

            return ErrorNumber.InvalidArgument;
        }

        _inodeCache[VXFS_ROOT_INO] = rootInode;

        AaruLogging.Debug(MODULE_NAME, "Root inode mode: 0x{0:X8}",  rootInode.vdi_mode);
        AaruLogging.Debug(MODULE_NAME, "Root inode org type: {0}",   (InodeOrgType)rootInode.vdi_orgtype);
        AaruLogging.Debug(MODULE_NAME, "Root inode size: {0} bytes", rootInode.vdi_size);
        AaruLogging.Debug(MODULE_NAME, "Root inode blocks: {0}",     rootInode.vdi_blocks);

        // Read directory data
        byte[] dirData = ReadInodeData(rootInode);

        if(dirData == null || dirData.Length == 0)
        {
            AaruLogging.Debug(MODULE_NAME, "Could not read root directory data");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME, "Root directory data: {0} bytes", dirData.Length);

        // Parse directory entries
        int blockSize = _superblock.vs_bsize;
        var pos       = 0;

        while(pos < dirData.Length)
        {
            int blockEnd = pos + blockSize;

            if(blockEnd > dirData.Length) blockEnd = dirData.Length;

            // Read directory block header
            if(pos + 4 > dirData.Length) break;

            var dFree  = BitConverter.ToUInt16(dirData, pos);
            var dNhash = BitConverter.ToUInt16(dirData, pos + 2);

            if(_bigEndian)
            {
                dFree  = (ushort)(dFree  >> 8 | dFree  << 8);
                dNhash = (ushort)(dNhash >> 8 | dNhash << 8);
            }

            // Skip overhead: DirectoryBlock header (4 bytes) + hash table (2 * d_nhash bytes)
            // d_hash[] entries are __fs16 (2 bytes each)
            int overhead   = 4   + 2 * dNhash;
            int entryStart = pos + overhead;

            while(entryStart + 10 <= blockEnd) // minimum: 4(ino) + 2(reclen) + 2(namelen) + 2(hashnext)
            {
                var dIno     = BitConverter.ToUInt32(dirData, entryStart);
                var dReclen  = BitConverter.ToUInt16(dirData, entryStart + 4);
                var dNamelen = BitConverter.ToUInt16(dirData, entryStart + 6);

                if(_bigEndian)
                {
                    dIno = dIno >> 24 & 0xFF | dIno >> 8 & 0xFF00 | dIno << 8 & 0xFF0000 | dIno << 24 & 0xFF000000;

                    dReclen  = (ushort)(dReclen  >> 8 | dReclen  << 8);
                    dNamelen = (ushort)(dNamelen >> 8 | dNamelen << 8);
                }

                if(dReclen == 0) break;

                if(dIno != 0 && dNamelen > 0 && entryStart + 10 + dNamelen <= dirData.Length)
                {
                    string name = _encoding.GetString(dirData, entryStart + 10, dNamelen);

                    // Trim null terminators
                    int nullIdx = name.IndexOf('\0');

                    if(nullIdx >= 0) name = name[..nullIdx];

                    if(name.Length > 0)
                    {
                        _rootDirectoryCache[name] = dIno;

                        AaruLogging.Debug(MODULE_NAME, "Root dir entry: \"{0}\" -> inode {1}", name, dIno);
                    }
                }

                entryStart += dReclen;
            }

            pos = blockEnd;
        }

        AaruLogging.Debug(MODULE_NAME, "Cached {0} root directory entries", _rootDirectoryCache.Count);

        return ErrorNumber.NoError;
    }
}