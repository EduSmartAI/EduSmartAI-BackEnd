using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.StudentTests;

public class AdminStudentTestsSelectQueryHandler : IQueryHandler<AdminStudentTestsSelectQuery, AdminStudentTestsSelectResponse>
{
    private readonly IQueryRepository<StudentTestCollection> _studentTestQueryRepository;
    private readonly IQueryRepository<TestCollection> _testQueryRepository;

    public AdminStudentTestsSelectQueryHandler(
        IQueryRepository<StudentTestCollection> studentTestQueryRepository,
        IQueryRepository<TestCollection> testQueryRepository)
    {
        _studentTestQueryRepository = studentTestQueryRepository;
        _testQueryRepository = testQueryRepository;
    }

    public async Task<AdminStudentTestsSelectResponse> Handle(AdminStudentTestsSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminStudentTestsSelectResponse { Success = false };

        // Build query
        var query = await _studentTestQueryRepository.ToListAsync(st => st.IsActive);

        // Filter by StudentId if provided
        if (request.StudentId.HasValue)
        {
            query = query.Where(st => st.StudentId == request.StudentId.Value).ToList();
        }

        // Filter by TestId if provided
        if (request.TestId.HasValue)
        {
            query = query.Where(st => st.TestId == request.TestId.Value).ToList();
        }

        // Get total count
        var totalCount = query.Count;

        // Apply pagination
        var studentTests = query
            .OrderByDescending(st => st.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        if (!studentTests.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra nào");
            return response;
        }

        // Get test details
        var testIds = studentTests.Select(st => st.TestId).Distinct().ToList();
        var tests = await _testQueryRepository.ToListAsync(t => testIds.Contains(t.TestId));

        // Map to response
        var studentTestItems = studentTests.Select(st =>
        {
            var test = tests.FirstOrDefault(t => t.TestId == st.TestId);
            var totalQuestions = st.StudentQuizzes?
                .SelectMany(sq => sq.Quiz?.Questions ?? new List<QuestionCollection>())
                .Count() ?? 0;
            
            var totalCorrectAnswers = CalculateTotalCorrectAnswers(st, test);
            var studentLevel = DetermineStudentLevel(st, test);

            return new AdminStudentTestItem
            {
                StudentTestId = st.StudentTestId,
                StudentId = st.StudentId,
                StudentName = st.Student.FullName,
                StudentEmail = st.Student.Email,
                TestId = st.TestId,
                TestName = test?.TestName!,
                TotalQuizzes = st.StudentQuizzes?.Count ?? 0,
                TotalQuestions = totalQuestions,
                TotalCorrectAnswers = totalCorrectAnswers,
                StudentLevel = studentLevel,
                StartedAt = st.StartedAt,
                FinishedAt = st.FinishedAt,
                Duration = st.FinishedAt - st.StartedAt
            };
        }).ToList();

        response.Success = true;
        response.Response = new AdminStudentTestsSelectResponseEntity
        {
            StudentTests = studentTestItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        response.SetMessage(MessageId.I00001, "Lấy danh sách bài kiểm tra của học sinh");
        
        return response;
    }

    private int CalculateTotalCorrectAnswers(StudentTestCollection studentTest, TestCollection? test)
    {
        if (test == null) return 0;

        var quizzes = test.Quizzes?.ToList() ?? new List<QuizCollection>();
        var answeredQuestionIds = studentTest.StudentAnswers.Select(sa => sa.QuestionId).Distinct().ToHashSet();

        return quizzes
            .SelectMany(q => q.Questions)
            .Where(q => answeredQuestionIds.Contains(q.QuestionId))
            .Count(q => studentTest.StudentAnswers.Any(sa =>
                sa.QuestionId == q.QuestionId &&
                q.Answers.Any(a => a.AnswerId == sa.AnswerId && a.IsCorrect)
            ));
    }

    private int DetermineStudentLevel(StudentTestCollection studentTest, TestCollection? test)
    {
        if (test == null) return 1;

        var difficultyPerformance = new Dictionary<int, (int correct, int total)>();
        for (int i = 1; i <= 3; i++) difficultyPerformance[i] = (0, 0);

        var allQuestions = test.Quizzes?.SelectMany(q => q.Questions).ToList() ?? new List<QuestionCollection>();

        foreach (var question in allQuestions)
        {
            if (!question.DifficultyLevel.HasValue || question.DifficultyLevel.Value < 1 || question.DifficultyLevel.Value > 3)
                continue;

            int level = question.DifficultyLevel.Value;
            var performance = difficultyPerformance[level];

            var studentAnswersForQuestion = studentTest.StudentAnswers
                .Where(sa => sa.QuestionId == question.QuestionId)
                .Select(sa => sa.AnswerId)
                .ToList();

            if (studentAnswersForQuestion.Any())
            {
                var correctAnswerIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.AnswerId)
                    .ToHashSet();

                bool isCorrect = studentAnswersForQuestion.Count == correctAnswerIds.Count &&
                                 studentAnswersForQuestion.All(id => correctAnswerIds.Contains(id ?? Guid.Empty));

                if (isCorrect) performance.correct++;
                performance.total++;
            }

            difficultyPerformance[level] = performance;
        }

        int studentLevel = 1;
        for (int level = 3; level >= 1; level--)
        {
            var (correct, total) = difficultyPerformance[level];
            if (total == 0) continue;

            double accuracy = (double)correct / total;
            if (accuracy >= 0.7)
            {
                studentLevel = level;
                break;
            }
        }

        return studentLevel;
    }
}

