using Aaru.CommonTypes.Enums;
using Aaru.Core.Image.WiiU;
using Aaru.Localization;
using Humanizer;

namespace Aaru.Core.Image;

public partial class Convert
{
    /// <summary>Wii U disc key (16 bytes).</summary>
    byte[] _wiiuDiscKey;
    /// <summary>Parsed Wii U partitions from TOC, used by both InjectWiiuMediaTags and ConvertWiiuSectors.</summary>
    WiiuPartition[] _wiiuPartitions;
    /// <summary>Wii U partition region map, used by ConvertWiiuSectors.</summary>
    WiiuPartitionRegion[] _wiiuRegions;

    /// <summary>
    ///     Resolves the Wii U disc key, parses the TOC, extracts title keys,
    ///     builds the partition key map, and writes Wii U-specific media tags
    ///     (WiiUDiscKey, WiiUPartitionKeyMap) to the output image.
    ///     Must be called before ConvertMediaTags.
    /// </summary>
    ErrorNumber InjectWiiuMediaTags()
    {
        if(_aborted) return ErrorNumber.NoError;

        InitProgress?.Invoke();

        // Step 1: Get disc key from source image media tags
        ErrorNumber errno = _inputImage.ReadMediaTag(MediaTagType.WiiUDiscKey, out _wiiuDiscKey);

        if(errno != ErrorNumber.NoError || _wiiuDiscKey is not { Length: 16 })
        {
            EndProgress?.Invoke();
            StoppingErrorMessage?.Invoke(UI.WiiU_no_disc_key_found);

            return ErrorNumber.NoData;
        }

        UpdateStatus?.Invoke(UI.WiiU_disc_key_loaded);

        // Step 2: Parse TOC
        PulseProgress?.Invoke(UI.WiiU_parsing_partition_table);

        if(!WiiuToc.ParseToc(_inputImage, _wiiuDiscKey, out _wiiuPartitions))
        {
            EndProgress?.Invoke();
            StoppingErrorMessage?.Invoke(UI.WiiU_cannot_parse_toc);

            return ErrorNumber.InvalidArgument;
        }

        UpdateStatus?.Invoke(string.Format(UI.WiiU_found_0_partitions, _wiiuPartitions.Length));

        // Step 3: Extract title keys from SI/GI partitions
        PulseProgress?.Invoke(UI.WiiU_extracting_title_keys);

        int keysFound = WiiuTitleKeys.ExtractTitleKeys(_inputImage, _wiiuDiscKey, _wiiuPartitions);

        UpdateStatus?.Invoke(string.Format(UI.WiiU_extracted_0_title_keys, keysFound));

        // Step 4: Build partition region map
        PulseProgress?.Invoke(UI.WiiU_building_partition_key_map);

        var totalPhysicalSectors = (uint)(_inputImage.Info.Sectors / WiiuCrypto.LOGICAL_PER_PHYSICAL);
        _wiiuRegions = WiiuPartitionMap.BuildRegionMap(_wiiuPartitions, totalPhysicalSectors);

        // Step 5: Write media tags
        PulseProgress?.Invoke(string.Format(UI.WiiU_writing_media_tag_0, MediaTagType.WiiUDiscKey.Humanize()));

        _outputImage.WriteMediaTag(_wiiuDiscKey, MediaTagType.WiiUDiscKey);

        byte[] keyMapData = WiiuPartitionMap.Serialize(_wiiuRegions);

        PulseProgress?.Invoke(string.Format(UI.WiiU_writing_media_tag_0, MediaTagType.WiiUPartitionKeyMap.Humanize()));

        _outputImage.WriteMediaTag(keyMapData, MediaTagType.WiiUPartitionKeyMap);

        EndProgress?.Invoke();

        return ErrorNumber.NoError;
    }
}