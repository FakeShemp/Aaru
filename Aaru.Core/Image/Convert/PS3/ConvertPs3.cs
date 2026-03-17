using System;
using System.Text;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core.Image.PS3;
using Aaru.Localization;
using Humanizer;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>Parsed IRD file (if any), used by both InjectPs3MediaTags and EnrichPs3TitleAndPartNumber.</summary>
    IrdFile? _ps3Ird;
    /// <summary>PS3 plaintext regions parsed from sector 0, used by both InjectPs3MediaTags and ConvertPs3Sectors.</summary>
    Ps3PlaintextRegion[] _ps3PlaintextRegions;

    /// <summary>
    ///     Resolves PS3 encryption keys and writes PS3-specific media tags
    ///     (disc key, data1, data2, PIC, encryption map) to the output image.
    ///     Must be called before ConvertMediaTags so the lazy-init in libaaruformat sees them.
    /// </summary>
    ErrorNumber InjectPs3MediaTags()
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();

        // Resolve keys
        Ps3KeyResolver.ResolveKeys(_inputPath, _inputImage, out byte[] discKey, out byte[] data1Key, out _ps3Ird);

        if(discKey == null)
        {
            EndProgress?.Invoke();
            StoppingErrorMessage?.Invoke(UI.PS3_no_disc_key_found);

            return ErrorNumber.NoData;
        }

        string keySource = data1Key != null ? "data1 derivation" : "disc key";

        if(_ps3Ird?.Valid == true) keySource = "IRD (" + _ps3Ird.Value.GameId + ")";

        UpdateStatus?.Invoke(string.Format(UI.PS3_disc_key_resolved_from_0, keySource));

        // Read sector 0 and parse encryption map
        ErrorNumber errno = _inputImage.ReadSector(0, false, out byte[] sector0, out _);

        if(errno != ErrorNumber.NoError)
        {
            EndProgress?.Invoke();
            StoppingErrorMessage?.Invoke(UI.PS3_error_reading_sector_0);

            return errno;
        }

        _ps3PlaintextRegions = Ps3EncryptionMap.ParseFromSector0(sector0);

        UpdateStatus?.Invoke(string.Format(UI.PS3_encryption_map_0_regions, _ps3PlaintextRegions.Length));

        // Write PS3 media tags
        PulseProgress?.Invoke(string.Format(UI.PS3_writing_media_tag_0, MediaTagType.PS3_DiscKey.Humanize()));
        _outputImage.WriteMediaTag(discKey, MediaTagType.PS3_DiscKey);

        if(data1Key != null)
        {
            PulseProgress?.Invoke(string.Format(UI.PS3_writing_media_tag_0, MediaTagType.PS3_Data1.Humanize()));
            _outputImage.WriteMediaTag(data1Key, MediaTagType.PS3_Data1);
        }

        if(_ps3Ird?.Valid == true)
        {
            IrdFile ird = _ps3Ird.Value;

            if(ird.D2 is { Length: 16 })
            {
                PulseProgress?.Invoke(string.Format(UI.PS3_writing_media_tag_0, MediaTagType.PS3_Data2.Humanize()));
                _outputImage.WriteMediaTag(ird.D2, MediaTagType.PS3_Data2);
            }

            if(ird.HasPic && ird.Pic is { Length: 115 })
            {
                PulseProgress?.Invoke(string.Format(UI.PS3_writing_media_tag_0, MediaTagType.PS3_PIC.Humanize()));
                _outputImage.WriteMediaTag(ird.Pic, MediaTagType.PS3_PIC);
            }
        }

        // Serialize and write encryption map
        byte[] encMapData = Ps3EncryptionMap.Serialize(_ps3PlaintextRegions);
        PulseProgress?.Invoke(string.Format(UI.PS3_writing_media_tag_0, MediaTagType.PS3_EncryptionMap.Humanize()));
        _outputImage.WriteMediaTag(encMapData, MediaTagType.PS3_EncryptionMap);

        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     Transfers all sectors from input to output with per-sector SectorStatus
    ///     derived from the PS3 encryption map. Sectors in encrypted regions get
    ///     <see cref="SectorStatus.Unencrypted" />, sectors in plaintext regions get
    ///     <see cref="SectorStatus.Dumped" />.
    /// </summary>
    ErrorNumber ConvertPs3Sectors()
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();
        ulong doneSectors = 0;

        while(doneSectors < _inputImage.Info.Sectors)
        {
            if(_aborted) break;

            uint sectorsToDo;

            if(_inputImage.Info.Sectors - doneSectors >= _count)
                sectorsToDo = _count;
            else
                sectorsToDo = (uint)(_inputImage.Info.Sectors - doneSectors);

            UpdateProgress?.Invoke(string.Format(UI.PS3_converting_sectors_0_to_1,
                                                 doneSectors,
                                                 doneSectors + sectorsToDo),
                                   (long)doneSectors,
                                   (long)_inputImage.Info.Sectors);

            // Read sectors from source (short read — PS3 raw ISOs have no sector tags)
            ErrorNumber errno = sectorsToDo == 1
                                    ? _inputImage.ReadSector(doneSectors, false, out byte[] sectorData, out _)
                                    : _inputImage.ReadSectors(doneSectors, false, sectorsToDo, out sectorData, out _);

            if(errno != ErrorNumber.NoError)
            {
                if(_force)
                    ErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_continuing, errno, doneSectors));
                else
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_reading_sector_1_not_continuing,
                                                               errno,
                                                               doneSectors));

                    return errno;
                }

                doneSectors += sectorsToDo;

                continue;
            }

            // Determine per-sector status from encryption map
            bool result;

            if(sectorsToDo == 1)
            {
                SectorStatus status = Ps3EncryptionMap.IsSectorEncrypted(_ps3PlaintextRegions, doneSectors)
                                          ? SectorStatus.Unencrypted
                                          : SectorStatus.Dumped;

                result = _outputImage.WriteSector(sectorData, doneSectors, false, status);
            }
            else
            {
                var statusArray = new SectorStatus[sectorsToDo];

                for(uint i = 0; i < sectorsToDo; i++)
                {
                    statusArray[i] = Ps3EncryptionMap.IsSectorEncrypted(_ps3PlaintextRegions, doneSectors + i)
                                         ? SectorStatus.Unencrypted
                                         : SectorStatus.Dumped;
                }

                result = _outputImage.WriteSectors(sectorData, doneSectors, false, sectorsToDo, statusArray);
            }

            if(!result)
            {
                if(_force)
                {
                    ErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_continuing,
                                                       _outputImage.ErrorMessage,
                                                       doneSectors));
                }
                else
                {
                    StoppingErrorMessage?.Invoke(string.Format(UI.Error_0_writing_sector_1_not_continuing,
                                                               _outputImage.ErrorMessage,
                                                               doneSectors));

                    return ErrorNumber.WriteError;
                }
            }

            doneSectors += sectorsToDo;
        }

        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }

    /// <summary>
    ///     After all normal metadata has been copied, checks if MediaTitle or MediaPartNumber
    ///     are still unset and enriches them from the IRD sidecar or PARAM.SFO within the disc image.
    /// </summary>
    void EnrichPs3TitleAndPartNumber()
    {
        if(_aborted) return;

        // Check if title/part number are already set (from CLI overrides or source image metadata)
        bool needTitle      = string.IsNullOrEmpty(_outputImage.Info.MediaTitle);
        bool needPartNumber = string.IsNullOrEmpty(_outputImage.Info.MediaPartNumber);

        if(!needTitle && !needPartNumber) return;

        // Priority 1: IRD sidecar
        if(_ps3Ird?.Valid == true)
        {
            IrdFile ird = _ps3Ird.Value;

            string title      = needTitle ? ird.GameName : null;
            string partNumber = needPartNumber ? ird.GameId : null;

            if(!string.IsNullOrEmpty(title) || !string.IsNullOrEmpty(partNumber))
            {
                ApplyPs3TitleOverrides(title, partNumber);
                UpdateStatus?.Invoke(UI.PS3_title_from_ird);

                return;
            }
        }

        // Priority 2: Mount with UDF and read PARAM.SFO
        TryEnrichFromParamSfo();
    }

    /// <summary>
    ///     Attempts to mount the source image with the UDF filesystem plugin,
    ///     read /PS3_GAME/PARAM.SFO, parse it, and extract title/part number.
    /// </summary>
    void TryEnrichFromParamSfo()
    {
        // Find UDF plugin
        IReadOnlyFilesystem udfPlugin = null;

        foreach(IFilesystem fs in _plugins.Filesystems.Values)
        {
            if(fs is not IReadOnlyFilesystem rofs) continue;

            // UDF plugin's Name or Id — check by name
            if(!fs.Name.Contains("UDF", StringComparison.OrdinalIgnoreCase)) continue;

            // Create a whole-image partition
            Partition wholeImage = new()
            {
                Start    = 0,
                Length   = _inputImage.Info.Sectors,
                Size     = _inputImage.Info.Sectors * _inputImage.Info.SectorSize,
                Sequence = 0,
                Type     = "UDF"
            };

            if(!fs.Identify(_inputImage, wholeImage)) continue;

            if(rofs.Mount(_inputImage, wholeImage, Encoding.UTF8, null, null) != ErrorNumber.NoError) continue;

            udfPlugin = rofs;

            break;
        }

        if(udfPlugin == null) return;

        try
        {
            // Open and read /PS3_GAME/PARAM.SFO
            ErrorNumber errno = udfPlugin.OpenFile("/PS3_GAME/PARAM.SFO", out IFileNode node);

            if(errno != ErrorNumber.NoError) return;

            var sfoData = new byte[node.Length];
            errno = udfPlugin.ReadFile(node, node.Length, sfoData, out long bytesRead);
            udfPlugin.CloseFile(node);

            if(errno != ErrorNumber.NoError || bytesRead == 0) return;

            // Parse SFO — will be implemented in Step 6 (SfoParser)
            // For now, we prepare the infrastructure and will call SfoParser here
            // SfoParser.Parse will be called here once it exists
        }
        catch
        {
            // Best-effort: if UDF mount or SFO read fails, we simply skip enrichment
        }
        finally
        {
            udfPlugin.Unmount();
        }
    }

    /// <summary>Applies title and/or part number overrides to the output image via SetImageInfo.</summary>
    void ApplyPs3TitleOverrides(string title, string partNumber)
    {
        CommonTypes.Structs.ImageInfo updatedInfo = _outputImage.Info;

        if(!string.IsNullOrEmpty(title)) updatedInfo.MediaTitle = title;

        if(!string.IsNullOrEmpty(partNumber)) updatedInfo.MediaPartNumber = partNumber;

        _outputImage.SetImageInfo(updatedInfo);
    }
}