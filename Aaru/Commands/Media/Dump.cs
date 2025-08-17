// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : Dump.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Commands.
//
// --[ Description ] ----------------------------------------------------------
//
//     Implements the 'dump' command.
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
// Copyright © 2020-2025 Rebecca Wallander
// ****************************************************************************/

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Xml.Serialization;
using Aaru.CommonTypes;
using Aaru.CommonTypes.AaruMetadata;
using Aaru.CommonTypes.Enums;
using Aaru.CommonTypes.Interfaces;
using Aaru.CommonTypes.Interop;
using Aaru.CommonTypes.Metadata;
using Aaru.CommonTypes.Structs.Devices.SCSI;
using Aaru.Core;
using Aaru.Core.Devices.Dumping;
using Aaru.Core.Logging;
using Aaru.Localization;
using Aaru.Logging;
using Schemas;
using Spectre.Console;
using Spectre.Console.Cli;
using Dump = Aaru.Core.Devices.Dumping.Dump;
using File = System.IO.File;

namespace Aaru.Commands.Media;

// TODO: Add raw dumping
sealed class DumpMediaCommand : Command<DumpMediaCommand.Settings>
{
    const  string       MODULE_NAME = "Dump-Media command";
    static ProgressTask _progressTask1;
    static ProgressTask _progressTask2;

    public override int Execute(CommandContext context, Settings settings)
    {
        MainClass.PrintCopyright();

        bool fixSubchannel         = settings.FixSubchannel;
        bool fixSubchannelCrc      = settings.FixSubchannelCrc;
        bool fixSubchannelPosition = settings.FixSubchannelPosition;
        uint maxBlocks             = settings.MaxBlocks;
        bool eject                 = settings.Eject;

        fixSubchannel         |= fixSubchannelCrc;
        fixSubchannelPosition |= settings.RetrySubchannel || fixSubchannel;

        if(maxBlocks == 0) maxBlocks = 64;

        Statistics.AddCommand("dump-media");

        AaruConsole.DebugWriteLine(MODULE_NAME, "--cicm-xml={0}", Markup.Escape(settings.CicmXml ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--debug={0}", settings.Debug);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--device={0}", Markup.Escape(settings.DevicePath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--encoding={0}", Markup.Escape(settings.Encoding ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--first-pregap={0}", settings.FirstPregap);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--fix-offset={0}", settings.FixOffset);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--force={0}", settings.Force);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--format={0}", Markup.Escape(settings.Format ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--metadata={0}", settings.Metadata);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--options={0}", Markup.Escape(settings.Options ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--output={0}", Markup.Escape(settings.OutputPath ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "--persistent={0}", settings.Persistent);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--resume={0}", settings.Resume);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--retry-passes={0}", settings.RetryPasses);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--skip={0}", settings.Skip);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--stop-on-error={0}", settings.StopOnError);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--trim={0}", settings.Trim);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--verbose={0}", settings.Verbose);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--subchannel={0}", Markup.Escape(settings.Subchannel ?? ""));
        AaruConsole.DebugWriteLine(MODULE_NAME, "----private={0}", settings.Private);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--fix-subchannel-position={0}", settings.FixSubchannelPosition);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--retry-subchannel={0}", settings.RetrySubchannel);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--fix-subchannel={0}", fixSubchannel);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--fix-subchannel-crc={0}", fixSubchannelCrc);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--generate-subchannels={0}", settings.GenerateSubchannels);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--skip-cdiready-hole={0}", settings.SkipCdiReadyHole);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--eject={0}", eject);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--max-blocks={0}", maxBlocks);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--use-buffered-reads={0}", settings.UseBufferedReads);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--store-encrypted={0}", settings.StoreEncrypted);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--title-keys={0}", settings.TitleKeys);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--ignore-cdr-runouts={0}", settings.IgnoreCdrRunOuts);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--create-graph={0}", settings.CreateGraph);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--dimensions={0}", settings.Dimensions);
        AaruConsole.DebugWriteLine(MODULE_NAME, "--aaru-metadata={0}", Markup.Escape(settings.AaruMetadata ?? ""));

        // TODO: Disabled temporarily
        //AaruConsole.DebugWriteLine(MODULE_NAME, "--raw={0}", Markup.Escape(raw ?? ""));

        Dictionary<string, string> parsedOptions = Options.Parse(settings.Options);
        AaruConsole.DebugWriteLine(MODULE_NAME, UI.Parsed_options);

        foreach(KeyValuePair<string, string> parsedOption in parsedOptions)
            AaruConsole.DebugWriteLine(MODULE_NAME, "{0} = {1}", parsedOption.Key, parsedOption.Value);

        Encoding encodingClass = null;

        if(settings.Encoding != null)
        {
            try
            {
                encodingClass = Claunia.Encoding.Encoding.GetEncoding(settings.Encoding);

                if(settings.Verbose) AaruConsole.VerboseWriteLine(UI.encoding_for_0, encodingClass.EncodingName);
            }
            catch(ArgumentException)
            {
                AaruConsole.ErrorWriteLine(UI.Specified_encoding_is_not_supported);

                return (int)ErrorNumber.EncodingUnknown;
            }
        }

        DumpSubchannel wantedSubchannel = DumpSubchannel.Any;

        if(settings.Subchannel?.ToLower(CultureInfo.CurrentUICulture) == UI.Subchannel_name_any ||
           settings.Subchannel is null)
            wantedSubchannel = DumpSubchannel.Any;
        else if(settings.Subchannel?.ToLowerInvariant() == UI.Subchannel_name_rw)
            wantedSubchannel = DumpSubchannel.Rw;
        else if(settings.Subchannel?.ToLowerInvariant() == UI.Subchannel_name_rw_or_pq)
            wantedSubchannel = DumpSubchannel.RwOrPq;
        else if(settings.Subchannel?.ToLowerInvariant() == UI.Subchannel_name_pq)
            wantedSubchannel = DumpSubchannel.Pq;
        else if(settings.Subchannel?.ToLowerInvariant() == UI.Subchannel_name_none)
            wantedSubchannel = DumpSubchannel.None;
        else
            AaruConsole.WriteLine(UI.Incorrect_subchannel_type_0_requested, settings.Subchannel);

        string filename = Path.GetFileNameWithoutExtension(settings.OutputPath);

        bool isResponse = filename.StartsWith("#", StringComparison.OrdinalIgnoreCase) &&
                          File.Exists(Path.Combine(Path.GetDirectoryName(settings.OutputPath),
                                                   Path.GetFileNameWithoutExtension(settings.OutputPath)));

        TextReader resReader;

        if(isResponse)
        {
            resReader = new StreamReader(Path.Combine(Path.GetDirectoryName(settings.OutputPath),
                                                      Path.GetFileNameWithoutExtension(settings.OutputPath)));
        }
        else
            resReader = new StringReader(Path.GetFileNameWithoutExtension(settings.OutputPath));

        if(isResponse) eject = true;

        PluginRegister           plugins    = PluginRegister.Singleton;
        List<IBaseWritableImage> candidates = [];
        string                   extension  = Path.GetExtension(settings.OutputPath);

        // Try extension
        if(string.IsNullOrEmpty(settings.Format))
        {
            candidates.AddRange(from plugin in plugins.WritableImages.Values
                                where plugin is not null
                                where plugin.KnownExtensions.Contains(extension)
                                select plugin);
        }

        // Try Id
        else if(Guid.TryParse(settings.Format, out Guid outId))
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
                                where plugin.Name.Equals(settings.Format, StringComparison.InvariantCultureIgnoreCase)
                                select plugin);
        }

        switch(candidates.Count)
        {
            case 0:
                AaruConsole.WriteLine(UI.No_plugin_supports_requested_extension);

                return (int)ErrorNumber.FormatNotFound;
            case > 1:
                AaruConsole.WriteLine(UI.More_than_one_plugin_supports_requested_extension);

                return (int)ErrorNumber.TooManyFormats;
        }

        while(true)
        {
            string responseLine = resReader.ReadLine();

            if(responseLine is null) break;

            if(responseLine.Any(c => c < 0x20))
            {
                AaruConsole.ErrorWriteLine(UI.Invalid_characters_found_in_list_of_files);

                return (int)ErrorNumber.InvalidArgument;
            }

            if(isResponse)
            {
                AaruConsole.WriteLine(UI.Please_insert_media_with_title_0_and_press_any_key_to_continue_, responseLine);

                Console.ReadKey();
                Thread.Sleep(1000);
            }

            responseLine = responseLine.Replace('/', '／');

            // Replace Windows forbidden filename characters with Japanese equivalents that are visually the same, but bigger.
            if(DetectOS.IsWindows)
            {
                responseLine = responseLine.Replace('<', '\uFF1C')
                                           .Replace('>',  '\uFF1E')
                                           .Replace(':',  '\uFF1A')
                                           .Replace('"',  '\u2033')
                                           .Replace('\\', '＼')
                                           .Replace('|',  '｜')
                                           .Replace('?',  '？')
                                           .Replace('*',  '＊');
            }

            string devicePath = settings.DevicePath;

            if(devicePath.Length == 2 && devicePath[1] == ':' && devicePath[0] != '/' && char.IsLetter(devicePath[0]))
                devicePath = "\\\\.\\" + char.ToUpper(devicePath[0]) + ':';

            Devices.Device dev      = null;
            ErrorNumber    devErrno = ErrorNumber.NoError;

            Core.Spectre.ProgressSingleSpinner(ctx =>
            {
                ctx.AddTask(UI.Opening_device).IsIndeterminate();
                dev = Devices.Device.Create(devicePath, out devErrno);
            });

            switch(dev)
            {
                case null:
                {
                    AaruConsole.ErrorWriteLine(string.Format(UI.Could_not_open_device_error_0, devErrno));

                    if(isResponse) continue;

                    return (int)devErrno;
                }
                case Devices.Remote.Device remoteDev:
                    Statistics.AddRemote(remoteDev.RemoteApplication,
                                         remoteDev.RemoteVersion,
                                         remoteDev.RemoteOperatingSystem,
                                         remoteDev.RemoteOperatingSystemVersion,
                                         remoteDev.RemoteArchitecture);

                    break;
            }

            if(dev.Error)
            {
                AaruConsole.ErrorWriteLine(Error.Print(dev.LastError));

                if(isResponse) continue;

                return (int)ErrorNumber.CannotOpenDevice;
            }

            Statistics.AddDevice(dev);

            string outputPrefix = Path.Combine(Path.GetDirectoryName(settings.OutputPath), responseLine);

            Resume resumeClass = null;

            if(settings.Resume)
            {
                try
                {
                    if(File.Exists(outputPrefix + ".resume.json"))
                    {
                        var fs = new FileStream(outputPrefix + ".resume.json", FileMode.Open);

                        resumeClass =
                            (JsonSerializer.Deserialize(fs,
                                                        typeof(ResumeJson),
                                                        ResumeJsonContext.Default) as ResumeJson)?.Resume;

                        fs.Close();
                    }

                    // DEPRECATED: To be removed in Aaru 7
                    else if(File.Exists(outputPrefix + ".resume.xml") && settings.Resume)
                    {
                        // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026
                        var xs = new XmlSerializer(typeof(Resume));
#pragma warning restore IL2026

                        var sr = new StreamReader(outputPrefix + ".resume.xml");

                        // Should be covered by virtue of being the same exact class as the JSON above
#pragma warning disable IL2026
                        resumeClass = (Resume)xs.Deserialize(sr);
#pragma warning restore IL2026

                        sr.Close();
                    }
                }
                catch
                {
                    AaruConsole.ErrorWriteLine(UI.Incorrect_resume_file_not_continuing);

                    if(isResponse) continue;

                    return (int)ErrorNumber.InvalidResume;
                }
            }

            if(resumeClass                 != null                                               &&
               resumeClass.NextBlock       > resumeClass.LastBlock                               &&
               resumeClass.BadBlocks.Count == 0                                                  &&
               !resumeClass.Tape                                                                 &&
               (resumeClass.BadSubchannels is null   || resumeClass.BadSubchannels.Count   == 0) &&
               (resumeClass.MissingTitleKeys is null || resumeClass.MissingTitleKeys.Count == 0))
            {
                AaruConsole.WriteLine(UI.Media_already_dumped_correctly_not_continuing);

                if(isResponse) continue;

                return (int)ErrorNumber.AlreadyDumped;
            }

            Metadata sidecar = null;

            if(settings.AaruMetadata != null)
            {
                if(File.Exists(settings.AaruMetadata))
                {
                    try
                    {
                        var fs = new FileStream(settings.AaruMetadata, FileMode.Open);

                        sidecar =
                            (JsonSerializer.Deserialize(fs, typeof(MetadataJson), MetadataJsonContext.Default) as
                                 MetadataJson)?.AaruMetadata;

                        fs.Close();
                    }
                    catch
                    {
                        AaruConsole.ErrorWriteLine(UI.Incorrect_metadata_sidecar_file_not_continuing);

                        if(isResponse) continue;

                        return (int)ErrorNumber.InvalidSidecar;
                    }
                }
                else
                {
                    AaruConsole.ErrorWriteLine(UI.Could_not_find_metadata_sidecar);

                    if(isResponse) continue;

                    return (int)ErrorNumber.NoSuchFile;
                }
            }
            else if(settings.CicmXml != null)
            {
                if(File.Exists(settings.CicmXml))
                {
                    try
                    {
                        var sr = new StreamReader(settings.CicmXml);

                        // Bypassed by JSON source generator used above
#pragma warning disable IL2026
                        var sidecarXs = new XmlSerializer(typeof(CICMMetadataType));
#pragma warning restore IL2026

                        // Bypassed by JSON source generator used above
#pragma warning disable IL2026
                        sidecar = (CICMMetadataType)sidecarXs.Deserialize(sr);
#pragma warning restore IL2026

                        sr.Close();
                    }
                    catch
                    {
                        AaruConsole.ErrorWriteLine(UI.Incorrect_metadata_sidecar_file_not_continuing);

                        if(isResponse) continue;

                        return (int)ErrorNumber.InvalidSidecar;
                    }
                }
                else
                {
                    AaruConsole.ErrorWriteLine(UI.Could_not_find_metadata_sidecar);

                    if(isResponse) continue;

                    return (int)ErrorNumber.NoSuchFile;
                }
            }

            plugins    = PluginRegister.Singleton;
            candidates = [];

            // Try extension
            if(string.IsNullOrEmpty(settings.Format))
            {
                candidates.AddRange(from plugin in plugins.WritableImages.Values
                                    where plugin is not null
                                    where plugin.KnownExtensions.Contains(Path.GetExtension(settings.OutputPath))
                                    select plugin);
            }

            // Try Id
            else if(Guid.TryParse(settings.Format, out Guid outId))
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
                                    where plugin.Name.Equals(settings.Format,
                                                             StringComparison.InvariantCultureIgnoreCase)
                                    select plugin);
            }

            IBaseWritableImage outputFormat = candidates[0];

            var dumpLog = new DumpLog(outputPrefix + ".log", dev, settings.Private);

            if(settings.Verbose)
            {
                dumpLog.WriteLine(UI.Output_image_format_0_1, outputFormat.Name, outputFormat.Id);
                AaruConsole.VerboseWriteLine(UI.Output_image_format_0_1, outputFormat.Name, outputFormat.Id);
            }
            else
            {
                dumpLog.WriteLine(UI.Output_image_format_0, outputFormat.Name);
                AaruConsole.WriteLine(UI.Output_image_format_0, outputFormat.Name);
            }

            var errorLog = new ErrorLog(outputPrefix + ".error.log");

            var dumper = new Dump(settings.Resume,
                                  dev,
                                  devicePath,
                                  outputFormat,
                                  settings.RetryPasses,
                                  settings.Force,
                                  false,
                                  settings.Persistent,
                                  settings.StopOnError,
                                  resumeClass,
                                  dumpLog,
                                  encodingClass,
                                  outputPrefix,
                                  outputPrefix + extension,
                                  parsedOptions,
                                  sidecar,
                                  settings.Skip,
                                  settings.Metadata,
                                  settings.Trim,
                                  settings.FirstPregap,
                                  settings.FixOffset,
                                  settings.Debug,
                                  wantedSubchannel,
                                  settings.Speed,
                                  settings.Private,
                                  fixSubchannelPosition,
                                  settings.RetrySubchannel,
                                  fixSubchannel,
                                  fixSubchannelCrc,
                                  settings.SkipCdiReadyHole,
                                  errorLog,
                                  settings.GenerateSubchannels,
                                  maxBlocks,
                                  settings.UseBufferedReads,
                                  settings.StoreEncrypted,
                                  settings.TitleKeys,
                                  settings.IgnoreCdrRunOuts,
                                  settings.CreateGraph,
                                  settings.Dimensions);

            AnsiConsole.Progress()
                       .AutoClear(true)
                       .HideCompleted(true)
                       .Columns(new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn())
                       .Start(ctx =>
                        {
                            dumper.UpdateStatus += text => { AaruConsole.WriteLine(Markup.Escape(text)); };

                            dumper.ErrorMessage += text =>
                                {
                                    AaruConsole.ErrorWriteLine($"[red]{Markup.Escape(text)}[/]");
                                };

                            dumper.StoppingErrorMessage += text =>
                            {
                                AaruConsole.ErrorWriteLine($"[red]{Markup.Escape(text)}[/]");
                            };

                            dumper.UpdateProgress += (text, current, maximum) =>
                            {
                                _progressTask1             ??= ctx.AddTask("Progress");
                                _progressTask1.Description =   Markup.Escape(text);
                                _progressTask1.Value       =   current;
                                _progressTask1.MaxValue    =   maximum;
                            };

                            dumper.PulseProgress += text =>
                            {
                                if(_progressTask1 is null)
                                    ctx.AddTask(Markup.Escape(text)).IsIndeterminate();
                                else
                                {
                                    _progressTask1.Description     = Markup.Escape(text);
                                    _progressTask1.IsIndeterminate = true;
                                }
                            };

                            dumper.InitProgress += () => { _progressTask1 = ctx.AddTask("Progress"); };

                            dumper.EndProgress += () =>
                            {
                                _progressTask1?.StopTask();
                                _progressTask1 = null;
                            };

                            dumper.InitProgress2 += () => { _progressTask2 = ctx.AddTask("Progress"); };

                            dumper.EndProgress2 += () =>
                            {
                                _progressTask2?.StopTask();
                                _progressTask2 = null;
                            };

                            dumper.UpdateProgress2 += (text, current, maximum) =>
                            {
                                _progressTask2             ??= ctx.AddTask("Progress");
                                _progressTask2.Description =   Markup.Escape(text);
                                _progressTask2.Value       =   current;
                                _progressTask2.MaxValue    =   maximum;
                            };

                            Console.CancelKeyPress += (_, e) =>
                            {
                                e.Cancel = true;
                                dumper.Abort();
                            };

                            dumper.Start();
                        });

            if(eject && dev.IsRemovable)
            {
                Core.Spectre.ProgressSingleSpinner(ctx =>
                {
                    ctx.AddTask(UI.Ejecting_media).IsIndeterminate();

                    switch(dev.Type)
                    {
                        case DeviceType.ATA:
                            dev.DoorUnlock(out _, dev.Timeout, out _);
                            dev.MediaEject(out _, dev.Timeout, out _);

                            break;
                        case DeviceType.ATAPI:
                        case DeviceType.SCSI:
                            switch(dev.ScsiType)
                            {
                                case PeripheralDeviceTypes.DirectAccess:
                                case PeripheralDeviceTypes.SimplifiedDevice:
                                case PeripheralDeviceTypes.SCSIZonedBlockDevice:
                                case PeripheralDeviceTypes.WriteOnceDevice:
                                case PeripheralDeviceTypes.OpticalDevice:
                                case PeripheralDeviceTypes.OCRWDevice:
                                    dev.SpcAllowMediumRemoval(out _, dev.Timeout, out _);
                                    dev.EjectTray(out _, dev.Timeout, out _);

                                    break;
                                case PeripheralDeviceTypes.MultiMediaDevice:
                                    dev.AllowMediumRemoval(out _, dev.Timeout, out _);
                                    dev.EjectTray(out _, dev.Timeout, out _);

                                    break;
                                case PeripheralDeviceTypes.SequentialAccess:
                                    dev.SpcAllowMediumRemoval(out _, dev.Timeout, out _);
                                    dev.LoadUnload(out _, true, false, false, false, false, dev.Timeout, out _);

                                    break;
                            }

                            break;
                    }
                });
            }

            dev.Close();
        }

        return (int)ErrorNumber.NoError;
    }

#region Nested type: Settings

    public class Settings : MediaFamily
    {
        [Description("Take metadata from existing CICM XML sidecar.")]
        [CommandOption("-x|--cicm-xml")]
        [DefaultValue(null)]
        public string CicmXml { get; init; }
        [Description("Name of character encoding to use.")]
        [CommandOption("-e|--encoding")]
        [DefaultValue(null)]
        public string Encoding { get; init; }
        [Description("Try to read first track pregap. Only applicable to CD/DDCD/GD.")]
        [CommandOption("--first-pregap")]
        [DefaultValue(false)]
        public bool FirstPregap { get; init; }
        [Description("Fix audio tracks offset. Only applicable to CD/GD.")]
        [CommandOption("--fix-offset")]
        [DefaultValue(true)]
        public bool FixOffset { get; init; }
        [Description("Continue dumping whatever happens.")]
        [CommandOption("-f|--force")]
        [DefaultValue(false)]
        public bool Force { get; init; }
        [Description("Format of the output image, as plugin name or plugin id. If not present, will try to detect it from output image extension.")]
        [CommandOption("-t|--format")]
        [DefaultValue(null)]
        public string Format { get; init; }
        [Description("Enables creating Aaru Metadata sidecar.")]
        [CommandOption("--metadata")]
        [DefaultValue(true)]
        public bool Metadata { get; init; }
        [Description("Enables trimming errored from skipped sectors.")]
        [CommandOption("--trim")]
        [DefaultValue(true)]
        public bool Trim { get; init; }
        [Description("Comma separated name=value pairs of options to pass to output image plugin.")]
        [CommandOption("-O|--options")]
        [DefaultValue(null)]
        public string Options { get; init; }
        [Description("Try to recover partial or incorrect data.")]
        [CommandOption("--persistent")]
        [DefaultValue(false)]
        public bool Persistent { get; init; }
        [Description("Create/use resume mapfile.")]
        [CommandOption("-r|--resume")]
        [DefaultValue(true)]
        public bool Resume { get; init; }
        [Description("How many retry passes to do.")]
        [CommandOption("-p|--retry-passes")]
        [DefaultValue(5)]
        public ushort RetryPasses { get; init; }
        [Description("When an unreadable sector is found skip this many sectors.")]
        [CommandOption("-k|--skip")]
        [DefaultValue(512)]
        public uint Skip { get; init; }
        [Description("Stop media dump on first error.")]
        [CommandOption("-s|--stop-on-error")]
        [DefaultValue(false)]
        public bool StopOnError { get; init; }
        [Description("Subchannel to dump. Only applicable to CD/GD. Values: any, rw, rw-or-pq, pq, none.")]
        [CommandOption("--subchannel")]
        [DefaultValue("any")]
        public string Subchannel { get; init; }
        [Description("Speed to dump. Only applicable to optical drives, 0 for maximum.")]
        [CommandOption("--speed")]
        [DefaultValue(0)]
        public byte Speed { get; init; }
        [Description("Do not store paths and serial numbers in log or metadata.")]
        [CommandOption("--private")]
        [DefaultValue(false)]
        public bool Private { get; init; }
        [Description("Store subchannel according to the sector they describe.")]
        [CommandOption("--fix-subchannel-position")]
        [DefaultValue(true)]
        public bool FixSubchannelPosition { get; init; }
        [Description("Retry subchannel. Implies fixing subchannel position.")]
        [CommandOption("--retry-subchannel")]
        [DefaultValue(true)]
        public bool RetrySubchannel { get; init; }
        [Description("Try to fix subchannel. Implies fixing subchannel position.")]
        [CommandOption("--fix-subchannel")]
        [DefaultValue(false)]
        public bool FixSubchannel { get; init; }
        [Description("If subchannel looks OK but CRC fails, rewrite it. Implies fixing subchannel.")]
        [CommandOption("--fix-subchannel-crc")]
        [DefaultValue(false)]
        public bool FixSubchannelCrc { get; init; }
        [Description("Generates missing subchannels (they don\'t count as dumped in resume file).")]
        [CommandOption("--generate-subchannels")]
        [DefaultValue(false)]
        public bool GenerateSubchannels { get; init; }
        [Description("Skip the hole between data and audio in a CD-i Ready disc.")]
        [CommandOption("--skip-cdiready-hole")]
        [DefaultValue(true)]
        public bool SkipCdiReadyHole { get; init; }
        [Description("Eject media after dump finishes.")]
        [CommandOption("--eject")]
        [DefaultValue(false)]
        public bool Eject { get; init; }
        [Description("Maximum number of blocks to read at once.")]
        [CommandOption("--max-blocks")]
        [DefaultValue(64)]
        public uint MaxBlocks { get; init; }
        [Description("Use OS buffered reads if CMD23 is not supported. Only applicable to MMC/SD.")]
        [CommandOption("--use-buffered-reads")]
        [DefaultValue(true)]
        public bool UseBufferedReads { get; init; }
        [Description("Store encrypted data as is.")]
        [CommandOption("--store-encrypted")]
        [DefaultValue(true)]
        public bool StoreEncrypted { get; init; }
        [Description("Try to read the title keys from CSS encrypted DVDs (very slow).")]
        [CommandOption("--title-keys")]
        [DefaultValue(true)]
        public bool TitleKeys { get; init; }
        [Description("How many CD-R(W) run-out sectors to ignore and regenerate (0 for none).")]
        [CommandOption("--ignore-cdr-runouts")]
        [DefaultValue(10)]
        public uint IgnoreCdrRunOuts { get; init; }
        [Description("Create graph of dumped media. Currently only supported for CD/DVD/BD/GD/UMD.")]
        [CommandOption("-g|--create-graph")]
        [DefaultValue(true)]
        public bool CreateGraph { get; init; }
        [Description("Dimensions in pixels of the square that will contain the graph of dumped media.")]
        [CommandOption("--dimensions")]
        [DefaultValue(1080)]
        public uint Dimensions { get; init; }
        [Description("Take metadata from existing Aaru Metadata sidecar.")]
        [CommandOption("--aaru-metadata")]
        [DefaultValue(null)]
        public string AaruMetadata { get; init; }
        [Description("Device path")]
        [CommandArgument(0, "<device-path>")]
        public string DevicePath { get; init; }
        [Description("Output image path. If filename starts with # and exists, it will be read as a list of output images, its extension will be used to detect the image output format, each media will be ejected and confirmation for the next one will be asked.")]
        [CommandArgument(1, "<output-path>")]
        public string OutputPath { get; init; }
    }

#endregion
}