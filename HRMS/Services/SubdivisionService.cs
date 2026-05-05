using HRMS.Data;
using HRMS.Helpers;
using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Services;

public class SubdivisionService
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public SubdivisionService(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<List<Subdivision>> GetAllAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Subdivisions
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Subdivision>> GetActiveAsync()
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Subdivisions
            .AsNoTracking()
            .Where(subdivision => subdivision.Status == "Active")
            .OrderBy(subdivision => subdivision.Name)
            .ToListAsync();
    }

    public async Task<Subdivision?> GetByIdAsync(int id)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        return await ctx.Subdivisions
            .AsNoTracking()
            .SingleOrDefaultAsync(s => s.SubdivisionId == id);
    }

    public async Task<Subdivision?> GetByUserAsync(int userId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        var user = await ctx.Users
            .AsNoTracking()
            .Include(u => u.Subdivision)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        return user?.Subdivision;
    }

    public async Task AddAsync(Subdivision subdivision, int actorUserId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await EnsureCanWriteAsync(ctx, actorUserId, "subscriptions", "Only Super Admin can manage subscriptions.");
        ctx.Subdivisions.Add(subdivision);
        await ctx.SaveChangesAsync();
        await LogAsync(ctx, actorUserId, "Create", subdivision.SubdivisionId, $"Created subdivision '{subdivision.Name}'.");
    }

    public async Task UpdateAsync(Subdivision subdivision, int actorUserId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await EnsureCanWriteAsync(ctx, actorUserId, "subscriptions", "Only Super Admin can manage subscriptions.");
        ctx.Subdivisions.Update(subdivision);
        await ctx.SaveChangesAsync();
        await LogAsync(ctx, actorUserId, "Update", subdivision.SubdivisionId, $"Updated subdivision '{subdivision.Name}'.");
    }

    public async Task DeleteAsync(int id, int actorUserId)
    {
        await using var ctx = await _factory.CreateDbContextAsync();
        await EnsureCanWriteAsync(ctx, actorUserId, "subscriptions", "Only Super Admin can manage subscriptions.");
        var subdivision = await ctx.Subdivisions.FindAsync(id);
        if (subdivision is null)
        {
            return;
        }

        ctx.Subdivisions.Remove(subdivision);
        await ctx.SaveChangesAsync();
        await LogAsync(ctx, actorUserId, "Delete", subdivision.SubdivisionId, $"Deleted subdivision '{subdivision.Name}'.");
    }

    private static async Task EnsureCanWriteAsync(AppDbContext ctx, int actorUserId, string module, string message)
    {
        var role = await ctx.Users
            .AsNoTracking()
            .Where(user => user.UserId == actorUserId)
            .Select(user => user.Role.RoleName)
            .SingleOrDefaultAsync();

        if (!AccessHelper.CanWrite(role ?? string.Empty, module))
        {
            throw new UnauthorizedAccessException(message);
        }
    }

    private static async Task LogAsync(AppDbContext ctx, int actorUserId, string action, int subdivisionId, string details)
    {
        ctx.AuditLogs.Add(new AuditLog
        {
            UserId = actorUserId,
            Action = action,
            TableAffected = "Subdivisions",
            RecordId = subdivisionId,
            Details = details,
            Timestamp = DateTime.UtcNow.ToString("o")
        });

        await ctx.SaveChangesAsync();
    }
}
