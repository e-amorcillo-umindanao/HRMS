using HRMS.Data;
using HRMS.Helpers;
using HRMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestPDF.Infrastructure;
#if WINDOWS
using Microsoft.Maui.LifecycleEvents;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;
#endif

namespace HRMS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureLifecycleEvents(events =>
            {
#if WINDOWS
                events.AddWindows(windows =>
                {
                    windows.OnWindowCreated(window =>
                    {
                        var hwnd = WindowNative.GetWindowHandle(window);
                        var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
                        var appWindow = AppWindow.GetFromWindowId(windowId);

                        if (!AppWindowTitleBar.IsCustomizationSupported())
                        {
                            return;
                        }

                        var titleBar = appWindow.TitleBar;
                        var backgroundColor = Microsoft.UI.Colors.WhiteSmoke;
                        var foregroundColor = Microsoft.UI.Colors.Black;

                        titleBar.BackgroundColor = backgroundColor;
                        titleBar.ForegroundColor = foregroundColor;
                        titleBar.InactiveBackgroundColor = backgroundColor;
                        titleBar.InactiveForegroundColor = foregroundColor;

                        titleBar.ButtonBackgroundColor = backgroundColor;
                        titleBar.ButtonForegroundColor = foregroundColor;
                        titleBar.ButtonHoverBackgroundColor = Microsoft.UI.Colors.Gainsboro;
                        titleBar.ButtonHoverForegroundColor = foregroundColor;
                        titleBar.ButtonPressedBackgroundColor = Microsoft.UI.Colors.LightGray;
                        titleBar.ButtonPressedForegroundColor = foregroundColor;
                        titleBar.ButtonInactiveBackgroundColor = backgroundColor;
                        titleBar.ButtonInactiveForegroundColor = Microsoft.UI.Colors.DimGray;
                    });
                });
#endif
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        var connectionString = @"Server=(localdb)\mssqllocaldb;Database=HRMS;Trusted_Connection=True;MultipleActiveResultSets=true;";

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(connectionString));
        builder.Services.AddDbContextFactory<AppDbContext>(options =>
            options.UseSqlServer(connectionString));

        builder.Services.AddScoped<AuthService>();
        builder.Services.AddScoped<HomeownerService>();
        builder.Services.AddScoped<UnitService>();
        builder.Services.AddScoped<EngagementService>();
        builder.Services.AddScoped<EventService>();
        builder.Services.AddScoped<AttendanceService>();
        builder.Services.AddScoped<InteractionService>();
        builder.Services.AddScoped<MSMEService>();
        builder.Services.AddScoped<DuesService>();
        builder.Services.AddScoped<ViolationService>();
        builder.Services.AddScoped<ClearanceService>();
        builder.Services.AddScoped<DocumentService>();
        builder.Services.AddScoped<ReportService>();
        builder.Services.AddScoped<AuditService>();
        builder.Services.AddScoped<SessionHelper>();
        builder.Services.AddSingleton<ThemeService>();
        builder.Services.AddSingleton(new BackupService(connectionString));

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        SeedData.Initialize(app.Services);

        return app;
    }
}
