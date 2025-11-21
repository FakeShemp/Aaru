using System.ComponentModel;
using Aaru.CommonTypes;
using Aaru.Localization;
using Spectre.Console.Cli;

namespace Aaru.Commands;

public class BaseSettings : CommandSettings
{
    [CommandOption("-v|--verbose")]
    [LocalizedDescription(nameof(UI.Shows_verbose_output))]
    [DefaultValue(false)]
    public bool Verbose { get; init; }

    [CommandOption("-d|--debug")]
    [LocalizedDescription(nameof(UI.Shows_debug_output_from_plugins))]
    [DefaultValue(false)]
    public bool Debug { get; init; }

    [CommandOption("--logfile <PATH>")]
    [LocalizedDescription(nameof(UI.Path_to_log_file))]
    public string? LogFile { get; set; }

    [CommandOption("--pause")]
    [LocalizedDescription(nameof(UI.Pauses_before_exiting))]
    [DefaultValue(false)]
    public bool Pause { get; init; }
}