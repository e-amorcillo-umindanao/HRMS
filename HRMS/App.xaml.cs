using HRMS.Services;

namespace HRMS
{
    public partial class App : Application
    {
        private readonly BackupService _backupService;
        private bool _backupTriggered;

        public App(BackupService backupService)
        {
            _backupService = backupService;
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new MainPage()) { Title = "HRMS" };
            window.Destroying += HandleWindowDestroying;
            return window;
        }

        private async void HandleWindowDestroying(object? sender, EventArgs e)
        {
            if (_backupTriggered)
            {
                return;
            }

            _backupTriggered = true;

            try
            {
                await _backupService.BackupAsync();
            }
            catch
            {
                // Avoid blocking shutdown if backup fails.
            }
        }
    }
}
