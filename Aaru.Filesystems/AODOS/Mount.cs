// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Mount.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : AO-DOS file system plugin.
//
// --[ Description ] ----------------------------------------------------------
//
//     Mounts the AO-DOS file system and caches the root directory.
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
using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;
using Marshal = Aaru.Helpers.Marshal;

namespace Aaru.Filesystems;

public sealed partial class AODOS
{
    /// <inheritdoc />
    public ErrorNumber Mount(IMediaImage                imagePlugin, Partition partition, Encoding encoding,
                             Dictionary<string, string> options,     string    @namespace)
    {
        AaruLogging.Debug(MODULE_NAME, "Mounting volume");

        _imagePlugin = imagePlugin;
        _partition   = partition;

        // AO-DOS uses KOI8-R encoding
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        _encoding = Encoding.GetEncoding("koi8-r");

        // Validate sector size
        if(imagePlugin.Info.SectorSize != SECTOR_SIZE)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid sector size: {0}", imagePlugin.Info.SectorSize);

            return ErrorNumber.InvalidArgument;
        }

        // Validate disk size (AO-DOS only supports 800 or 1600 sector disks)
        if(imagePlugin.Info.Sectors != 800 && imagePlugin.Info.Sectors != 1600)
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid disk size: {0} sectors", imagePlugin.Info.Sectors);

            return ErrorNumber.InvalidArgument;
        }

        // Read and validate boot block (sector 0)
        ErrorNumber errno = imagePlugin.ReadSector(partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error reading boot block: {0}", errno);

            return errno;
        }

        _bootBlock = Marshal.ByteArrayToStructureLittleEndian<BootBlock>(sector);

        // Validate identifier
        if(!_bootBlock.identifier.SequenceEqual(_identifier))
        {
            AaruLogging.Debug(MODULE_NAME, "Invalid AO-DOS identifier");

            return ErrorNumber.InvalidArgument;
        }

        AaruLogging.Debug(MODULE_NAME,
                          "Boot block validated: {0} files, {1} used sectors",
                          _bootBlock.files,
                          _bootBlock.usedSectors);

        // Load the directory
        errno = LoadDirectory();

        if(errno != ErrorNumber.NoError)
        {
            AaruLogging.Debug(MODULE_NAME, "Error loading directory: {0}", errno);

            return errno;
        }

        AaruLogging.Debug(MODULE_NAME, "Directory loaded with {0} entries", _directoryCache.Count);

        Metadata = new FileSystem
        {
            Type         = FS_TYPE,
            Clusters     = imagePlugin.Info.Sectors,
            ClusterSize  = imagePlugin.Info.SectorSize,
            Files        = _bootBlock.files,
            FreeClusters = imagePlugin.Info.Sectors - _bootBlock.usedSectors,
            VolumeName   = StringHandlers.SpacePaddedToString(_bootBlock.volumeLabel, _encoding),
            Bootable     = true
        };

        _mounted = true;

        AaruLogging.Debug(MODULE_NAME, "Mount complete");

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber Unmount()
    {
        if(!_mounted) return ErrorNumber.AccessDenied;

        AaruLogging.Debug(MODULE_NAME, "Unmounting volume");

        _directoryCache.Clear();
        _mounted     = false;
        _imagePlugin = null;
        _partition   = default(Partition);
        _bootBlock   = default(BootBlock);
        _encoding    = null;
        Metadata     = null;

        AaruLogging.Debug(MODULE_NAME, "Volume unmounted successfully");


        return ErrorNumber.NoError;
    }
}