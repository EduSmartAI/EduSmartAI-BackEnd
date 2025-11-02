using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.Quizzes;

public class AdminQuizzesSelectQueryHandler : IQueryHandler<AdminQuizzesSelectQuery, AdminQuizzesSelectResponse>
{
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;

    public AdminQuizzesSelectQueryHandler(
        IQueryRepository<QuizCollection> quizQueryRepository,
        IQueryRepository<StudentQuizCollection> studentQuizQueryRepository)
    {
        _quizQueryRepository = quizQueryRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
    }

    public async Task<AdminQuizzesSelectResponse> Handle(AdminQuizzesSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminQuizzesSelectResponse { Success = false };

        // Build query
        var query = await _quizQueryRepository.ToListAsync(q => true);

        // Filter by QuizType if provided
        if (request.QuizType.HasValue)
        {
            query = query.Where(q => q.QuizType == (short) request.QuizType.Value).ToList();
        }

        // Filter by SubjectCode if provided (for Placement Test/Quiz)
        if (!request.SubjectCode.HasValue)
        {
            query = query.Where(q => q.PlacementTestQuizSetting != null && q.PlacementTestQuizSetting.SubjectCode == request.SubjectCode).ToList();
        }

        // Filter by SurveyCode if provided (for Survey)
        if (!string.IsNullOrEmpty(request.SurveyCode))
        {
            query = query.Where(q => q.SurveyQuizSetting != null && q.SurveyQuizSetting.SurveyCode == request.SurveyCode).ToList();
        }

        // Get total count
        var totalCount = query.Count;

        // Apply pagination
        var quizzes = query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        if (!quizzes.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy quiz/survey nào");
            return response;
        }

        // Get student counts for each quiz
        var quizIds = quizzes.Select(q => q.QuizId).ToList();
        var studentCounts = await _studentQuizQueryRepository.ToListAsync(sq => quizIds.Contains(sq.QuizId) && sq.IsActive);

        // Build dictionary: QuizId -> Count
        var studentCountDict = studentCounts
            .GroupBy(sq => sq.QuizId)
            .ToDictionary(g => g.Key, g => g.Count());
        // Map to response
        var quizItems = quizzes.Select(q => new AdminQuizItem
        {
            QuizId = q.QuizId,
            QuizType = q.QuizType,
            QuizTypeName = nameof(ConstantEnum.TestType.Quiz),
            Title = q.QuizType == (short) ConstantEnum.TestType.Quiz ? q.PlacementTestQuizSetting?.Title! : q.SurveyQuizSetting?.Title!,
            Description = q.QuizType == (short)ConstantEnum.TestType.Quiz ? q.PlacementTestQuizSetting?.Description : q.SurveyQuizSetting?.Description,
            SubjectCode = q.PlacementTestQuizSetting?.SubjectCode,
            SubjectCodeName = q.PlacementTestQuizSetting?.SubjectCodeName,
            SurveyCode = q.SurveyQuizSetting?.SurveyCode,
            TotalQuestions = q.Questions?.Count ?? 0,
            TotalStudentsTaken = studentCountDict.GetValueOrDefault(q.QuizId, 0),
            IsActive = q.IsActive,
            CreatedAt = q.CreatedAt
        }).ToList();

        response.Success = true;
        response.Response = new AdminQuizzesSelectResponseEntity
        {
            Quizzes = quizItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        response.SetMessage(MessageId.I00001, "Lấy danh sách quiz/survey");
        
        return response;
    }
}

