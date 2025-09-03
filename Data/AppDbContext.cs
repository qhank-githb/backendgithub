using Microsoft.EntityFrameworkCore;
using MinioWebBackend.Models;


    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // 业务实体（确保这些模型的命名空间也是 MinioWebBackend.Models）
        public DbSet<FileRecord> FileRecords { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<FileTag> FileTags { get; set; }

        // 日志实体（SerilogLog 在 MinioWebBackend.Models 中）
        public DbSet<SerilogLog> SerilogLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // 业务模型配置（不变）
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

            // 日志表索引（不变）
            modelBuilder.Entity<SerilogLog>()
                .HasIndex(l => l.Timestamp)
                .HasDatabaseName("IX_SerilogLogs_Timestamp");
        }
    }

