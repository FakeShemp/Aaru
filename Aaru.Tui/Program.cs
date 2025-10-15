using Avalonia;
using Consolonia;

namespace Aaru.Tui
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            return BuildAvaloniaApp().StartWithConsoleLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
        {
            return AppBuilder.Configure<App>().UseConsolonia().UseAutoDetectedConsole().LogToException();
        }
    }
}