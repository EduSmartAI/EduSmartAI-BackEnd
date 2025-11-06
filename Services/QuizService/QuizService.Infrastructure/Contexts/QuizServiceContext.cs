using BaseService.Infrastructure.Contexts;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Contexts;

public partial class QuizServiceContext : AppDbContext
{
    public QuizServiceContext(DbContextOptions<QuizServiceContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Answer> Answers { get; set; }

    public virtual DbSet<AnswerRule> AnswerRules { get; set; }

    public virtual DbSet<CodeLanguage> CodeLanguages { get; set; }

    public virtual DbSet<CourseQuizSetting> CourseQuizSettings { get; set; }

    public virtual DbSet<OutboxMessage> OutboxMessages { get; set; }

    public virtual DbSet<PlacementTestQuizSetting> PlacementTestQuizSettings { get; set; }

    public virtual DbSet<Problem> Problems { get; set; }

    public virtual DbSet<ProblemExample> ProblemExamples { get; set; }

    public virtual DbSet<Question> Questions { get; set; }

    public virtual DbSet<Quiz> Quizzes { get; set; }

    public virtual DbSet<StudentAnswer> StudentAnswers { get; set; }

    public virtual DbSet<StudentQuiz> StudentQuizzes { get; set; }

    public virtual DbSet<StudentQuizAnswer> StudentQuizAnswers { get; set; }

    public virtual DbSet<StudentTest> StudentTests { get; set; }

    public virtual DbSet<Submission> Submissions { get; set; }

    public virtual DbSet<SubmissionTestResult> SubmissionTestResults { get; set; }

    public virtual DbSet<SurveyQuizSetting> SurveyQuizSettings { get; set; }

    public virtual DbSet<SurveyType> SurveyTypes { get; set; }

    public virtual DbSet<Test> Tests { get; set; }

    public virtual DbSet<TestCase> TestCases { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Answer>(entity =>
        {
            entity.HasKey(e => e.AnswerId).HasName("answers_pkey");

            entity.ToTable("answers");

            entity.HasIndex(e => e.QuestionId, "IX_answers_question_id");

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

            entity.HasIndex(e => e.AnswerId, "IX_answer_rules_answer_id");

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

        modelBuilder.Entity<CodeLanguage>(entity =>
        {
            entity.HasKey(e => e.LanguageId).HasName("code_languages_pkey");

            entity.ToTable("code_languages");

            entity.Property(e => e.LanguageId)
                .ValueGeneratedNever()
                .HasColumnName("language_id");
            entity.Property(e => e.CompileCmd).HasColumnName("compile_cmd");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsArchived)
                .HasDefaultValue(false)
                .HasColumnName("is_archived");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.RunCmd).HasColumnName("run_cmd");
            entity.Property(e => e.SourceFile)
                .HasMaxLength(100)
                .HasColumnName("source_file");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
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

        modelBuilder.Entity<Problem>(entity =>
        {
            entity.HasKey(e => e.ProblemId).HasName("problems_pkey");

            entity.ToTable("problems");

            entity.Property(e => e.ProblemId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("problem_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.Difficulty)
                .HasMaxLength(20)
                .HasColumnName("difficulty");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");
        });

        modelBuilder.Entity<ProblemExample>(entity =>
        {
            entity.HasKey(e => e.ExampleId).HasName("problem_examples_pkey");

            entity.ToTable("problem_examples");

            entity.HasIndex(e => e.ProblemId, "idx_problem_examples_problem_id");

            entity.Property(e => e.ExampleId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("example_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.ExampleOrder).HasColumnName("example_order");
            entity.Property(e => e.Explanation).HasColumnName("explanation");
            entity.Property(e => e.InputData).HasColumnName("input_data");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.OutputData).HasColumnName("output_data");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Problem).WithMany(p => p.ProblemExamples)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("problem_examples_problem_id_fkey");
        });

        modelBuilder.Entity<Question>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("questions_pkey");

            entity.ToTable("questions");

            entity.HasIndex(e => e.QuizId, "IX_questions_quiz_id");

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

            entity.HasIndex(e => e.TestId, "IX_quizzes_test_id");

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

            entity.HasIndex(e => e.AnswerId, "IX_student_answers_answer_id");

            entity.HasIndex(e => e.QuestionId, "IX_student_answers_question_id");

            entity.HasIndex(e => e.StudentTestId, "IX_student_answers_student_test_id");

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

            entity.HasIndex(e => e.QuizId, "IX_student_quizzes_quiz_id");

            entity.Property(e => e.StudentQuizId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("student_quiz_id");
            entity.Property(e => e.CourseId).HasColumnName("course_id");
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive).HasColumnName("is_active");
            entity.Property(e => e.QuizId).HasColumnName("quiz_id");
            entity.Property(e => e.QuizType).HasColumnName("quiz_type");
            entity.Property(e => e.Scope).HasColumnName("scope");
            entity.Property(e => e.ScopeId).HasColumnName("scope_id");
            entity.Property(e => e.Score100).HasColumnName("score_100");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.TotalCorrect).HasColumnName("total_correct");
            entity.Property(e => e.TotalQuestions).HasColumnName("total_questions");
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

            entity.HasIndex(e => e.AnswerId, "IX_student_quiz_answers_answer_id");

            entity.HasIndex(e => e.QuestionId, "IX_student_quiz_answers_question_id");

            entity.HasIndex(e => e.StudentQuizId, "IX_student_quiz_answers_student_quiz_id");

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

            entity.HasIndex(e => e.TestId, "IX_student_tests_test_id");

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

        modelBuilder.Entity<Submission>(entity =>
        {
            entity.HasKey(e => e.SubmissionId).HasName("submissions_pkey");

            entity.ToTable("submissions");

            entity.Property(e => e.SubmissionId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("submission_id");
            entity.Property(e => e.Code).HasColumnName("code");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.LanguageId)
                .ValueGeneratedOnAdd()
                .HasColumnName("language_id");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.RuntimeMs).HasColumnName("runtime_ms");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");
            entity.Property(e => e.StudentId).HasColumnName("student_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Language).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.LanguageId)
                .HasConstraintName("submissions_language_id_fkey");

            entity.HasOne(d => d.Problem).WithMany(p => p.Submissions)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("submissions_problem_id_fkey");
        });

        modelBuilder.Entity<SubmissionTestResult>(entity =>
        {
            entity.HasKey(e => e.SubmissionTestId).HasName("submission_test_results_pkey");

            entity.ToTable("submission_test_results");

            entity.Property(e => e.SubmissionTestId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("submission_test_id");
            entity.Property(e => e.ActualOutput).HasColumnName("actual_output");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.Passed)
                .HasDefaultValue(false)
                .HasColumnName("passed");
            entity.Property(e => e.SubmissionId).HasColumnName("submission_id");
            entity.Property(e => e.TestcaseId).HasColumnName("testcase_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Submission).WithMany(p => p.SubmissionTestResults)
                .HasForeignKey(d => d.SubmissionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("submission_test_results_submission_id_fkey");

            entity.HasOne(d => d.Testcase).WithMany(p => p.SubmissionTestResults)
                .HasForeignKey(d => d.TestcaseId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("submission_test_results_testcase_id_fkey");
        });

        modelBuilder.Entity<SurveyQuizSetting>(entity =>
        {
            entity.HasKey(e => e.QuizId).HasName("survey_quiz_settings_pkey");

            entity.ToTable("survey_quiz_settings");

            entity.HasIndex(e => e.SurveyTypeId, "IX_survey_quiz_settings_survey_type_id");

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

        modelBuilder.Entity<TestCase>(entity =>
        {
            entity.HasKey(e => e.TestcaseId).HasName("test_cases_pkey");

            entity.ToTable("test_cases");

            entity.Property(e => e.TestcaseId)
                .HasDefaultValueSql("gen_random_uuid()")
                .HasColumnName("testcase_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy)
                .HasMaxLength(100)
                .HasColumnName("created_by");
            entity.Property(e => e.ExpectedOutput).HasColumnName("expected_output");
            entity.Property(e => e.InputData).HasColumnName("input_data");
            entity.Property(e => e.IsActive)
                .HasDefaultValue(true)
                .HasColumnName("is_active");
            entity.Property(e => e.IsPublic)
                .HasDefaultValue(true)
                .HasColumnName("is_public");
            entity.Property(e => e.ProblemId).HasColumnName("problem_id");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("now()")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy)
                .HasMaxLength(100)
                .HasColumnName("updated_by");

            entity.HasOne(d => d.Problem).WithMany(p => p.TestCases)
                .HasForeignKey(d => d.ProblemId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("test_cases_problem_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
