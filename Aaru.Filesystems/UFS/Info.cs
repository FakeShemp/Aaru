// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Info.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : UNIX FIle System plugin.
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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Helpers;
using Aaru.Logging;
using Sentry;
using Partition = Aaru.CommonTypes.Partition;

namespace Aaru.Filesystems;

// Using information from Linux kernel headers
/// <inheritdoc />
/// <summary>Implements detection of BSD Fast File System (FFS, aka UNIX File System)</summary>
[SuppressMessage("ReSharper", "InconsistentNaming")]
public sealed partial class UFSPlugin
{
#region IFilesystem Members

    /// <inheritdoc />
    public bool Identify(IMediaImage imagePlugin, Partition partition)
    {
        if(2 + partition.Start >= partition.End) return false;

        uint sbSizeInSectors;

        if(imagePlugin.Info.SectorSize is 2336 or 2352 or 2448)
            sbSizeInSectors = SBSIZE / 2048;
        else
            sbSizeInSectors = SBSIZE / imagePlugin.Info.SectorSize;

        ulong[] locations =
        [
            SBLOCK_FLOPPY, SBLOCK, SBLOCK_LONG_BOOT, SBLOCK_PIGGY, SBLOCK_ATT_DSDD,
            4096  / imagePlugin.Info.SectorSize, 8192   / imagePlugin.Info.SectorSize,
            65536 / imagePlugin.Info.SectorSize, 262144 / imagePlugin.Info.SectorSize
        ];

        try
        {
            foreach(ulong loc in locations.Where(loc => partition.End > partition.Start + loc + sbSizeInSectors))
            {
                ErrorNumber errno =
                    imagePlugin.ReadSectors(partition.Start + loc,
                                            false,
                                            sbSizeInSectors,
                                            out byte[] ufsSbSectors,
                                            out _);

                if(errno != ErrorNumber.NoError) continue;

                var magic = BitConverter.ToUInt32(ufsSbSectors, 0x055C);

                if(magic is UFS_MAGIC
                         or UFS_CIGAM
                         or UFS_MAGIC_BW
                         or UFS_CIGAM_BW
                         or UFS2_MAGIC
                         or UFS2_CIGAM
                         or UFS_BAD_MAGIC
                         or UFS_BAD_CIGAM
                         or FS_MAGIC_LFN
                         or FS_CIGAM_LFN
                         or FD_FSMAGIC
                         or FD_FSCIGAM
                         or FS_SEC_MAGIC
                         or FS_SEC_CIGAM
                         or MTB_UFS_MAGIC
                         or MTB_UFS_CIGAM)
                    return true;
            }

            return false;
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            return false;
        }
    }

    /// <inheritdoc />
    public void GetInformation(IMediaImage imagePlugin, Partition partition, Encoding encoding, out string information,
                               out FileSystem metadata)
    {
        information = "";
        metadata    = new FileSystem();
        var sbInformation = new StringBuilder();

        uint   magic = 0;
        uint   sb_size_in_sectors;
        byte[] ufs_sb_sectors;
        ulong  sb_offset       = partition.Start;
        var    fs_type_42bsd   = false;
        var    fs_type_43bsd   = false;
        var    fs_type_44bsd   = false;
        var    fs_type_ufs     = false;
        var    fs_type_ufs2    = false;
        var    fs_type_sun     = false;
        var    fs_type_sun86   = false;
        var    fs_type_hpux    = false;
        var    fs_type_svr4    = false;
        var    fs_type_aux     = false;
        var    fs_type_ultrix  = false;
        var    fs_type_386bsd  = false;
        var    fs_type_solaris = false;
        var    fs_type_osf1    = false;

        if(imagePlugin.Info.SectorSize is 2336 or 2352 or 2448)
            sb_size_in_sectors = SBSIZE / 2048;
        else
            sb_size_in_sectors = SBSIZE / imagePlugin.Info.SectorSize;

        ulong[] locations =
        [
            SBLOCK_FLOPPY, SBLOCK, SBLOCK_LONG_BOOT, SBLOCK_PIGGY, SBLOCK_ATT_DSDD,
            4096  / imagePlugin.Info.SectorSize, 8192   / imagePlugin.Info.SectorSize,
            65536 / imagePlugin.Info.SectorSize, 262144 / imagePlugin.Info.SectorSize
        ];

        ErrorNumber errno;

        foreach(ulong loc in locations.Where(loc => partition.End > partition.Start + loc + sb_size_in_sectors))
        {
            errno = imagePlugin.ReadSectors(partition.Start + loc,
                                            false,
                                            sb_size_in_sectors,
                                            out ufs_sb_sectors,
                                            out _);

            if(errno != ErrorNumber.NoError) continue;

            magic = BitConverter.ToUInt32(ufs_sb_sectors, 0x055C);

            if(magic is UFS_MAGIC
                     or UFS_CIGAM
                     or UFS_MAGIC_BW
                     or UFS_CIGAM_BW
                     or UFS2_MAGIC
                     or UFS2_CIGAM
                     or UFS_BAD_MAGIC
                     or UFS_BAD_CIGAM
                     or FS_MAGIC_LFN
                     or FS_CIGAM_LFN
                     or FD_FSMAGIC
                     or FD_FSCIGAM
                     or FS_SEC_MAGIC
                     or FS_SEC_CIGAM
                     or MTB_UFS_MAGIC
                     or MTB_UFS_CIGAM)
            {
                sb_offset = partition.Start + loc;

                break;
            }

            magic = 0;
        }

        if(magic == 0)
        {
            information = Localization.Not_a_UFS_filesystem_I_shouldnt_have_arrived_here;

            return;
        }

        metadata = new FileSystem();

        switch(magic)
        {
            case UFS_MAGIC:
                sbInformation.AppendLine(Localization.UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case UFS_CIGAM:
                sbInformation.AppendLine(Localization.Big_endian_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case UFS_MAGIC_BW:
                sbInformation.AppendLine(Localization.BorderWare_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case UFS_CIGAM_BW:
                sbInformation.AppendLine(Localization.Big_endian_BorderWare_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case UFS2_MAGIC:
                sbInformation.AppendLine(Localization.UFS2_filesystem);
                metadata.Type = FS_TYPE_UFS2;

                break;
            case UFS2_CIGAM:
                sbInformation.AppendLine(Localization.Big_endian_UFS2_filesystem);
                metadata.Type = FS_TYPE_UFS2;

                break;
            case UFS_BAD_MAGIC:
                sbInformation.AppendLine(Localization.Incompletely_initialized_UFS_filesystem);
                sbInformation.AppendLine(Localization.BEWARE_Following_information_may_be_completely_wrong);
                metadata.Type = FS_TYPE_UFS;

                break;
            case UFS_BAD_CIGAM:
                sbInformation.AppendLine(Localization.Incompletely_initialized_big_endian_UFS_filesystem);
                sbInformation.AppendLine(Localization.BEWARE_Following_information_may_be_completely_wrong);
                metadata.Type = FS_TYPE_UFS;

                break;
            case FS_MAGIC_LFN:
            case FS_CIGAM_LFN:
                sbInformation.AppendLine(Localization.HPUX_UFS_filesystem_long_file_names);
                metadata.Type = FS_TYPE_UFS;

                break;
            case FD_FSMAGIC:
            case FD_FSCIGAM:
                sbInformation.AppendLine(Localization.HPUX_UFS_filesystem_feature_bits);
                metadata.Type = FS_TYPE_UFS;

                break;
            case FS_SEC_MAGIC:
                sbInformation.AppendLine(Localization.OSF1_secure_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case FS_SEC_CIGAM:
                sbInformation.AppendLine(Localization.Big_endian_OSF1_secure_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case MTB_UFS_MAGIC:
                sbInformation.AppendLine(Localization.Solaris_multi_terabyte_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
            case MTB_UFS_CIGAM:
                sbInformation.AppendLine(Localization.Big_endian_Solaris_multi_terabyte_UFS_filesystem);
                metadata.Type = FS_TYPE_UFS;

                break;
        }

        // Fun with seeking follows on superblock reading!
        errno = imagePlugin.ReadSectors(sb_offset, false, sb_size_in_sectors, out ufs_sb_sectors, out _);

        if(errno != ErrorNumber.NoError) return;

        SuperBlockAux    sb_aux    = Marshal.ByteArrayToStructureBigEndian<SuperBlockAux>(ufs_sb_sectors);
        SuperBlockOldBSD sb_41bsd  = Marshal.ByteArrayToStructureLittleEndian<SuperBlockOldBSD>(ufs_sb_sectors);
        SuperBlock44BSD  sb_44bsd  = Marshal.ByteArrayToStructureLittleEndian<SuperBlock44BSD>(ufs_sb_sectors);
        SuperblockUltrix sb_ultrix = Marshal.ByteArrayToStructureLittleEndian<SuperblockUltrix>(ufs_sb_sectors);
        SuperblockSVR4   sb_svr4   = Marshal.ByteArrayToStructureLittleEndian<SuperblockSVR4>(ufs_sb_sectors);
        Superblock386BSD sb_386bsd = Marshal.ByteArrayToStructureLittleEndian<Superblock386BSD>(ufs_sb_sectors);
        SuperblockRISCos sb_riscos = Marshal.ByteArrayToStructureLittleEndian<SuperblockRISCos>(ufs_sb_sectors);
        SuperblockSun    sb_sun    = Marshal.ByteArrayToStructureLittleEndian<SuperblockSun>(ufs_sb_sectors);
        SuperblockOSF1   sb_osf1   = Marshal.ByteArrayToStructureLittleEndian<SuperblockOSF1>(ufs_sb_sectors);
        SuperblockHPUX   sb_hpux   = Marshal.ByteArrayToStructureLittleEndian<SuperblockHPUX>(ufs_sb_sectors);

        if(magic is UFS_CIGAM
                 or UFS_CIGAM_BW
                 or UFS_BAD_CIGAM
                 or FS_CIGAM_LFN
                 or FD_FSCIGAM
                 or FS_SEC_CIGAM
                 or MTB_UFS_CIGAM)
        {
            sb_41bsd  = Marshal.ByteArrayToStructureBigEndian<SuperBlockOldBSD>(ufs_sb_sectors);
            sb_44bsd  = Marshal.ByteArrayToStructureBigEndian<SuperBlock44BSD>(ufs_sb_sectors);
            sb_ultrix = Marshal.ByteArrayToStructureBigEndian<SuperblockUltrix>(ufs_sb_sectors);
            sb_svr4   = Marshal.ByteArrayToStructureBigEndian<SuperblockSVR4>(ufs_sb_sectors);
            sb_386bsd = Marshal.ByteArrayToStructureBigEndian<Superblock386BSD>(ufs_sb_sectors);
            sb_riscos = Marshal.ByteArrayToStructureBigEndian<SuperblockRISCos>(ufs_sb_sectors);
            sb_sun    = Marshal.ByteArrayToStructureBigEndian<SuperblockSun>(ufs_sb_sectors);
            sb_osf1   = Marshal.ByteArrayToStructureBigEndian<SuperblockOSF1>(ufs_sb_sectors);
            sb_hpux   = Marshal.ByteArrayToStructureBigEndian<SuperblockHPUX>(ufs_sb_sectors);
        }

        SuperBlock sb = Marshal.ByteArrayToStructureLittleEndian<SuperBlock>(ufs_sb_sectors);

        SuperBlock bs_sfu = Marshal.ByteArrayToStructureBigEndian<SuperBlock>(ufs_sb_sectors);

        if(bs_sfu.fs_magic == UFS_MAGIC     && sb.fs_magic == UFS_CIGAM     ||
           bs_sfu.fs_magic == UFS_MAGIC_BW  && sb.fs_magic == UFS_CIGAM_BW  ||
           bs_sfu.fs_magic == UFS2_MAGIC    && sb.fs_magic == UFS2_CIGAM    ||
           bs_sfu.fs_magic == UFS_BAD_MAGIC && sb.fs_magic == UFS_BAD_CIGAM ||
           bs_sfu.fs_magic == FS_MAGIC_LFN  && sb.fs_magic == FS_CIGAM_LFN  ||
           bs_sfu.fs_magic == FD_FSMAGIC    && sb.fs_magic == FD_FSCIGAM)
        {
            sb                           = bs_sfu;
            sb.fs_old_cstotal.cs_nbfree  = Swapping.Swap(sb.fs_old_cstotal.cs_nbfree);
            sb.fs_old_cstotal.cs_ndir    = Swapping.Swap(sb.fs_old_cstotal.cs_ndir);
            sb.fs_old_cstotal.cs_nffree  = Swapping.Swap(sb.fs_old_cstotal.cs_nffree);
            sb.fs_old_cstotal.cs_nifree  = Swapping.Swap(sb.fs_old_cstotal.cs_nifree);
            sb.fs_cstotal.cs_numclusters = Swapping.Swap(sb.fs_cstotal.cs_numclusters);
            sb.fs_cstotal.cs_nbfree      = Swapping.Swap(sb.fs_cstotal.cs_nbfree);
            sb.fs_cstotal.cs_ndir        = Swapping.Swap(sb.fs_cstotal.cs_ndir);
            sb.fs_cstotal.cs_nffree      = Swapping.Swap(sb.fs_cstotal.cs_nffree);
            sb.fs_cstotal.cs_nifree      = Swapping.Swap(sb.fs_cstotal.cs_nifree);
            sb.fs_cstotal.cs_spare[0]    = Swapping.Swap(sb.fs_cstotal.cs_spare[0]);
            sb.fs_cstotal.cs_spare[1]    = Swapping.Swap(sb.fs_cstotal.cs_spare[1]);
            sb.fs_cstotal.cs_spare[2]    = Swapping.Swap(sb.fs_cstotal.cs_spare[2]);
        }

        AaruLogging.Debug(MODULE_NAME, "sb offset: 0x{0:X8}",    sb_offset);
        AaruLogging.Debug(MODULE_NAME, "fs_rlink: 0x{0:X8}",     sb.fs_rlink);
        AaruLogging.Debug(MODULE_NAME, "fs_sblkno: 0x{0:X8}",    sb.fs_sblkno);
        AaruLogging.Debug(MODULE_NAME, "fs_cblkno: 0x{0:X8}",    sb.fs_cblkno);
        AaruLogging.Debug(MODULE_NAME, "fs_iblkno: 0x{0:X8}",    sb.fs_iblkno);
        AaruLogging.Debug(MODULE_NAME, "fs_dblkno: 0x{0:X8}",    sb.fs_dblkno);
        AaruLogging.Debug(MODULE_NAME, "fs_size: 0x{0:X8}",      sb.fs_size);
        AaruLogging.Debug(MODULE_NAME, "fs_dsize: 0x{0:X8}",     sb.fs_dsize);
        AaruLogging.Debug(MODULE_NAME, "fs_ncg: 0x{0:X8}",       sb.fs_ncg);
        AaruLogging.Debug(MODULE_NAME, "fs_bsize: 0x{0:X8}",     sb.fs_bsize);
        AaruLogging.Debug(MODULE_NAME, "fs_fsize: 0x{0:X8}",     sb.fs_fsize);
        AaruLogging.Debug(MODULE_NAME, "fs_frag: 0x{0:X8}",      sb.fs_frag);
        AaruLogging.Debug(MODULE_NAME, "fs_minfree: 0x{0:X8}",   sb.fs_minfree);
        AaruLogging.Debug(MODULE_NAME, "fs_bmask: 0x{0:X8}",     sb.fs_bmask);
        AaruLogging.Debug(MODULE_NAME, "fs_fmask: 0x{0:X8}",     sb.fs_fmask);
        AaruLogging.Debug(MODULE_NAME, "fs_bshift: 0x{0:X8}",    sb.fs_bshift);
        AaruLogging.Debug(MODULE_NAME, "fs_fshift: 0x{0:X8}",    sb.fs_fshift);
        AaruLogging.Debug(MODULE_NAME, "fs_maxcontig: 0x{0:X8}", sb.fs_maxcontig);
        AaruLogging.Debug(MODULE_NAME, "fs_maxbpg: 0x{0:X8}",    sb.fs_maxbpg);
        AaruLogging.Debug(MODULE_NAME, "fs_fragshift: 0x{0:X8}", sb.fs_fragshift);
        AaruLogging.Debug(MODULE_NAME, "fs_fsbtodb: 0x{0:X8}",   sb.fs_fsbtodb);
        AaruLogging.Debug(MODULE_NAME, "fs_sbsize: 0x{0:X8}",    sb.fs_sbsize);
        AaruLogging.Debug(MODULE_NAME, "fs_csmask: 0x{0:X8}",    sb.fs_csmask);
        AaruLogging.Debug(MODULE_NAME, "fs_csshift: 0x{0:X8}",   sb.fs_csshift);
        AaruLogging.Debug(MODULE_NAME, "fs_nindir: 0x{0:X8}",    sb.fs_nindir);
        AaruLogging.Debug(MODULE_NAME, "fs_inopb: 0x{0:X8}",     sb.fs_inopb);
        AaruLogging.Debug(MODULE_NAME, "fs_optim: 0x{0:X8}",     (int)sb.fs_optim);
        AaruLogging.Debug(MODULE_NAME, "fs_id_1: 0x{0:X8}",      sb.fs_id_1);
        AaruLogging.Debug(MODULE_NAME, "fs_id_2: 0x{0:X8}",      sb.fs_id_2);
        AaruLogging.Debug(MODULE_NAME, "fs_csaddr: 0x{0:X8}",    sb.fs_csaddr);
        AaruLogging.Debug(MODULE_NAME, "fs_cssize: 0x{0:X8}",    sb.fs_cssize);
        AaruLogging.Debug(MODULE_NAME, "fs_cgsize: 0x{0:X8}",    sb.fs_cgsize);
        AaruLogging.Debug(MODULE_NAME, "fs_ipg: 0x{0:X8}",       sb.fs_ipg);
        AaruLogging.Debug(MODULE_NAME, "fs_fpg: 0x{0:X8}",       sb.fs_fpg);
        AaruLogging.Debug(MODULE_NAME, "fs_fmod: 0x{0:X2}",      sb.fs_fmod);
        AaruLogging.Debug(MODULE_NAME, "fs_clean: 0x{0:X2}",     sb.fs_clean);
        AaruLogging.Debug(MODULE_NAME, "fs_ronly: 0x{0:X2}",     sb.fs_ronly);
        AaruLogging.Debug(MODULE_NAME, "fs_flags: 0x{0:X2}",     sb.fs_flags);
        AaruLogging.Debug(MODULE_NAME, "fs_magic: 0x{0:X8}",     sb.fs_magic);

        if(sb.fs_magic == UFS2_MAGIC)
            fs_type_ufs2 = true;
        else
        {
            // HP-UX is identified by its own magic numbers
            if(magic is FS_MAGIC_LFN or FS_CIGAM_LFN or FD_FSMAGIC or FD_FSCIGAM)
                fs_type_hpux = true;

            // OSF/1 secure UFS is identified by its magic
            else if(magic is FS_SEC_MAGIC or FS_SEC_CIGAM)
                fs_type_osf1 = true;

            // Solaris multi-terabyte UFS is identified by its magic
            else if(magic is MTB_UFS_MAGIC or MTB_UFS_CIGAM)
                fs_type_solaris = true;

            // For standard UFS/BorderWare/bad magic, use heuristics
            else
            {
                // Check variant-specific state formula: fs_state + fs_time == FSOKAY (BSD)
                // or fs_time - fs_state == FSOKAY (Sun/SVR4). Each variant stores fs_state
                // at a different offset, so a match at a specific offset identifies the variant.
                unchecked
                {
                    // SVR4: fs_state at offset 0x084
                    if((uint)(sb_svr4.fs_state + sb_svr4.fs_time)  == FSOKAY ||
                       (uint)(sb_svr4.fs_time  - sb_svr4.fs_state) == FSOKAY)
                        fs_type_svr4 = true;

                    // A/UX: fs_state at offset 0x08C
                    else if((uint)(sb_aux.fs_state + sb_aux.fs_time)  == FSOKAY ||
                            (uint)(sb_aux.fs_time  - sb_aux.fs_state) == FSOKAY)
                        fs_type_aux = true;

                    // Solaris: fs_state at 0x538, must also have Solaris-specific markers
                    else if(((uint)(sb_sun.fs_state + sb_sun.fs_time)  == FSOKAY ||
                             (uint)(sb_sun.fs_time  - sb_sun.fs_state) == FSOKAY) &&
                            (sb_sun.fs_version > 0 || sb_sun.fs_logbno != 0 || sb_sun.fs_rolled is > 0 and <= 2))
                        fs_type_solaris = true;

                    // 386BSD: fs_state at 0x538 (same offset as Solaris, no Solaris markers)
                    else if((uint)(sb_386bsd.fs_state + sb_386bsd.fs_time)  == FSOKAY ||
                            (uint)(sb_386bsd.fs_time  - sb_386bsd.fs_state) == FSOKAY)
                        fs_type_386bsd = true;

                    // 4.4BSD: fs_state at 0x548, or inodefmt == 2
                    else if(sb_44bsd.fs_inodefmt                          == InodeFormat.FS_44INODEFMT ||
                            (uint)(sb_44bsd.fs_state + sb_44bsd.fs_time)  == FSOKAY                    ||
                            (uint)(sb_44bsd.fs_time  - sb_44bsd.fs_state) == FSOKAY)
                        fs_type_44bsd = true;
                }

                // If no variant matched by state formula, try other heuristics
                if(!fs_type_svr4 && !fs_type_aux && !fs_type_solaris && !fs_type_386bsd && !fs_type_44bsd)
                {
                    // HP-UX by its distinctive fs_clean values
                    if(sb.fs_clean is HPUX_FS_CLEAN or HPUX_FS_OK or HPUX_FS_NOTOK)
                        fs_type_hpux = true;

                    // OSF/1 by fs_clean value
                    else if(sb.fs_clean == OSF1_FS_CLEAN)
                        fs_type_osf1 = true;

                    // Solaris by its distinctive fs_clean values (logging states)
                    else if(sb_sun.fs_clean is 0xFC or 0xFD or 0xFE)
                        fs_type_solaris = true;

                    // Ultrix: has valid fs_lastfsck timestamp
                    else if(sb_ultrix.fs_lastfsck                              > 0                        &&
                            DateHandlers.UnixToDateTime(sb_ultrix.fs_lastfsck) > new DateTime(1980, 1, 1) &&
                            DateHandlers.UnixToDateTime(sb_ultrix.fs_lastfsck) < DateTime.Now)
                        fs_type_ultrix = true;

                    // Fall back to existing heuristics for older BSD/SunOS variants
                    else
                    {
                        const uint SunOSEpoch = 0x1A54C580;

                        fs_type_43bsd = true; // Default

                        if(sb.fs_link > 0)
                        {
                            fs_type_42bsd = true;
                            fs_type_43bsd = false;
                        }

                        if((sb.fs_maxfilesize & 0xFFFFFFFF)                                    > SunOSEpoch &&
                           DateHandlers.UnixUnsignedToDateTime(sb.fs_maxfilesize & 0xFFFFFFFF) < DateTime.Now)
                        {
                            fs_type_42bsd = false;
                            fs_type_sun   = true;
                            fs_type_43bsd = false;
                        }

                        // This is for sure, as it is shared with sectors/track with non-x86 SunOS
                        if(sb.fs_old_npsect                              > SunOSEpoch &&
                           DateHandlers.UnixToDateTime(sb.fs_old_npsect) < DateTime.Now)
                        {
                            fs_type_42bsd = false;
                            fs_type_sun86 = true;
                            fs_type_sun   = false;
                            fs_type_43bsd = false;
                        }

                        if(sb.fs_cgrotor > 0x00000000 && (uint)sb.fs_cgrotor < 0xFFFFFFFF)
                        {
                            fs_type_42bsd = false;
                            fs_type_sun   = false;
                            fs_type_sun86 = false;
                            fs_type_ufs   = true;
                            fs_type_43bsd = false;
                        }

                        // Original 4.3BSD code does not use fs_id fields, but modern implementations
                        // like DragonFlyBSD do set them. Only check fs_id if we think it's 4.2BSD.
                        if(fs_type_42bsd && sb is not { fs_id_2: 0, fs_id_1: 0 })
                        {
                            // Has fs_id set, so it's not original 4.2BSD - promote to 4.3BSD
                            fs_type_42bsd = false;
                            fs_type_43bsd = true;
                        }

                        // This is the only 4.4BSD inode format
                        fs_type_44bsd |= sb.fs_old_inodefmt == 2;
                    }
                }
            }
        }

        // Ensure at least one variant is selected - default to 4.3BSD/generic FFS
        if(!fs_type_ufs2    &&
           !fs_type_42bsd   &&
           !fs_type_43bsd   &&
           !fs_type_44bsd   &&
           !fs_type_sun     &&
           !fs_type_sun86   &&
           !fs_type_ufs     &&
           !fs_type_hpux    &&
           !fs_type_svr4    &&
           !fs_type_aux     &&
           !fs_type_ultrix  &&
           !fs_type_386bsd  &&
           !fs_type_solaris &&
           !fs_type_osf1)
            fs_type_43bsd = true; // Safe default for unknown FFS variants

        if(!fs_type_ufs2)
        {
            sbInformation.AppendLine(Localization
                                        .There_are_a_lot_of_variants_of_UFS_using_overlapped_values_on_same_fields);

            sbInformation.AppendLine(Localization
                                        .I_will_try_to_guess_which_one_it_is_but_unless_its_UFS2_I_may_be_surely_wrong);
        }

        if(fs_type_42bsd) sbInformation.AppendLine(Localization.Guessed_as_42BSD_FFS);

        if(fs_type_43bsd) sbInformation.AppendLine(Localization.Guessed_as_43BSD_FFS);

        if(fs_type_44bsd) sbInformation.AppendLine(Localization.Guessed_as_44BSD_FFS);

        if(fs_type_sun) sbInformation.AppendLine(Localization.Guessed_as_SunOS_FFS);

        if(fs_type_sun86) sbInformation.AppendLine(Localization.Guessed_as_SunOS_x86_FFS);

        if(fs_type_ufs) sbInformation.AppendLine(Localization.Guessed_as_UFS);

        if(fs_type_hpux) sbInformation.AppendLine(Localization.Guessed_as_HPUX_UFS);

        if(fs_type_svr4) sbInformation.AppendLine(Localization.Guessed_as_SVR4_UFS);

        if(fs_type_aux) sbInformation.AppendLine(Localization.Guessed_as_AUX_UFS);

        if(fs_type_ultrix) sbInformation.AppendLine(Localization.Guessed_as_Ultrix_UFS);

        if(fs_type_386bsd) sbInformation.AppendLine(Localization.Guessed_as_386BSD_FFS);

        if(fs_type_solaris) sbInformation.AppendLine(Localization.Guessed_as_Solaris_UFS);

        if(fs_type_osf1) sbInformation.AppendLine(Localization.Guessed_as_OSF1_UFS);

        // Use variant-specific superblocks for accurate field display
        if(fs_type_svr4)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_svr4.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_svr4.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_svr4.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_svr4.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_svr4.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_svr4.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_svr4.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_svr4.fs_size,
                                       (long)sb_svr4.fs_size * sb_svr4.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_svr4.fs_size;
            metadata.ClusterSize = (uint)sb_svr4.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_svr4.fs_dsize,
                                       (long)sb_svr4.fs_dsize * sb_svr4.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_svr4.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_svr4.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_svr4.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_svr4.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_svr4.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_svr4.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_svr4.fs_rps,
                                       sb_svr4.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_svr4.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_svr4.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_svr4.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_svr4.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_svr4.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_svr4.fs_nspf).AppendLine();

            switch(sb_svr4.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_svr4.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_svr4.fs_id[0] > 0 || sb_svr4.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_svr4.fs_id[0], sb_svr4.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_svr4.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_svr4.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_svr4.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_svr4.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_svr4.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_svr4.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_svr4.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_svr4.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_svr4.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_svr4.fs_fpg / sb_svr4.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_svr4.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_svr4.fs_cstotal.cs_nbfree,
                                       (long)sb_svr4.fs_cstotal.cs_nbfree * sb_svr4.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_svr4.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_svr4.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_svr4.fs_cstotal.cs_nffree).AppendLine();

            if(sb_svr4.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_svr4.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_svr4.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_svr4.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_svr4.fs_fsmnt))
               .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_state_0_X8, sb_svr4.fs_state).AppendLine();
        }
        else if(fs_type_aux)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_aux.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_aux.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_aux.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_aux.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_aux.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_aux.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_aux.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_aux.fs_size,
                                       (long)sb_aux.fs_size * sb_aux.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_aux.fs_size;
            metadata.ClusterSize = (uint)sb_aux.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_aux.fs_dsize,
                                       (long)sb_aux.fs_dsize * sb_aux.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_aux.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_aux.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_aux.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_aux.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_aux.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_aux.fs_rotdelay).AppendLine();

            sbInformation
               .AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm, sb_aux.fs_rps, sb_aux.fs_rps * 60)
               .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_aux.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_aux.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_aux.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_aux.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_aux.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_aux.fs_nspf).AppendLine();

            switch(sb_aux.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_aux.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_aux.fs_id[0] > 0 || sb_aux.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_aux.fs_id[0], sb_aux.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_aux.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_aux.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_aux.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_aux.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_aux.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_aux.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_aux.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_aux.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_aux.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_aux.fs_fpg / sb_aux.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_aux.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_aux.fs_cstotal.cs_nbfree,
                                       (long)sb_aux.fs_cstotal.cs_nbfree * sb_aux.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_aux.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_aux.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_aux.fs_cstotal.cs_nffree).AppendLine();

            if(sb_aux.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_aux.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_aux.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_aux.fs_flags).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_aux.fs_fsmnt))
                         .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_name_0, StringHandlers.CToString(sb_aux.fs_fname))
                         .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_pack_name_0, StringHandlers.CToString(sb_aux.fs_fpack))
                         .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_state_0_X8, sb_aux.fs_state).AppendLine();
        }
        else if(fs_type_solaris)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_sun.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_sun.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_sun.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_sun.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_sun.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_sun.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_sun.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_sun.fs_size,
                                       (long)sb_sun.fs_size * sb_sun.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_sun.fs_size;
            metadata.ClusterSize = (uint)sb_sun.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_sun.fs_dsize,
                                       (long)sb_sun.fs_dsize * sb_sun.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_sun.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_sun.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_sun.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_sun.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_sun.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_sun.fs_rotdelay).AppendLine();

            sbInformation
               .AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm, sb_sun.fs_rps, sb_sun.fs_rps * 60)
               .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_sun.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_sun.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_sun.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_sun.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_sun.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_sun.fs_nspf).AppendLine();

            switch(sb_sun.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_sun.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_sun.fs_id[0] > 0 || sb_sun.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_sun.fs_id[0], sb_sun.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_sun.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_sun.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_sun.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_sun.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_sun.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_sun.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_sun.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_sun.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_sun.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_sun.fs_fpg / sb_sun.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_sun.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_sun.fs_cstotal.cs_nbfree,
                                       (long)sb_sun.fs_cstotal.cs_nbfree * sb_sun.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_sun.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_sun.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_sun.fs_cstotal.cs_nffree).AppendLine();

            if(sb_sun.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_sun.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_sun.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_sun.fs_flags).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_sun.fs_fsmnt))
                         .AppendLine();

            if(sb_sun.fs_version > 0)
                sbInformation.AppendFormat(Localization.Solaris_UFS_version_0, sb_sun.fs_version).AppendLine();

            if(sb_sun.fs_logbno != 0)
                sbInformation.AppendFormat(Localization.Log_block_number_0, sb_sun.fs_logbno).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_state_on_0, DateHandlers.UnixToDateTime(sb_sun.fs_state))
                         .AppendLine();

            if(sb_sun.fs_nrpos > 0)
                sbInformation.AppendFormat(Localization._0_rotational_positions, sb_sun.fs_nrpos).AppendLine();

            if(sb_sun.fs_rotbloff > 0)
                sbInformation.AppendFormat(Localization._0_blocks_per_rotation, sb_sun.fs_rotbloff).AppendLine();
        }
        else if(fs_type_44bsd)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_44bsd.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_44bsd.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_44bsd.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_44bsd.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_44bsd.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_44bsd.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_44bsd.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_44bsd.fs_size,
                                       (long)sb_44bsd.fs_size * sb_44bsd.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_44bsd.fs_size;
            metadata.ClusterSize = (uint)sb_44bsd.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_44bsd.fs_dsize,
                                       (long)sb_44bsd.fs_dsize * sb_44bsd.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_44bsd.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_44bsd.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_44bsd.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_44bsd.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_44bsd.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_44bsd.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_44bsd.fs_rps,
                                       sb_44bsd.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_44bsd.fs_maxcontig)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_44bsd.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_44bsd.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_44bsd.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_44bsd.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_44bsd.fs_nspf).AppendLine();

            switch(sb_44bsd.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_44bsd.fs_optim)
                                 .AppendLine();

                    break;
            }

            sbInformation.AppendFormat(Localization.Hardware_sector_interleave_0, sb_44bsd.fs_interleave).AppendLine();
            sbInformation.AppendFormat(Localization.Sector_zero_skew_0_track,     sb_44bsd.fs_trackskew).AppendLine();

            if(sb_44bsd.fs_headswitch > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_head_switch, sb_44bsd.fs_headswitch).AppendLine();

            if(sb_44bsd.fs_trkseek > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_track_to_track_seek, sb_44bsd.fs_trkseek)
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_44bsd.fs_csaddr).AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_44bsd.fs_cssize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group,   sb_44bsd.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder,           sb_44bsd.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track,             sb_44bsd.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder,          sb_44bsd.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume,       sb_44bsd.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group,           sb_44bsd.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_44bsd.fs_ipg).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_44bsd.fs_fpg / sb_44bsd.fs_frag)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_directories, sb_44bsd.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_44bsd.fs_cstotal.cs_nbfree,
                                       (long)sb_44bsd.fs_cstotal.cs_nbfree * sb_44bsd.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_44bsd.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_44bsd.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_44bsd.fs_cstotal.cs_nffree).AppendLine();

            if(sb_44bsd.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_44bsd.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_44bsd.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_44bsd.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_44bsd.fs_fsmnt))
               .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_on_cluster_summary_array, sb_44bsd.fs_contigsumsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Maximum_length_of_a_symbolic_link_0, sb_44bsd.fs_maxsymlinklen)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.A_file_can_be_0_bytes_at_max, sb_44bsd.fs_maxfilesize).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_state_on_0, DateHandlers.UnixToDateTime(sb_44bsd.fs_state))
                         .AppendLine();

            if(sb_44bsd.fs_nrpos > 0)
                sbInformation.AppendFormat(Localization._0_rotational_positions, sb_44bsd.fs_nrpos).AppendLine();

            if(sb_44bsd.fs_rotbloff > 0)
                sbInformation.AppendFormat(Localization._0_blocks_per_rotation, sb_44bsd.fs_rotbloff).AppendLine();
        }
        else if(fs_type_ufs2)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb.fs_old_cgoffset)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb.fs_time))
                         .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_1_bytes, sb.fs_size, sb.fs_size * sb.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb.fs_size;
            metadata.ClusterSize = (uint)sb.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_1_bytes, sb.fs_dsize, sb.fs_dsize * sb.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb.fs_old_rotdelay).AppendLine();

            sbInformation
               .AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm, sb.fs_old_rps, sb.fs_old_rps * 60)
               .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb.fs_maxcontig).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb.fs_maxbpg).AppendLine();
            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0, sb.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0, sb.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0, sb.fs_old_nspf).AppendLine();

            switch(sb.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb.fs_optim)
                                 .AppendLine();

                    break;
            }

            sbInformation.AppendFormat(Localization.Hardware_sector_interleave_0, sb.fs_old_interleave).AppendLine();
            sbInformation.AppendFormat(Localization.Sector_zero_skew_0_track,     sb.fs_old_trackskew).AppendLine();

            if(sb.fs_id_1 > 0 || sb.fs_id_2 > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb.fs_id_1, sb.fs_id_2).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_area_LBA_0, sb.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb.fs_old_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb.fs_old_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb.fs_old_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb.fs_old_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb.fs_old_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb.fs_fpg / sb.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb.fs_cstotal.cs_nbfree,
                                       sb.fs_cstotal.cs_nbfree * sb.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes,   sb.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,    sb.fs_cstotal.cs_nffree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_clusters, sb.fs_cstotal.cs_numclusters).AppendLine();

            if(sb.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb.fs_flags).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb.fs_fsmnt))
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Volume_name_0, StringHandlers.CToString(sb.fs_volname))
                         .AppendLine();

            metadata.VolumeName = StringHandlers.CToString(sb.fs_volname);
            sbInformation.AppendFormat(Localization.Volume_ID_0_X16,                sb.fs_swuid).AppendLine();
            sbInformation.AppendFormat(Localization.Last_searched_cylinder_group_0, sb.fs_cgrotor).AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguously_allocated_directories, sb.fs_contigdirs)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Standard_superblock_LBA_0, sb.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories,            sb.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb.fs_cstotal.cs_nbfree,
                                       sb.fs_cstotal.cs_nbfree * sb.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes,   sb.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,    sb.fs_cstotal.cs_nffree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_clusters, sb.fs_cstotal.cs_numclusters).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb.fs_time))
                         .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_1_bytes, sb.fs_size, sb.fs_size * sb.fs_fsize)
                         .AppendLine();

            metadata.Clusters = (ulong)sb.fs_size;

            sbInformation.AppendFormat(Localization._0_data_blocks_1_bytes, sb.fs_dsize, sb.fs_dsize * sb.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_area_LBA_0, sb.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_pending_of_being_freed, sb.fs_pendingblocks).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_pending_of_being_freed, sb.fs_pendinginodes).AppendLine();
        }
        else if(fs_type_42bsd || fs_type_43bsd)
        {
            sbInformation.AppendFormat(Localization.Linked_list_of_filesystems_0, sb_41bsd.fs_link).AppendLine();
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,             sb_41bsd.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,         sb_41bsd.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,            sb_41bsd.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0,       sb_41bsd.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_41bsd.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_41bsd.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_41bsd.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_41bsd.fs_size,
                                       (long)sb_41bsd.fs_size * sb_41bsd.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_41bsd.fs_size;
            metadata.ClusterSize = (uint)sb_41bsd.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_41bsd.fs_dsize,
                                       (long)sb_41bsd.fs_dsize * sb_41bsd.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_41bsd.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_41bsd.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_41bsd.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_41bsd.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_41bsd.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_41bsd.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_41bsd.fs_rps,
                                       sb_41bsd.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_41bsd.fs_maxcontig)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_41bsd.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_41bsd.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_41bsd.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_41bsd.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_41bsd.fs_nspf).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_41bsd.fs_csaddr).AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_41bsd.fs_cssize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group,   sb_41bsd.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder,           sb_41bsd.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track,             sb_41bsd.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder,          sb_41bsd.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume,       sb_41bsd.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group,           sb_41bsd.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_41bsd.fs_ipg).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_41bsd.fs_fpg / sb_41bsd.fs_frag)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_directories, sb_41bsd.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_41bsd.fs_cstotal.cs_nbfree,
                                       (long)sb_41bsd.fs_cstotal.cs_nbfree * sb_41bsd.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_41bsd.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_41bsd.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_41bsd.fs_cstotal.cs_nffree).AppendLine();

            if(sb_41bsd.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_41bsd.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_41bsd.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_41bsd.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_41bsd.fs_fsmnt))
               .AppendLine();
        }
        else if(fs_type_386bsd)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_386bsd.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_386bsd.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_386bsd.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_386bsd.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_386bsd.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_386bsd.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_386bsd.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_386bsd.fs_size,
                                       (long)sb_386bsd.fs_size * sb_386bsd.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_386bsd.fs_size;
            metadata.ClusterSize = (uint)sb_386bsd.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_386bsd.fs_dsize,
                                       (long)sb_386bsd.fs_dsize * sb_386bsd.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_386bsd.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_386bsd.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_386bsd.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_386bsd.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_386bsd.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_386bsd.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_386bsd.fs_rps,
                                       sb_386bsd.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_386bsd.fs_maxcontig)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_386bsd.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_386bsd.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_386bsd.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_386bsd.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_386bsd.fs_nspf).AppendLine();

            switch(sb_386bsd.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_386bsd.fs_optim)
                                 .AppendLine();

                    break;
            }

            sbInformation.AppendFormat(Localization.Hardware_sector_interleave_0, sb_386bsd.fs_interleave).AppendLine();
            sbInformation.AppendFormat(Localization.Sector_zero_skew_0_track,     sb_386bsd.fs_trackskew).AppendLine();

            if(sb_386bsd.fs_headswitch > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_head_switch, sb_386bsd.fs_headswitch).AppendLine();

            if(sb_386bsd.fs_trkseek > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_track_to_track_seek, sb_386bsd.fs_trkseek)
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_386bsd.fs_csaddr).AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_386bsd.fs_cssize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group,   sb_386bsd.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder,           sb_386bsd.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track,             sb_386bsd.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder,          sb_386bsd.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume,       sb_386bsd.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group,           sb_386bsd.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_386bsd.fs_ipg).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_386bsd.fs_fpg / sb_386bsd.fs_frag)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_directories, sb_386bsd.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_386bsd.fs_cstotal.cs_nbfree,
                                       (long)sb_386bsd.fs_cstotal.cs_nbfree * sb_386bsd.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_386bsd.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_386bsd.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_386bsd.fs_cstotal.cs_nffree).AppendLine();

            if(sb_386bsd.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_386bsd.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_386bsd.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_386bsd.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_386bsd.fs_fsmnt))
               .AppendLine();

            sbInformation.AppendFormat(Localization.Volume_state_on_0, DateHandlers.UnixToDateTime(sb_386bsd.fs_state))
                         .AppendLine();

            if(sb_386bsd.fs_nrpos > 0)
                sbInformation.AppendFormat(Localization._0_rotational_positions, sb_386bsd.fs_nrpos).AppendLine();

            if(sb_386bsd.fs_rotbloff > 0)
                sbInformation.AppendFormat(Localization._0_blocks_per_rotation, sb_386bsd.fs_rotbloff).AppendLine();
        }
        else if(fs_type_ultrix)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_ultrix.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_ultrix.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_ultrix.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_ultrix.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_ultrix.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_ultrix.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_ultrix.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_ultrix.fs_size,
                                       (long)sb_ultrix.fs_size * sb_ultrix.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_ultrix.fs_size;
            metadata.ClusterSize = (uint)sb_ultrix.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_ultrix.fs_dsize,
                                       (long)sb_ultrix.fs_dsize * sb_ultrix.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_ultrix.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_ultrix.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_ultrix.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_ultrix.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_ultrix.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_ultrix.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_ultrix.fs_rps,
                                       sb_ultrix.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_ultrix.fs_maxcontig)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_ultrix.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_ultrix.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_ultrix.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_ultrix.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_ultrix.fs_nspf).AppendLine();

            switch(sb_ultrix.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_ultrix.fs_optim)
                                 .AppendLine();

                    break;
            }

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_ultrix.fs_csaddr).AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_ultrix.fs_cssize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group,   sb_ultrix.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder,           sb_ultrix.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track,             sb_ultrix.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder,          sb_ultrix.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume,       sb_ultrix.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group,           sb_ultrix.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_ultrix.fs_ipg).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_ultrix.fs_fpg / sb_ultrix.fs_frag)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_directories, sb_ultrix.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_ultrix.fs_cstotal.cs_nbfree,
                                       (long)sb_ultrix.fs_cstotal.cs_nbfree * sb_ultrix.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_ultrix.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_ultrix.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_ultrix.fs_cstotal.cs_nffree).AppendLine();

            if(sb_ultrix.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_ultrix.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_ultrix.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_ultrix.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_ultrix.fs_fsmnt))
               .AppendLine();

            if(sb_ultrix.fs_lastfsck > 0)
                sbInformation
                   .AppendFormat(Localization.Last_fsck_on_0, DateHandlers.UnixToDateTime(sb_ultrix.fs_lastfsck))
                   .AppendLine();

            if(sb_ultrix.fs_gennum > 0)
                sbInformation.AppendFormat(Localization.Unique_file_system_id_0, sb_ultrix.fs_gennum).AppendLine();
        }
        else if(fs_type_hpux)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_hpux.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_hpux.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_hpux.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_hpux.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_hpux.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_hpux.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_hpux.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_hpux.fs_size,
                                       (long)sb_hpux.fs_size * sb_hpux.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_hpux.fs_size;
            metadata.ClusterSize = (uint)sb_hpux.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_hpux.fs_dsize,
                                       (long)sb_hpux.fs_dsize * sb_hpux.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_hpux.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_hpux.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_hpux.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_hpux.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_hpux.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_hpux.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_hpux.fs_rps,
                                       sb_hpux.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_hpux.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_hpux.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_hpux.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_hpux.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_hpux.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_hpux.fs_nspf).AppendLine();

            switch(sb_hpux.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_hpux.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_hpux.fs_id[0] > 0 || sb_hpux.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_hpux.fs_id[0], sb_hpux.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_hpux.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_hpux.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_hpux.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_hpux.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_hpux.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_hpux.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_hpux.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_hpux.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_hpux.fs_fpg / sb_hpux.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_hpux.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_hpux.fs_cstotal.cs_nbfree,
                                       (long)sb_hpux.fs_cstotal.cs_nbfree * sb_hpux.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_hpux.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_hpux.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_hpux.fs_cstotal.cs_nffree).AppendLine();

            if(sb_hpux.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_hpux.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_hpux.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_hpux.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_hpux.fs_fsmnt))
               .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_name_0, StringHandlers.CToString(sb_hpux.fs_fname))
                         .AppendLine();

            sbInformation.AppendFormat(Localization.File_system_pack_name_0, StringHandlers.CToString(sb_hpux.fs_fpack))
                         .AppendLine();

            if(sb_hpux.fs_featurebits != 0)
                sbInformation.AppendFormat(Localization.Feature_bits_0_X8, sb_hpux.fs_featurebits).AppendLine();
        }
        else if(fs_type_osf1)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_osf1.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_osf1.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_osf1.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_osf1.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_osf1.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_osf1.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_osf1.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_osf1.fs_size,
                                       (long)sb_osf1.fs_size * sb_osf1.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_osf1.fs_size;
            metadata.ClusterSize = (uint)sb_osf1.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_osf1.fs_dsize,
                                       (long)sb_osf1.fs_dsize * sb_osf1.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_osf1.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_osf1.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_osf1.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_osf1.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_osf1.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_osf1.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_osf1.fs_rps,
                                       sb_osf1.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_osf1.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_osf1.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_osf1.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_osf1.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_osf1.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_osf1.fs_nspf).AppendLine();

            switch(sb_osf1.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_osf1.fs_optim)
                                 .AppendLine();

                    break;
            }

            sbInformation.AppendFormat(Localization.Hardware_sector_interleave_0, sb_osf1.fs_interleave).AppendLine();
            sbInformation.AppendFormat(Localization.Sector_zero_skew_0_track,     sb_osf1.fs_trackskew).AppendLine();

            if(sb_osf1.fs_headswitch > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_head_switch, sb_osf1.fs_headswitch).AppendLine();

            if(sb_osf1.fs_trkseek > 0)
                sbInformation.AppendFormat(Localization._0_µsec_for_track_to_track_seek, sb_osf1.fs_trkseek)
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_osf1.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_osf1.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_osf1.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_osf1.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_osf1.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_osf1.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_osf1.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_osf1.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_osf1.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_osf1.fs_fpg / sb_osf1.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_osf1.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_osf1.fs_cstotal.cs_nbfree,
                                       (long)sb_osf1.fs_cstotal.cs_nbfree * sb_osf1.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_osf1.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_osf1.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_osf1.fs_cstotal.cs_nffree).AppendLine();

            if(sb_osf1.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_osf1.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_osf1.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_osf1.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_osf1.fs_fsmnt))
               .AppendLine();

            if(sb_osf1.fs_nrpos > 0)
                sbInformation.AppendFormat(Localization._0_rotational_positions, sb_osf1.fs_nrpos).AppendLine();

            if(sb_osf1.fs_rotbloff > 0)
                sbInformation.AppendFormat(Localization._0_blocks_per_rotation, sb_osf1.fs_rotbloff).AppendLine();
        }
        else if(fs_type_sun || fs_type_sun86)
        {
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_sun.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_sun.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_sun.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_sun.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_sun.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_sun.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_sun.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_sun.fs_size,
                                       (long)sb_sun.fs_size * sb_sun.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_sun.fs_size;
            metadata.ClusterSize = (uint)sb_sun.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_sun.fs_dsize,
                                       (long)sb_sun.fs_dsize * sb_sun.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_sun.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_sun.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_sun.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_sun.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_sun.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_sun.fs_rotdelay).AppendLine();

            sbInformation
               .AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm, sb_sun.fs_rps, sb_sun.fs_rps * 60)
               .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_sun.fs_maxcontig).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_sun.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_sun.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_sun.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_sun.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_sun.fs_nspf).AppendLine();

            switch(sb_sun.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_sun.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_sun.fs_id[0] > 0 || sb_sun.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_sun.fs_id[0], sb_sun.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_sun.fs_csaddr).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_sun.fs_cssize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group, sb_sun.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder, sb_sun.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track, sb_sun.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder, sb_sun.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume, sb_sun.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group, sb_sun.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_sun.fs_ipg).AppendLine();
            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_sun.fs_fpg / sb_sun.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_directories, sb_sun.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_sun.fs_cstotal.cs_nbfree,
                                       (long)sb_sun.fs_cstotal.cs_nbfree * sb_sun.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_sun.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_sun.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_sun.fs_cstotal.cs_nffree).AppendLine();

            if(sb_sun.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_sun.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_sun.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_sun.fs_flags).AppendLine();

            sbInformation.AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_sun.fs_fsmnt))
                         .AppendLine();

            if(fs_type_sun)
                sbInformation
                   .AppendFormat(Localization.Volume_state_on_0, DateHandlers.UnixToDateTime(sb_sun.fs_npsect))
                   .AppendLine();

            if(sb_sun.fs_nrpos > 0)
                sbInformation.AppendFormat(Localization._0_rotational_positions, sb_sun.fs_nrpos).AppendLine();

            if(sb_sun.fs_rotbloff > 0)
                sbInformation.AppendFormat(Localization._0_blocks_per_rotation, sb_sun.fs_rotbloff).AppendLine();
        }
        else if(fs_type_ufs)
        {
            // Generic UFS - use sb_riscos as it's a basic variant close to original
            sbInformation.AppendFormat(Localization.Superblock_LBA_0,       sb_riscos.fs_sblkno).AppendLine();
            sbInformation.AppendFormat(Localization.Cylinder_block_LBA_0,   sb_riscos.fs_cblkno).AppendLine();
            sbInformation.AppendFormat(Localization.inode_block_LBA_0,      sb_riscos.fs_iblkno).AppendLine();
            sbInformation.AppendFormat(Localization.First_data_block_LBA_0, sb_riscos.fs_dblkno).AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_offset_in_cylinder_0, sb_riscos.fs_cgoffset)
                         .AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_written_on_0, DateHandlers.UnixToDateTime(sb_riscos.fs_time))
               .AppendLine();

            metadata.ModificationDate = DateHandlers.UnixToDateTime(sb_riscos.fs_time);

            sbInformation.AppendFormat(Localization._0_blocks_in_volume_1_bytes,
                                       sb_riscos.fs_size,
                                       (long)sb_riscos.fs_size * sb_riscos.fs_fsize)
                         .AppendLine();

            metadata.Clusters    = (ulong)sb_riscos.fs_size;
            metadata.ClusterSize = (uint)sb_riscos.fs_fsize;

            sbInformation.AppendFormat(Localization._0_data_blocks_in_volume_1_bytes,
                                       sb_riscos.fs_dsize,
                                       (long)sb_riscos.fs_dsize * sb_riscos.fs_fsize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_cylinder_groups_in_volume, sb_riscos.fs_ncg).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_basic_block,    sb_riscos.fs_bsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_bytes_in_a_frag_block,     sb_riscos.fs_fsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_frags_in_a_block,          sb_riscos.fs_frag).AppendLine();
            sbInformation.AppendFormat(Localization._0_of_blocks_must_be_free,    sb_riscos.fs_minfree).AppendLine();
            sbInformation.AppendFormat(Localization._0_ms_for_optimal_next_block, sb_riscos.fs_rotdelay).AppendLine();

            sbInformation.AppendFormat(Localization.Disk_rotates_0_times_per_second_1_rpm,
                                       sb_riscos.fs_rps,
                                       sb_riscos.fs_rps * 60)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_contiguous_blocks_at_maximum, sb_riscos.fs_maxcontig)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_cylinder_group_at_maximum, sb_riscos.fs_maxbpg)
                         .AppendLine();

            sbInformation.AppendFormat(Localization.Superblock_is_0_bytes, sb_riscos.fs_sbsize).AppendLine();
            sbInformation.AppendFormat(Localization.NINDIR_0,              sb_riscos.fs_nindir).AppendLine();
            sbInformation.AppendFormat(Localization.INOPB_0,               sb_riscos.fs_inopb).AppendLine();
            sbInformation.AppendFormat(Localization.NSPF_0,                sb_riscos.fs_nspf).AppendLine();

            switch(sb_riscos.fs_optim)
            {
                case FsOptim.FS_OPTTIME:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_allocation_time);

                    break;
                case FsOptim.FS_OPTSPACE:
                    sbInformation.AppendLine(Localization.Filesystem_will_minimize_volume_fragmentation);

                    break;
                default:
                    sbInformation.AppendFormat(Localization.Unknown_optimization_value_0, (int)sb_riscos.fs_optim)
                                 .AppendLine();

                    break;
            }

            if(sb_riscos.fs_id[0] > 0 || sb_riscos.fs_id[1] > 0)
                sbInformation.AppendFormat(Localization.Volume_ID_0_X8_1_X8, sb_riscos.fs_id[0], sb_riscos.fs_id[1])
                             .AppendLine();

            sbInformation.AppendFormat(Localization.Cylinder_group_summary_LBA_0, sb_riscos.fs_csaddr).AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group_summary, sb_riscos.fs_cssize)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_bytes_in_cylinder_group,   sb_riscos.fs_cgsize).AppendLine();
            sbInformation.AppendFormat(Localization._0_tracks_cylinder,           sb_riscos.fs_ntrak).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_track,             sb_riscos.fs_nsect).AppendLine();
            sbInformation.AppendFormat(Localization._0_sectors_cylinder,          sb_riscos.fs_spc).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_in_volume,       sb_riscos.fs_ncyl).AppendLine();
            sbInformation.AppendFormat(Localization._0_cylinders_group,           sb_riscos.fs_cpg).AppendLine();
            sbInformation.AppendFormat(Localization._0_inodes_per_cylinder_group, sb_riscos.fs_ipg).AppendLine();

            sbInformation.AppendFormat(Localization._0_blocks_per_group, sb_riscos.fs_fpg / sb_riscos.fs_frag)
                         .AppendLine();

            sbInformation.AppendFormat(Localization._0_directories, sb_riscos.fs_cstotal.cs_ndir).AppendLine();

            sbInformation.AppendFormat(Localization._0_free_blocks_1_bytes,
                                       sb_riscos.fs_cstotal.cs_nbfree,
                                       (long)sb_riscos.fs_cstotal.cs_nbfree * sb_riscos.fs_fsize)
                         .AppendLine();

            metadata.FreeClusters = (ulong)sb_riscos.fs_cstotal.cs_nbfree;
            sbInformation.AppendFormat(Localization._0_free_inodes, sb_riscos.fs_cstotal.cs_nifree).AppendLine();
            sbInformation.AppendFormat(Localization._0_free_frags,  sb_riscos.fs_cstotal.cs_nffree).AppendLine();

            if(sb_riscos.fs_fmod == 1)
            {
                sbInformation.AppendLine(Localization.Superblock_is_being_modified);
                metadata.Dirty = true;
            }

            if(sb_riscos.fs_clean == 1) sbInformation.AppendLine(Localization.Volume_is_clean);
            if(sb_riscos.fs_ronly == 1) sbInformation.AppendLine(Localization.Volume_is_read_only);

            sbInformation.AppendFormat(Localization.Volume_flags_0_X2, sb_riscos.fs_flags).AppendLine();

            sbInformation
               .AppendFormat(Localization.Volume_last_mounted_at_0, StringHandlers.CToString(sb_riscos.fs_fsmnt))
               .AppendLine();
        }


        information = sbInformation.ToString();
    }

#endregion
}