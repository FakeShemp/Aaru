using System.ComponentModel;
using Spectre.Console.Cli;

namespace Aaru.Commands;

public class BaseSettings : CommandSettings
{
    [CommandOption("-v|--verbose")]
    [Description("Shows verbose output.")]
    [DefaultValue(false)]
    public bool Verbose { get; init; }

    [CommandOption("-d|--debug")]
    [Description("Shows debug output from plugins.")]
    [DefaultValue(false)]
    public bool Debug { get; init; }

    [CommandOption("--logfile <PATH>")]
    [Description("Path to log file.")]
    public string? LogFile { get; set; }

    [CommandOption("--pause")]
    [Description("Pauses before exiting.")]
    [DefaultValue(false)]
    public bool Pause { get; init; }
}