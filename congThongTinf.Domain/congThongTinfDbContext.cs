using congThongTinf.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace congThongTinf.Domain;

public class congThongTinfDbContext : DbContext
{
    public congThongTinfDbContext()
    {
    }

    public congThongTinfDbContext(DbContextOptions<congThongTinfDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<User> Users { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<News> News { get; set; }

    public DbSet<Document> Documents { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(256);
            entity.Property(e => e.PasswordHash).IsRequired();
            entity.Property(e => e.FullName).IsRequired().HasMaxLength(256);

            entity.HasOne(e => e.Role)
                .WithMany(r => r.Users)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Articles)
                .WithOne(n => n.Author)
                .HasForeignKey(n => n.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Role entity
        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
        });

        // Configure News entity
        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Summary).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Category).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Status).IsRequired();

            entity.HasOne(e => e.Author)
                .WithMany(u => u.Articles)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configure Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.FileUrl).IsRequired();
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(256);
            entity.Property(e => e.FileType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Category).IsRequired();

            entity.HasOne(e => e.UploadedByUser)
                .WithMany()
                .HasForeignKey(e => e.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
