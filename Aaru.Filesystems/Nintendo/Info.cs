// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Nintendo optical filesystems plugin.
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

using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

public sealed partial class NintendoPlugin
{
#region IReadOnlyFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Start != 0) return false;

        if(imagePlugin.Info.Sectors * imagePlugin.Info.SectorSize < 0x50000) return false;

        ErrorNumber errno =
            imagePlugin.ReadSectors(0, false, 0x50000 / imagePlugin.Info.SectorSize, out byte[] header, out _);

        if(errno != ErrorNumber.NoError) return false;

        var magicGc  = BigEndianBitConverter.ToUInt32(header, 0x1C);
        var magicWii = BigEndianBitConverter.ToUInt32(header, 0x18);

        return magicGc == 0xC2339F3D || magicWii == 0x5D1C9EA3;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding ??= Encoding.GetEncoding("shift_jis");
        var sbInformation = new StringBuilder();
        information = "";
        metadata    = new FileSystem();

        ErrorNumber errno =
            imagePlugin.ReadSectors(0, false, 0x50000 / imagePlugin.Info.SectorSize, out byte[] header, out _);

        if(errno != ErrorNumber.NoError) return;

        var wii = false;

        DiscHeader discHeader = Marshal.ByteArrayToStructureBigEndian<DiscHeader>(header);

        if(discHeader.WiiMagic == 0x5D1C9EA3)
            wii = true;
        else if(discHeader.GcMagic != 0xC2339F3D) return;

        string discType = Encoding.ASCII.GetString(new[]
        {
            discHeader.DiscType
        });

        string gameCode = Encoding.ASCII.GetString(discHeader.GameCode);

        string regionCode = Encoding.ASCII.GetString(new[]
        {
            discHeader.RegionCode
        });

        string publisherCode = Encoding.ASCII.GetString(discHeader.PublisherCode);
        string discId        = discType + gameCode + regionCode + publisherCode;
        string title         = StringHandlers.CToString(discHeader.Title, encoding);

        WiiPartitionTableEntry[] firstPartitions;
        WiiPartitionTableEntry[] secondPartitions;
        WiiPartitionTableEntry[] thirdPartitions;
        WiiPartitionTableEntry[] fourthPartitions;
        WiiRegionSettings        regionSettings = default;

        if(wii)
        {
            uint offset1 = BigEndianBitConverter.ToUInt32(header, 0x40004) << 2;
            uint offset2 = BigEndianBitConverter.ToUInt32(header, 0x4000C) << 2;
            uint offset3 = BigEndianBitConverter.ToUInt32(header, 0x40014) << 2;
            uint offset4 = BigEndianBitConverter.ToUInt32(header, 0x4001C) << 2;

            firstPartitions  = new WiiPartitionTableEntry[BigEndianBitConverter.ToUInt32(header, 0x40000)];
            secondPartitions = new WiiPartitionTableEntry[BigEndianBitConverter.ToUInt32(header, 0x40008)];
            thirdPartitions  = new WiiPartitionTableEntry[BigEndianBitConverter.ToUInt32(header, 0x40010)];
            fourthPartitions = new WiiPartitionTableEntry[BigEndianBitConverter.ToUInt32(header, 0x40018)];

            for(var i = 0; i < firstPartitions.Length; i++)
            {
                if(offset1 + i * 8 + 8 >= 0x50000) continue;

                firstPartitions[i] =
                    Marshal.ByteArrayToStructureBigEndian<WiiPartitionTableEntry>(header, (int)(offset1 + i * 8), 8);

                firstPartitions[i].Offset <<= 2;
            }

            for(var i = 0; i < secondPartitions.Length; i++)
            {
                if(offset2 + i * 8 + 8 >= 0x50000) continue;

                secondPartitions[i] =
                    Marshal.ByteArrayToStructureBigEndian<WiiPartitionTableEntry>(header, (int)(offset2 + i * 8), 8);

                secondPartitions[i].Offset <<= 2;
            }

            for(var i = 0; i < thirdPartitions.Length; i++)
            {
                if(offset3 + i * 8 + 8 >= 0x50000) continue;

                thirdPartitions[i] =
                    Marshal.ByteArrayToStructureBigEndian<WiiPartitionTableEntry>(header, (int)(offset3 + i * 8), 8);

                thirdPartitions[i].Offset <<= 2;
            }

            for(var i = 0; i < fourthPartitions.Length; i++)
            {
                if(offset4 + i * 8 + 8 >= 0x50000) continue;

                fourthPartitions[i] =
                    Marshal.ByteArrayToStructureBigEndian<WiiPartitionTableEntry>(header, (int)(offset4 + i * 8), 8);

                fourthPartitions[i].Offset <<= 2;
            }

            regionSettings = Marshal.ByteArrayToStructureBigEndian<WiiRegionSettings>(header, 0x4E000, 26);
        }
        else
        {
            firstPartitions  = [];
            secondPartitions = [];
            thirdPartitions  = [];
            fourthPartitions = [];
        }

        AaruLogging.Debug(MODULE_NAME, "discType = {0}",         discType);
        AaruLogging.Debug(MODULE_NAME, "gameCode = {0}",         gameCode);
        AaruLogging.Debug(MODULE_NAME, "regionCode = {0}",       regionCode);
        AaruLogging.Debug(MODULE_NAME, "publisherCode = {0}",    publisherCode);
        AaruLogging.Debug(MODULE_NAME, "discID = {0}",           discId);
        AaruLogging.Debug(MODULE_NAME, "discNumber = {0}",       discHeader.DiscNumber);
        AaruLogging.Debug(MODULE_NAME, "discVersion = {0}",      discHeader.DiscVersion);
        AaruLogging.Debug(MODULE_NAME, "streaming = {0}",        discHeader.Streaming > 0);
        AaruLogging.Debug(MODULE_NAME, "streamBufferSize = {0}", discHeader.StreamBufferSize);
        AaruLogging.Debug(MODULE_NAME, "title = \"{0}\"",        title);
        AaruLogging.Debug(MODULE_NAME, "debugOff = 0x{0:X8}",    discHeader.DebugOff);
        AaruLogging.Debug(MODULE_NAME, "debugAddr = 0x{0:X8}",   discHeader.DebugAddr);
        AaruLogging.Debug(MODULE_NAME, "dolOff = 0x{0:X8}",      discHeader.DolOff);
        AaruLogging.Debug(MODULE_NAME, "fstOff = 0x{0:X8}",      discHeader.FstOff);
        AaruLogging.Debug(MODULE_NAME, "fstSize = {0}",          discHeader.FstSize);
        AaruLogging.Debug(MODULE_NAME, "fstMax = {0}",           discHeader.FstMax);

        for(var i = 0; i < firstPartitions.Length; i++)
        {
            AaruLogging.Debug(MODULE_NAME, "firstPartitions[{1}].offset = {0}", firstPartitions[i].Offset, i);

            AaruLogging.Debug(MODULE_NAME, "firstPartitions[{1}].type = {0}", firstPartitions[i].Type, i);
        }

        for(var i = 0; i < secondPartitions.Length; i++)
        {
            AaruLogging.Debug(MODULE_NAME, "secondPartitions[{1}].offset = {0}", secondPartitions[i].Offset, i);

            AaruLogging.Debug(MODULE_NAME, "secondPartitions[{1}].type = {0}", secondPartitions[i].Type, i);
        }

        for(var i = 0; i < thirdPartitions.Length; i++)
        {
            AaruLogging.Debug(MODULE_NAME, "thirdPartitions[{1}].offset = {0}", thirdPartitions[i].Offset, i);

            AaruLogging.Debug(MODULE_NAME, "thirdPartitions[{1}].type = {0}", thirdPartitions[i].Type, i);
        }

        for(var i = 0; i < fourthPartitions.Length; i++)
        {
            AaruLogging.Debug(MODULE_NAME, "fourthPartitions[{1}].offset = {0}", fourthPartitions[i].Offset, i);

            AaruLogging.Debug(MODULE_NAME, "fourthPartitions[{1}].type = {0}", fourthPartitions[i].Type, i);
        }

        AaruLogging.Debug(MODULE_NAME, "region = {0}",       regionSettings.Region);
        AaruLogging.Debug(MODULE_NAME, "japanAge = {0}",     regionSettings.JapanAge);
        AaruLogging.Debug(MODULE_NAME, "usaAge = {0}",       regionSettings.UsaAge);
        AaruLogging.Debug(MODULE_NAME, "germanAge = {0}",    regionSettings.GermanAge);
        AaruLogging.Debug(MODULE_NAME, "pegiAge = {0}",      regionSettings.PegiAge);
        AaruLogging.Debug(MODULE_NAME, "finlandAge = {0}",   regionSettings.FinlandAge);
        AaruLogging.Debug(MODULE_NAME, "portugalAge = {0}",  regionSettings.PortugalAge);
        AaruLogging.Debug(MODULE_NAME, "ukAge = {0}",        regionSettings.UkAge);
        AaruLogging.Debug(MODULE_NAME, "australiaAge = {0}", regionSettings.AustraliaAge);
        AaruLogging.Debug(MODULE_NAME, "koreaAge = {0}",     regionSettings.KoreaAge);

        sbInformation.AppendLine(Localization.Nintendo_optical_filesystem);

        sbInformation.AppendLine(wii
                                     ? Localization.Nintendo_Wii_Optical_Disc
                                     : Localization.Nintendo_GameCube_Optical_Disc);

        sbInformation.AppendFormat(Localization.Disc_ID_is_0,     discId).AppendLine();
        sbInformation.AppendFormat(Localization.Disc_is_a_0_disc, DiscTypeToString(discType)).AppendLine();
        sbInformation.AppendFormat(Localization.Disc_region_is_0, RegionCodeToString(regionCode)).AppendLine();

        sbInformation.AppendFormat(Localization.Published_by_0, PublisherCodeToString(publisherCode)).AppendLine();

        if(discHeader.DiscNumber > 0)
        {
            sbInformation.AppendFormat(Localization.Disc_number_0_of_a_multi_disc_set, discHeader.DiscNumber + 1)
                         .AppendLine();
        }

        if(discHeader.Streaming > 0) sbInformation.AppendLine(Localization.Disc_is_prepared_for_audio_streaming);

        if(discHeader.StreamBufferSize > 0)
        {
            sbInformation.AppendFormat(Localization.Audio_streaming_buffer_size_is_0_bytes, discHeader.StreamBufferSize)
                         .AppendLine();
        }

        sbInformation.AppendFormat(Localization.Title_0, title).AppendLine();

        if(wii)
        {
            for(var i = 0; i < firstPartitions.Length; i++)
            {
                sbInformation.AppendFormat(Localization.First_0_partition_starts_at_sector_1,
                                           PartitionTypeToString(firstPartitions[i].Type),
                                           firstPartitions[i].Offset / 2048)
                             .AppendLine();
            }

            for(var i = 0; i < secondPartitions.Length; i++)
            {
                sbInformation.AppendFormat(Localization.Second_0_partition_starts_at_sector_1,
                                           PartitionTypeToString(secondPartitions[i].Type),
                                           secondPartitions[i].Offset / 2048)
                             .AppendLine();
            }

            for(var i = 0; i < thirdPartitions.Length; i++)
            {
                sbInformation.AppendFormat(Localization.Third_0_partition_starts_at_sector_1,
                                           PartitionTypeToString(thirdPartitions[i].Type),
                                           thirdPartitions[i].Offset / 2048)
                             .AppendLine();
            }

            for(var i = 0; i < fourthPartitions.Length; i++)
            {
                sbInformation.AppendFormat(Localization.Fourth_0_partition_starts_at_sector_1,
                                           PartitionTypeToString(fourthPartitions[i].Type),
                                           fourthPartitions[i].Offset / 2048)
                             .AppendLine();
            }

            //                sbInformation.AppendFormat("Region byte is {0}", regionSettings.Region).AppendLine();
            if((regionSettings.JapanAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.Japan_age_rating_is_0, regionSettings.JapanAge).AppendLine();

            if((regionSettings.UsaAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.ESRB_age_rating_is_0, regionSettings.UsaAge).AppendLine();

            if((regionSettings.GermanAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.German_age_rating_is_0, regionSettings.GermanAge).AppendLine();

            if((regionSettings.PegiAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.PEGI_age_rating_is_0, regionSettings.PegiAge).AppendLine();

            if((regionSettings.FinlandAge & 0x80) != 0x80)
            {
                sbInformation.AppendFormat(Localization.Finland_age_rating_is_0, regionSettings.FinlandAge)
                             .AppendLine();
            }

            if((regionSettings.PortugalAge & 0x80) != 0x80)
            {
                sbInformation.AppendFormat(Localization.Portugal_age_rating_is_0, regionSettings.PortugalAge)
                             .AppendLine();
            }

            if((regionSettings.UkAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.UK_age_rating_is_0, regionSettings.UkAge).AppendLine();

            if((regionSettings.AustraliaAge & 0x80) != 0x80)
            {
                sbInformation.AppendFormat(Localization.Australia_age_rating_is_0, regionSettings.AustraliaAge)
                             .AppendLine();
            }

            if((regionSettings.KoreaAge & 0x80) != 0x80)
                sbInformation.AppendFormat(Localization.Korea_age_rating_is_0, regionSettings.KoreaAge).AppendLine();
        }
        else
        {
            sbInformation
               .AppendFormat(Localization.FST_starts_at_0_and_has_1_bytes, discHeader.FstOff, discHeader.FstSize)
               .AppendLine();
        }

        information           = sbInformation.ToString();
        metadata.Bootable     = true;
        metadata.Clusters     = imagePlugin.Info.Sectors * imagePlugin.Info.SectorSize / 2048;
        metadata.ClusterSize  = 2048;
        metadata.Type         = wii ? FS_TYPE_WII : FS_TYPE_NGC;
        metadata.VolumeName   = title;
        metadata.VolumeSerial = discId;
    }

#endregion
}