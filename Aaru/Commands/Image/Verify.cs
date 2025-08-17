// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Verify.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'verify' command.
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

using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using Aaru.CommonTypes;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Structs;
using Aaru.Core;
using Aaru.Core.Graphics;
using Aaru.Localization;
using Aaru.Logging;
using Humanizer;
using Humanizer.Localisation;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Aaru.Commands.Image;

sealed class VerifyCommand : Command<VerifyCommand.Settings>
{
    const string MODULE_NAME = "Verify command";

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        Statistics.AddCommand("verify");

        AaruLogging.Debug(MODULE_NAME, "--debug={0}",          settings.Debug);
        AaruLogging.Debug(MODULE_NAME, "--input={0}",          Markup.Escape(settings.ImagePath ?? ""));
        AaruLogging.Debug(MODULE_NAME, "--verbose={0}",        settings.Verbose);
        AaruLogging.Debug(MODULE_NAME, "--verify-disc={0}",    settings.VerifyDisc);
        AaruLogging.Debug(MODULE_NAME, "--verify-sectors={0}", settings.VerifySectors);
        AaruLogging.Debug(MODULE_NAME, "--create-graph={0}",   settings.CreateGraph);
        AaruLogging.Debug(MODULE_NAME, "--dimensions={0}",     settings.Dimensions);

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

        Statistics.AddMediaFormat(inputFormat.Format);
        Statistics.AddMedia(inputFormat.Info.MediaType, false);
        Statistics.AddFilter(inputFilter.Name);

        bool? correctImage   = null;
        bool? correctSectors = null;

        var verifiableImage        = inputFormat as IVerifiableImage;
        var verifiableSectorsImage = inputFormat as IVerifiableSectorsImage;

        if(verifiableImage is null && verifiableSectorsImage is null)
        {
            AaruLogging.Error(UI.The_specified_image_does_not_support_any_kind_of_verification);

            return (int)ErrorNumber.NotVerifiable;
        }

        var chkWatch = new Stopwatch();

        if(settings.VerifyDisc && verifiableImage != null)
        {
            bool? discCheckStatus = null;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Verifying_image_checksums).IsIndeterminate();

                chkWatch.Start();
                discCheckStatus = verifiableImage.VerifyMediaImage();
                chkWatch.Stop();
            });

            switch(discCheckStatus)
            {
                case true:
                    AaruLogging.WriteLine(UI.Disc_image_checksums_are_correct);

                    break;
                case false:
                    AaruLogging.WriteLine(UI.Disc_image_checksums_are_incorrect);

                    break;
                case null:
                    AaruLogging.WriteLine(UI.Disc_image_does_not_contain_checksums);

                    break;
            }

            correctImage = discCheckStatus;

            AaruLogging.Verbose(UI.Checking_disc_image_checksums_took_0,
                                         chkWatch.Elapsed.Humanize(minUnit: TimeUnit.Second));
        }

        if(!settings.VerifySectors)
        {
            return correctImage switch
                   {
                       null  => (int)ErrorNumber.NotVerifiable,
                       false => (int)ErrorNumber.BadImageSectorsNotVerified,
                       true  => (int)ErrorNumber.CorrectImageSectorsNotVerified
                   };
        }

        var         stopwatch   = new Stopwatch();
        List<ulong> failingLbas = [];
        List<ulong> unknownLbas = [];
        IMediaGraph mediaGraph  = null;

        if(verifiableSectorsImage is IOpticalMediaImage { Tracks: not null } opticalMediaImage)
        {
            Spiral.DiscParameters spiralParameters = null;

            if(settings.CreateGraph)
                spiralParameters = Spiral.DiscParametersFromMediaType(opticalMediaImage.Info.MediaType);

            if(spiralParameters is not null)
            {
                mediaGraph = new Spiral((int)settings.Dimensions,
                                        (int)settings.Dimensions,
                                        spiralParameters,
                                        opticalMediaImage.Info.Sectors);
            }
            else if(settings.CreateGraph)
            {
                mediaGraph = new BlockMap((int)settings.Dimensions,
                                          (int)settings.Dimensions,
                                          opticalMediaImage.Info.Sectors);
            }

            List<Track> inputTracks      = opticalMediaImage.Tracks;
            ulong       currentSectorAll = 0;

            stopwatch.Start();

            AnsiConsole.Progress()
                       .AutoClear(true)
                       .HideCompleted(true)
                       .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                       .Start(ctx =>
                        {
                            ProgressTask discTask = ctx.AddTask(UI.Checking_tracks);
                            discTask.MaxValue = inputTracks.Count;

                            foreach(Track currentTrack in inputTracks)
                            {
                                discTask.Description =
                                    string.Format(UI.Checking_track_0_of_1, discTask.Value + 1, inputTracks.Count);

                                ulong remainingSectors = currentTrack.EndSector - currentTrack.StartSector + 1;

                                ulong currentSector = 0;

                                ProgressTask trackTask = ctx.AddTask(UI.Checking_sector);
                                trackTask.MaxValue = remainingSectors;

                                while(remainingSectors > 0)
                                {
                                    trackTask.Description = string.Format(UI.Checking_sector_0_of_1_on_track_2,
                                                                          currentSectorAll,
                                                                          inputFormat.Info.Sectors,
                                                                          currentTrack.Sequence);

                                    List<ulong> tempFailingLbas;
                                    List<ulong> tempUnknownLbas;

                                    if(remainingSectors < 512)
                                    {
                                        opticalMediaImage.VerifySectors(currentSector,
                                                                        (uint)remainingSectors,
                                                                        currentTrack.Sequence,
                                                                        out tempFailingLbas,
                                                                        out tempUnknownLbas);
                                    }
                                    else
                                    {
                                        opticalMediaImage.VerifySectors(currentSector,
                                                                        512,
                                                                        currentTrack.Sequence,
                                                                        out tempFailingLbas,
                                                                        out tempUnknownLbas);
                                    }

                                    if(mediaGraph != null)
                                    {
                                        List<ulong> tempCorrectLbas = [];

                                        for(ulong l = 0; l < (remainingSectors < 512 ? remainingSectors : 512); l++)
                                            tempCorrectLbas.Add(currentSector + l);

                                        foreach(ulong f in tempFailingLbas) tempCorrectLbas.Remove(f);

                                        foreach(ulong u in tempUnknownLbas)
                                        {
                                            tempCorrectLbas.Remove(u);
                                            mediaGraph.PaintSectorUnknown(currentTrack.StartSector + u);
                                        }

                                        foreach(ulong lba in tempCorrectLbas)
                                            mediaGraph.PaintSectorGood(currentTrack.StartSector + lba);

                                        foreach(ulong f in tempFailingLbas)
                                            mediaGraph.PaintSectorBad(currentTrack.StartSector + f);
                                    }

                                    failingLbas.AddRange(tempFailingLbas);

                                    unknownLbas.AddRange(tempUnknownLbas);

                                    if(remainingSectors < 512)
                                    {
                                        currentSector    += remainingSectors;
                                        currentSectorAll += remainingSectors;
                                        trackTask.Value  += remainingSectors;
                                        remainingSectors =  0;
                                    }
                                    else
                                    {
                                        currentSector    += 512;
                                        currentSectorAll += 512;
                                        trackTask.Value  += 512;
                                        remainingSectors -= 512;
                                    }
                                }

                                trackTask.StopTask();
                                discTask.Increment(1);
                            }

                            stopwatch.Stop();
                        });
        }
        else if(verifiableSectorsImage != null)
        {
            ulong remainingSectors = inputFormat.Info.Sectors;
            ulong currentSector    = 0;

            AnsiConsole.Progress()
                       .AutoClear(true)
                       .HideCompleted(true)
                       .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                       .Start(ctx =>
                        {
                            ProgressTask diskTask = ctx.AddTask(UI.Checking_sectors);
                            diskTask.MaxValue = inputFormat.Info.Sectors;

                            stopwatch.Restart();

                            while(remainingSectors > 0)
                            {
                                diskTask.Description =
                                    string.Format(UI.Checking_sector_0_of_1, currentSector, inputFormat.Info.Sectors);

                                List<ulong> tempFailingLbas;
                                List<ulong> tempUnknownLbas;

                                if(remainingSectors < 512)
                                {
                                    verifiableSectorsImage.VerifySectors(currentSector,
                                                                         (uint)remainingSectors,
                                                                         out tempFailingLbas,
                                                                         out tempUnknownLbas);
                                }
                                else
                                {
                                    verifiableSectorsImage.VerifySectors(currentSector,
                                                                         512,
                                                                         out tempFailingLbas,
                                                                         out tempUnknownLbas);
                                }

                                failingLbas.AddRange(tempFailingLbas);

                                unknownLbas.AddRange(tempUnknownLbas);

                                if(mediaGraph != null)
                                {
                                    List<ulong> tempCorrectLbas = [];

                                    for(ulong l = 0; l < (remainingSectors < 512 ? remainingSectors : 512); l++)
                                        tempCorrectLbas.Add(currentSector + l);

                                    foreach(ulong f in tempFailingLbas) tempCorrectLbas.Remove(f);

                                    foreach(ulong u in tempUnknownLbas) tempCorrectLbas.Remove(u);

                                    mediaGraph.PaintSectorsUnknown(tempUnknownLbas);
                                    mediaGraph.PaintSectorsGood(tempCorrectLbas);
                                    mediaGraph.PaintSectorsBad(tempFailingLbas);
                                }

                                if(remainingSectors < 512)
                                {
                                    currentSector    += remainingSectors;
                                    diskTask.Value   += remainingSectors;
                                    remainingSectors =  0;
                                }
                                else
                                {
                                    currentSector    += 512;
                                    diskTask.Value   += 512;
                                    remainingSectors -= 512;
                                }
                            }

                            stopwatch.Stop();
                        });
        }

        if(unknownLbas.Count > 0)
            AaruLogging.WriteLine(UI.There_is_at_least_one_sector_that_does_not_contain_a_checksum);

        if(failingLbas.Count > 0)
            AaruLogging.WriteLine(UI.There_is_at_least_one_sector_with_incorrect_checksum_or_errors);

        if(unknownLbas.Count == 0 && failingLbas.Count == 0) AaruLogging.WriteLine(UI.All_sector_checksums_are_correct);

        AaruLogging.Verbose(UI.Checking_sector_checksums_took_0,
                                     stopwatch.Elapsed.Humanize(minUnit: TimeUnit.Second));

        if(settings.Verbose)
        {
            AaruLogging.Verbose($"[red]{UI.LBAs_with_error}[/]");

            if(failingLbas.Count == (int)inputFormat.Info.Sectors)
                AaruLogging.Verbose($"\t[red]{UI.all_sectors}[/]");
            else
            {
                foreach(ulong t in failingLbas) AaruLogging.Verbose("\t{0}", t);
            }

            AaruLogging.WriteLine($"[yellow3_1]{UI.LBAs_without_checksum}[/]");

            if(unknownLbas.Count == (int)inputFormat.Info.Sectors)
                AaruLogging.Verbose($"\t[yellow3_1]{UI.all_sectors}[/]");
            else
            {
                foreach(ulong t in unknownLbas) AaruLogging.Verbose("\t{0}", t);
            }
        }

        // TODO: Convert to table
        AaruLogging.WriteLine($"[italic]{UI.Total_sectors}[/] {inputFormat.Info.Sectors}");
        AaruLogging.WriteLine($"[italic]{UI.Total_errors}[/] {failingLbas.Count}");
        AaruLogging.WriteLine($"[italic]{UI.Total_unknowns}[/] {unknownLbas.Count}");
        AaruLogging.WriteLine($"[italic]{UI.Total_errors_plus_unknowns}[/] {failingLbas.Count + unknownLbas.Count}");

        mediaGraph?.WriteTo($"{Path.GetFileNameWithoutExtension(inputFilter.Filename)}.verify.png");

        if(failingLbas.Count > 0)
            correctSectors                                                          = false;
        else if((ulong)unknownLbas.Count < inputFormat.Info.Sectors) correctSectors = true;

        return correctImage switch
               {
                   null when correctSectors is null   => (int)ErrorNumber.NotVerifiable,
                   null when correctSectors == false  => (int)ErrorNumber.BadSectorsImageNotVerified,
                   null                               => (int)ErrorNumber.CorrectSectorsImageNotVerified,
                   false when correctSectors is null  => (int)ErrorNumber.BadImageSectorsNotVerified,
                   false when correctSectors == false => (int)ErrorNumber.BadImageBadSectors,
                   false                              => (int)ErrorNumber.CorrectSectorsBadImage,
                   true when correctSectors is null   => (int)ErrorNumber.CorrectImageSectorsNotVerified,
                   true when correctSectors == false  => (int)ErrorNumber.CorrectImageBadSectors,
                   true                               => (int)ErrorNumber.NoError
               };
    }

#region Nested type: Settings

    public class Settings : ImageFamily
    {
        [Description("Verify media image if supported.")]
        [DefaultValue(true)]
        [CommandOption("-w|--verify-disc")]
        public bool VerifyDisc { get; init; }
        [Description("Verify all sectors if supported.")]
        [DefaultValue(true)]
        [CommandOption("-s|--verify-sectors")]
        public bool VerifySectors { get; init; }
        [Description("Create graph of verified disc (currently only implemented for optical discs).")]
        [DefaultValue(true)]
        [CommandOption("-g|--create-graph")]
        public bool CreateGraph { get; init; }
        [Description("Dimensions, as a square, in pixels, for the graph of verified media.")]
        [DefaultValue(1080)]
        [CommandOption("-d|--dimensions")]
        public uint Dimensions { get; init; }
        [Description("Disc image path")]
        [CommandArgument(0, "<image-path>")]
        public string ImagePath { get; init; }
    }

#endregion
}