using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using SixSeven.Domain.Entities;
using SixSeven.Domain.Enums;

namespace SixSeven.Data;

[ExcludeFromCodeCoverage]
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserCredentials> UserCredentials => Set<UserCredentials>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<Notification> Notifications => Set<Notification>();
    
    public DbSet<VolunteerHistory> VolunteerHistories => Set<VolunteerHistory>();
    
    public DbSet<UserCredentials> Users => Set<UserCredentials>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresEnum<VolunteerSkill>();
        modelBuilder.HasPostgresEnum<EventUrgency>();
        modelBuilder.HasPostgresEnum<UserRole>();

        ConfigureUserCredentials(modelBuilder);
        ConfigureUserProfile(modelBuilder);
        ConfigureEvent(modelBuilder);
        ConfigureNotification(modelBuilder);
        ConfigureVolunteerHistory(modelBuilder);
    }

    private static void ConfigureUserCredentials(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserCredentials>();

        entity.ToTable("UserCredentials");

        entity.HasKey(u => u.Id);

        entity.Property(u => u.Id)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(256);

        entity.HasIndex(u => u.Email)
            .IsUnique();

        entity.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(512);

        entity.Property(u => u.Role)
            .HasConversion<string>()
            .HasMaxLength(32);

        // 1:1 with profile
        entity.HasOne(u => u.Profile)
            .WithOne(p => p.Credentials)
            .HasForeignKey<UserProfile>(p => p.UserCredentialsId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasMany(u => u.Notifications)
            .WithOne(n => n.UserCredentials)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    private static void ConfigureUserProfile(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<UserProfile>();

        entity.ToTable("UserProfiles");

        entity.HasKey(u => u.Id);

        entity.Property(u => u.Id)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(u => u.UserCredentialsId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(50);

        entity.Property(u => u.AddressOne)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(u => u.AddressTwo)
            .HasMaxLength(100);

        entity.Property(u => u.City)
            .IsRequired()
            .HasMaxLength(100);

        entity.Property(u => u.State)
            .IsRequired()
            .HasMaxLength(2);

        entity.Property(u => u.ZipCode)
            .IsRequired()
            .HasMaxLength(9);

        entity.Property(u => u.Preferences)
            .IsRequired()
            .HasMaxLength(2000);

        entity.Property(u => u.Skills)
            .HasConversion(
                v => v == null || v.Count == 0
                    ? string.Empty
                    : string.Join(",", v.Select(s => s.ToString())),
                v => string.IsNullOrEmpty(v)
                    ? new List<VolunteerSkill>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => Enum.Parse<VolunteerSkill>(s))
                        .ToList()
            )
            .HasMaxLength(2000);

        entity.Property(u => u.Availability)
            .HasConversion(
                v => v == null || v.Count == 0
                    ? string.Empty
                    : string.Join(",", v),
                v => string.IsNullOrEmpty(v)
                    ? new List<string>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .ToList()
            )
            .HasMaxLength(2000);
    }


    private static void ConfigureEvent(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Event>();

        entity.ToTable("Events");

        entity.HasKey(e => e.Id);

        entity.Property(e => e.Id)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        entity.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(4000);

        entity.Property(e => e.Location)
            .IsRequired()
            .HasMaxLength(500);

        entity.Property(e => e.DateUtc)
            .IsRequired();

        entity.Property(e => e.Urgency)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.Property(e => e.RequiredSkills)
            .HasConversion(
                v => v == null || v.Count == 0
                    ? string.Empty
                    : string.Join(",", v.Select(s => s.ToString())),
                v => string.IsNullOrEmpty(v)
                    ? new List<VolunteerSkill>()
                    : v.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => Enum.Parse<VolunteerSkill>(s))
                        .ToList()
            )
            .HasMaxLength(2000);
    }


    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<Notification>();

        entity.ToTable("Notifications");

        entity.HasKey(n => n.Id);

        entity.Property(n => n.Id)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(2000);

        entity.Property(n => n.UserId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(n => n.Read)
            .IsRequired()
            .HasDefaultValue(false);

        entity.Property(n => n.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("CURRENT_TIMESTAMP");
    }
    
    private static void ConfigureVolunteerHistory(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<VolunteerHistory>();

        entity.ToTable("VolunteerHistory");

        entity.HasKey(v => v.Id);

        entity.Property(v => v.Id)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(v => v.UserId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(v => v.EventId)
            .IsRequired()
            .HasMaxLength(64);

        entity.Property(v => v.DateUtc)
            .IsRequired();

        entity.Property(v => v.DurationMinutes)
            .IsRequired();

        entity.Property(v => v.CreatedAtUtc)
            .IsRequired();

        entity.HasOne(v => v.User)
            .WithMany() 
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(v => v.Event)
            .WithMany()
            .HasForeignKey(v => v.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(v => new { v.UserId, v.DateUtc });
    }
}
