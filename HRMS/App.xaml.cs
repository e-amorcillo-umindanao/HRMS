using HRMS.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HRMS
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "HRMS" };
            window.Destroying += HandleWindowDestroying;
            return window;
        }

        private static void HandleWindowDestroying(object? sender, EventArgs e)
        {
            try
            {
                if (sender is not Window window)
                {
                    return;
                }

                var services = window.Page?.Handler?.MauiContext?.Services;
                var backupService = services?.GetService<BackupService>();
                backupService?.BackupAsync().GetAwaiter().GetResult();
            }
            catch
            {
            }
        }
    }
}
