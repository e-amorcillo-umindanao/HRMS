using HRMS.Data;
using HRMS.Helpers;
using HRMS.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
        builder.Services.AddScoped<SettingsService>();
        builder.Services.AddScoped<SubdivisionService>();
        builder.Services.AddScoped<SubdivisionContextService>();
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
