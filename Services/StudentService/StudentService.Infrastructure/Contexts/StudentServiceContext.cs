using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.WriteModels;
using Type = StudentService.Domain.WriteModels.Type;

namespace StudentService.Infrastructure.Contexts;

public partial class StudentServiceContext : AppDbContext
{
    public StudentServiceContext(DbContextOptions<StudentServiceContext> options)
        : base(options)
    {
    }


    public virtual DbSet<CourseLearningPath> CourseLearningPaths { get; set; }

    public virtual DbSet<LearningGoal> LearningGoals { get; set; }

    public virtual DbSet<LearningPath> LearningPaths { get; set; }

    public virtual DbSet<Major> Majors { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentLearningGoal> StudentLearningGoals { get; set; }

    public virtual DbSet<Technology> Technologies { get; set; }

    public virtual DbSet<TechnologyType> TechnologyTypes { get; set; }

    public virtual DbSet<Type> Types { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

        modelBuilder.Entity<CourseLearningPath>(entity =>
        {
            entity.HasKey(e => new { e.CourseId, e.PathId }).HasName("course_learning_paths_pkey");

            entity.ToTable("course_learning_paths");

            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.PathId).HasColumnName("path_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Path).WithMany(p => p.CourseLearningPaths)
                .HasForeignKey(d => d.PathId)
                .HasConstraintName("fk_path");
        });

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
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<Major>(entity =>
        {
            entity.HasKey(e => e.MajorId).HasName("majors_pkey");

            entity.ToTable("majors");

            entity.Property(e => e.MajorId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("major_id");
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
            entity.Property(e => e.MajorName)
                .HasMaxLength(100)
                .HasColumnName("major_name");
            entity.Property(e => e.MajorCode)
                .HasMaxLength(100)
                .HasColumnName("major_code");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });
        
        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.SemesterId).HasName("semesters_pkey");

            entity.ToTable("semesters");

            entity.Property(e => e.SemesterId).HasColumnName("semester_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.SemesterName)
                .HasMaxLength(100)
                .HasColumnName("semester_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.StudentId).HasName("students_pkey");

            entity.ToTable("students");

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
        });

        modelBuilder.Entity<Technology>(entity =>
        {
            entity.HasKey(e => e.TechnologyId).HasName("technologies_pkey");

            entity.ToTable("technologies");

            entity.HasIndex(e => e.TechnologyName, "technologies_technology_name_key").IsUnique();

            entity.Property(e => e.TechnologyId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("technology_id");
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
            entity.Property(e => e.TechnologyName)
                .HasMaxLength(100)
                .HasColumnName("technology_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<TechnologyType>(entity =>
        {
            entity.HasKey(e => new { e.TechnologyId, e.TypeId }).HasName("technology_types_pkey");

            entity.ToTable("technology_types");

            entity.Property(e => e.TechnologyId).HasColumnName("technology_id");
            entity.Property(e => e.TypeId).HasColumnName("type_id");
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

            entity.HasOne(d => d.Technology).WithMany(p => p.TechnologyTypes)
                .HasForeignKey(d => d.TechnologyId)
                .HasConstraintName("fk_technology");

            entity.HasOne(d => d.Type).WithMany(p => p.TechnologyTypes)
                .HasForeignKey(d => d.TypeId)
                .HasConstraintName("fk_type");
        });

        modelBuilder.Entity<Type>(entity =>
        {
            entity.HasKey(e => e.TypeId).HasName("types_pkey");

            entity.ToTable("types");

            entity.HasIndex(e => e.TypeName, "types_type_name_key").IsUnique();

            entity.Property(e => e.TypeId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("type_id");
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
            entity.Property(e => e.TypeName)
                .HasMaxLength(100)
                .HasColumnName("type_name");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
