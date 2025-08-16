using Aaru.Commands;
using Spectre.Console.Cli;

namespace Aaru;

public class PausingInterceptor : ICommandInterceptor
{
#region ICommandInterceptor Members

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if(settings is not BaseSettings global) return;

        if(global is not { Pause: true }) return;

        MainClass.PauseBeforeExiting = true;
    }

#endregion
}