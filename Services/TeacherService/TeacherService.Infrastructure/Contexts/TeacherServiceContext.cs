using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using TeacherService.Domain.WriteModels;

namespace TeacherService.Infrastructure.Contexts;

public partial class TeacherServiceContext : AppDbContext
{
    public TeacherServiceContext(DbContextOptions<TeacherServiceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherCertificate> TeacherCertificates { get; set; }

    public virtual DbSet<TeacherExperience> TeacherExperiences { get; set; }

    public virtual DbSet<TeacherQualification> TeacherQualifications { get; set; }

    public virtual DbSet<TeacherRating> TeacherRatings { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("outbox_messages_pkey");

            entity.ToTable("outbox_messages");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Content)
                .HasColumnType("jsonb")
                .HasColumnName("content");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.OccurredOnUtc).HasColumnName("occurred_on_utc");
            entity.Property(e => e.ProcessedOnUtc).HasColumnName("processed_on_utc");
            entity.Property(e => e.Type)
                .HasMaxLength(255)
                .HasColumnName("type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.TeacherId).HasName("teachers_pkey");

            entity.ToTable("teachers");

            entity.HasIndex(e => e.DisplayName, "idx_teachers_display_name");

            entity.HasIndex(e => e.IsActive, "idx_teachers_is_active");
            
            entity.Property(e => e.TeacherId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("teacher_id");
            entity.Property(e => e.Bio).HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DisplayName)
                .HasMaxLength(200)
                .HasColumnName("display_name");
            entity.Property(e => e.FirstName)
                .HasMaxLength(100)
                .HasColumnName("first_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.ProfilePictureUrl).HasColumnName("profile_picture_url");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<TeacherCertificate>(entity =>
        {
            entity.HasKey(e => e.CertificateId).HasName("teacher_certificates_pkey");

            entity.ToTable("teacher_certificates");

            entity.Property(e => e.CertificateId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("certificate_id");
            entity.Property(e => e.CertName)
                .HasMaxLength(255)
                .HasColumnName("cert_name");
            entity.Property(e => e.CertUrl).HasColumnName("cert_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.ExpireDate).HasColumnName("expire_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IssuedDate).HasColumnName("issued_date");
            entity.Property(e => e.Issuer)
                .HasMaxLength(255)
                .HasColumnName("issuer");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherCertificates)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("teacher_certificates_teacher_id_fkey");
        });

        modelBuilder.Entity<TeacherExperience>(entity =>
        {
            entity.HasKey(e => e.ExperienceId).HasName("teacher_experiences_pkey");

            entity.ToTable("teacher_experiences");

            entity.HasIndex(e => e.TeacherId, "idx_teacher_experiences_teacher");

            entity.Property(e => e.ExperienceId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("experience_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsCurrent)
                .HasDefaultValue(false)
                .HasColumnName("is_current");
            entity.Property(e => e.Organization)
                .HasMaxLength(255)
                .HasColumnName("organization");
            entity.Property(e => e.RoleTitle)
                .HasMaxLength(255)
                .HasColumnName("role_title");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherExperiences)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("teacher_experiences_teacher_id_fkey");
        });

        modelBuilder.Entity<TeacherQualification>(entity =>
        {
            entity.HasKey(e => e.QualificationId).HasName("teacher_qualifications_pkey");

            entity.ToTable("teacher_qualifications");

            entity.Property(e => e.QualificationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("qualification_id");
            entity.Property(e => e.CertificateUrl).HasColumnName("certificate_url");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DegreeTitle)
                .HasMaxLength(255)
                .HasColumnName("degree_title");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.EndDate).HasColumnName("end_date");
            entity.Property(e => e.Institution)
                .HasMaxLength(255)
                .HasColumnName("institution");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.StartDate).HasColumnName("start_date");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherQualifications)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("teacher_qualifications_teacher_id_fkey");
        });

        modelBuilder.Entity<TeacherRating>(entity =>
        {
            entity.HasKey(e => e.RatingId).HasName("teacher_ratings_pkey");

            entity.ToTable("teacher_ratings");

            entity.HasIndex(e => e.TeacherId, "idx_teacher_ratings_teacher");

            entity.Property(e => e.RatingId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("rating_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Rating).HasColumnName("rating");
            entity.Property(e => e.Review).HasColumnName("review");
            entity.Property(e => e.ReviewTitle)
                .HasMaxLength(255)
                .HasColumnName("review_title");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Teacher).WithMany(p => p.TeacherRatings)
                .HasForeignKey(d => d.TeacherId)
                .HasConstraintName("teacher_ratings_teacher_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
