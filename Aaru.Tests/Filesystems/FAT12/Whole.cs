// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : FAT12.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Aaru unit testing.
//
// --[ License ] --------------------------------------------------------------
//
//     This program is free software: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as
//     published by the Free Software Foundation, either version 3 of the
//     License, or (at your option) any later version.
//
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//
//     You should have received a copy of the GNU General Public License
//     along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2026 Natalia Portillo
// ****************************************************************************/

using System.IO;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Interfaces;
using Aaru.Filesystems;
using NUnit.Framework;

namespace Aaru.Tests.Filesystems.FAT12;

[TestFixture]
public class Whole() : ReadOnlyFilesystemTest("fat12")
{
    public override string DataFolder => Path.Combine(Consts.TestFilesRoot, "Filesystems", "FAT12");

    public override IFilesystem Plugin     => new FAT();
    public override bool        Partitions => false;

    public override FileSystemTest[] Tests =>
    [
        new()
        {
            TestFile    = "concurrentdos_6.00_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "concurrentdos_6.00_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "concurrentdos_6.00_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.40_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_3.41_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512,
            SystemId    = "DIGITAL "
        },
        new()
        {
            TestFile    = "drdos_5.00_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_5.00_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "drdos_6.00_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_mf2ed.img.lz",
            MediaType   = MediaType.ECMA_147,
            Sectors     = 5760,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2863,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "drdos_6.00_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512,
            SystemId    = "IBM  3.3",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile     = "drdos_7.02_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF63C69"
        },
        new()
        {
            TestFile     = "drdos_7.02_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF70E75"
        },
        new()
        {
            TestFile     = "drdos_7.02_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF7185F"
        },
        new()
        {
            TestFile     = "drdos_7.02_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF80C4F"
        },
        new()
        {
            TestFile     = "drdos_7.02_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF90F1D"
        },
        new()
        {
            TestFile     = "drdos_7.02_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1607282D"
        },
        new()
        {
            TestFile     = "drdos_7.02_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF72430"
        },
        new()
        {
            TestFile     = "drdos_7.02_ssdd8.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_8,
            Sectors      = 320,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 313,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BF72F1E"
        },
        new()
        {
            TestFile     = "drdos_7.03_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0C1A2013"
        },
        new()
        {
            TestFile     = "drdos_7.03_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0CE22B5B"
        },
        new()
        {
            TestFile     = "drdos_7.03_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0CEA1D3E"
        },
        new()
        {
            TestFile     = "drdos_7.03_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0CEE102F"
        },
        new()
        {
            TestFile     = "drdos_7.03_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0CEE3760"
        },
        new()
        {
            TestFile     = "drdos_7.03_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "16080521"
        },
        new()
        {
            TestFile     = "drdos_8.00_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFD1977"
        },
        new()
        {
            TestFile     = "drdos_8.00_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFD2D3F"
        },
        new()
        {
            TestFile     = "drdos_8.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFD3531"
        },
        new()
        {
            TestFile     = "drdos_8.00_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFC3231"
        },
        new()
        {
            TestFile     = "drdos_8.00_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFA1D58"
        },
        new()
        {
            TestFile     = "drdos_8.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "16081A56"
        },
        new()
        {
            TestFile     = "drdos_8.00_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFE0971"
        },
        new()
        {
            TestFile     = "drdos_8.00_ssdd8.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_8,
            Sectors      = 320,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 313,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFE1423"
        },
        new()
        {
            TestFile     = "drdos_8.10_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "DRDOS  7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "00000000"
        },
        new()
        {
            TestFile    = "msdos_3.30_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_3.30A_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "msdos_3.30A_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_3.31_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_3.31_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_3.31_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_3.31_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_3.31_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_3.31_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_3.31_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_4.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "07200903"
        },
        new()
        {
            TestFile     = "msdos_4.01_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "122C190A"
        },
        new()
        {
            TestFile    = "msdos_4.01_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_4.01_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2480190A"
        },
        new()
        {
            TestFile     = "msdos_4.01_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2D471909"
        },
        new()
        {
            TestFile     = "msdos_4.01_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0F5A1908"
        },
        new()
        {
            TestFile     = "msdos_4.01_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2F3D190A"
        },
        new()
        {
            TestFile    = "msdos_4.01_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_5.00_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0B6018F8"
        },
        new()
        {
            TestFile    = "msdos_5.00_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_5.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1E3518F8"
        },
        new()
        {
            TestFile     = "msdos_5.00_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "285A18FB"
        },
        new()
        {
            TestFile     = "msdos_5.00_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "231D18FE"
        },
        new()
        {
            TestFile     = "msdos_5.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2159090B"
        },
        new()
        {
            TestFile     = "msdos_5.00_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "316118F8"
        },
        new()
        {
            TestFile    = "msdos_5.00_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_5.00a_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "383D090C"
        },
        new()
        {
            TestFile     = "msdos_6.00_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "067B18F6"
        },
        new()
        {
            TestFile    = "msdos_6.00_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_6.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "193418F6"
        },
        new()
        {
            TestFile     = "msdos_6.00_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1F3A18F5"
        },
        new()
        {
            TestFile     = "msdos_6.00_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "165318F3"
        },
        new()
        {
            TestFile     = "msdos_6.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1A2C08FB"
        },
        new()
        {
            TestFile     = "msdos_6.00_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "234918F6"
        },
        new()
        {
            TestFile    = "msdos_6.00_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_6.20_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "265418ED"
        },
        new()
        {
            TestFile    = "msdos_6.20_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_6.20_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0B7018EE"
        },
        new()
        {
            TestFile     = "msdos_6.20_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "127418F0"
        },
        new()
        {
            TestFile     = "msdos_6.20_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "137F18F2"
        },
        new()
        {
            TestFile     = "msdos_6.20_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2C090907"
        },
        new()
        {
            TestFile     = "msdos_6.20_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "185C18EE"
        },
        new()
        {
            TestFile    = "msdos_6.20_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "064B18EB"
        },
        new()
        {
            TestFile    = "msdos_6.20rc1_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "192518EB"
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "244C18EA"
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C3118E7"
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "344118E9"
        },
        new()
        {
            TestFile     = "msdos_6.20rc1_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "267E18EB"
        },
        new()
        {
            TestFile    = "msdos_6.20rc1_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_6.21_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2A41181B"
        },
        new()
        {
            TestFile    = "msdos_6.21_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_6.21_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0641181C"
        },
        new()
        {
            TestFile     = "msdos_6.21_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B26181C"
        },
        new()
        {
            TestFile     = "msdos_6.21_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "082518E2"
        },
        new()
        {
            TestFile     = "msdos_6.21_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "214B0917"
        },
        new()
        {
            TestFile     = "msdos_6.21_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "123F181C"
        },
        new()
        {
            TestFile    = "msdos_6.21_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_6.22_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "317C1818"
        },
        new()
        {
            TestFile    = "msdos_6.22_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_6.22_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0D3A1819"
        },
        new()
        {
            TestFile     = "msdos_6.22_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C251817"
        },
        new()
        {
            TestFile     = "msdos_6.22_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "387A1815"
        },
        new()
        {
            TestFile     = "msdos_6.22_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0F2808F3"
        },
        new()
        {
            TestFile     = "msdos_6.22_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "18231819"
        },
        new()
        {
            TestFile    = "msdos_6.22_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_7.10_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1156180A"
        },
        new()
        {
            TestFile    = "msdos_7.10_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "msdos_7.10_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2951180A"
        },
        new()
        {
            TestFile     = "msdos_7.10_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3057180B"
        },
        new()
        {
            TestFile     = "msdos_7.10_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2B4A1811"
        },
        new()
        {
            TestFile     = "msdos_7.10_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "344B180C"
        },
        new()
        {
            TestFile     = "msdos_7.10_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "352D180A"
        },
        new()
        {
            TestFile    = "msdos_7.10_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_amstrad_3.20_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_att_2.11_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "PSA 1.04"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_dell_3.30_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_epson_3.10_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "EPS 3.10"
        },
        new()
        {
            TestFile    = "msdos_epson_3.10_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "EPS 3.10"
        },
        new()
        {
            TestFile    = "msdos_epson_3.10_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "EPS 3.10"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_epson_3.20_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512,
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2853,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hp_3.20_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2853,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_hyonsung_3.21_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2853,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "MSDOS3.2"
        },
        new()
        {
            TestFile    = "msdos_kaypro_3.21_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "msdos_olivetti_3.10_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.1"
        },
        new()
        {
            TestFile    = "msdos_olivetti_3.10_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.1"
        },
        new()
        {
            TestFile    = "msdos_olivetti_3.10_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.1"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_ssdd.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_9,
            Sectors     = 360,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 351,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "msdos_toshiba_3.30_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "T V4.00 ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0B2519E7"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "T V4.00 ",
            VolumeName   = "NO NAME",
            VolumeSerial = "163419E7"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "T V4.00 ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1E3119E7"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "T V4.00 ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "133919E9"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "T V4.00 ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "177419EA"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "T V4.00 ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "317E19E7"
        },
        new()
        {
            TestFile     = "msdos_toshiba_4.01_ssdd8.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_8,
            Sectors      = 320,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 313,
            ClusterSize  = 512,
            SystemId     = "T V4.00 ",
            VolumeName   = "NO NAME",
            VolumeSerial = "3B7319E7"
        },
        new()
        {
            TestFile     = "novelldos_7.00_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE7254C"
        },
        new()
        {
            TestFile     = "novelldos_7.00_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE73024"
        },
        new()
        {
            TestFile     = "novelldos_7.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE7397C"
        },
        new()
        {
            TestFile     = "novelldos_7.00_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE63635"
        },
        new()
        {
            TestFile     = "novelldos_7.00_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE51661"
        },
        new()
        {
            TestFile     = "novelldos_7.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "161B1226"
        },
        new()
        {
            TestFile     = "novelldos_7.00_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE80A5D"
        },
        new()
        {
            TestFile     = "novelldos_7.00_ssdd8.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_8,
            Sectors      = 320,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 313,
            ClusterSize  = 512,
            SystemId     = "NWDOS7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE8144C"
        },
        new()
        {
            TestFile     = "opendos_7.01_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BE93E2B"
        },
        new()
        {
            TestFile     = "opendos_7.01_dsdd8.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_8,
            Sectors      = 640,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 315,
            ClusterSize  = 1024,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEA234D"
        },
        new()
        {
            TestFile     = "opendos_7.01_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEA325D"
        },
        new()
        {
            TestFile     = "opendos_7.01_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEB294F"
        },
        new()
        {
            TestFile     = "opendos_7.01_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEC2C2E"
        },
        new()
        {
            TestFile     = "opendos_7.01_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "16090D37"
        },
        new()
        {
            TestFile     = "opendos_7.01_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEA3E60"
        },
        new()
        {
            TestFile     = "opendos_7.01_ssdd8.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_8,
            Sectors      = 320,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 313,
            ClusterSize  = 512,
            SystemId     = "OPENDOS7",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BEB0E26"
        },
        new()
        {
            TestFile    = "pcdos_2.00_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  2.0"
        },
        new()
        {
            TestFile    = "pcdos_2.10_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "IBM  2.0"
        },
        new()
        {
            TestFile     = "pcdos_2000_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2634100E"
        },
        new()
        {
            TestFile    = "pcdos_2000_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "pcdos_2000_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3565100E"
        },
        new()
        {
            TestFile     = "pcdos_2000_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B6B1012"
        },
        new()
        {
            TestFile     = "pcdos_2000_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B2D1013"
        },
        new()
        {
            TestFile     = "pcdos_2000_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3E46090A"
        },
        new()
        {
            TestFile     = "pcdos_2000_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "4136100E"
        },
        new()
        {
            TestFile    = "pcdos_2000_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile    = "pcdos_3.00_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.0"
        },
        new()
        {
            TestFile    = "pcdos_3.10_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.1"
        },
        new()
        {
            TestFile    = "pcdos_3.20_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2853,
            ClusterSize = 512,
            VolumeName  = "VOLUMELABEL",
            SystemId    = "IBM  3.2"
        },
        new()
        {
            TestFile    = "pcdos_3.30_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile    = "pcdos_3.30_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "IBM  3.3"
        },
        new()
        {
            TestFile     = "pcdos_4.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C240FE3"
        },
        new()
        {
            TestFile     = "pcdos_4.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0E6409F3"
        },
        new()
        {
            TestFile     = "pcdos_4.01_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0F2F0A01"
        },
        new()
        {
            TestFile     = "pcdos_5.00_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "33260FF9"
        },
        new()
        {
            TestFile    = "pcdos_5.00_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "pcdos_5.00_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "11550FFA"
        },
        new()
        {
            TestFile     = "pcdos_5.00_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "234F0FFB"
        },
        new()
        {
            TestFile     = "pcdos_5.00_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2F600FFC"
        },
        new()
        {
            TestFile     = "pcdos_5.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "31090904"
        },
        new()
        {
            TestFile     = "pcdos_5.00_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1D630FFA"
        },
        new()
        {
            TestFile    = "pcdos_5.00_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "pcdos_5.02_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "06231000"
        },
        new()
        {
            TestFile    = "pcdos_5.02_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "pcdos_5.02_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1A3E1000"
        },
        new()
        {
            TestFile     = "pcdos_5.02_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1F3B0FFF"
        },
        new()
        {
            TestFile     = "pcdos_5.02_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3D750FFD"
        },
        new()
        {
            TestFile     = "pcdos_5.02_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "09410902"
        },
        new()
        {
            TestFile     = "pcdos_5.02_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "IBM  5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "26471000"
        },
        new()
        {
            TestFile    = "pcdos_5.02_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "pcdos_6.10_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "25551004"
        },
        new()
        {
            TestFile    = "pcdos_6.10_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "pcdos_6.10_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3E5F1004"
        },
        new()
        {
            TestFile     = "pcdos_6.10_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "142D1006"
        },
        new()
        {
            TestFile     = "pcdos_6.10_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "17541007"
        },
        new()
        {
            TestFile     = "pcdos_6.10_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "382408FE"
        },
        new()
        {
            TestFile     = "pcdos_6.10_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0D5E1005"
        },
        new()
        {
            TestFile    = "pcdos_6.10_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "pcdos_6.30_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2B22100C"
        },
        new()
        {
            TestFile    = "pcdos_6.30_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "pcdos_6.30_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B47100C"
        },
        new()
        {
            TestFile     = "pcdos_6.30_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0C55100C"
        },
        new()
        {
            TestFile     = "pcdos_6.30_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1B80100A"
        },
        new()
        {
            TestFile     = "pcdos_6.30_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1F2A0901"
        },
        new()
        {
            TestFile     = "pcdos_6.30_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "IBM  6.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0A3A100D"
        },
        new()
        {
            TestFile    = "pcdos_6.30_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "pcdos_7.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM  7.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1407090D"
        },
        new()
        {
            TestFile     = "mkfs.vfat_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "mkfs.fat",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "20C279B1"
        },
        new()
        {
            TestFile     = "mkfs.vfat_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "mkfs.fat",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "20FD9501"
        },
        new()
        {
            TestFile     = "mkfs.vfat_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "mkfs.fat",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2132D70A"
        },
        new()
        {
            TestFile     = "mkfs.vfat_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "mkfs.fat",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2118F1AA"
        },
        new()
        {
            TestFile     = "mkfs.vfat_atari_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Clusters     = 1188,
            ClusterSize  = 1024,
            SystemId     = "mkdosf",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "83E030"
        },
        new()
        {
            TestFile     = "mkfs.vfat_atari_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "mkdosf",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "C53F06"
        },
        new()
        {
            TestFile     = "mkfs.vfat_atari_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "mkdosf",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "A154CD"
        },
        new()
        {
            TestFile     = "mkfs.vfat_atari_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Clusters     = 1427,
            ClusterSize  = 1024,
            SystemId     = "mkdosf",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "D54DEE"
        },
        new()
        {
            TestFile     = "msos2_1.00_tandy_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "TAN 10.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C170C15"
        },
        new()
        {
            TestFile     = "msos2_1.00_tandy_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "TAN 10.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9BFB0C15"
        },
        new()
        {
            TestFile     = "msos2_1.00_tandy_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "TAN 10.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C13FC15"
        },
        new()
        {
            TestFile     = "msos2_1.00_tandy_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "TAN 10.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9BF99C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_ast_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66A42C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_ast_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "67696C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_ast_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66DEBC15"
        },
        new()
        {
            TestFile     = "msos2_1.10_ast_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66DC4C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_nokia_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "676B4C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_nokia_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "67768C15"
        },
        new()
        {
            TestFile     = "msos2_1.10_nokia_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C12DC15"
        },
        new()
        {
            TestFile     = "msos2_1.10_nokia_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66A74C15"
        },
        new()
        {
            TestFile     = "msos2_1.21_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C074C15"
        },
        new()
        {
            TestFile     = "msos2_1.21_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66BCFC15"
        },
        new()
        {
            TestFile     = "msos2_1.21_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66C1AC15"
        },
        new()
        {
            TestFile     = "msos2_1.21_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66C7FC15"
        },
        new()
        {
            TestFile     = "msos2_1.30.1_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66C47C15"
        },
        new()
        {
            TestFile     = "msos2_1.30.1_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "66CBEC15"
        },
        new()
        {
            TestFile     = "msos2_1.30.1_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C167C15"
        },
        new()
        {
            TestFile     = "msos2_1.30.1_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C147C15"
        },
        new()
        {
            TestFile     = "msos2_1.30.1_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C0FEC15"
        },
        new()
        {
            TestFile     = "os2_1.20_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5BF5E015"
        },
        new()
        {
            TestFile     = "os2_1.20_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5BE61015"
        },
        new()
        {
            TestFile     = "os2_1.20_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5C26F015"
        },
        new()
        {
            TestFile     = "os2_1.20_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5D0CC815"
        },
        new()
        {
            TestFile     = "os2_1.30_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5C418015"
        },
        new()
        {
            TestFile     = "os2_1.30_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5BE20015"
        },
        new()
        {
            TestFile     = "os2_1.30_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5C7F1015"
        },
        new()
        {
            TestFile     = "os2_1.30_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 10.2",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5D0DE815"
        },
        new()
        {
            TestFile     = "os2_6.307_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5C3BD015"
        },
        new()
        {
            TestFile     = "os2_6.307_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5B807015"
        },
        new()
        {
            TestFile     = "os2_6.307_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5BE69015"
        },
        new()
        {
            TestFile     = "os2_6.307_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5C187015"
        },
        new()
        {
            TestFile     = "os2_6.307_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5D14F815"
        },
        new()
        {
            TestFile     = "os2_6.514_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFCB414"
        },
        new()
        {
            TestFile     = "os2_6.514_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6C6C414"
        },
        new()
        {
            TestFile     = "os2_6.514_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6CCF414"
        },
        new()
        {
            TestFile     = "os2_6.514_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6AF6414"
        },
        new()
        {
            TestFile     = "os2_6.514_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5D490415"
        },
        new()
        {
            TestFile     = "os2_6.617_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6AEB414"
        },
        new()
        {
            TestFile     = "os2_6.617_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1C00D414"
        },
        new()
        {
            TestFile     = "os2_6.617_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1C03B414"
        },
        new()
        {
            TestFile     = "os2_6.617_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6C90414"
        },
        new()
        {
            TestFile     = "os2_6.617_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5D23B415"
        },
        new()
        {
            TestFile     = "os2_8.162_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6AF7414"
        },
        new()
        {
            TestFile     = "os2_8.162_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6D63414"
        },
        new()
        {
            TestFile     = "os2_8.162_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6A65414"
        },
        new()
        {
            TestFile     = "os2_8.162_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5CFCB415"
        },
        new()
        {
            TestFile     = "os2_9.023_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6CD9414"
        },
        new()
        {
            TestFile     = "os2_9.023_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1BFAD414"
        },
        new()
        {
            TestFile     = "os2_9.023_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6DFF414"
        },
        new()
        {
            TestFile     = "os2_9.023_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 20.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "5CFB8415"
        },
        new()
        {
            TestFile     = "ecs_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "IBM 4.50",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6CA5814"
        },
        new()
        {
            TestFile     = "ecs_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "IBM 4.50",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6CBC814"
        },
        new()
        {
            TestFile     = "ecs_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "IBM 4.50",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E6B81814"
        },
        new()
        {
            TestFile     = "ecs_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 4.50",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1C013814"
        },
        new()
        {
            TestFile     = "ecs20_mf2hd_fstester.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "IBM 4.50",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9BF37814"
        },
        new()
        {
            TestFile    = "win95_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win95_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B360D0D"
        },
        new()
        {
            TestFile     = "win95_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "24240D0D"
        },
        new()
        {
            TestFile     = "win95_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C260D11"
        },
        new()
        {
            TestFile     = "win95_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "30050D10"
        },
        new()
        {
            TestFile     = "win95_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "275A0D11"
        },
        new()
        {
            TestFile    = "win95_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win95_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3B100D0F"
        },
        new()
        {
            TestFile    = "win95osr2_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win95osr2_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1C5B0D19"
        },
        new()
        {
            TestFile     = "win95osr2_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "11510D19"
        },
        new()
        {
            TestFile     = "win95osr2_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0F1F0D15"
        },
        new()
        {
            TestFile     = "win95osr2_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "40200D17"
        },
        new()
        {
            TestFile     = "win95osr2_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3D610D14"
        },
        new()
        {
            TestFile    = "win95osr2_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win95osr2_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "280B0D19"
        },
        new()
        {
            TestFile    = "win95osr2.1_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win95osr2.1_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1F3B0D1C"
        },
        new()
        {
            TestFile     = "win95osr2.1_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "14470D1C"
        },
        new()
        {
            TestFile     = "win95osr2.1_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1C510DE4"
        },
        new()
        {
            TestFile     = "win95osr2.1_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2E250DE2"
        },
        new()
        {
            TestFile     = "win95osr2.1_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "10640DE4"
        },
        new()
        {
            TestFile    = "win95osr2.1_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win95osr2.1_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2B3E0D1C"
        },
        new()
        {
            TestFile    = "win95osr2.5_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win95osr2.5_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "18190DFB"
        },
        new()
        {
            TestFile     = "win95osr2.5_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0A240DFB"
        },
        new()
        {
            TestFile     = "win95osr2.5_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1E320DE7"
        },
        new()
        {
            TestFile     = "win95osr2.5_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "33230DE8"
        },
        new()
        {
            TestFile     = "win95osr2.5_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "125B0DE7"
        },
        new()
        {
            TestFile    = "win95osr2.5_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win95osr2.5_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "21410DFB"
        },
        new()
        {
            TestFile    = "win98_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win98_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "40090E0F"
        },
        new()
        {
            TestFile     = "win98_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "28140E0F"
        },
        new()
        {
            TestFile     = "win98_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0E620E0A"
        },
        new()
        {
            TestFile     = "win98_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "14390E0D"
        },
        new()
        {
            TestFile     = "win98_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0E081246"
        },
        new()
        {
            TestFile    = "win98_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win98_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "30600E10"
        },
        new()
        {
            TestFile    = "win98se_dsdd8.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_8,
            Sectors     = 640,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 315,
            ClusterSize = 1024
        },
        new()
        {
            TestFile     = "win98se_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1B550EEC"
        },
        new()
        {
            TestFile     = "win98se_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "1B100EEB"
        },
        new()
        {
            TestFile     = "win98se_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "08410EE6"
        },
        new()
        {
            TestFile     = "win98se_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0E0F0EE8"
        },
        new()
        {
            TestFile     = "win98se_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "325D0EE4"
        },
        new()
        {
            TestFile    = "win98se_ssdd8.img.lz",
            MediaType   = MediaType.DOS_525_SS_DD_8,
            Sectors     = 320,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 313,
            ClusterSize = 512
        },
        new()
        {
            TestFile     = "win98se_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 351,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "13380EEC"
        },
        new()
        {
            TestFile     = "winme_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2F200F02"
        },
        new()
        {
            TestFile     = "winme_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "103A0F01"
        },
        new()
        {
            TestFile     = "winme_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2F1C0EFC"
        },
        new()
        {
            TestFile     = "winme_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "21570EFF"
        },
        new()
        {
            TestFile     = "winme_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSWIN4.1",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "07040EFB"
        },
        new()
        {
            TestFile     = "winnt_3.10_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "60EA50BC"
        },
        new()
        {
            TestFile     = "winnt_3.10_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "6C857D51"
        },
        new()
        {
            TestFile     = "winnt_3.10_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "4009440C"
        },
        new()
        {
            TestFile     = "winnt_3.10_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "30761EDC"
        },
        new()
        {
            TestFile     = "winnt_3.50_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "0C478404"
        },
        new()
        {
            TestFile     = "winnt_3.50_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "7CBEB35B"
        },
        new()
        {
            TestFile     = "winnt_3.50_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "7C1E8DCB"
        },
        new()
        {
            TestFile     = "winnt_3.50_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "ECB276AF"
        },
        new()
        {
            TestFile     = "winnt_3.51_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "482D8681"
        },
        new()
        {
            TestFile     = "winnt_3.51_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "8889C95E"
        },
        new()
        {
            TestFile     = "winnt_3.51_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "54DE6C39"
        },
        new()
        {
            TestFile     = "winnt_3.51_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "F47D2516"
        },
        new()
        {
            TestFile     = "winnt_4_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "D8CAAC1F"
        },
        new()
        {
            TestFile     = "winnt_4_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E0BB6D70"
        },
        new()
        {
            TestFile     = "winnt_4_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "C08C3C60"
        },
        new()
        {
            TestFile     = "winnt_4_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "9C44B411"
        },
        new()
        {
            TestFile     = "winnt_4_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "D4F453A2"
        },
        new()
        {
            TestFile     = "winnt_4_ssdd.img.lz",
            MediaType    = MediaType.DOS_525_SS_DD_9,
            Sectors      = 360,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 348,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "4CD82982"
        },
        new()
        {
            TestFile     = "win2000_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "4019989C"
        },
        new()
        {
            TestFile     = "win2000_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "78F30AF8"
        },
        new()
        {
            TestFile     = "win2000_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "E4217DDE"
        },
        new()
        {
            TestFile     = "win2000_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "80B3B996"
        },
        new()
        {
            TestFile     = "win2000_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "28043527"
        },
        new()
        {
            TestFile     = "winvista_dsdd.img.lz",
            MediaType    = MediaType.DOS_525_DS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 354,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C9F0BD2"
        },
        new()
        {
            TestFile     = "winvista_dshd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3A8E465C"
        },
        new()
        {
            TestFile     = "winvista_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "B2EFB822"
        },
        new()
        {
            TestFile     = "winvista_mf2ed.img.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "3C30C632"
        },
        new()
        {
            TestFile     = "winvista_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "MSDOS5.0",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "16DAB07A"
        },
        new()
        {
            TestFile     = "beos_r4.5_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "BeOS    ",
            VolumeName   = "VOLUME LABE",
            VolumeSerial = "00000000"
        },
        new()
        {
            TestFile     = "hatari_mf1dd.st.lz",
            MediaType    = MediaType.DOS_35_SS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Clusters     = 351,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "A82270"
        },
        new()
        {
            TestFile     = "hatari_mf1dd_10.st.lz",
            MediaType    = MediaType.ATARI_35_SS_DD,
            Sectors      = 800,
            SectorSize   = 512,
            Clusters     = 391,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "D08917"
        },
        new()
        {
            TestFile     = "hatari_mf1dd_11.st.lz",
            MediaType    = MediaType.ATARI_35_SS_DD_11,
            Sectors      = 880,
            SectorSize   = 512,
            Clusters     = 431,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "37AD91"
        },
        new()
        {
            TestFile     = "hatari_mf2dd.st.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Clusters     = 711,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "1ED910"
        },
        new()
        {
            TestFile     = "hatari_mf2dd_10.st.lz",
            MediaType    = MediaType.ATARI_35_DS_DD,
            Sectors      = 1600,
            SectorSize   = 512,
            Clusters     = 791,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "299DFE"
        },
        new()
        {
            TestFile     = "hatari_mf2dd_11.st.lz",
            MediaType    = MediaType.ATARI_35_DS_DD_11,
            Sectors      = 1760,
            SectorSize   = 512,
            Clusters     = 871,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "94AE59"
        },
        new()
        {
            TestFile     = "hatari_mf2ed.st.lz",
            MediaType    = MediaType.ECMA_147,
            Sectors      = 5760,
            SectorSize   = 512,
            Clusters     = 2863,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "3A1757"
        },
        new()
        {
            TestFile     = "hatari_mf2hd.st.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Clusters     = 1423,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "volumelabel",
            VolumeSerial = "C08249"
        },
        new()
        {
            TestFile     = "tos_1.04_mf1dd.st.lz",
            MediaType    = MediaType.DOS_35_SS_DD_9,
            Sectors      = 720,
            SectorSize   = 512,
            Clusters     = 351,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2356F0"
        },
        new()
        {
            TestFile     = "tos_1.04_mf2dd.st.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Clusters     = 711,
            ClusterSize  = 1024,
            SystemId     = "NNNNNN",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "51C7A3"
        },
        new()
        {
            TestFile     = "netbsd_1.6_mf2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 713,
            ClusterSize  = 1024,
            SystemId     = "BSD  4.4",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "EEB51A0C"
        },
        new()
        {
            TestFile     = "netbsd_1.6_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "BSD  4.4",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "CCFD1A06"
        },
        new()
        {
            TestFile    = "nextstep_3.3_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "nextstep_3.3_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUME LABE"
        },
        new()
        {
            TestFile    = "openstep_4.0_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "openstep_4.0_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "openstep_4.2_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "openstep_4.2_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "NEXT    ",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "solaris_2.4_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "solaris_2.4_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "MSDOS3.3"
        },
        new()
        {
            TestFile    = "coherentunix_4.2.10_dsdd.img.lz",
            MediaType   = MediaType.DOS_525_DS_DD_9,
            Sectors     = 720,
            SectorSize  = 512,
            Clusters    = 354,
            ClusterSize = 1024,
            SystemId    = "COHERENT",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "coherentunix_4.2.10_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "COHERENT",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "coherentunix_4.2.10_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Clusters    = 713,
            ClusterSize = 1024,
            SystemId    = "COHERENT",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "coherentunix_4.2.10_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "COHERENT",
            VolumeName  = "VOLUMELABEL"
        },
        new()
        {
            TestFile    = "scoopenserver_5.0.7hw_dshd.img.lz",
            MediaType   = MediaType.DOS_525_HD,
            Sectors     = 2400,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2371,
            ClusterSize = 512,
            SystemId    = "SCO BOOT"
        },
        new()
        {
            TestFile    = "scoopenserver_5.0.7hw_mf2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 1422,
            ClusterSize = 512,
            SystemId    = "SCO BOOT"
        },
        new()
        {
            TestFile    = "scoopenserver_5.0.7hw_mf2hd.img.lz",
            MediaType   = MediaType.DOS_35_HD,
            Sectors     = 2880,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 2847,
            ClusterSize = 512,
            SystemId    = "SCO BOOT"
        },
        new()
        {
            TestFile     = "msdos_epson_pc98_5.00_md2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 634,
            ClusterSize  = 1024,
            SystemId     = "EPSON5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "27021316"
        },
        new()
        {
            TestFile     = "msdos_epson_pc98_5.00_md2hd.img.lz",
            MediaType    = MediaType.SHARP_525,
            Sectors      = 1232,
            SectorSize   = 1024,
            Bootable     = true,
            Clusters     = 1221,
            ClusterSize  = 1024,
            SystemId     = "EPSON5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "11021317"
        },
        new()
        {
            TestFile    = "msdos_pc98_3.30_md2dd.img.lz",
            MediaType   = MediaType.DOS_35_DS_DD_9,
            Sectors     = 1440,
            SectorSize  = 512,
            Bootable    = true,
            Clusters    = 634,
            ClusterSize = 1024,
            SystemId    = "NEC 2.00"
        },
        new()
        {
            TestFile    = "msdos_pc98_3.30_md2hd.img.lz",
            MediaType   = MediaType.SHARP_525,
            Sectors     = 1232,
            SectorSize  = 1024,
            Bootable    = true,
            Clusters    = 1221,
            ClusterSize = 1024,
            SystemId    = "NEC 2.00"
        },
        new()
        {
            TestFile     = "msdos_pc98_5.00_md2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 634,
            ClusterSize  = 1024,
            SystemId     = "NEC  5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "1002120E"
        },
        new()
        {
            TestFile     = "msdos_pc98_5.00_md2hd.img.lz",
            MediaType    = MediaType.SHARP_525,
            Sectors      = 1232,
            SectorSize   = 1024,
            Bootable     = true,
            Clusters     = 1221,
            ClusterSize  = 1024,
            SystemId     = "NEC  5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "41021209"
        },
        new()
        {
            TestFile     = "msdos_pc98_6.20_md2dd.img.lz",
            MediaType    = MediaType.DOS_35_DS_DD_9,
            Sectors      = 1440,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 634,
            ClusterSize  = 1024,
            SystemId     = "NEC  5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "3D021418"
        },
        new()
        {
            TestFile     = "msdos_pc98_6.20_md2hd.img.lz",
            MediaType    = MediaType.SHARP_525,
            Sectors      = 1232,
            SectorSize   = 1024,
            Bootable     = true,
            Clusters     = 1221,
            ClusterSize  = 1024,
            SystemId     = "NEC  5.0",
            VolumeName   = "NO NAME",
            VolumeSerial = "16021409"
        },
        new()
        {
            TestFile     = "geos12_md2hd.img.lz",
            MediaType    = MediaType.DOS_525_HD,
            Sectors      = 2400,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2371,
            ClusterSize  = 512,
            SystemId     = "GEOWORKS",
            VolumeName   = "GEOS12",
            VolumeSerial = "0000049C"
        },
        new()
        {
            TestFile     = "geos20_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "GEOWORKS",
            VolumeName   = "GEOS20",
            VolumeSerial = "8DC94C67"
        },
        new()
        {
            TestFile     = "geos31_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "GEOWORKS",
            VolumeName   = "GEOS32",
            VolumeSerial = "8E0D4C67"
        },
        new()
        {
            TestFile     = "geos32_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "GEOWORKS",
            VolumeName   = "NDO2000",
            VolumeSerial = "8EDB4C67"
        },
        new()
        {
            TestFile     = "geos41_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "GEOWORKS",
            VolumeName   = "GEOS41",
            VolumeSerial = "8D684C67"
        },
        new()
        {
            TestFile     = "beos_r5_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "BeOS    ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "00000000"
        },
        new()
        {
            TestFile     = "dflybsd_1.00_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "BSD  4.4",
            VolumeName   = "NO NAME",
            VolumeSerial = "3E8C1A1F"
        },
        new()
        {
            TestFile     = "netbsd_6.1.5_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2829,
            ClusterSize  = 512,
            SystemId     = "NetBSD  ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "2EE71B0B"
        },
        new()
        {
            TestFile     = "netbsd_7.1_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2829,
            ClusterSize  = 512,
            SystemId     = "NetBSD  ",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "80C21715"
        },
        new()
        {
            TestFile     = "openbsd_4.7_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            SystemId     = "BSD  4.4",
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "4E6B1F17"
        },
        new()
        {
            TestFile     = "linux_2.0.0_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeSerial = "670000"
        },
        new()
        {
            TestFile     = "linux_2.0.29_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609AC294"
        },
        new()
        {
            TestFile     = "linux_2.0.34_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609B8CD9"
        },
        new()
        {
            TestFile     = "linux_2.0.37_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VOLUMELABEL",
            VolumeSerial = "609D1849"
        },
        new()
        {
            TestFile     = "linux_2.0.38_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609BB0AA"
        },
        new()
        {
            TestFile     = "linux_2.2.17_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609C4FE6"
        },
        new()
        {
            TestFile     = "linux_2.2.20_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609C815D"
        },
        new()
        {
            TestFile     = "linux_2.4.18_mf2hd.img.lz",
            MediaType    = MediaType.DOS_35_HD,
            Sectors      = 2880,
            SectorSize   = 512,
            Bootable     = true,
            Clusters     = 2847,
            ClusterSize  = 512,
            VolumeName   = "VolumeLabel",
            VolumeSerial = "609CA596"
        }
    ];
}