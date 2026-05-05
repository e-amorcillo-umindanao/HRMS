using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class AuthService
{
    private static readonly Dictionary<string, int> RoleHierarchy = new(StringComparer.Ordinal)
    {
        ["HOA Staff"] = 1,
        ["Staff"] = 1,
        ["Board Member"] = 2,
        ["HOA President"] = 3,
        ["Super Admin"] = 4
    };

    private readonly AppDbContext _context;
    private readonly AuditService _auditService;
    private readonly NavigationManager _navigationManager;

    public AuthService(AppDbContext context, AuditService auditService, NavigationManager navigationManager)
    {
        _context = context;
        _auditService = auditService;
        _navigationManager = navigationManager;
    }

    public event Action? StateChanged;

    public User? CurrentUser { get; private set; }

    public int? CurrentHomeownerId => CurrentUser?.HomeownerId;
    public int? CurrentSubdivisionId => CurrentUser?.SubdivisionId;
    public string CurrentSubdivisionName => CurrentUser?.Subdivision?.Name ?? string.Empty;

    public bool IsAuthenticated => CurrentUser is not null;
    public bool IsSuperAdmin => HasRole("Super Admin");

    public string? CurrentRole => CurrentUser?.Role?.RoleName;

    public string DefaultRoute => IsHomeowner() ? "/profile" : "/dashboard";

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Subdivision)
            .SingleOrDefaultAsync(u => u.Username == username);

        if (user is null || !user.IsActive)
        {
            return false;
        }

        if (!HashHelper.Verify(password, user.PasswordHash))
        {
            return false;
        }

        user.LastLoginAt = DateTime.UtcNow.ToString("o");
        await _context.SaveChangesAsync();

        CurrentUser = user;
        NotifyStateChanged();

        await _auditService.LogAsync(user.UserId, "Login", "Users", user.UserId, $"User '{user.Username}' logged in.");

        return true;
    }

    public async Task LogoutAsync()
    {
        var user = CurrentUser;
        CurrentUser = null;
        NotifyStateChanged();
        _navigationManager.NavigateTo("/login");

        if (user is not null)
        {
            await _auditService.LogAsync(user.UserId, "Logout", "Users", user.UserId, $"User '{user.Username}' logged out.");
        }
    }

    public bool IsHomeowner() => string.Equals(CurrentRole, "Homeowner", StringComparison.Ordinal);

    public bool HasRole(string role) => string.Equals(CurrentRole, role, StringComparison.Ordinal);

    public bool IsAtLeast(string role)
    {
        if (IsHomeowner() || string.IsNullOrWhiteSpace(CurrentRole))
        {
            return false;
        }

        if (!RoleHierarchy.TryGetValue(CurrentRole, out var currentLevel) ||
            !RoleHierarchy.TryGetValue(role, out var requiredLevel))
        {
            return false;
        }

        return currentLevel >= requiredLevel;
    }

    public bool CanAccessRoute(string? route)
    {
        var normalized = NormalizeRoute(route);
        var role = CurrentRole ?? string.Empty;

        if (normalized == "/")
        {
            return true;
        }

        if (normalized == "/profile")
        {
            return AccessHelper.CanRead(role, "profile");
        }

        if (normalized == "/dashboard")
        {
            return AccessHelper.CanRead(role, "dashboard");
        }

        if (normalized.StartsWith("/subscriptions", StringComparison.Ordinal))
        {
            return IsSuperAdmin;
        }

        if (normalized.StartsWith("/settings", StringComparison.Ordinal))
        {
            return IsSuperAdmin;
        }

        if (IsHomeowner())
        {
            return normalized.StartsWith("/clearance", StringComparison.Ordinal);
        }

        if (IsWriteRoute(normalized, "homeowners"))
        {
            return AccessHelper.CanWrite(role, "homeowners");
        }

        if (IsWriteRoute(normalized, "units"))
        {
            return AccessHelper.CanWrite(role, "units");
        }

        if (IsWriteRoute(normalized, "events"))
        {
            return AccessHelper.CanWrite(role, "events");
        }

        if (IsWriteRoute(normalized, "msme"))
        {
            return AccessHelper.CanWrite(role, "msme");
        }

        if (IsWriteRoute(normalized, "dues"))
        {
            return AccessHelper.CanWrite(role, "dues");
        }

        if (IsWriteRoute(normalized, "violations"))
        {
            return AccessHelper.CanWrite(role, "violations");
        }

        if (normalized.StartsWith("/engagement/log", StringComparison.Ordinal))
        {
            return AccessHelper.CanWrite(role, "engagement");
        }

        return normalized switch
        {
            var value when value.StartsWith("/homeowners", StringComparison.Ordinal) => AccessHelper.CanRead(role, "homeowners"),
            var value when value.StartsWith("/units", StringComparison.Ordinal) => AccessHelper.CanRead(role, "units"),
            var value when value.StartsWith("/engagement", StringComparison.Ordinal) => AccessHelper.CanRead(role, "engagement"),
            var value when value.StartsWith("/events", StringComparison.Ordinal) => AccessHelper.CanRead(role, "events"),
            var value when value.StartsWith("/msme", StringComparison.Ordinal) => AccessHelper.CanRead(role, "msme"),
            var value when value.StartsWith("/dues", StringComparison.Ordinal) => AccessHelper.CanRead(role, "dues"),
            var value when value.StartsWith("/violations", StringComparison.Ordinal) => AccessHelper.CanRead(role, "violations"),
            var value when value.StartsWith("/clearance", StringComparison.Ordinal) => AccessHelper.CanRead(role, "clearance"),
            var value when value.StartsWith("/documents", StringComparison.Ordinal) => AccessHelper.CanRead(role, "documents"),
            var value when value.StartsWith("/reports", StringComparison.Ordinal) => AccessHelper.CanRead(role, "reports"),
            _ => false
        };
    }

    public static string NormalizeRoute(string? route)
    {
        if (string.IsNullOrWhiteSpace(route))
        {
            return "/";
        }

        var normalized = route.Trim();

        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        return normalized.TrimEnd('/') switch
        {
            "" => "/",
            var value => value
        };
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();

    private static bool IsWriteRoute(string route, string module) => module switch
    {
        "homeowners" => route is "/homeowners/add" or "/homeowners/new" ||
                        route.StartsWith("/homeowners/edit/", StringComparison.Ordinal) ||
                        route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/homeowners/", StringComparison.Ordinal),
        "units" => route is "/units/add" or "/units/new" ||
                   route.StartsWith("/units/edit/", StringComparison.Ordinal) ||
                   route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/units/", StringComparison.Ordinal),
        "events" => route is "/events/add" or "/events/new" ||
                    route.StartsWith("/events/edit/", StringComparison.Ordinal) ||
                    route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/events/", StringComparison.Ordinal),
        "msme" => route is "/msme/add" or "/msme/new" ||
                  route.StartsWith("/msme/edit/", StringComparison.Ordinal) ||
                  route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/msme/", StringComparison.Ordinal),
        "dues" => route is "/dues/add" or "/dues/new" ||
                  route.StartsWith("/dues/edit/", StringComparison.Ordinal) ||
                  route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/dues/", StringComparison.Ordinal),
        "violations" => route is "/violations/add" or "/violations/new" ||
                        route.StartsWith("/violations/edit/", StringComparison.Ordinal) ||
                        route.EndsWith("/edit", StringComparison.Ordinal) && route.StartsWith("/violations/", StringComparison.Ordinal),
        _ => false
    };
}
