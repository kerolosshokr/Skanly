// Skanly.Infrastructure/Persistence/SkanlyDbContext.cs
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Skanly.Domain.Entities;
using Skanly.Infrastructure.Identity;
using System.Reflection;

namespace Skanly.Infrastructure.Persistence;

public class SkanlyDbContext : IdentityDbContext<ApplicationUser>
{
    public SkanlyDbContext(DbContextOptions<SkanlyDbContext> options)
        : base(options)
    {
    }

    // Reference / Lookup
    public DbSet<University> Universities => Set<University>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Amenity> Amenities => Set<Amenity>();

    // Role Profiles
    public DbSet<Student> Students => Set<Student>();
    public DbSet<Owner> Owners => Set<Owner>();
    public DbSet<Admin> Admins => Set<Admin>();

    // Verification
    public DbSet<IdentityVerification> IdentityVerifications => Set<IdentityVerification>();

    // Property
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<PropertyVideo> PropertyVideos => Set<PropertyVideo>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();

    // Booking & Money
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Contract> Contracts => Set<Contract>();
    public DbSet<CommissionSetting> CommissionSettings => Set<CommissionSetting>();

    // Engagement
    public DbSet<Favorite> Favorites => Set<Favorite>();
    public DbSet<ChatConversation> ChatConversations => Set<ChatConversation>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Report> Reports => Set<Report>();

    // System
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder); // Required first — configures Identity tables

        // Auto-apply every IEntityTypeConfiguration<T> in this assembly
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Rename Identity tables to conventional names (optional, cleaner DB)
        builder.Entity<ApplicationUser>(b => b.ToTable("AspNetUsers"));
    }

    public override int SaveChanges()
    {
        ApplyAuditTimestamps();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditTimestamps();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyAuditTimestamps()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Entities.Common.BaseEntity &&
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Domain.Entities.Common.BaseEntity)entry.Entity;
            if (entry.State == EntityState.Added)
                entity.CreatedAt = DateTime.UtcNow;
            else
                entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}