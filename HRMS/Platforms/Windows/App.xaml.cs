using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml;
using System.Text;

namespace HRMS.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            this.InitializeComponent();
            UnhandledException += HandleUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += HandleAppDomainUnhandledException;
        }

        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            base.OnLaunched(args);
        }

        private void HandleUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            WriteCrashLog("WinUI UnhandledException", e.Exception);
        }

        private void HandleAppDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
        {
            WriteCrashLog("AppDomain UnhandledException", e.ExceptionObject as Exception);
        }

        private static void WriteCrashLog(string source, Exception? exception)
        {
            try
            {
                var directory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HRMS");
                Directory.CreateDirectory(directory);

                var path = Path.Combine(directory, "startup-crash.log");
                var builder = new StringBuilder()
                    .AppendLine($"[{DateTime.Now:O}] {source}");

                if (exception is not null)
                {
                    builder.AppendLine(exception.ToString());
                }
                else
                {
                    builder.AppendLine("No exception details were provided.");
                }

                builder.AppendLine(new string('-', 80));
                File.AppendAllText(path, builder.ToString());
            }
            catch
            {
                // Best effort only.
            }
        }
    }
}
