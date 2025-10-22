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

    public virtual DbSet<AiEvaluation> AiEvaluations { get; set; }

    public virtual DbSet<LearningGoal> LearningGoals { get; set; }

    public virtual DbSet<LearningPath> LearningPaths { get; set; }

    public virtual DbSet<LearningPathCourse> LearningPathCourses { get; set; }

    public virtual DbSet<LearningPathMajor> LearningPathMajors { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentLearningGoal> StudentLearningGoals { get; set; }

    public virtual DbSet<StudentOrientation> StudentOrientations { get; set; }

    public virtual DbSet<StudentTechnology> StudentTechnologies { get; set; }

    public virtual DbSet<Technology> Technologies { get; set; }

    public virtual DbSet<UserBehaviour> UserBehaviours { get; set; }
    
    public virtual DbSet<CourseSuggestion> CourseSuggestions { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");

		modelBuilder.Entity<AiEvaluation>(entity =>
		{
			entity.HasKey(e => e.EvaluationId).HasName("ai_evaluations_pkey");

			entity.ToTable("ai_evaluations");

			entity.HasIndex(e => e.AttemptId, "ai_evaluations_attempt_id_key").IsUnique();

			entity.HasIndex(e => e.CreatedAt, "idx_ai_eval_created").IsDescending();

			entity.HasIndex(e => new { e.UserId, e.CourseId, e.Scope, e.ScopeId }, "idx_ai_eval_user_course_scope");

			entity.Property(e => e.EvaluationId)
				.HasDefaultValueSql("gen_random_uuid()")
				.HasColumnName("evaluation_id");
			entity.Property(e => e.Actions)
				.IsRequired()
				.HasColumnType("jsonb")
				.HasColumnName("actions");
			entity.Property(e => e.AttemptId).HasColumnName("attempt_id");
			entity.Property(e => e.Confidence)
				.HasPrecision(3, 2)
				.HasColumnName("confidence");
			entity.Property(e => e.CourseId).HasColumnName("course_id");
			entity.Property(e => e.CreatedAt)
				.HasDefaultValueSql("now()")
				.HasColumnName("created_at");
			entity.Property(e => e.Improvements)
				.IsRequired()
				.HasColumnType("jsonb")
				.HasColumnName("improvements");
			entity.Property(e => e.Model)
				.IsRequired()
				.HasColumnName("model");
			entity.Property(e => e.QuizId).HasColumnName("quiz_id");
			entity.Property(e => e.RubricVersion)
				.IsRequired()
				.HasColumnName("rubric_version");
			entity.Property(e => e.Scope).HasColumnName("scope");
			entity.Property(e => e.ScopeId).HasColumnName("scope_id");
			entity.Property(e => e.Score100).HasColumnName("score_100");
			entity.Property(e => e.Score100Raw).HasColumnName("score_100_raw");
			entity.Property(e => e.SkillGaps)
				.IsRequired()
				.HasColumnType("jsonb")
				.HasColumnName("skill_gaps");
			entity.Property(e => e.Strengths)
				.IsRequired()
				.HasColumnType("jsonb")
				.HasColumnName("strengths");
			entity.Property(e => e.Summary).HasColumnName("summary");
			entity.Property(e => e.UserId).HasColumnName("user_id");

			entity.HasOne(d => d.User).WithMany(p => p.AiEvaluations)
				.HasForeignKey(d => d.UserId)
				.HasConstraintName("ai_evaluations_user_id_fkey");
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
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.PathName)
                .HasMaxLength(200)
                .HasColumnName("path_name");
            entity.Property(e => e.Status).HasColumnName("status");
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
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasDefaultValueSql("'system'::character varying")
                .HasColumnName("created_by");
            entity.Property(e => e.ExternalCourseDuration)
                .HasMaxLength(255)
                .HasColumnName("external_course_duration");
            entity.Property(e => e.ExternalCourseLevel)
                .HasMaxLength(255)
                .HasColumnName("external_course_level");
            entity.Property(e => e.ExternalCourseLink)
                .HasMaxLength(255)
                .HasColumnName("external_course_link");
            entity.Property(e => e.ExternalCourseProvider)
                .HasMaxLength(255)
                .HasColumnName("external_course_provider");
            entity.Property(e => e.ExternalCourseRating)
                .HasPrecision(3, 2)
                .HasColumnName("external_course_rating");
            entity.Property(e => e.ExternalCourseReason)
                .HasMaxLength(255)
                .HasColumnName("external_course_reason");
            entity.Property(e => e.InternalCourseId).HasColumnName("internal_course_id");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LearningPathMajorId).HasColumnName("learning_path_major_id");
            entity.Property(e => e.Position).HasColumnName("position");
            entity.Property(e => e.Status).HasColumnName("status");
            entity.Property(e => e.StepName)
                .HasMaxLength(255)
                .HasColumnName("step_name");
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
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.MajorCode)
                .HasColumnType("character varying")
                .HasColumnName("major_code");
            entity.Property(e => e.PathId).HasColumnName("path_id");
            entity.Property(e => e.Reason).HasColumnName("reason");
            entity.Property(e => e.Type)
                .HasComment("1: Internal, 2: External")
                .HasColumnName("type");
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
            entity.Property(e => e.PositionIndex).HasColumnName("position_index");
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

        modelBuilder.Entity<UserBehaviour>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("user_behaviours_pkey");

            entity.ToTable("user_behaviours");

            entity.HasIndex(e => e.ActionType, "idx_user_behaviours_action_type");

            entity.HasIndex(e => e.CreatedAt, "idx_user_behaviours_created_at").IsDescending();

            entity.HasIndex(e => e.StudentId, "idx_user_behaviours_user_id");

            entity.Property(e => e.Id)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("id");
            entity.Property(e => e.ActionType)
                .HasMaxLength(50)
                .HasColumnName("action_type");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Metadata)
                .HasColumnType("jsonb")
                .HasColumnName("metadata");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TargetId).HasColumnName("target_id");
            entity.Property(e => e.TargetType)
                .HasMaxLength(30)
                .HasColumnName("target_type");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Student).WithMany(p => p.UserBehaviours)
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.SetNull)
                .HasConstraintName("fk_user_behaviour_user");
        });

        modelBuilder.Entity<CourseSuggestion>(entity =>
        {
            entity.HasKey(e => e.CourseSuggestionId).HasName("course_suggestions_pkey");

            entity.ToTable("course_suggestions");

            entity.HasIndex(e => new { e.StudentId, e.IsActive }, "idx_course_suggestion_student");
            entity.HasIndex(e => new { e.StudentId, e.IsViewed, e.CreatedAt }, "idx_course_suggestion_viewed").IsDescending(false, false, true);
            entity.HasIndex(e => new { e.StudentId, e.IsAccepted }, "idx_course_suggestion_accepted");
            entity.HasIndex(e => e.CreatedAt, "idx_course_suggestion_created").IsDescending();
            entity.HasIndex(e => new { e.StudentId, e.OriginalCourseId, e.SuggestedCourseId, e.IsActive }, "uq_course_suggestion").IsUnique();

            entity.Property(e => e.CourseSuggestionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("course_suggestion_id");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.OriginalCourseId).HasColumnName("original_course_id");
            entity.Property(e => e.SuggestedCourseId).HasColumnName("suggested_course_id");
            entity.Property(e => e.Reason)
                .IsRequired()
                .HasColumnName("reason");
            entity.Property(e => e.SuggestionType).HasColumnName("suggestion_type");
            entity.Property(e => e.IsViewed)
                .HasDefaultValue(false)
                .HasColumnName("is_viewed");
            entity.Property(e => e.IsAccepted).HasColumnName("is_accepted");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");

            entity.HasOne(d => d.Student).WithMany()
                .HasForeignKey(d => d.StudentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("fk_course_suggestion_student");
        });
    }
}
