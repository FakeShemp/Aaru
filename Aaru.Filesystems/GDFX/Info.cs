// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
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
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Logging;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class GDFX
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        (ulong baseOffset, uint vdSector)[] probes =
        [
            (STANDARD_OFFSET, VD_SECTOR), (GLOBAL_PARTITION_OFFSET, VD_SECTOR), (XGD3_PARTITION_OFFSET, VD_SECTOR),
            (XGD1_PARTITION_OFFSET, VD_SECTOR), (STANDARD_OFFSET, REBUILT_VD_SECTOR)
        ];

        byte[] magicBytes = Encoding.ASCII.GetBytes(MAGIC);

        foreach((ulong baseOffset, uint vdSector) in probes)
        {
            ulong absoluteSector = baseOffset / SECTOR_SIZE + vdSector + partition.Start;

            if(absoluteSector >= partition.End) continue;

            ErrorNumber errno = imagePlugin.ReadSector(absoluteSector, false, out byte[] sector, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(sector.Length < MAGIC1_OFFSET + MAGIC.Length) continue;

            var magic0Match = true;
            var magic1Match = true;

            for(var i = 0; i < magicBytes.Length; i++)
            {
                if(sector[i] != magicBytes[i]) magic0Match = false;

                if(sector[MAGIC1_OFFSET + i] != magicBytes[i]) magic1Match = false;
            }

            if(!magic0Match || !magic1Match) continue;

            AaruLogging.Debug(MODULE_NAME,
                              "Found XDVDFS volume descriptor at base offset 0x{0:X8}, VD sector {1}",
                              baseOffset,
                              vdSector);

            return true;
        }

        return false;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();

        (ulong baseOffset, uint vdSector)[] probes =
        [
            (STANDARD_OFFSET, VD_SECTOR), (GLOBAL_PARTITION_OFFSET, VD_SECTOR), (XGD3_PARTITION_OFFSET, VD_SECTOR),
            (XGD1_PARTITION_OFFSET, VD_SECTOR), (STANDARD_OFFSET, REBUILT_VD_SECTOR)
        ];

        byte[] magicBytes      = Encoding.ASCII.GetBytes(MAGIC);
        byte[] sector          = null;
        ulong  matchBaseOffset = 0;
        uint   matchVdSector   = VD_SECTOR;

        foreach((ulong baseOffset, uint vdSector) in probes)
        {
            ulong absoluteSector = baseOffset / SECTOR_SIZE + vdSector + partition.Start;

            if(absoluteSector >= partition.End) continue;

            ErrorNumber errno = imagePlugin.ReadSector(absoluteSector, false, out byte[] s, out _);

            if(errno != ErrorNumber.NoError) continue;

            if(s.Length < MAGIC1_OFFSET + MAGIC.Length) continue;

            var magic0Match = true;
            var magic1Match = true;

            for(var i = 0; i < magicBytes.Length; i++)
            {
                if(s[i] != magicBytes[i]) magic0Match = false;

                if(s[MAGIC1_OFFSET + i] != magicBytes[i]) magic1Match = false;
            }

            if(!magic0Match || !magic1Match) continue;

            sector          = s;
            matchBaseOffset = baseOffset;
            matchVdSector   = vdSector;

            break;
        }

        if(sector is null) return;

        VolumeDescriptor vd = Marshal.ByteArrayToStructureLittleEndian<VolumeDescriptor>(sector);

        var sb = new StringBuilder();
        sb.AppendLine(Localization.Xbox_DVD_File_System);

        string discType = (matchBaseOffset, matchVdSector) switch
                          {
                              (var b, REBUILT_VD_SECTOR) when b == STANDARD_OFFSET => Localization.Rebuilt_XISO,
                              var (b, _) when b                 == XGD1_PARTITION_OFFSET => Localization.XGD1,
                              var (b, _) when b                 == GLOBAL_PARTITION_OFFSET => Localization.XGD2,
                              var (b, _) when b                 == XGD3_PARTITION_OFFSET => Localization.XGD3,
                              _ => Localization.Standard_Xbox_image
                          };

        sb.AppendFormat(Localization.Disc_type_0,                 discType).AppendLine();
        sb.AppendFormat(Localization.Root_directory_sector_0,     vd.rootDirSector).AppendLine();
        sb.AppendFormat(Localization.Root_directory_size_0_bytes, vd.rootDirSize).AppendLine();

        if(vd.fileTime > 0)
        {
            var creationDate = DateTime.FromFileTimeUtc((long)vd.fileTime);
            sb.AppendFormat(Localization.Volume_created_on_0, creationDate).AppendLine();
            metadata.CreationDate = creationDate;
        }

        information = sb.ToString();

        metadata.Type        = FS_TYPE;
        metadata.ClusterSize = SECTOR_SIZE;
        metadata.Clusters    = partition.Size / SECTOR_SIZE;
    }

#endregion
}