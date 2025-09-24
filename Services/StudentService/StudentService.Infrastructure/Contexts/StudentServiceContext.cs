using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Contexts;

public partial class StudentServiceContext : AppDbContext
{

    public StudentServiceContext(DbContextOptions<StudentServiceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<LearningGoal> LearningGoals { get; set; }

    public virtual DbSet<LearningPath> LearningPaths { get; set; }

    public virtual DbSet<LearningPathCourse> LearningPathCourses { get; set; }

    public virtual DbSet<LearningPathMajor> LearningPathMajors { get; set; }

    public virtual DbSet<LearningPathMajorType> LearningPathMajorTypes { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentLearningGoal> StudentLearningGoals { get; set; }

    public virtual DbSet<StudentOrientation> StudentOrientations { get; set; }

    public virtual DbSet<StudentTechnology> StudentTechnologies { get; set; }

    public virtual DbSet<Technology> Technologies { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<LearningGoal>(entity =>
        {
            entity.HasKey(e => e.GoalId).HasName("learning_goals_pkey");

            entity.ToTable("learning_goals");

            entity.Property(e => e.GoalId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("goal_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.GoalName)
                .HasMaxLength(200)
                .HasColumnName("goal_name");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LearningGoalType).HasColumnName("learning_goal_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<LearningPath>(entity =>
        {
            entity.HasKey(e => e.PathId).HasName("learning_paths_pkey");

            entity.ToTable("learning_paths");

            entity.Property(e => e.PathId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("path_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PathName)
                .HasMaxLength(200)
                .HasColumnName("path_name");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Student).WithMany(p => p.LearningPaths)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_learning_paths_user");
        });

        modelBuilder.Entity<LearningPathCourse>(entity =>
        {
            entity.HasKey(e => e.LearningPathCourseId).HasName("learning_path_courses_pkey");

            entity.ToTable("learning_path_courses");

            entity.Property(e => e.LearningPathCourseId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("learning_path_course_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LearningPathMajorId).HasColumnName("learning_path_major_id");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.LearningPathMajor).WithMany(p => p.LearningPathCourses)
                .HasForeignKey(d => d.LearningPathMajorId)
                .HasConstraintName("fk_lpc_lpm");
        });

        modelBuilder.Entity<LearningPathMajor>(entity =>
        {
            entity.HasKey(e => e.LearningPathMajorId).HasName("learning_path_majors_pkey");

            entity.ToTable("learning_path_majors");

            entity.Property(e => e.LearningPathMajorId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("learning_path_major_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MajorId).HasColumnName("major_id");
            entity.Property(e => e.PathId).HasColumnName("path_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Path).WithMany(p => p.LearningPathMajors)
                .HasForeignKey(d => d.PathId)
                .HasConstraintName("fk_lpm_path");
        });

        modelBuilder.Entity<LearningPathMajorType>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("learning_path_major_types_pkey");

            entity.ToTable("learning_path_major_types");

            entity.Property(e => e.TypeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("type_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LearningPathMajorId).HasColumnName("learning_path_major_id");
            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .HasColumnName("type_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("updated_by");

            entity.HasOne(d => d.LearningPathMajor).WithMany(p => p.LearningPathMajorTypes)
                .HasForeignKey(d => d.LearningPathMajorId)
                .HasConstraintName("fk_lpmt_lpm");
        });

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

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("students_pkey");

            entity.ToTable("students");

            entity.HasIndex(e => e.MajorId, "IX_students_MajorId");

            entity.Property(e => e.StudentId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_id");
            entity.Property(e => e.Address)
                .HasMaxLength(200)
                .HasColumnName("address");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.Bio)
                .HasMaxLength(200)
                .HasColumnName("bio");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DateOfBirth).HasColumnName("date_of_birth");
            entity.Property(e => e.FirstName)
                .HasMaxLength(50)
                .HasColumnName("first_name");
            entity.Property(e => e.Gender).HasColumnName("gender");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LastName)
                .HasMaxLength(100)
                .HasColumnName("last_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<StudentLearningGoal>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.GoalId }).HasName("student_learning_goals_pkey");

            entity.ToTable("student_learning_goals");

            entity.HasIndex(e => e.GoalId, "IX_student_learning_goals_goal_id");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.GoalId).HasColumnName("goal_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Goal).WithMany(p => p.StudentLearningGoals)
                .HasForeignKey(d => d.GoalId)
                .HasConstraintName("fk_goal");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentLearningGoals).HasForeignKey(d => d.StudentId);
        });

        modelBuilder.Entity<StudentOrientation>(entity =>
        {
            entity.HasKey(e => e.StudentOrientationId).HasName("student_orientation_pkey");

            entity.ToTable("student_orientations");

            entity.Property(e => e.StudentOrientationId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_orientation_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.ReasonRecommend)
                .HasMaxLength(500)
                .HasColumnName("reason_recommend");
            entity.Property(e => e.RecommendType).HasColumnName("recommend_type");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.Technology)
                .HasMaxLength(20)
                .HasColumnName("technology");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentOrientations)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("student_orientations_student_id_fkey");
        });

        modelBuilder.Entity<StudentTechnology>(entity =>
        {
            entity.HasKey(e => new { e.StudentId, e.TechnologyId }).HasName("student_technologies_pkey");

            entity.ToTable("student_technologies");

            entity.HasIndex(e => e.StudentId, "idx_student_technologies_student_id");

            entity.HasIndex(e => e.TechnologyId, "idx_student_technologies_technology_id");

            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TechnologyId).HasColumnName("technology_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Student).WithMany(p => p.StudentTechnologies)
                .HasForeignKey(d => d.StudentId)
                .HasConstraintName("fk_student");

            entity.HasOne(d => d.Technology).WithMany(p => p.StudentTechnologies)
                .HasForeignKey(d => d.TechnologyId)
                .HasConstraintName("fk_technology");
        });

        modelBuilder.Entity<Technology>(entity =>
        {
            entity.HasKey(e => e.TechnologyId).HasName("technologies_pkey");

            entity.ToTable("technologies");

            entity.HasIndex(e => e.TechnologyName, "technologies_technology_name_key").IsUnique();

            entity.Property(e => e.TechnologyId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("technology_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.TechnologyName)
                .HasMaxLength(100)
                .HasColumnName("technology_name");
            entity.Property(e => e.TechnologyType).HasColumnName("technology_type");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
