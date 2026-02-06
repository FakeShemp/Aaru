// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Apple ProDOS filesystem plugin.
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

// ReSharper disable NotAccessedField.Local

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Claunia.Encoding;
using Encoding = System.Text.Encoding;
using Marshal = Aaru.Helpers.Marshal;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Information from Apple ProDOS 8 Technical Reference
/// <inheritdoc />
[SuppressMessage("ReSharper", "UnusedMember.Local")]
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class ProDOSPlugin
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Length < 3) return false;

        var multiplier = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = imagePlugin.ReadSectors(2 * multiplier + partition.Start,
                                                    false,
                                                    multiplier,
                                                    out byte[] rootDirectoryKeyBlock,
                                                    out _);

        if(errno != ErrorNumber.NoError) return false;

        var apmFromHddOnCd = false;

        if(imagePlugin.Info.SectorSize is 2352 or 2448 or 2048)
        {
            errno = imagePlugin.ReadSectors(partition.Start, false, 2, out byte[] tmp, out _);

            if(errno != ErrorNumber.NoError) return false;

            foreach(int offset in new[]
                    {
                        0, 0x200, 0x400, 0x600, 0x800, 0xA00
                    }.Where(offset => tmp.Length                                            > offset + 0x200       &&
                                      BitConverter.ToUInt16(tmp, offset)                    == 0                   &&
                                      (byte)((tmp[offset + 0x04] & STORAGE_TYPE_MASK) >> 4) == ROOT_DIRECTORY_TYPE &&
                                      tmp[offset + 0x23]                                    == ENTRY_LENGTH        &&
                                      tmp[offset + 0x24]                                    == ENTRIES_PER_BLOCK))
            {
                Array.Copy(tmp, offset, rootDirectoryKeyBlock, 0, 0x200);
                apmFromHddOnCd = true;

                break;
            }
        }

        var prePointer = BitConverter.ToUInt16(rootDirectoryKeyBlock, 0);
        AaruLogging.Debug(MODULE_NAME, "prePointer = {0}", prePointer);

        if(prePointer != 0) return false;

        var storageType = (byte)((rootDirectoryKeyBlock[0x04] & STORAGE_TYPE_MASK) >> 4);
        AaruLogging.Debug(MODULE_NAME, "storage_type = {0}", storageType);

        if(storageType != ROOT_DIRECTORY_TYPE) return false;

        byte entryLength = rootDirectoryKeyBlock[0x23];
        AaruLogging.Debug(MODULE_NAME, "entry_length = {0}", entryLength);

        if(entryLength != ENTRY_LENGTH) return false;

        byte entriesPerBlock = rootDirectoryKeyBlock[0x24];
        AaruLogging.Debug(MODULE_NAME, "entries_per_block = {0}", entriesPerBlock);

        if(entriesPerBlock != ENTRIES_PER_BLOCK) return false;

        var bitMapPointer = BitConverter.ToUInt16(rootDirectoryKeyBlock, 0x27);
        AaruLogging.Debug(MODULE_NAME, "bit_map_pointer = {0}", bitMapPointer);

        if(bitMapPointer > partition.End) return false;

        var totalBlocks = BitConverter.ToUInt16(rootDirectoryKeyBlock, 0x29);

        if(apmFromHddOnCd) totalBlocks /= 4;

        AaruLogging.Debug(MODULE_NAME,
                          "{0} <= ({1} - {2} + 1)? {3}",
                          totalBlocks,
                          partition.End,
                          partition.Start,
                          totalBlocks <= partition.End - partition.Start + 1);

        return totalBlocks <= partition.End - partition.Start + 1;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= new Apple2c();
        information =   "";
        metadata    =   new FileSystem();
        var sbInformation = new StringBuilder();
        var multiplier    = (uint)(imagePlugin.Info.SectorSize == 256 ? 2 : 1);

        // Blocks 0 and 1 are boot code
        ErrorNumber errno = imagePlugin.ReadSectors(2 * multiplier + partition.Start,
                                                    false,
                                                    multiplier,
                                                    out byte[] rootDirectoryKeyBlockBytes,
                                                    out _);

        if(errno != ErrorNumber.NoError) return;

        var apmFromHddOnCd = false;

        if(imagePlugin.Info.SectorSize is 2352 or 2448 or 2048)
        {
            errno = imagePlugin.ReadSectors(partition.Start, false, 2, out byte[] tmp, out _);

            if(errno != ErrorNumber.NoError) return;

            foreach(int offset in new[]
                    {
                        0, 0x200, 0x400, 0x600, 0x800, 0xA00
                    }.Where(offset => BitConverter.ToUInt16(tmp, offset)                    == 0                   &&
                                      (byte)((tmp[offset + 0x04] & STORAGE_TYPE_MASK) >> 4) == ROOT_DIRECTORY_TYPE &&
                                      tmp[offset + 0x23]                                    == ENTRY_LENGTH        &&
                                      tmp[offset + 0x24]                                    == ENTRIES_PER_BLOCK))
            {
                Array.Copy(tmp, offset, rootDirectoryKeyBlockBytes, 0, 0x200);
                apmFromHddOnCd = true;

                break;
            }
        }

        // Read volume directory header using marshalling (starts at offset 4, after block header)
        VolumeDirectoryHeader rootDirectoryKeyBlock =
            Marshal.ByteArrayToStructureLittleEndian<VolumeDirectoryHeader>(rootDirectoryKeyBlockBytes,
                                                                            4,
                                                                            System.Runtime.InteropServices.Marshal
                                                                               .SizeOf<VolumeDirectoryHeader>());

        var    nameLength = (byte)(rootDirectoryKeyBlock.storage_type_name_length & NAME_LENGTH_MASK);
        string volumeName = encoding.GetString(rootDirectoryKeyBlock.volume_name, 0, nameLength);

        bool dateCorrect;

        DateTime creationTime = DateHandlers.ProDosToDateTime(rootDirectoryKeyBlock.creation_date,
                                                              rootDirectoryKeyBlock.creation_time);

        dateCorrect = creationTime != DateTime.MinValue;

        if(apmFromHddOnCd)
        {
            sbInformation.AppendLine(Localization.ProDOS_uses_512_bytes_sector_while_device_uses_2048_bytes_sector)
                         .AppendLine();
        }

        if(rootDirectoryKeyBlock.version != VERSION1 || rootDirectoryKeyBlock.min_version != VERSION1)
        {
            sbInformation.AppendLine(Localization.Warning_Detected_unknown_ProDOS_version_ProDOS_filesystem);
            sbInformation.AppendLine(Localization.All_of_the_following_information_may_be_incorrect);
        }

        if(rootDirectoryKeyBlock.version == VERSION1)
            sbInformation.AppendLine(Localization.ProDOS_version_one_used_to_create_this_volume);
        else
        {
            sbInformation.AppendFormat(Localization.Unknown_ProDOS_version_with_field_0_used_to_create_this_volume,
                                       rootDirectoryKeyBlock.version)
                         .AppendLine();
        }

        if(rootDirectoryKeyBlock.min_version == VERSION1)
            sbInformation.AppendLine(Localization.ProDOS_version_one_at_least_required_for_reading_this_volume);
        else
        {
            sbInformation
               .AppendFormat(Localization
                                .Unknown_ProDOS_version_with_field_0_is_at_least_required_for_reading_this_volume,
                             rootDirectoryKeyBlock.min_version)
               .AppendLine();
        }

        sbInformation.AppendFormat(Localization.Volume_name_is_0, volumeName).AppendLine();

        if(dateCorrect) sbInformation.AppendFormat(Localization.Volume_created_on_0, creationTime).AppendLine();

        sbInformation.AppendFormat(Localization._0_bytes_per_directory_entry, rootDirectoryKeyBlock.entry_length)
                     .AppendLine();

        sbInformation.AppendFormat(Localization._0_entries_per_directory_block, rootDirectoryKeyBlock.entries_per_block)
                     .AppendLine();

        sbInformation.AppendFormat(Localization._0_files_in_root_directory, rootDirectoryKeyBlock.entry_count)
                     .AppendLine();

        sbInformation.AppendFormat(Localization._0_blocks_in_volume, rootDirectoryKeyBlock.total_blocks).AppendLine();

        sbInformation.AppendFormat(Localization.Bitmap_starts_at_block_0, rootDirectoryKeyBlock.bitmap_block)
                     .AppendLine();

        if((rootDirectoryKeyBlock.access & READ_ATTRIBUTE) == READ_ATTRIBUTE)
            sbInformation.AppendLine(Localization.Volume_can_be_read);

        if((rootDirectoryKeyBlock.access & WRITE_ATTRIBUTE) == WRITE_ATTRIBUTE)
            sbInformation.AppendLine(Localization.Volume_can_be_written);

        if((rootDirectoryKeyBlock.access & RENAME_ATTRIBUTE) == RENAME_ATTRIBUTE)
            sbInformation.AppendLine(Localization.Volume_can_be_renamed);

        if((rootDirectoryKeyBlock.access & DESTROY_ATTRIBUTE) == DESTROY_ATTRIBUTE)
            sbInformation.AppendLine(Localization.Volume_can_be_destroyed);

        if((rootDirectoryKeyBlock.access & BACKUP_ATTRIBUTE) == BACKUP_ATTRIBUTE)
            sbInformation.AppendLine(Localization.Volume_must_be_backed_up);

        // TODO: Fix mask
        if((rootDirectoryKeyBlock.access & RESERVED_ATTRIBUTE_MASK) != 0)
            AaruLogging.Debug(MODULE_NAME, Localization.Reserved_attributes_are_set_0, rootDirectoryKeyBlock.access);

        information = sbInformation.ToString();

        metadata = new FileSystem
        {
            VolumeName = volumeName,
            Files      = rootDirectoryKeyBlock.entry_count,
            Clusters   = rootDirectoryKeyBlock.total_blocks,
            Type       = FS_TYPE
        };

        metadata.ClusterSize =
            (uint)((partition.End - partition.Start + 1) * imagePlugin.Info.SectorSize / metadata.Clusters);

        if(!dateCorrect) return;

        metadata.CreationDate = creationTime;
    }

#endregion
}