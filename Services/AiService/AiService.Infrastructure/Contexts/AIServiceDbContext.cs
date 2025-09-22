using AiService.Domain.Models;
using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;

namespace AiService.Infrastructure.Contexts;

public partial class AIServiceDbContext : AppDbContext
{
    public AIServiceDbContext(DbContextOptions<AIServiceDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<MajorEmbedding> MajorEmbeddings { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<MajorEmbedding>(entity =>
        {
            entity.HasKey(e => e.MajorCode).HasName("major_embeddings_pkey");

            entity.ToTable("major_embeddings");

            entity.Property(e => e.MajorCode)
                .HasMaxLength(64)
                .HasColumnName("major_code");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.Embedding)
                .HasMaxLength(1536)
                .HasColumnName("embedding");
            entity.Property(e => e.MajorName).HasColumnName("major_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
