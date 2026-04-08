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

    public bool IsAuthenticated => CurrentUser is not null;

    public string? CurrentRole => CurrentUser?.Role?.RoleName;

    public async Task<bool> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .Include(u => u.Role)
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

    public Task LogoutAsync()
    {
        CurrentUser = null;
        NotifyStateChanged();
        _navigationManager.NavigateTo("/login");

        return Task.CompletedTask;
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

        if (normalized is "/" or "/dashboard")
        {
            return true;
        }

        if (normalized == "/profile")
        {
            return IsHomeowner();
        }

        if (normalized.StartsWith("/clearance", StringComparison.Ordinal))
        {
            return IsAuthenticated;
        }

        if (normalized.StartsWith("/settings", StringComparison.Ordinal))
        {
            return HasRole("Super Admin");
        }

        if (normalized.StartsWith("/engagement", StringComparison.Ordinal) ||
            normalized.StartsWith("/msme", StringComparison.Ordinal) ||
            normalized.StartsWith("/violations", StringComparison.Ordinal))
        {
            return IsAtLeast("Board Member");
        }

        if (normalized.StartsWith("/homeowners", StringComparison.Ordinal) ||
            normalized.StartsWith("/units", StringComparison.Ordinal) ||
            normalized.StartsWith("/events", StringComparison.Ordinal) ||
            normalized.StartsWith("/dues", StringComparison.Ordinal) ||
            normalized.StartsWith("/reports", StringComparison.Ordinal))
        {
            return IsAtLeast("Staff");
        }

        if (normalized.StartsWith("/documents", StringComparison.Ordinal))
        {
            return IsAtLeast("HOA President");
        }

        return false;
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
}
