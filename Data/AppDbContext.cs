using Microsoft.EntityFrameworkCore;
using ConsoleApp1.Models;

// Data/AppDbContext.cs

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<FileTag> FileTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FileTag>()
            .HasKey(ft => new { ft.FileId, ft.TagId });

        modelBuilder.Entity<FileTag>()
            .HasOne(ft => ft.FileRecord)
            .WithMany(f => f.FileTags)
            .HasForeignKey(ft => ft.FileId);

        modelBuilder.Entity<FileTag>()
            .HasOne(ft => ft.Tag)
            .WithMany(t => t.FileTags)
            .HasForeignKey(ft => ft.TagId);

        modelBuilder.Entity<Tag>()
            .HasIndex(t => t.Name).IsUnique();
    }
}
