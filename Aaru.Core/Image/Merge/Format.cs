using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Localization;
using Aaru.Logging;

namespace Aaru.Core.Image;

public sealed partial class Merger
{
    (ErrorNumber error, IMediaImage inputFormat) GetInputImage(string imagePath)
    {
        // Identify input file filter (determines file type handler)

        InitProgress?.Invoke();

        PulseProgress?.Invoke(UI.Identifying_file_filter);

        IFilter inputFilter = PluginRegister.Singleton.GetFilter(imagePath);

        EndProgress?.Invoke();

        if(inputFilter == null)
        {
            AaruLogging.Error(UI.Cannot_open_specified_file);

            return (ErrorNumber.CannotOpenFile, null);
        }

        // Identify input image format

        InitProgress?.Invoke();
        PulseProgress?.Invoke(UI.Identifying_image_format);

        IBaseImage baseImage   = ImageFormat.Detect(inputFilter);
        var        inputFormat = baseImage as IMediaImage;

        EndProgress?.Invoke();


        if(baseImage == null)
        {
            StoppingErrorMessage?.Invoke(UI.Input_image_format_not_identified);

            return (ErrorNumber.UnrecognizedFormat, null);
        }

        if(inputFormat == null)
        {
            StoppingErrorMessage?.Invoke(UI.Command_not_yet_supported_for_this_image_type);

            return (ErrorNumber.InvalidArgument, null);
        }

        UpdateStatus?.Invoke(string.Format(UI.Input_image_format_identified_by_0, inputFormat.Name));

        try
        {
            // Open the input image file for reading

            InitProgress?.Invoke();
            PulseProgress?.Invoke(UI.Invoke_Opening_image_file);

            ErrorNumber opened = inputFormat.Open(inputFilter);

            EndProgress?.Invoke();

            if(opened != ErrorNumber.NoError)
            {
                StoppingErrorMessage?.Invoke(UI.Unable_to_open_image_format +
                                             Environment.NewLine            +
                                             string.Format(Localization.Core.Error_0, opened));

                return (opened, null);
            }

            // Get media type and handle obsolete type mappings for backwards compatibility
            MediaType mediaType = inputFormat.Info.MediaType;

            // Obsolete types
#pragma warning disable 612
            mediaType = mediaType switch
                        {
                            MediaType.SQ1500     => MediaType.SyJet,
                            MediaType.Bernoulli  => MediaType.Bernoulli10,
                            MediaType.Bernoulli2 => MediaType.BernoulliBox2_20,
                            _                    => inputFormat.Info.MediaType
                        };
#pragma warning restore 612

            AaruLogging.Debug(MODULE_NAME, UI.Correctly_opened_image_file);

            // Log image statistics for debugging
            AaruLogging.Debug(MODULE_NAME, UI.Image_without_headers_is_0_bytes, inputFormat.Info.ImageSize);

            AaruLogging.Debug(MODULE_NAME, UI.Image_has_0_sectors, inputFormat.Info.Sectors);

            AaruLogging.Debug(MODULE_NAME, UI.Image_identifies_media_type_as_0, mediaType);

            Statistics.AddMediaFormat(inputFormat.Format);
            Statistics.AddMedia(mediaType, false);
            Statistics.AddFilter(inputFilter.Name);

            return (ErrorNumber.NoError, inputFormat);
        }
        catch(Exception ex)
        {
            StoppingErrorMessage?.Invoke(UI.Unable_to_open_image_format +
                                         Environment.NewLine            +
                                         string.Format(Localization.Core.Error_0, ex.Message));

            AaruLogging.Exception(ex, Localization.Core.Error_0, ex.Message);

            return (ErrorNumber.CannotOpenFormat, null);
        }
    }

    private IWritableImage FindOutputFormat(PluginRegister plugins, string format, string outputPath)
    {
        // Discovers output format plugin by extension, GUID, or name
        // Searches writable format plugins matching any of three methods:
        // 1. By file extension (if format not specified)
        // 2. By plugin GUID (if format is valid GUID)
        // 3. By plugin name (case-insensitive string match)
        // Returns null if no match or multiple matches found

        List<IBaseWritableImage> candidates = [];

        // Try extension
        if(string.IsNullOrEmpty(format))
        {
            candidates.AddRange(from plugin in plugins.WritableImages.Values
                                where plugin is not null
                                where plugin.KnownExtensions.Contains(Path.GetExtension(outputPath))
                                select plugin);
        }

        // Try Id
        else if(Guid.TryParse(format, CultureInfo.CurrentCulture, out Guid outId))
        {
            candidates.AddRange(from plugin in plugins.WritableImages.Values
                                where plugin is not null
                                where plugin.Id == outId
                                select plugin);
        }

        // Try name
        else
        {
            candidates.AddRange(from plugin in plugins.WritableImages.Values
                                where plugin is not null
                                where plugin.Name.Equals(format, StringComparison.InvariantCultureIgnoreCase)
                                select plugin);
        }

        switch(candidates.Count)
        {
            case 0:
                StoppingErrorMessage?.Invoke(UI.No_plugin_supports_requested_extension);

                return null;
            case > 1:
                StoppingErrorMessage?.Invoke(UI.More_than_one_plugin_supports_requested_extension);

                return null;
        }

        return candidates[0] as IWritableImage;
    }
}