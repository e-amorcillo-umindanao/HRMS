using HRMS.Data;
using HRMS.Helpers;
using HRMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using QuestPDF.Infrastructure;

namespace HRMS;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "hrms.db");

        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices();
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

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
        builder.Services.AddScoped<BackupService>();
        builder.Services.AddScoped<SessionHelper>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();
        SeedData.Initialize(app.Services);

        return app;
    }
}
