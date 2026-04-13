// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : PlayStation FileSystem plugin.
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Helpers;
using FileSystemInfo = Aaru.CommonTypes.Structs.FileSystemInfo;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public partial class SonyPFS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding = encoding ?? Encoding.ASCII;
        _image    = imagePlugin;

        _sectorSize     = imagePlugin.Info.SectorSize;
        _partitionStart = partition.Start;

        if(_sectorSize < 512) return ErrorNumber.InvalidArgument;

        // Read superblock from sector 0 of partition data area
        int sbSize        = Marshal.SizeOf<SuperBlock>();
        var sectorsToRead = (uint)((sbSize + _sectorSize - 1) / _sectorSize);

        ErrorNumber errno = imagePlugin.ReadSectors(partition.Start, false, sectorsToRead, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        if(sector.Length < sbSize) return ErrorNumber.InvalidArgument;

        _superBlock = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

        if(_superBlock.magic != PFS_SUPER_MAGIC) return ErrorNumber.InvalidArgument;

        if(_superBlock.version > PFS_FORMAT_VERSION) return ErrorNumber.InvalidArgument;

        // Validate zone size is a power of 2 and within range
        if((_superBlock.zone_size & _superBlock.zone_size - 1) != 0   ||
           _superBlock.zone_size                               < 2048 ||
           _superBlock.zone_size                               > 131072)
            return ErrorNumber.InvalidArgument;

        // Calculate scale factors (matching libpfs pfsMountSuperBlock)
        // sector_scale = log2(zone_size / 512)
        // inode_scale  = log2(zone_size / PFS_META_SIZE)
        _sectorsPerZone = _superBlock.zone_size / _sectorSize;
        _inodeScale     = GetScale(_superBlock.zone_size, PFS_META_SIZE);
        _blockScale     = GetScale(PFS_META_SIZE,         _sectorSize);

        // Read root directory inode
        errno = ReadInode(_superBlock.root.number, _superBlock.root.subpart, out Inode rootInode);

        if(errno != ErrorNumber.NoError) return errno;

        if((rootInode.mode & (ushort)FileType.IFMT) != (ushort)FileType.IFDIR) return ErrorNumber.InvalidArgument;

        // Read and cache root directory
        _rootDirectoryCache = ReadDirectory(rootInode);
        _directoryCache     = new Dictionary<string, Dictionary<string, DirEntry>>();

        // Calculate total zones for the main partition
        ulong partitionSectors = partition.End - partition.Start + 1;
        ulong totalZones       = partitionSectors / _sectorsPerZone;

        Metadata = new FileSystem
        {
            Type        = FS_TYPE,
            ClusterSize = _superBlock.zone_size,
            Clusters    = totalZones,
            Dirty       = _superBlock.pfsFsckStat != 0
        };

        _statfs = new FileSystemInfo
        {
            Blocks         = totalZones,
            FilenameLength = PFS_NAME_LEN,
            FreeBlocks     = 0,
            Id             = new FileSystemId(),
            PluginId       = Id,
            Type           = FS_TYPE
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        _rootDirectoryCache = null;
        _directoryCache     = null;
        _mounted            = false;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber StatFs(out FileSystemInfo stat)
    {
        stat = null;

        if(!_mounted) return ErrorNumber.AccessDenied;

        stat = _statfs.ShallowCopy();

        return ErrorNumber.NoError;
    }

    /// <summary>Calculates log2(num / size), matching libpfs pfsGetScale.</summary>
    static uint GetScale(uint num, uint size)
    {
        uint scale = 0;

        while(size << (int)scale != num) scale++;

        return scale;
    }
}