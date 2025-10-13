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

    public virtual DbSet<CourseEmbedding> CourseEmbeddings { get; set; }

    public virtual DbSet<MajorEmbedding> MajorEmbeddings { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("vector");

        modelBuilder.Entity<CourseEmbedding>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("course_embeddings_pkey");

            entity.ToTable("course_embeddings");

            entity.HasIndex(e => e.DocId, "course_embeddings_doc_id_key").IsUnique();

            entity.HasIndex(e => e.Embedding, "course_embeddings_embedding_cos_idx")
                .HasMethod("ivfflat")
                .HasOperators(new[] { "vector_cosine_ops" })
                .HasAnnotation("Npgsql:StorageParameter:lists", "100");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.DocId).HasColumnName("doc_id");
            entity.Property(e => e.Embedding)
                .HasMaxLength(1536)
                .HasColumnName("embedding");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
        });

        modelBuilder.Entity<MajorEmbedding>(entity =>
        {
            entity.HasKey(e => e.MajorCode).HasName("major_embeddings_pkey");

            entity.ToTable("major_embeddings", "public");
            
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
