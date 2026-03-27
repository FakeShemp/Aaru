// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : WriteMetadata.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'write-metadata' command.
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

using System;
using System.ComponentModel;
using System.Threading;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.Core;
using Aaru.Images;
using Aaru.Localization;
using Aaru.Logging;
using Humanizer;
using Spectre.Console;
using Spectre.Console.Cli;
using ImageInfo = Aaru.CommonTypes.Structs.ImageInfo;

namespace Aaru.Commands.Image;

sealed class WriteMetadataCommand : Command<WriteMetadataCommand.Settings>
{
    const string MODULE_NAME = "Write-metadata command";

    /// <inheritdoc />
    public override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("write-metadata");

        AaruLogging.Debug(MODULE_NAME, "--comments={0}",           Markup.Escape(settings.Comments ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--creator={0}",            Markup.Escape(settings.Creator  ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--debug={0}",              settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--drive-manufacturer={0}", Markup.Escape(settings.DriveManufacturer     ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--drive-model={0}",        Markup.Escape(settings.DriveModel            ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--drive-revision={0}",     Markup.Escape(settings.DriveFirmwareRevision ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--drive-serial={0}",       Markup.Escape(settings.DriveSerialNumber     ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-barcode={0}",      Markup.Escape(settings.MediaBarcode          ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-lastsequence={0}", settings.LastMediaSequence);
        AaruLogging.Debug(MODULE_NAME, "--media-manufacturer={0}", Markup.Escape(settings.MediaManufacturer ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-model={0}",        Markup.Escape(settings.MediaModel        ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-partnumber={0}",   Markup.Escape(settings.MediaPartNumber   ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-sequence={0}",     settings.MediaSequence);
        AaruLogging.Debug(MODULE_NAME, "--media-serial={0}",       Markup.Escape(settings.MediaSerialNumber ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--media-title={0}",        Markup.Escape(settings.MediaTitle        ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}",            settings.Verbose);

        IFilter inputFilter = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_file_filter).IsIndeterminate();
            inputFilter = PluginRegister.Singleton.GetFilter(settings.ImagePath);
        });

        if(inputFilter == null)
        {
            AaruLogging.Error(UI.Cannot_open_specified_file);

            return (int)ErrorNumber.CannotOpenFile;
        }

        IBaseImage inputFormat = null;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Identifying_image_format).IsIndeterminate();
            inputFormat = ImageFormat.Detect(inputFilter);
        });

        if(inputFormat == null)
        {
            AaruLogging.Error(UI.Unable_to_recognize_image_format_not_verifying);

            return (int)ErrorNumber.FormatNotFound;
        }

        if(inputFormat is not AaruFormat aif)
        {
            AaruLogging.Error(UI.File_is_not_an_AaruFormat_image);

            return (int)ErrorNumber.InvalidArgument;
        }

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

        if(aif.Info.Version.StartsWith("1.", StringComparison.OrdinalIgnoreCase))
        {
            AaruLogging.Error(UI.AaruFormat_images_version_1_x_are_read_only);

            return (int)ErrorNumber.InvalidArgument;
        }

        ulong     sectors         = aif.Info.Sectors;
        MediaType mediaType       = aif.Info.MediaType;
        uint      negativeSectors = aif.Info.NegativeSectors;
        uint      overflowSectors = aif.Info.OverflowSectors;
        uint      sectorSize      = aif.Info.SectorSize;
        ImageInfo info            = aif.Info;

        aif.Close();

        var ret = false;

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Invoke_Opening_image_file).IsIndeterminate();
            ret = aif.Create(settings.ImagePath, mediaType, [], sectors, negativeSectors, overflowSectors, sectorSize);
        });

        if(!ret)
        {
            AaruLogging.Error(UI.Error_reopening_image_for_writing);
            AaruLogging.Error(aif.ErrorMessage);

            return (int)ErrorNumber.WriteError;
        }

        if(settings.LastMediaSequence is not null || settings.MediaSequence is not null)
        {
            info.LastMediaSequence = settings.LastMediaSequence ?? 0;
            info.MediaSequence     = settings.MediaSequence     ?? 0;
        }

        if(settings.Comments is not null) info.Comments = settings.Comments?.Length == 0 ? null : settings.Comments;
        if(settings.Creator is not null) info.Creator   = settings.Creator?.Length  == 0 ? null : settings.Creator;

        if(settings.DriveManufacturer is not null)
            info.DriveManufacturer = settings.DriveManufacturer?.Length == 0 ? null : settings.DriveManufacturer;

        if(settings.DriveModel is not null)
            info.DriveModel = settings.DriveModel?.Length == 0 ? null : settings.DriveModel;

        if(settings.DriveFirmwareRevision is not null)
        {
            info.DriveFirmwareRevision =
                settings.DriveFirmwareRevision?.Length == 0 ? null : settings.DriveFirmwareRevision;
        }

        if(settings.DriveSerialNumber is not null)
            info.DriveSerialNumber = settings.DriveSerialNumber?.Length == 0 ? null : settings.DriveSerialNumber;

        if(settings.MediaBarcode is not null)
            info.MediaBarcode = settings.MediaBarcode?.Length == 0 ? null : settings.MediaBarcode;

        if(settings.MediaManufacturer is not null)
            info.MediaManufacturer = settings.MediaManufacturer?.Length == 0 ? null : settings.MediaManufacturer;

        if(settings.MediaModel is not null)
            info.MediaModel = settings.MediaModel?.Length == 0 ? null : settings.MediaModel;

        if(settings.MediaPartNumber is not null)
            info.MediaPartNumber = settings.MediaPartNumber?.Length == 0 ? null : settings.MediaPartNumber;

        if(settings.MediaSerialNumber is not null)
            info.MediaSerialNumber = settings.MediaSerialNumber?.Length == 0 ? null : settings.MediaSerialNumber;

        if(settings.MediaTitle is not null)
            info.MediaTitle = settings.MediaTitle?.Length == 0 ? null : settings.MediaTitle;

        // Now we set the metadata
        aif.SetImageInfo(info);

        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Closing_output_image).IsIndeterminate();
            aif.Close();
        });

        // And we re-open it in read-only mode
        Core.Spectre.ProgressSingleSpinner(ctx =>
        {
            ctx.AddTask(UI.Invoke_Opening_image_file).IsIndeterminate();
            opened = aif.Open(inputFilter);
        });

        if(opened != ErrorNumber.NoError)
        {
            AaruLogging.Error(UI.Error_reopening_image_in_read_only_mode_after_writing_metadata);
            AaruLogging.Error(Localization.Core.Error_0, opened);

            return (int)opened;
        }

        AaruLogging.WriteLine(Localization.Core.Image_information_WithMarkup);

        if(!string.IsNullOrWhiteSpace(aif.Info.Version))
        {
            AaruLogging.WriteLine(Localization.Core.Format_0_version_1_WithMarkup,
                                  Markup.Escape(aif.Format),
                                  Markup.Escape(aif.Info.Version));
        }
        else
            AaruLogging.WriteLine(Localization.Core.Format_0_WithMarkup, Markup.Escape(aif.Format));

        switch(string.IsNullOrWhiteSpace(aif.Info.Application))
        {
            case false when !string.IsNullOrWhiteSpace(aif.Info.ApplicationVersion):
                AaruLogging.WriteLine(Localization.Core.Was_created_with_0_version_1_WithMarkup,
                                      Markup.Escape(aif.Info.Application),
                                      Markup.Escape(aif.Info.ApplicationVersion));

                break;
            case false:
                AaruLogging.WriteLine(Localization.Core.Was_created_with_0_WithMarkup,
                                      Markup.Escape(aif.Info.Application));

                break;
        }

        AaruLogging.WriteLine(Localization.Core.Image_without_headers_is_0_bytes_long, aif.Info.ImageSize);

        AaruLogging.WriteLine(Localization.Core.Contains_a_media_of_0_sectors_with_a_maximum_sector_size_of_1_bytes_etc,
                              aif.Info.Sectors,
                              aif.Info.SectorSize,
                              ByteSize.FromBytes(aif.Info.Sectors * aif.Info.SectorSize).Humanize());

        if(aif.Info.NegativeSectors > 0 || aif.Info.OverflowSectors > 0)
        {
            AaruLogging.WriteLine(Localization.Core.Image_has_0_leadin_and_1_leadout_sectors_WithMarkup,
                                  aif.Info.NegativeSectors,
                                  aif.Info.OverflowSectors);
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.Creator))
            AaruLogging.WriteLine(Localization.Core.Created_by_0_WithMarkup, Markup.Escape(aif.Info.Creator));

        if(aif.Info.CreationTime != DateTime.MinValue)
            AaruLogging.WriteLine(Localization.Core.Created_on_0, aif.Info.CreationTime);

        if(aif.Info.LastModificationTime != DateTime.MinValue)
            AaruLogging.WriteLine(Localization.Core.Last_modified_on_0, aif.Info.LastModificationTime);

        AaruLogging.WriteLine(Localization.Core.Contains_a_media_of_type_0_and_XML_type_1_WithMarkup,
                              aif.Info.MediaType.Humanize(),
                              aif.Info.MetadataMediaType);

        AaruLogging.WriteLine(aif.Info.HasPartitions
                                  ? Localization.Core.Has_partitions
                                  : Localization.Core.Doesnt_have_partitions);

        AaruLogging.WriteLine(aif.Info.HasSessions
                                  ? Localization.Core.Has_sessions
                                  : Localization.Core.Doesnt_have_sessions);

        if(!string.IsNullOrWhiteSpace(aif.Info.Comments))
            AaruLogging.WriteLine(Localization.Core.Comments_0_WithMarkup, Markup.Escape(aif.Info.Comments));

        if(aif.Info.MediaSequence != 0 && aif.Info.LastMediaSequence != 0)
        {
            AaruLogging.WriteLine(Localization.Core.Media_is_number_0_on_a_set_of_1_medias,
                                  aif.Info.MediaSequence,
                                  aif.Info.LastMediaSequence);
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaTitle))
            AaruLogging.WriteLine(Localization.Core.Media_title_0_WithMarkup, Markup.Escape(aif.Info.MediaTitle));

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaManufacturer))
        {
            AaruLogging.WriteLine(Localization.Core.Media_manufacturer_0_WithMarkup,
                                  Markup.Escape(aif.Info.MediaManufacturer));
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaModel))
            AaruLogging.WriteLine(Localization.Core.Media_model_0_WithMarkup, Markup.Escape(aif.Info.MediaModel));

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaSerialNumber))
        {
            AaruLogging.WriteLine(Localization.Core.Media_serial_number_0_WithMarkup,
                                  Markup.Escape(aif.Info.MediaSerialNumber));
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaBarcode))
            AaruLogging.WriteLine(Localization.Core.Media_barcode_0_WithMarkup, Markup.Escape(aif.Info.MediaBarcode));

        if(!string.IsNullOrWhiteSpace(aif.Info.MediaPartNumber))
        {
            AaruLogging.WriteLine(Localization.Core.Media_part_number_0_WithMarkup,
                                  Markup.Escape(aif.Info.MediaPartNumber));
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.DriveManufacturer))
        {
            AaruLogging.WriteLine(Localization.Core.Drive_manufacturer_0_WithMarkup,
                                  Markup.Escape(aif.Info.DriveManufacturer));
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.DriveModel))
            AaruLogging.WriteLine(Localization.Core.Drive_model_0_WithMarkup, Markup.Escape(aif.Info.DriveModel));

        if(!string.IsNullOrWhiteSpace(aif.Info.DriveSerialNumber))
        {
            AaruLogging.WriteLine(Localization.Core.Drive_serial_number_0_WithMarkup,
                                  Markup.Escape(aif.Info.DriveSerialNumber));
        }

        if(!string.IsNullOrWhiteSpace(aif.Info.DriveFirmwareRevision))
        {
            AaruLogging.WriteLine(Localization.Core.Drive_firmware_info_0_WithMarkup,
                                  Markup.Escape(aif.Info.DriveFirmwareRevision));
        }

        return (int)ErrorNumber.NoError;
    }

    public class Settings : ImageFamily
    {
        [LocalizedDescription(nameof(UI.Image_comments))]
        [DefaultValue(null)]
        [CommandOption("--comments")]
        public string Comments { get; init; }
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
        [LocalizedDescription(nameof(UI.Barcode_of_the_media))]
        [DefaultValue(null)]
        [CommandOption("--media-barcode")]
        public string MediaBarcode { get; init; }
        [LocalizedDescription(nameof(UI.Last_media_of_sequence_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-lastsequence")]
        public int? LastMediaSequence { get; init; }
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
        [DefaultValue(null)]
        [CommandOption("--media-sequence")]
        public int? MediaSequence { get; init; }
        [LocalizedDescription(nameof(UI.Serial_number_of_media_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-serial")]
        public string MediaSerialNumber { get; init; }
        [LocalizedDescription(nameof(UI.Title_of_media_represented_by_image))]
        [DefaultValue(null)]
        [CommandOption("--media-title")]
        public string MediaTitle { get; init; }
        [LocalizedDescription(nameof(UI.Media_image_path))]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }
}