using Microsoft.EntityFrameworkCore;
using MinioWebBackend.Models;
using MinioWebBackend.Service; // 引入 SqlServerJsonFunctions

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // 业务实体
    public DbSet<FileRecord> FileRecords { get; set; }
    public DbSet<Tag> Tags { get; set; }
    public DbSet<FileTag> FileTags { get; set; }
    public DbSet<User> Users { get; set; }

    // 日志实体
    public DbSet<SerilogLog> SerilogLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 业务模型配置
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

        // User 配置
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();
        modelBuilder.Entity<User>()
            .Property(u => u.Username)
            .HasMaxLength(100)
            .IsRequired();
        modelBuilder.Entity<User>()
            .Property(u => u.PasswordHash)
            .IsRequired();

        // 日志表索引
        modelBuilder.Entity<SerilogLog>()
            .HasIndex(l => l.Timestamp)
            .HasDatabaseName("IX_SerilogLogs_Timestamp");

        // ✅ 注册 SQL Server JSON_VALUE 函数
        modelBuilder
            .HasDbFunction(typeof(SqlServerJsonFunctions)
                .GetMethod(nameof(SqlServerJsonFunctions.JsonValue)))
            .HasName("JSON_VALUE")
            .IsBuiltIn();
    }
}
