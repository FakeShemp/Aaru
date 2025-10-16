// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Text User Interface.
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

using Aaru.Core;
using Avalonia;
using Consolonia;

namespace Aaru.Tui;

public static class Program
{
    public static int Main(string[] args)
    {
        SentrySdk.Init(options =>
        {
            // A Sentry Data Source Name (DSN) is required.
            // See https://docs.sentry.io/product/sentry-basics/dsn-explainer/
            // You can set it in the SENTRY_DSN environment variable, or you can set it in code here.
            options.Dsn = "https://153a04fb97b78bb57a8013b8b30db04f@sentry.claunia.com/8";

            // When debug is enabled, the Sentry client will emit detailed debugging information to the console.
            // This might be helpful, or might interfere with the normal operation of your application.
            // We enable it here for demonstration purposes when first trying Sentry.
            // You shouldn't do this in your applications unless you're troubleshooting issues with Sentry.
            //options.Debug = true;

            // This option is recommended. It enables Sentry's "Release Health" feature.
            options.AutoSessionTracking = true;

            // Set TracesSampleRate to 1.0 to capture 100%
            // of transactions for tracing.
            // We recommend adjusting this value in production.
            options.TracesSampleRate = 1.0;

            options.IsGlobalModeEnabled = true;
        });

        SentrySdk.ConfigureScope(scope => scope.SetExtra("Args", Environment.GetCommandLineArgs()));

        // There are too many places that depend on this being inited to be sure all are covered, so init it here.
        PluginBase.Init();

        try
        {
            return BuildAvaloniaApp().StartWithConsoleLifetime(args);
        }
        catch(Exception ex)
        {
            SentrySdk.CaptureException(ex);

            return -1;
        }
    }

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<App>().UseConsolonia().UseAutoDetectedConsole().LogToException();
}