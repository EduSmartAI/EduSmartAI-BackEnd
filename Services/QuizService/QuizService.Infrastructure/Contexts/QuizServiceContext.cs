using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Contexts;

public partial class QuizServiceContext(DbContextOptions options) : AppDbContext(options)
{
    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<PlacementTestQuizSetting> PlacementTestQuizSettings { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

    public virtual DbSet<StudentQuiz> StudentQuizzes { get; set; }

    public virtual DbSet<StudentQuizAnswer> StudentQuizAnswers { get; set; }

    public virtual DbSet<StudentTest> StudentTests { get; set; }

    public virtual DbSet<SurveyQuizSetting> SurveyQuizSettings { get; set; }

    public virtual DbSet<SurveyType> SurveyTypes { get; set; }

    public virtual DbSet<Test> Tests { get; set; }
    
    public virtual DbSet<AnswerRule> AnswerRules { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("answers_pkey");

            entity.ToTable("answers");

            entity.Property(e => e.AnswerId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("answer_id");
            entity.Property(e => e.AnswerText).HasColumnName("answer_text");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Question).WithMany(p => p.Answers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("answers_question_id_fkey");
        });

        modelBuilder.Entity<AnswerRule>(entity =>
        {
            entity.HasKey(e => e.RuleId).HasName("answer_rules_pkey");

            entity.ToTable("answer_rules");

            entity.Property(e => e.RuleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("rule_id");
            entity.Property(e => e.AnswerId).HasColumnName("answer_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Formula).HasColumnName("formula");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.MappedField)
                .HasMaxLength(100)
                .HasColumnName("mapped_field");
            entity.Property(e => e.NumericMax).HasColumnName("numeric_max");
            entity.Property(e => e.NumericMin).HasColumnName("numeric_min");
            entity.Property(e => e.Unit)
                .HasMaxLength(50)
                .HasColumnName("unit");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Answer).WithMany(p => p.AnswerRules)
                .HasForeignKey(d => d.AnswerId)
                .HasConstraintName("answer_rules_answer_id_fkey");
        });

        modelBuilder.Entity<CourseQuizSetting>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("course_quiz_settings_pkey");

            entity.ToTable("course_quiz_settings");

            entity.Property(e => e.QuizId)
                .ValueGeneratedNever()
                .HasColumnName("quiz_id");
            entity.Property(e => e.AllowRetake).HasColumnName("allow_retake");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DurationMinutes).HasColumnName("duration_minutes");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.PassingScorePercentage).HasColumnName("passing_score_percentage");
            entity.Property(e => e.ShowResultsImmediately).HasColumnName("show_results_immediately");
            entity.Property(e => e.ShuffleQuestions)
                .HasDefaultValue(false)
                .HasColumnName("shuffle_questions");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Quiz).WithOne(p => p.CourseQuizSetting)
                .HasForeignKey<CourseQuizSetting>(d => d.QuizId)
                .HasConstraintName("course_quiz_settings_quiz_id_fkey");
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

        modelBuilder.Entity<PlacementTestQuizSetting>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("placement_test_quiz_settings_pkey");

            entity.ToTable("placement_test_quiz_settings");

            entity.Property(e => e.QuizId)
                .ValueGeneratedNever()
                .HasColumnName("quiz_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SubjectCode).HasColumnName("subject_code");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Quiz).WithOne(p => p.PlacementTestQuizSetting)
                .HasForeignKey<PlacementTestQuizSetting>(d => d.QuizId)
                .HasConstraintName("placement_test_quiz_settings_quiz_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("questions_pkey");

            entity.ToTable("questions");

            entity.Property(e => e.QuestionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("question_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.DifficultyLevel).HasColumnName("difficulty_level");
            entity.Property(e => e.Explanation)
                .HasMaxLength(500)
                .HasColumnName("explanation");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuestionText).HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasDefaultValue((short)1)
                .HasColumnName("question_type");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Quiz).WithMany(p => p.Questions)
                .HasForeignKey(d => d.QuizId)
                .HasConstraintName("questions_quiz_id_fkey");
        });

        modelBuilder.Entity<Quiz>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("quizzes_pkey");

            entity.ToTable("quizzes");

            entity.Property(e => e.QuizId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("quiz_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuizType)
                .HasComment("1: Survey, 2: PlacementTest, 3: Course Quiz")
                .HasColumnName("quiz_type");
            entity.Property(e => e.TestId).HasColumnName("test_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Test).WithMany(p => p.Quizzes)
                .HasForeignKey(d => d.TestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("student_tests_test_id_fkey");
        });

        modelBuilder.Entity<StudentAnswer>(entity =>
        {
            entity.HasKey(e => e.StudentAnswerId).HasName("student_answers_pkey");

            entity.ToTable("student_answers");

            entity.Property(e => e.StudentAnswerId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_answer_id");
            entity.Property(e => e.AnswerId).HasColumnName("answer_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.StudentTestId).HasColumnName("student_test_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Answer).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.AnswerId)
                .HasConstraintName("student_answers_answer_id_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("student_answers_question_id_fkey");

            entity.HasOne(d => d.StudentTest).WithMany(p => p.StudentAnswers)
                .HasForeignKey(d => d.StudentTestId)
                .HasConstraintName("student_answers_student_test_id_fkey");
        });

        modelBuilder.Entity<StudentQuiz>(entity =>
        {
            entity.HasKey(e => e.StudentQuizId).HasName("student_quizzes_pkey");

            entity.ToTable("student_quizzes");

            entity.Property(e => e.StudentQuizId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_quiz_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.QuizType).HasColumnName("quiz_type");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Quiz).WithMany(p => p.StudentQuizzes)
                .HasForeignKey(d => d.QuizId)
                .HasConstraintName("student_quizzes_quiz_id_fkey");
        });

        modelBuilder.Entity<StudentQuizAnswer>(entity =>
        {
            entity.HasKey(e => e.StudentQuizAnswerId).HasName("student_quiz_answers_pkey");

            entity.ToTable("student_quiz_answers");

            entity.Property(e => e.StudentQuizAnswerId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_quiz_answer_id");
            entity.Property(e => e.AnswerId).HasColumnName("answer_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.StudentQuizId).HasColumnName("student_quiz_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Answer).WithMany(p => p.StudentQuizAnswers)
                .HasForeignKey(d => d.AnswerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("student_quiz_answers_answer_id_fkey");

            entity.HasOne(d => d.Question).WithMany(p => p.StudentQuizAnswers)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("student_quiz_answers_question_id_fkey");

            entity.HasOne(d => d.StudentQuiz).WithMany(p => p.StudentQuizAnswers)
                .HasForeignKey(d => d.StudentQuizId)
                .HasConstraintName("student_quiz_answers_student_quiz_id_fkey");
        });

        modelBuilder.Entity<StudentTest>(entity =>
        {
            entity.HasKey(e => e.StudentTestId).HasName("student_tests_pkey");

            entity.ToTable("student_tests");

            entity.Property(e => e.StudentTestId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_test_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.FinishedAt).HasColumnName("finished_at");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.StartedAt).HasColumnName("started_at");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TestId).HasColumnName("test_id");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Test).WithMany(p => p.StudentTests)
                .HasForeignKey(d => d.TestId)
                .HasConstraintName("student_tests_test_id_fkey");
        });

        modelBuilder.Entity<SurveyQuizSetting>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("survey_quiz_settings_pkey");

            entity.ToTable("survey_quiz_settings");

            entity.Property(e => e.QuizId)
                .ValueGeneratedNever()
                .HasColumnName("quiz_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.SurveyTypeId).HasColumnName("survey_type_id");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Quiz).WithOne(p => p.SurveyQuizSetting)
                .HasForeignKey<SurveyQuizSetting>(d => d.QuizId)
                .HasConstraintName("survey_quiz_settings_quiz_id_fkey");

            entity.HasOne(d => d.SurveyType).WithMany(p => p.SurveyQuizSettings)
                .HasForeignKey(d => d.SurveyTypeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("survey_quiz_settings_survey_type_id_fkey");
        });

        modelBuilder.Entity<SurveyType>(entity =>
        {
            entity.HasKey(e => e.SurveyTypeId).HasName("survey_types_pkey");

            entity.ToTable("survey_types");

            entity.Property(e => e.SurveyTypeId)
                .ValueGeneratedNever()
                .HasColumnName("survey_type_id");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.SurveyCode)
                .HasMaxLength(20)
                .HasColumnName("survey_code");
            entity.Property(e => e.SurveyTypeName)
                .HasMaxLength(50)
                .HasColumnName("survey_type_name");
        });

        modelBuilder.Entity<Test>(entity =>
        {
            entity.HasKey(e => e.TestId).HasName("tests_pkey");

            entity.ToTable("tests");

            entity.Property(e => e.TestId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("test_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.TestName)
                .HasMaxLength(255)
                .HasColumnName("test_name");
            entity.Property(e => e.UpdatedAt).HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
