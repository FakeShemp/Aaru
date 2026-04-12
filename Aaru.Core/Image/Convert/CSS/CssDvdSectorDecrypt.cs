using System;
using System.Collections.Generic;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core.Media;
using Aaru.Decryption.DVD;
using Aaru.Devices;
using Aaru.Localization;
using Aaru.Logging;
using MediaType = Aaru.CommonTypes.MediaType;

namespace Aaru.Core.Image;

/// <summary>CSS decryption and <see cref="SectorStatus"/> tagging for DVD optical pipelines.</summary>
static class CssDvdSectorDecrypt
{
    internal static bool IsDvdMedia(MediaType mediaType) =>
        mediaType is MediaType.DVDROM
                  or MediaType.DVDR
                  or MediaType.DVDRW
                  or MediaType.DVDPR
                  or MediaType.DVDPRW
                  or MediaType.DVDPRWDL
                  or MediaType.DVDRDL
                  or MediaType.DVDPRDL
                  or MediaType.DVDRAM
                  or MediaType.DVDRWDL
                  or MediaType.DVDDownload
                  or MediaType.PS2DVD
                  or MediaType.PS3DVD
                  or MediaType.XGD
                  or MediaType.XGD2
                  or MediaType.XGD3
                  or MediaType.Nuon;

    /// <summary>Generates DVD title keys.</summary>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="generatedTitleKeys">Generated title keys.</param>
    /// <param name="logModuleName">Log module name.</param>
    /// <param name="isAborted">Is aborted function.</param>
    internal static void GenerateDvdTitleKeys(IOpticalMediaImage inputOptical, PluginRegister plugins,
                                              ref byte[] generatedTitleKeys, string logModuleName,
                                              Func<bool> isAborted)
    {
        if(isAborted()) return;

        List<Partition> allPartitions = Partitions.GetAll(inputOptical);

        byte[] lastKeysFromVideoTsFilesystem = null;

        foreach(IReadOnlyFilesystem rofs in plugins.ReadOnlyFilesystems.Values)
        {
            if(isAborted()) return;

            List<Partition> supportedPartitions = [];

            foreach(Partition partition in allPartitions)
            {
                if(!rofs.Identify(inputOptical, partition))
                    continue;

                supportedPartitions.Add(partition);
            }

            if(supportedPartitions.Count == 0)
                continue;

            bool hasVideoTs = false;

            foreach(Partition partition in supportedPartitions)
            {
                if(CSS.HasVideoTsFolder(inputOptical, rofs, partition))
                {
                    hasVideoTs = true;

                    break;
                }
            }

            if(!hasVideoTs)
                continue;

            AaruLogging.Debug(logModuleName, UI.Generating_decryption_keys);

            byte[] keys = CSS.GenerateTitleKeys(inputOptical,
                                                supportedPartitions,
                                                inputOptical.Info.Sectors,
                                                rofs);

            lastKeysFromVideoTsFilesystem = keys;

            if(!TitleKeysBufferIsAllZero(keys))
            {
                generatedTitleKeys = keys;

                return;
            }
        }

        if(lastKeysFromVideoTsFilesystem != null)
            generatedTitleKeys = lastKeysFromVideoTsFilesystem;
    }

    /// <summary>Checks if the title keys buffer is all zero.</summary>
    /// <param name="keys">Title keys buffer.</param>
    /// <returns>True if the title keys buffer is all zero.</returns>
    static bool TitleKeysBufferIsAllZero(byte[] keys)
    {
        if(keys is null || keys.Length == 0)
            return true;

        for(int i = 0; i < keys.Length; i++)
        {
            if(keys[i] != 0)
                return false;
        }

        return true;
    }

    /// <summary>Applies CSS after reading a sector.</summary>
    /// <param name="sector">Sector data.</param>
    /// <param name="sectorStatus">Sector status.</param>
    /// <param name="sectorStatusArray">Sector status array.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="readOneSector">True if reading one sector.</param>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="sectorAddress">Sector address.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="generatedTitleKeys">Generated title keys.</param>
    /// <param name="isAborted">Is aborted function.</param>
    /// <param name="logModuleName">Log module name.</param>
    internal static void ApplyCssAfterRead(ref byte[] sector, ref SectorStatus sectorStatus,
                                                SectorStatus[] sectorStatusArray, uint sectorsToDo, bool readOneSector,
                                                IOpticalMediaImage inputOptical, ulong sectorAddress,
                                                PluginRegister plugins, ref byte[] generatedTitleKeys,
                                                Func<bool> isAborted, string logModuleName)
    {
        if(isAborted()) return;

        int blockSize = sector.Length / (int)sectorsToDo;

        if(sector.Length % sectorsToDo != 0 || blockSize <= 0)
            return;

        if(!Mpeg.ContainsMpegPackets(sector, sectorsToDo, (uint)blockSize))
        {
            SetStatusesDumped(ref sectorStatus, sectorStatusArray, sectorsToDo, readOneSector);

            return;
        }

        bool[] blockAppliedCssDecrypt = new bool[sectorsToDo];

        DecryptDvdSector(ref sector,
                              inputOptical,
                              sectorAddress,
                              sectorsToDo,
                              plugins,
                              ref generatedTitleKeys,
                              isAborted,
                              logModuleName,
                              blockAppliedCssDecrypt);

        ApplyDecryptFlagsToStatuses(blockAppliedCssDecrypt,
                                    sectorsToDo,
                                    ref sectorStatus,
                                    sectorStatusArray,
                                    readOneSector);
    }

    /// <summary>Applies CSS after reading a long sector.</summary>
    /// <param name="sector">Sector data.</param>
    /// <param name="sectorStatus">Sector status.</param>
    /// <param name="sectorStatusArray">Sector status array.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="readOneSector">True if reading one sector.</param>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="sectorAddress">Sector address.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="generatedTitleKeys">Generated title keys.</param>
    /// <param name="isAborted">Is aborted function.</param>
    /// <param name="logModuleName">Log module name.</param>
    internal static void ApplyCssAfterReadLong(ref byte[] sector, ref SectorStatus sectorStatus,
                                               SectorStatus[] sectorStatusArray, uint sectorsToDo, bool readOneSector,
                                               IOpticalMediaImage inputOptical, ulong sectorAddress,
                                               PluginRegister plugins, ref byte[] generatedTitleKeys,
                                               Func<bool> isAborted, string logModuleName)
    {
        if(isAborted()) return;

        if((long)sector.Length != (long)sectorsToDo * Mpeg.DvdLongSectorStride)
            return;

        if(!Mpeg.ContainsMpegPacketsLong(sector, sectorsToDo))
        {
            SetStatusesDumped(ref sectorStatus, sectorStatusArray, sectorsToDo, readOneSector);

            return;
        }

        bool[] blockAppliedCssDecrypt = new bool[sectorsToDo];

        DecryptDvdSectorLong(ref sector,
                             inputOptical,
                             sectorAddress,
                             sectorsToDo,
                             plugins,
                             ref generatedTitleKeys,
                             isAborted,
                             logModuleName,
                             blockAppliedCssDecrypt);

        ApplyDecryptFlagsToStatuses(blockAppliedCssDecrypt,
                                    sectorsToDo,
                                    ref sectorStatus,
                                    sectorStatusArray,
                                    readOneSector);
    }

    /// <summary>Sets all the statuses to dumped.</summary>
    /// <param name="sectorStatus">Sector status.</param>
    /// <param name="sectorStatusArray">Sector status array.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="readOneSector">True if reading one sector.</param>
    static void SetStatusesDumped(ref SectorStatus sectorStatus, SectorStatus[] sectorStatusArray, uint sectorsToDo,
                                   bool readOneSector)
    {
        if(readOneSector)
            sectorStatus = SectorStatus.Dumped;
        else
        {
            for(uint i = 0; i < sectorsToDo; i++)
                sectorStatusArray[i] = SectorStatus.Dumped;
        }
    }

    /// <summary>Applies the decrypt flags to the statuses.</summary>
    /// <param name="blockAppliedCssDecrypt">Block applied CSS decrypt.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="sectorStatus">Sector status.</param>
    /// <param name="sectorStatusArray">Sector status array.</param>
    /// <param name="readOneSector">True if reading one sector.</param>
    static void ApplyDecryptFlagsToStatuses(bool[] blockAppliedCssDecrypt, uint sectorsToDo,
                                            ref SectorStatus sectorStatus, SectorStatus[] sectorStatusArray,
                                            bool readOneSector)
    {
        if(readOneSector)
            sectorStatus = blockAppliedCssDecrypt[0] ? SectorStatus.Unencrypted : SectorStatus.Dumped;
        else
        {
            for(uint i = 0; i < sectorsToDo; i++)
                sectorStatusArray[i] = blockAppliedCssDecrypt[i] ? SectorStatus.Unencrypted : SectorStatus.Dumped;
        }
    }

    /// <summary>Decrypts a DVD sector.</summary>
    /// <param name="sector">Sector data.</param>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="sectorAddress">Sector address.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="generatedTitleKeys">Generated title keys.</param>
    /// <param name="isAborted">Is aborted function.</param>
    /// <param name="logModuleName">Log module name.</param>
    /// <param name="blockAppliedCssDecrypt">Block applied CSS decrypt.</param>
    static void DecryptDvdSector(ref byte[] sector, IOpticalMediaImage inputOptical, ulong sectorAddress,
                                      uint sectorsToDo, PluginRegister plugins, ref byte[] generatedTitleKeys,
                                      Func<bool> isAborted, string logModuleName, bool[] blockAppliedCssDecrypt)
    {
        if(isAborted()) return;

        int blockSize = sector.Length / (int)sectorsToDo;

        if(sector.Length % sectorsToDo != 0 || blockSize <= 0)
            return;

        if(!Mpeg.ContainsMpegPackets(sector, sectorsToDo, (uint)blockSize)) return;

        byte[] cmi, titleKey;

        if(sectorsToDo == 1)
        {
            if(inputOptical.ReadSectorTag(sectorAddress, false, SectorTagType.DvdSectorCmi, out cmi) ==
               ErrorNumber.NoError &&
               inputOptical.ReadSectorTag(sectorAddress, false, SectorTagType.DvdTitleKeyDecrypted, out titleKey) ==
               ErrorNumber.NoError)
                sector = CSS.DecryptSector(sector, titleKey, cmi, 1, (uint)blockSize, blockAppliedCssDecrypt);
            else
            {
                if(generatedTitleKeys == null)
                    GenerateDvdTitleKeys(inputOptical, plugins, ref generatedTitleKeys, logModuleName, isAborted);

                if(generatedTitleKeys != null)
                {
                    sector = CSS.DecryptSector(sector,
                                               generatedTitleKeys.Skip((int)(5 * sectorAddress)).Take(5).ToArray(),
                                               null,
                                               1,
                                               (uint)blockSize,
                                               blockAppliedCssDecrypt);
                }
            }
        }
        else
        {
            if(inputOptical.ReadSectorsTag(sectorAddress, false, sectorsToDo, SectorTagType.DvdSectorCmi, out cmi) ==
               ErrorNumber.NoError &&
               inputOptical.ReadSectorsTag(sectorAddress,
                                           false,
                                           sectorsToDo,
                                           SectorTagType.DvdTitleKeyDecrypted,
                                           out titleKey) ==
               ErrorNumber.NoError)
                sector = CSS.DecryptSector(sector, titleKey, cmi, sectorsToDo, (uint)blockSize, blockAppliedCssDecrypt);
            else
            {
                if(generatedTitleKeys == null)
                    GenerateDvdTitleKeys(inputOptical, plugins, ref generatedTitleKeys, logModuleName, isAborted);

                if(generatedTitleKeys != null)
                {
                    sector = CSS.DecryptSector(sector,
                                               generatedTitleKeys.Skip((int)(5 * sectorAddress))
                                                                 .Take((int)(5 * sectorsToDo))
                                                                 .ToArray(),
                                               null,
                                               sectorsToDo,
                                               (uint)blockSize,
                                               blockAppliedCssDecrypt);
                }
            }
        }
    }

    /// <summary>Decrypts a DVD long sector.</summary>
    /// <param name="sector">Sector data.</param>
    /// <param name="inputOptical">Input optical media image.</param>
    /// <param name="sectorAddress">Sector address.</param>
    /// <param name="sectorsToDo">Number of sectors to do.</param>
    /// <param name="plugins">Plugin register.</param>
    /// <param name="generatedTitleKeys">Generated title keys.</param>
    /// <param name="isAborted">Is aborted function.</param>
    /// <param name="logModuleName">Log module name.</param>
    /// <param name="blockAppliedCssDecrypt">Block applied CSS decrypt.</param>
    static void DecryptDvdSectorLong(ref byte[] sector, IOpticalMediaImage inputOptical, ulong sectorAddress,
                                     uint sectorsToDo, PluginRegister plugins, ref byte[] generatedTitleKeys,
                                     Func<bool> isAborted, string logModuleName, bool[] blockAppliedCssDecrypt)
    {
        if(isAborted()) return;

        if((long)sector.Length != (long)sectorsToDo * Mpeg.DvdLongSectorStride)
            return;

        if(!Mpeg.ContainsMpegPacketsLong(sector, sectorsToDo)) return;

        byte[] cmi, titleKey;

        if(sectorsToDo == 1)
        {
            if(inputOptical.ReadSectorTag(sectorAddress, false, SectorTagType.DvdSectorCmi, out cmi) ==
               ErrorNumber.NoError &&
               inputOptical.ReadSectorTag(sectorAddress, false, SectorTagType.DvdTitleKeyDecrypted, out titleKey) ==
               ErrorNumber.NoError)
                sector = CSS.DecryptSectorLong(sector, titleKey, cmi, 1, 2048, blockAppliedCssDecrypt);
            else
            {
                if(generatedTitleKeys == null)
                    GenerateDvdTitleKeys(inputOptical, plugins, ref generatedTitleKeys, logModuleName, isAborted);

                if(generatedTitleKeys != null)
                {
                    sector = CSS.DecryptSectorLong(sector,
                                                   generatedTitleKeys.Skip((int)(5 * sectorAddress)).Take(5).ToArray(),
                                                   null,
                                                   1,
                                                   2048,
                                                   blockAppliedCssDecrypt);
                }
            }
        }
        else
        {
            if(inputOptical.ReadSectorsTag(sectorAddress, false, sectorsToDo, SectorTagType.DvdSectorCmi, out cmi) ==
               ErrorNumber.NoError &&
               inputOptical.ReadSectorsTag(sectorAddress,
                                           false,
                                           sectorsToDo,
                                           SectorTagType.DvdTitleKeyDecrypted,
                                           out titleKey) ==
               ErrorNumber.NoError)
                sector = CSS.DecryptSectorLong(sector, titleKey, cmi, sectorsToDo, 2048, blockAppliedCssDecrypt);
            else
            {
                if(generatedTitleKeys == null)
                    GenerateDvdTitleKeys(inputOptical, plugins, ref generatedTitleKeys, logModuleName, isAborted);

                if(generatedTitleKeys != null)
                {
                    sector = CSS.DecryptSectorLong(sector,
                                                   generatedTitleKeys.Skip((int)(5 * sectorAddress))
                                                                     .Take((int)(5 * sectorsToDo))
                                                                     .ToArray(),
                                                   null,
                                                   sectorsToDo,
                                                   2048,
                                                   blockAppliedCssDecrypt);
                }
            }
        }
    }
}
