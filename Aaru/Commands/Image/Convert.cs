// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Convert.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Converts from one media image to another.
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
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Xml.Serialization;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Metadata;
using Aaru.Core;
using Aaru.Localization;
using Aaru.Logging;
using Schemas;
using Spectre.Console;
using Spectre.Console.Cli;
using Convert = Aaru.Core.Image.Convert;
using File = System.IO.File;
using MediaType = Aaru.CommonTypes.MediaType;

namespace Aaru.Commands.Image;

sealed class ConvertImageCommand : Command<ConvertImageCommand.Settings>
{
    const  string       MODULE_NAME = "Convert-image command";
    static ProgressTask _progressTask1;
    static ProgressTask _progressTask2;

    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MainClass.PrintCopyright();

        // Initialize subchannel fix flags with cascading dependencies
        bool fixSubchannel         = settings.FixSubchannel;
        bool fixSubchannelCrc      = settings.FixSubchannelCrc;
        bool fixSubchannelPosition = settings.FixSubchannelPosition;

        if(fixSubchannelCrc) fixSubchannel = true;

        if(fixSubchannel) fixSubchannelPosition = true;

        Statistics.AddCommand("convert-image");

        // Log all command parameters for debugging and auditing
        Dictionary<string, string> parsedOptions = Options.Parse(settings.Options);
        LogCommandParameters(settings, fixSubchannelPosition, fixSubchannel, fixSubchannelCrc, parsedOptions);

        // Validate sector count parameter
        if(settings.Count == 0)
        {
            AaruLogging.Error(UI.Need_to_specify_more_than_zero_sectors_to_copy_at_once);

            return (int)ErrorNumber.InvalidArgument;
        }

        // Parse and validate CHS geometry if specified
        (bool success, uint cylinders, uint heads, uint sectors)? geometryResult = ParseGeometry(settings.Geometry);
        (uint cylinders, uint heads, uint sectors)?               geometryValues = null;

        if(geometryResult.HasValue)
        {
            if(!geometryResult.Value.success) return (int)ErrorNumber.InvalidArgument;

            geometryValues = (geometryResult.Value.cylinders, geometryResult.Value.heads, geometryResult.Value.sectors);
        }

        // Load metadata and resume information from sidecar files
        Resume    resume  = null;
        Metadata  sidecar = null;
        MediaType mediaType;

        (bool success, Metadata sidecar, Resume resume) metadataResult =
            LoadMetadata(settings.AaruMetadata, settings.CicmXml, settings.ResumeFile);

        if(!metadataResult.success) return (int)ErrorNumber.InvalidArgument;

        sidecar = metadataResult.sidecar;
        resume  = metadataResult.resume;

        // Identify input file filter (determines file type handler)
        IFilter inputFilter = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_file_filter).IsIndeterminate();
            inputFilter = PluginRegister.Singleton.GetFilter(settings.InputPath);
        });

        if(inputFilter == null)
        {
            AaruLogging.Error(UI.Cannot_open_specified_file);

            return (int)ErrorNumber.CannotOpenFile;
        }

        // Verify output file doesn't already exist
        if(File.Exists(settings.OutputPath))
        {
            AaruLogging.Error(UI.Output_file_already_exists);

            return (int)ErrorNumber.FileExists;
        }

        // Identify input image format
        PluginRegister plugins     = PluginRegister.Singleton;
        IMediaImage    inputFormat = null;
        IBaseImage     baseImage   = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_image_format).IsIndeterminate();
            baseImage   = ImageFormat.Detect(inputFilter);
            inputFormat = baseImage as IMediaImage;
        });

        if(inputFormat == null)
        {
            AaruLogging.WriteLine(UI.Input_image_format_not_identified);

            return (int)ErrorNumber.UnrecognizedFormat;
        }

        // TODO: Implement
        if(inputFormat == null)
        {
            AaruLogging.WriteLine(UI.Command_not_yet_supported_for_this_image_type);

            return (int)ErrorNumber.InvalidArgument;
        }

        if(settings.Verbose)
            AaruLogging.Verbose(UI.Input_image_format_identified_by_0_1, inputFormat.Name, inputFormat.Id);
        else
            AaruLogging.WriteLine(UI.Input_image_format_identified_by_0, inputFormat.Name);

        uint nominalNegativeSectors = 0;
        uint nominalOverflowSectors = 0;

        try
        {
            // Open the input image file for reading
            ErrorNumber opened = ErrorNumber.NoData;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Invoke_Opening_image_file).IsIndeterminate();
                opened = inputFormat.Open(inputFilter);
            });

            if(opened != ErrorNumber.NoError)
            {
                AaruLogging.WriteLine(UI.Unable_to_open_image_format);
                AaruLogging.WriteLine(Localization.Core.Error_0, opened);

                return (int)opened;
            }

            nominalNegativeSectors = settings.IgnoreNegativeSectors ? 0 : inputFormat.Info.NegativeSectors;
            nominalOverflowSectors = settings.IgnoreOverflowSectors ? 0 : inputFormat.Info.OverflowSectors;

            // Get media type and handle obsolete type mappings for backwards compatibility
            mediaType = inputFormat.Info.MediaType;

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
        }
        catch(Exception ex)
        {
            AaruLogging.Error(UI.Unable_to_open_image_format);
            AaruLogging.Error(Localization.Core.Error_0, ex.Message);
            AaruLogging.Exception(ex, Localization.Core.Error_0, ex.Message);

            return (int)ErrorNumber.CannotOpenFormat;
        }

        // Discover and load output format plugin
        IBaseWritableImage outputFormat = FindOutputFormat(plugins, settings.Format, settings.OutputPath);

        if(outputFormat == null) return (int)ErrorNumber.FormatNotFound;

        if(settings.Verbose)
            AaruLogging.Verbose(UI.Output_image_format_0_1, outputFormat.Name, outputFormat.Id);
        else
            AaruLogging.WriteLine(UI.Output_image_format_0, outputFormat.Name);

        var converter = new Convert(inputFormat,
                                    outputFormat as IWritableImage,
                                    mediaType,
                                    settings.Force,
                                    settings.OutputPath,
                                    parsedOptions,
                                    nominalNegativeSectors,
                                    nominalOverflowSectors,
                                    settings.Comments,
                                    settings.Creator,
                                    settings.DriveFirmwareRevision,
                                    settings.DriveManufacturer,
                                    settings.DriveModel,
                                    settings.DriveSerialNumber,
                                    settings.LastMediaSequence,
                                    settings.MediaBarcode,
                                    settings.MediaManufacturer,
                                    settings.MediaModel,
                                    settings.MediaPartNumber,
                                    settings.MediaSequence,
                                    settings.MediaSerialNumber,
                                    settings.MediaTitle,
                                    settings.Decrypt,
                                    (uint)settings.Count,
                                    plugins,
                                    fixSubchannelPosition,
                                    fixSubchannel,
                                    fixSubchannelCrc,
                                    settings.GenerateSubchannels,
                                    geometryValues,
                                    resume,
                                    sidecar);

        ErrorNumber errno = ErrorNumber.NoError;

        AnsiConsole.Progress()
                   .AutoClear(true)
                   .HideCompleted(true)
                   .Columns(new ProgressBarColumn(), new PercentageColumn(), new TaskDescriptionColumn())
                   .Start(ctx =>
                    {
                        converter.UpdateStatus += static text => AaruLogging.WriteLine(text);

                        converter.ErrorMessage += static text => AaruLogging.Error(text);

                        converter.StoppingErrorMessage += static text => AaruLogging.Error(text);

                        converter.UpdateProgress += (text, current, maximum) =>
                        {
                            _progressTask1             ??= ctx.AddTask("Progress");
                            _progressTask1.Description =   text;
                            _progressTask1.Value       =   current;
                            _progressTask1.MaxValue    =   maximum;
                        };

                        converter.PulseProgress += text =>
                        {
                            if(_progressTask1 is null)
                                ctx.AddTask(text).IsIndeterminate();
                            else
                            {
                                _progressTask1.Description     = text;
                                _progressTask1.IsIndeterminate = true;
                            }
                        };

                        converter.InitProgress += () => _progressTask1 = ctx.AddTask("Progress");

                        converter.EndProgress += static () =>
                        {
                            _progressTask1?.StopTask();
                            _progressTask1 = null;
                        };

                        converter.InitProgress2 += () => _progressTask2 = ctx.AddTask("Progress");

                        converter.EndProgress2 += static () =>
                        {
                            _progressTask2?.StopTask();
                            _progressTask2 = null;
                        };

                        converter.UpdateProgress2 += (text, current, maximum) =>
                        {
                            _progressTask2             ??= ctx.AddTask("Progress");
                            _progressTask2.Description =   text;
                            _progressTask2.Value       =   current;
                            _progressTask2.MaxValue    =   maximum;
                        };

                        Console.CancelKeyPress += (_, e) =>
                        {
                            e.Cancel = true;
                            converter.Abort();
                        };

                        errno = converter.Start();
                    });

        return (int)errno;
    }

    private (bool success, uint cylinders, uint heads, uint sectors)? ParseGeometry(string geometryString)
    {
        // Parses CHS (Cylinder/Head/Sector) geometry string in format "C/H/S" or "C-H-S"
        // Returns tuple with success flag and parsed values, or null if not specified

        if(geometryString == null) return null;

        string[] geometryPieces = geometryString.Split('/');

        if(geometryPieces.Length == 0) geometryPieces = geometryString.Split('-');

        if(geometryPieces.Length != 3)
        {
            AaruLogging.Error(UI.Invalid_geometry_specified);

            return (false, 0, 0, 0);
        }

        if(!uint.TryParse(geometryPieces[0], out uint cylinders) || cylinders == 0)
        {
            AaruLogging.Error(UI.Invalid_number_of_cylinders_specified);

            return (false, 0, 0, 0);
        }

        if(!uint.TryParse(geometryPieces[1], out uint heads) || heads == 0)
        {
            AaruLogging.Error(UI.Invalid_number_of_heads_specified);

            return (false, 0, 0, 0);
        }

        if(uint.TryParse(geometryPieces[2], out uint sectors) && sectors != 0) return (true, cylinders, heads, sectors);

        AaruLogging.Error(UI.Invalid_sectors_per_track_specified);

        return (false, 0, 0, 0);
    }

    private (bool success, Metadata sidecar, Resume resume) LoadMetadata(
        string aaruMetadataPath, string cicmXmlPath, string resumeFilePath)
    {
        // Loads metadata and resume information from sidecar files
        // Supports both Aaru JSON and legacy CICM XML formats
        // Returns tuple with success flag, metadata, and resume information

        Metadata sidecar = null;
        Resume   resume  = null;

        if(aaruMetadataPath != null)
        {
            if(File.Exists(aaruMetadataPath))
            {
                try
                {
                    var fs = new FileStream(aaruMetadataPath, FileMode.Open);

                    sidecar =
                        (JsonSerializer.Deserialize(fs, typeof(MetadataJson), MetadataJsonContext.Default) as
                             MetadataJson)?.AaruMetadata;

                    fs.Close();
                }
                catch(Exception ex)
                {
                    AaruLogging.Error(UI.Incorrect_metadata_sidecar_file_not_continuing);
                    AaruLogging.Exception(ex, UI.Incorrect_metadata_sidecar_file_not_continuing);

                    return (false, null, null);
                }
            }
            else
            {
                AaruLogging.Error(UI.Could_not_find_metadata_sidecar);

                return (false, null, null);
            }
        }
        else if(cicmXmlPath != null)
        {
            if(File.Exists(cicmXmlPath))
            {
                try
                {
                    // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026, CS0618
                    var xs = new XmlSerializer(typeof(CICMMetadataType));
#pragma warning restore IL2026, CS0618

                    var sr = new StreamReader(cicmXmlPath);

                    // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026, CS0618
                    sidecar = (CICMMetadataType)xs.Deserialize(sr);
#pragma warning restore IL2026, CS0618

                    sr.Close();
                }
                catch(Exception ex)
                {
                    AaruLogging.Error(UI.Incorrect_metadata_sidecar_file_not_continuing);
                    AaruLogging.Exception(ex, UI.Incorrect_metadata_sidecar_file_not_continuing);

                    return (false, null, null);
                }
            }
            else
            {
                AaruLogging.Error(UI.Could_not_find_metadata_sidecar);

                return (false, null, null);
            }
        }

        if(resumeFilePath == null) return (true, sidecar, null);

        if(File.Exists(resumeFilePath))
        {
            try
            {
                if(resumeFilePath.EndsWith(".metadata.json", StringComparison.CurrentCultureIgnoreCase))
                {
                    var fs = new FileStream(resumeFilePath, FileMode.Open);

                    resume =
                        (JsonSerializer.Deserialize(fs, typeof(ResumeJson), ResumeJsonContext.Default) as ResumeJson)
                      ?.Resume;

                    fs.Close();
                }
                else
                {
                    // Bypassed by JSON source generator used above
#pragma warning disable IL2026
                    var xs = new XmlSerializer(typeof(Resume));
#pragma warning restore IL2026

                    var sr = new StreamReader(resumeFilePath);

                    // Bypassed by JSON source generator used above
#pragma warning disable IL2026
                    resume = (Resume)xs.Deserialize(sr);
#pragma warning restore IL2026

                    sr.Close();
                }
            }
            catch(Exception ex)
            {
                AaruLogging.Error(UI.Incorrect_resume_file_not_continuing);
                AaruLogging.Exception(ex, UI.Incorrect_resume_file_not_continuing);

                return (false, sidecar, null);
            }
        }
        else
        {
            AaruLogging.Error(UI.Could_not_find_resume_file);

            return (false, sidecar, null);
        }

        return (true, sidecar, resume);
    }

    private void LogCommandParameters(Settings settings,         bool fixSubchannelPosition, bool fixSubchannel,
                                      bool     fixSubchannelCrc, Dictionary<string, string> parsedOptions)
    {
        // Logs all command-line parameters for debugging and audit trail purposes
        // Consolidated from 46+ individual logging statements

        AaruLogging.Debug(MODULE_NAME, "--cicm-xml={0}", Markup.Escape(settings.CicmXml  ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--comments={0}", Markup.Escape(settings.Comments ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--count={0}",    settings.Count);
        AaruLogging.Debug(MODULE_NAME, "--creator={0}",  Markup.Escape(settings.Creator ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--debug={0}",    settings.Debug);

        AaruLogging.Debug(MODULE_NAME, "--drive-manufacturer={0}", Markup.Escape(settings.DriveManufacturer ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--drive-model={0}", Markup.Escape(settings.DriveModel ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--drive-revision={0}", Markup.Escape(settings.DriveFirmwareRevision ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--drive-serial={0}",       Markup.Escape(settings.DriveSerialNumber ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--force={0}",              settings.Force);
        AaruLogging.Debug(MODULE_NAME, "--format={0}",             Markup.Escape(settings.Format       ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--geometry={0}",           Markup.Escape(settings.Geometry     ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--input={0}",              Markup.Escape(settings.InputPath    ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-barcode={0}",      Markup.Escape(settings.MediaBarcode ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-lastsequence={0}", settings.LastMediaSequence);

        AaruLogging.Debug(MODULE_NAME, "--media-manufacturer={0}", Markup.Escape(settings.MediaManufacturer ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--media-model={0}", Markup.Escape(settings.MediaModel ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--media-partnumber={0}", Markup.Escape(settings.MediaPartNumber ?? ""));

        AaruLogging.Debug(MODULE_NAME, "--media-sequence={0}", settings.MediaSequence);
        AaruLogging.Debug(MODULE_NAME, "--media-serial={0}", Markup.Escape(settings.MediaSerialNumber ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-title={0}", Markup.Escape(settings.MediaTitle ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--options={0}", Markup.Escape(settings.Options ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--output={0}", Markup.Escape(settings.OutputPath ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--resume-file={0}", Markup.Escape(settings.ResumeFile ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}", settings.Verbose);
        AaruLogging.Debug(MODULE_NAME, "--fix-subchannel-position={0}", fixSubchannelPosition);
        AaruLogging.Debug(MODULE_NAME, "--fix-subchannel={0}", fixSubchannel);
        AaruLogging.Debug(MODULE_NAME, "--fix-subchannel-crc={0}", fixSubchannelCrc);
        AaruLogging.Debug(MODULE_NAME, "--generate-subchannels={0}", settings.GenerateSubchannels);
        AaruLogging.Debug(MODULE_NAME, "--decrypt={0}", settings.Decrypt);
        AaruLogging.Debug(MODULE_NAME, "--aaru-metadata={0}", Markup.Escape(settings.AaruMetadata ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--ignore-negative-sectors={0}", settings.IgnoreNegativeSectors);
        AaruLogging.Debug(MODULE_NAME, "--ignore-overflow-sectors={0}", settings.IgnoreOverflowSectors);

        AaruLogging.Debug(MODULE_NAME, UI.Parsed_options);

        foreach(KeyValuePair<string, string> parsedOption in parsedOptions)
            AaruLogging.Debug(MODULE_NAME, "{0} = {1}", parsedOption.Key, parsedOption.Value);
    }


    private IBaseWritableImage FindOutputFormat(PluginRegister plugins, string format, string outputPath)
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
        else if(Guid.TryParse(format, out Guid outId))
        {
            candidates.AddRange(from plugin in plugins.WritableImages.Values
                                where plugin is not null
                                where plugin.Id.Equals(outId)
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
                AaruLogging.WriteLine(UI.No_plugin_supports_requested_extension);

                return null;
            case > 1:
                AaruLogging.WriteLine(UI.More_than_one_plugin_supports_requested_extension);

                return null;
        }

        return candidates[0];
    }


    public class Settings : ImageFamily
    {
        [LocalizedDescription(nameof(UI.Take_metadata_from_existing_CICM_XML_sidecar))]
        [DefaultValue(null)]
        [CommandOption("-x|--cicm-xml")]
        public string CicmXml { get; init; }
        [LocalizedDescription(nameof(UI.Image_comments))]
        [DefaultValue(null)]
        [CommandOption("--comments")]
        public string Comments { get; init; }
        [LocalizedDescription(nameof(UI.How_many_sectors_to_convert_at_once))]
        [DefaultValue(64)]
        [CommandOption("-c|--count")]
        public int Count { get; init; }
        [LocalizedDescription(nameof(UI.Who_person_created_the_image))]
        [DefaultValue(null)]
        [CommandOption("--creator")]
        public string Creator { get; init; }
        [LocalizedDescription(nameof(UI.Manufacturer_of_drive_read_the_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--drive-manufacturer")]
        public string DriveManufacturer { get; init; }
        [LocalizedDescription(nameof(UI.Model_of_drive_used_by_media))]
        [DefaultValue(null)]
        [CommandOption("--drive-model")]
        public string DriveModel { get; init; }
        [LocalizedDescription(nameof(UI.Firmware_revision_of_drive_read_the_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--drive-revision")]
        public string DriveFirmwareRevision { get; init; }
        [LocalizedDescription(nameof(UI.Serial_number_of_drive_read_the_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--drive-serial")]
        public string DriveSerialNumber { get; init; }
        [LocalizedDescription(nameof(UI.Continue_conversion_even_if_data_lost))]
        [DefaultValue(false)]
        [CommandOption("-f|--force")]
        public bool Force { get; init; }
        [LocalizedDescription(nameof(UI.Format_of_the_output_image_as_plugin_name_or_plugin_id))]
        [DefaultValue(null)]
        [CommandOption("-p|--format")]
        public string Format { get; init; }
        [LocalizedDescription(nameof(UI.Barcode_of_the_media))]
        [DefaultValue(null)]
        [CommandOption("--media-barcode")]
        public string MediaBarcode { get; init; }
        [LocalizedDescription(nameof(UI.Last_media_of_sequence_by_image))]
        [DefaultValue(0)]
        [CommandOption("--media-lastsequence")]
        public int LastMediaSequence { get; init; }
        [LocalizedDescription(nameof(UI.Manufacturer_of_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-manufacturer")]
        public string MediaManufacturer { get; init; }
        [LocalizedDescription(nameof(UI.Model_of_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-model")]
        public string MediaModel { get; init; }
        [LocalizedDescription(nameof(UI.Part_number_of_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-partnumber")]
        public string MediaPartNumber { get; init; }
        [LocalizedDescription(nameof(UI.Number_in_sequence_for_media_by_image))]
        [DefaultValue(0)]
        [CommandOption("--media-sequence")]
        public int MediaSequence { get; init; }
        [LocalizedDescription(nameof(UI.Serial_number_of_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-serial")]
        public string MediaSerialNumber { get; init; }
        [LocalizedDescription(nameof(UI.Title_of_media_represented_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-title")]
        public string MediaTitle { get; init; }
        [LocalizedDescription(nameof(UI.Comma_separated_name_value_pairs_of_image_options))]
        [DefaultValue(null)]
        [CommandOption("-O|--options")]
        public string Options { get; init; }
        [LocalizedDescription(nameof(UI.Take_dump_hardware_from_existing_resume))]
        [DefaultValue(null)]
        [CommandOption("-r|--resume-file")]
        public string ResumeFile { get; init; }
        [LocalizedDescription(nameof(UI.Force_geometry_help))]
        [DefaultValue(null)]
        [CommandOption("-g|--geometry")]
        public string Geometry { get; init; }
        [LocalizedDescription(nameof(UI.Fix_subchannel_position_help))]
        [DefaultValue(true)]
        [CommandOption("--fix-subchannel-position")]
        public bool FixSubchannelPosition { get; init; }
        [LocalizedDescription(nameof(UI.Fix_subchannel_help))]
        [DefaultValue(false)]
        [CommandOption("--fix-subchannel")]
        public bool FixSubchannel { get; init; }
        [LocalizedDescription(nameof(UI.Fix_subchannel_crc_help))]
        [DefaultValue(false)]
        [CommandOption("--fix-subchannel-crc")]
        public bool FixSubchannelCrc { get; init; }
        [LocalizedDescription(nameof(UI.Generates_subchannels_help))]
        [DefaultValue(false)]
        [CommandOption("--generate-subchannels")]
        public bool GenerateSubchannels { get; init; }
        [LocalizedDescription(nameof(UI.Decrypt_sectors_help))]
        [DefaultValue(false)]
        [CommandOption("--decrypt")]
        public bool Decrypt { get; init; }
        [LocalizedDescription(nameof(UI.Take_metadata_from_existing_Aaru_sidecar))]
        [DefaultValue(null)]
        [CommandOption("-m|--aaru-metadata")]
        public string AaruMetadata { get; init; }
        [LocalizedDescription(nameof(UI.Input_image_path))]
        [CommandArgument(0, "<input-image>")]
        public string InputPath { get; init; }
        [LocalizedDescription(nameof(UI.Output_image_path))]
        [CommandArgument(1, "<output-image>")]
        public string OutputPath { get; init; }
        [LocalizedDescription(nameof(UI.Ignore_negative_sectors))]
        [DefaultValue(false)]
        [CommandOption("--ignore-negative-sectors")]
        public bool IgnoreNegativeSectors { get; init; }
        [LocalizedDescription(nameof(UI.Ignore_overflow_sectors))]
        [DefaultValue(false)]
        [CommandOption("--ignore-overflow-sectors")]
        public bool IgnoreOverflowSectors { get; init; }
    }
}