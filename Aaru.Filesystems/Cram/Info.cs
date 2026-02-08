// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Cram file system plugin.
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

// ReSharper disable UnusedMember.Local

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

/// <inheritdoc />
[SuppressMessage("ReSharper", "UnusedType.Local")]
public sealed partial class Cram
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(partition.Start >= partition.End) return false;

        ErrorNumber errno = imagePlugin.ReadSector(partition.Start, false, out byte[] sector, out _);

        if(errno != ErrorNumber.NoError) return false;

        var magic = BitConverter.ToUInt32(sector, 0x00);

        return magic is CRAM_MAGIC or CRAM_CIGAM;
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        encoding    ??= Encoding.GetEncoding("iso-8859-15");
        information =   "";
        ErrorNumber errno = imagePlugin.ReadSector(partition.Start, false, out byte[] sector, out _);
        metadata = new FileSystem();

        if(errno != ErrorNumber.NoError) return;

        var magic = BitConverter.ToUInt32(sector, 0x00);

        var crSb         = new SuperBlock();
        var littleEndian = true;

        switch(magic)
        {
            case CRAM_MAGIC:
                crSb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(sector);

                break;
            case CRAM_CIGAM:
                crSb         = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(sector);
                littleEndian = false;

                break;
        }

        var sbInformation = new StringBuilder();

        sbInformation.AppendLine(Localization.Cram_file_system);
        sbInformation.AppendLine(littleEndian ? Localization.Little_endian : Localization.Big_endian);
        sbInformation.AppendFormat(Localization.Volume_edition_0, crSb.fsid.edition).AppendLine();

        sbInformation.AppendFormat(Localization.Volume_name_0, StringHandlers.CToString(crSb.name, encoding))
                     .AppendLine();

        sbInformation.AppendFormat(Localization.Volume_has_0_bytes,  crSb.size).AppendLine();
        sbInformation.AppendFormat(Localization.Volume_has_0_blocks, crSb.fsid.blocks).AppendLine();
        sbInformation.AppendFormat(Localization.Volume_has_0_files,  crSb.fsid.files).AppendLine();

        sbInformation.AppendFormat(Localization.Cram_CRC_0, crSb.fsid.crc).AppendLine();

        // Decode and display flags
        var flags = (CramFlags)crSb.flags;

        if(flags != CramFlags.None)
        {
            sbInformation.AppendFormat(Localization.Cram_Flags_0, crSb.flags).AppendLine();

            if(flags.HasFlag(CramFlags.FsIdVersion2)) sbInformation.AppendLine(Localization.Cram_Flag_FsIdVersion2);

            if(flags.HasFlag(CramFlags.SortedDirs)) sbInformation.AppendLine(Localization.Cram_Flag_SortedDirs);

            if(flags.HasFlag(CramFlags.Holes)) sbInformation.AppendLine(Localization.Cram_Flag_Holes);

            if(flags.HasFlag(CramFlags.ShiftedRootOffset))
                sbInformation.AppendLine(Localization.Cram_Flag_ShiftedRootOffset);

            if(flags.HasFlag(CramFlags.ExtBlockPointers))
                sbInformation.AppendLine(Localization.Cram_Flag_ExtBlockPointers);
        }

        // Display root inode information
        // Bit extraction depends on endianness - LE: mode in low bits, BE: mode in high bits
        ushort rootMode = littleEndian ? (ushort)(crSb.root.modeUid & 0xFFFF) : (ushort)(crSb.root.modeUid >> 16);

        uint rootSize = littleEndian ? crSb.root.sizeGid & 0xFFFFFF : crSb.root.sizeGid >> 8;

        sbInformation.AppendFormat(Localization.Cram_Root_directory_mode_0, Convert.ToString(rootMode, 8)).AppendLine();

        sbInformation.AppendFormat(Localization.Cram_Root_directory_size_0_bytes, rootSize).AppendLine();

        information = sbInformation.ToString();

        metadata = new FileSystem
        {
            VolumeName   = StringHandlers.CToString(crSb.name, encoding),
            Type         = FS_TYPE,
            Clusters     = crSb.fsid.blocks,
            Files        = crSb.fsid.files,
            FreeClusters = 0
        };
    }

#endregion
}