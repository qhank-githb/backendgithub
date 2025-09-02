using Microsoft.EntityFrameworkCore;
using ConsoleApp1.Models;

// Data/AppDbContext.cs
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<FileTag> FileTags { get; set; }

    // ✅ 新增日志表
    public DbSet<OperationLog> OperationLogs { get; set; }

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

        // ✅ 可选：给日志表加索引（便于查询）
        modelBuilder.Entity<OperationLog>()
            .HasIndex(l => l.Timestamp);
            modelBuilder.Entity<FileInfoModel>(entity =>
    {
        entity.Property(e => e.StoredFileName).HasColumnType("longtext");
        entity.Property(e => e.OriginalFileName).HasColumnType("longtext");
        entity.Property(e => e.Bucketname).HasColumnType("varchar(255)");
        entity.Property(e => e.RelativePath).HasColumnType("longtext");
        entity.Property(e => e.AbsolutePath).HasColumnType("longtext");
        entity.Property(e => e.MimeType).HasColumnType("varchar(255)");
        entity.Property(e => e.Uploader).HasColumnType("varchar(255)");
        entity.Property(e => e.ETag).HasColumnType("varchar(255)");
    });
    }
}
