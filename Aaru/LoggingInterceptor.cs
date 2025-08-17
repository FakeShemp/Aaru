using Aaru.Commands;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.Spectre;
using Spectre.Console.Cli;

public class LoggingInterceptor : ICommandInterceptor
{
    private readonly LoggingLevelSwitch _levelSwitch = new();

#region ICommandInterceptor Members

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if(settings is not BaseSettings global) return;

        // Set log level
        if(global.Debug)
            _levelSwitch.MinimumLevel = LogEventLevel.Debug;
        else if(global.Verbose)
            _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
        else
            _levelSwitch.MinimumLevel = LogEventLevel.Information;

        // Configure Serilog
        LoggerConfiguration loggerConfig = new LoggerConfiguration().MinimumLevel.ControlledBy(_levelSwitch)
                                                                    .Enrich.FromLogContext()
                                                                    .WriteTo.Logger(lc => lc.Filter
                                                                        .ByIncludingOnly(e =>
                                                                             e.Level is LogEventLevel
                                                                                    .Debug
                                                                              or LogEventLevel.Verbose
                                                                              or LogEventLevel.Error)
                                                                        .WriteTo
                                                                        .Spectre(renderTextAsMarkup: true));

        // If logfile is present, add file sink and redirect Spectre.Console output
        if(!string.IsNullOrWhiteSpace(global.LogFile))
        {
            loggerConfig = loggerConfig.Enrich.FromLogContext()
                                       .WriteTo.File(global.LogFile,
                                                     outputTemplate:
                                                     "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}");
        }

        Log.Logger = loggerConfig.CreateLogger();
        Log.Information("Log level set to {Level}", _levelSwitch.MinimumLevel);
        if(global.LogFile != null) Log.Information("Logging to file: {Path}", global.LogFile);
    }

#endregion
}