// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : HP Logical Interchange Format plugin
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
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class LIF
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        _encoding    = encoding ?? Encoding.GetEncoding("iso-8859-15");
        _imagePlugin = imagePlugin;
        _partition   = partition;

        options ??= new Dictionary<string, string>();

        if(options.TryGetValue("debug", out string debugString)) bool.TryParse(debugString, out _debug);

        if(imagePlugin.Info.SectorSize < 256) return ErrorNumber.InvalidArgument;

        // Read the system block (record 0)
        ErrorNumber errno = imagePlugin.ReadSector(partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return errno;

        _systemBlock = Marshal.ByteArrayToStructureBigEndian<SystemBlock>(sector);

        AaruLogging.Debug(MODULE_NAME, Localization.magic_0_expected_1, _systemBlock.magic, LIF_MAGIC);

        if(_systemBlock.magic != LIF_MAGIC) return ErrorNumber.InvalidArgument;

        // Validate directory parameters
        if(_systemBlock.directoryStart == 0 || _systemBlock.directorySize == 0) return ErrorNumber.InvalidArgument;

        // Read the directory area
        errno = imagePlugin.ReadSectors(partition.Start + _systemBlock.directoryStart,
                                        false,
                                        _systemBlock.directorySize,
                                        out byte[] directoryData,
                                        out _);

        if(errno != ErrorNumber.NoError) return errno;

        // Parse directory entries (each is 32 bytes)
        const int directoryEntrySize = 32;

        _rootDirectoryCache = [];

        for(var offset = 0; offset + directoryEntrySize <= directoryData.Length; offset += directoryEntrySize)
        {
            DirectoryEntry entry =
                Marshal.ByteArrayToStructureBigEndian<DirectoryEntry>(directoryData, offset, directoryEntrySize);

            // End of directory marker
            if(entry.fileType == 0xFFFF) break;

            // Skip purged (deleted) entries
            if(entry.fileName[0] == 0xFF) continue;

            _rootDirectoryCache.Add(entry);
        }

        _statFs = new FileSystemInfo
        {
            Blocks         = partition.Size / 256,
            FilenameLength = 10,
            Files          = (ulong)_rootDirectoryCache.Count,
            PluginId       = Id,
            Type           = FS_TYPE
        };

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            ClusterSize  = 256,
            Clusters     = _statFs.Blocks,
            Files        = _statFs.Files,
            CreationDate = DateHandlers.LifToDateTime(_systemBlock.creationDate),
            VolumeName   = StringHandlers.CToString(_systemBlock.volumeLabel, _encoding)
        };

        _mounted = true;

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        _mounted            = false;
        _rootDirectoryCache = null;
        _imagePlugin        = null;

        return ErrorNumber.NoError;
    }
}