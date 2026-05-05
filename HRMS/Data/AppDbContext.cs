using HRMS.Models;
using Microsoft.EntityFrameworkCore;

namespace HRMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Subdivision> Subdivisions => Set<Subdivision>();
    public DbSet<User> Users => Set<User>();
    public DbSet<global::HRMS.Models.Phase> Phases => Set<global::HRMS.Models.Phase>();
    public DbSet<Homeowner> Homeowners => Set<Homeowner>();
    public DbSet<Unit> Units => Set<Unit>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<InteractionLog> InteractionLogs => Set<InteractionLog>();
    public DbSet<MSME> MSMEs => Set<MSME>();
    public DbSet<DuesRecord> DuesRecords => Set<DuesRecord>();
    public DbSet<ViolationRecord> ViolationRecords => Set<ViolationRecord>();
    public DbSet<ClearanceRequest> ClearanceRequests => Set<ClearanceRequest>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<HOASettings> HOASettings => Set<HOASettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Subdivision>()
            .HasIndex(s => s.Name)
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => new { a.EventId, a.HomeownerId })
            .IsUnique();

        modelBuilder.Entity<Attendance>()
            .HasIndex(a => a.SubdivisionId);

        modelBuilder.Entity<DuesRecord>()
            .HasIndex(d => new { d.HomeownerId, d.Month, d.Year })
            .IsUnique();

        modelBuilder.Entity<ViolationRecord>()
            .HasIndex(v => v.ViolationNumber)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasOne(u => u.Role)
            .WithMany()
            .HasForeignKey(u => u.RoleId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Subdivision)
            .WithMany(s => s.Users)
            .HasForeignKey(u => u.SubdivisionId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.Homeowner)
            .WithOne()
            .HasForeignKey<User>(u => u.HomeownerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Homeowner>()
            .HasOne(h => h.Subdivision)
            .WithMany(s => s.Homeowners)
            .HasForeignKey(h => h.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Homeowner>()
            .HasOne(h => h.Phase)
            .WithMany(p => p.Homeowners)
            .HasForeignKey(h => h.PhaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Homeowner>()
            .HasOne(h => h.Unit)
            .WithMany(u => u.Homeowners)
            .HasForeignKey(h => h.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Homeowner>()
            .HasOne(h => h.CreatedByUser)
            .WithMany()
            .HasForeignKey(h => h.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.Subdivision)
            .WithMany(s => s.Units)
            .HasForeignKey(u => u.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.Phase)
            .WithMany(p => p.Units)
            .HasForeignKey(u => u.PhaseId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.HeadHomeowner)
            .WithMany()
            .HasForeignKey(u => u.HeadHomeownerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Unit>()
            .HasOne(u => u.CreatedByUser)
            .WithMany()
            .HasForeignKey(u => u.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<global::HRMS.Models.Phase>()
            .HasOne(p => p.Subdivision)
            .WithMany(s => s.Phases)
            .HasForeignKey(p => p.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Subdivision)
            .WithMany(s => s.Attendances)
            .HasForeignKey(a => a.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.Subdivision)
            .WithMany(s => s.Events)
            .HasForeignKey(e => e.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Event>()
            .HasOne(e => e.CreatedByUser)
            .WithMany()
            .HasForeignKey(e => e.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Event)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.Homeowner)
            .WithMany()
            .HasForeignKey(a => a.HomeownerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attendance>()
            .HasOne(a => a.RecordedByUser)
            .WithMany()
            .HasForeignKey(a => a.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InteractionLog>()
            .HasOne(i => i.Subdivision)
            .WithMany(s => s.InteractionLogs)
            .HasForeignKey(i => i.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InteractionLog>()
            .HasOne(i => i.Homeowner)
            .WithMany()
            .HasForeignKey(i => i.HomeownerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InteractionLog>()
            .HasOne(i => i.MSME)
            .WithMany(m => m.InteractionLogs)
            .HasForeignKey(i => i.MSMEId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<InteractionLog>()
            .HasOne(i => i.CreatedByUser)
            .WithMany()
            .HasForeignKey(i => i.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MSME>()
            .HasOne(m => m.Subdivision)
            .WithMany(s => s.MSMEs)
            .HasForeignKey(m => m.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MSME>()
            .HasOne(m => m.Homeowner)
            .WithMany()
            .HasForeignKey(m => m.HomeownerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MSME>()
            .HasOne(m => m.Unit)
            .WithMany()
            .HasForeignKey(m => m.UnitId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<MSME>()
            .HasOne(m => m.CreatedByUser)
            .WithMany()
            .HasForeignKey(m => m.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DuesRecord>()
            .HasOne(d => d.Subdivision)
            .WithMany(s => s.DuesRecords)
            .HasForeignKey(d => d.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DuesRecord>()
            .HasOne(d => d.Homeowner)
            .WithMany()
            .HasForeignKey(d => d.HomeownerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DuesRecord>()
            .HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ViolationRecord>()
            .HasOne(v => v.Subdivision)
            .WithMany(s => s.ViolationRecords)
            .HasForeignKey(v => v.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ViolationRecord>()
            .HasOne(v => v.Homeowner)
            .WithMany()
            .HasForeignKey(v => v.HomeownerId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ViolationRecord>()
            .HasOne(v => v.FiledByUser)
            .WithMany()
            .HasForeignKey(v => v.FiledBy)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ViolationRecord>()
            .HasOne(v => v.UpdatedByUser)
            .WithMany()
            .HasForeignKey(v => v.UpdatedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<ClearanceRequest>()
            .HasOne(c => c.Subdivision)
            .WithMany(s => s.ClearanceRequests)
            .HasForeignKey(c => c.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClearanceRequest>()
            .HasOne(c => c.Homeowner)
            .WithMany()
            .HasForeignKey(c => c.HomeownerId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ClearanceRequest>()
            .HasOne(c => c.ProcessedByUser)
            .WithMany()
            .HasForeignKey(c => c.ProcessedBy)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<AuditLog>()
            .HasOne(a => a.User)
            .WithMany()
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HOASettings>()
            .HasIndex(s => s.SubdivisionId)
            .IsUnique();

        modelBuilder.Entity<HOASettings>()
            .HasOne(s => s.SubdivisionEntity)
            .WithMany(subdivision => subdivision.HOASettings)
            .HasForeignKey(s => s.SubdivisionId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HOASettings>()
            .HasOne(s => s.UpdatedByUser)
            .WithMany()
            .HasForeignKey(s => s.UpdatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
