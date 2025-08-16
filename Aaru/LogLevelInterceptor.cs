using Aaru.Commands;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Spectre.Console.Cli;

public class LogLevelInterceptor : ICommandInterceptor
{
    private readonly LoggingLevelSwitch _levelSwitch;

    public LogLevelInterceptor(LoggingLevelSwitch levelSwitch) => _levelSwitch = levelSwitch;

#region ICommandInterceptor Members

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if(settings is BaseSettings global)
        {
            if(global.Debug)
                _levelSwitch.MinimumLevel = LogEventLevel.Debug;
            else if(global.Verbose)
                _levelSwitch.MinimumLevel = LogEventLevel.Verbose;
            else
                _levelSwitch.MinimumLevel = LogEventLevel.Information;

            Log.Information("Log level set to {Level}", _levelSwitch.MinimumLevel);
        }
    }

#endregion
}