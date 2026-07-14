// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Microsoft Xbox DVD File System plugin.
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
using Aaru.CommonTypes.Structs;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _imagePlugin = imagePlugin;
        _partition   = partition;
        _encoding    = encoding ?? Encoding.GetEncoding(1252);

        options ??= new Dictionary<string, string>();

        if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out _debug);

        (ulong baseOffset, uint vdSector)[] probes =
        [
            (STANDARD_OFFSET, VD_SECTOR), (GLOBAL_PARTITION_OFFSET, VD_SECTOR), (XGD3_PARTITION_OFFSET, VD_SECTOR),
            (XGD1_PARTITION_OFFSET, VD_SECTOR), (STANDARD_OFFSET, REBUILT_VD_SECTOR)
        ];

        byte[] magicBytes = Encoding.ASCII.GetBytes(MAGIC);
        byte[] vdSector   = null;

        foreach((ulong baseOffset, uint vdSect) in probes)
        {
            ulong absoluteSector = baseOffset / SECTOR_SIZE + vdSect + partition.Start;

            if(absoluteSector >= partition.End) continue;

            ErrorNumber readErrno = imagePlugin.ReadSector(absoluteSector, false, out byte[] s, out _);

            if(readErrno != ErrorNumber.NoError) continue;

            if(s.Length < MAGIC1_OFFSET + MAGIC.Length) continue;

            var magic0Match = true;
            var magic1Match = true;

            for(var i = 0; i < magicBytes.Length; i++)
            {
                if(s[i] != magicBytes[i]) magic0Match = false;

                if(s[MAGIC1_OFFSET + i] != magicBytes[i]) magic1Match = false;
            }

            if(!magic0Match || !magic1Match) continue;

            _partitionBaseOffset = baseOffset;
            vdSector             = s;

            AaruLogging.Debug(MODULE_NAME,
                              "Mounting XDVDFS at base offset 0x{0:X8}, VD sector {1}",
                              baseOffset,
                              vdSect);

            break;
        }

        if(vdSector is null) return ErrorNumber.InvalidArgument;

        _volumeDescriptor = Marshal.ByteArrayToStructureLittleEndian<VolumeDescriptor>(vdSector);

        if(_volumeDescriptor.rootDirSize == 0) return ErrorNumber.InvalidArgument;

        _directoryCache = new Dictionary<string, List<DecodedEntry>>(StringComparer.OrdinalIgnoreCase);

        ErrorNumber errno = CacheDirectory("/", _volumeDescriptor.rootDirSector, _volumeDescriptor.rootDirSize);

        if(errno != ErrorNumber.NoError) return errno;

        _statFs = new FileSystemInfo
        {
            Blocks         = partition.Size / SECTOR_SIZE,
            FilenameLength = 255,
            FreeBlocks     = 0,
            PluginId       = Id,
            Type           = FS_TYPE
        };

        DateTime? creationDate = _volumeDescriptor.fileTime > 0
                                     ? DateTime.FromFileTimeUtc((long)_volumeDescriptor.fileTime)
                                     : null;

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            ClusterSize  = SECTOR_SIZE,
            Clusters     = _statFs.Blocks,
            CreationDate = creationDate
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        _mounted        = false;
        _directoryCache = null;
        _imagePlugin    = null;

        return ErrorNumber.NoError;
    }
}