using Aaru.Core;
using Avalonia;
using Consolonia;

namespace Aaru.Tui;

public static class Program
{
    public static int Main(string[] args)
    {
        // There are too many places that depend on this being inited to be sure all are covered, so init it here.
        PluginBase.Init();

        return BuildAvaloniaApp().StartWithConsoleLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseConsolonia().UseAutoDetectedConsole().LogToException();
}