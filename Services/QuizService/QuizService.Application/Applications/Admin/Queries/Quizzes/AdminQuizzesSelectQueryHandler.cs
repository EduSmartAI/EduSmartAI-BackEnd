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
        var query = await _quizQueryRepository.ToListAsync(q => q.IsActive);

        // Filter by QuizType if provided
        if (request.QuizType.HasValue)
        {
            query = query.Where(q => q.QuizType == (short) request.QuizType.Value).ToList();
        }

        // Filter by SubjectCode if provided (for Placement Test/Quiz)
        if (request.SubjectCode.HasValue)
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
        var quizItems = quizzes.Select(q =>
        {
            var quizTypeName = q.QuizType switch
            {
                (short)ConstantEnum.TestType.Exam => nameof(ConstantEnum.TestType.Exam),
                (short)ConstantEnum.TestType.Quiz => nameof(ConstantEnum.TestType.Quiz),
                (short)ConstantEnum.TestType.Survey => nameof(ConstantEnum.TestType.Survey),
                _ => "Unknown"
            };

            // Map settings và basic info dựa trên QuizType
            var (title, description, subjectCode, subjectCodeName, surveyCode, placementSetting, courseSetting, surveySetting) = q.QuizType switch
            {
                // Quiz = PlacementTestQuizSetting
                (short)ConstantEnum.TestType.Quiz => (
                    q.PlacementTestQuizSetting?.Title,
                    q.PlacementTestQuizSetting?.Description,
                    q.PlacementTestQuizSetting?.SubjectCode,
                    q.PlacementTestQuizSetting?.SubjectCodeName,
                    (string?)null,
                    q.PlacementTestQuizSetting != null ? new PlacementTestQuizSettingDto
                    {
                        SubjectCode = q.PlacementTestQuizSetting.SubjectCode,
                        SubjectCodeName = q.PlacementTestQuizSetting.SubjectCodeName,
                        Title = q.PlacementTestQuizSetting.Title,
                        Description = q.PlacementTestQuizSetting.Description
                    } : null,
                    (CourseQuizSettingDto?)null,
                    (SurveyQuizSettingDto?)null
                ),
                // Exam = CourseQuizSetting
                (short)ConstantEnum.TestType.Exam => (
                    (string?)null,
                    (string?)null,
                    (Guid?)null,
                    (string?)null,
                    (string?)null,
                    (PlacementTestQuizSettingDto?)null,
                    q.CourseQuizSetting != null ? new CourseQuizSettingDto
                    {
                        QuizId = q.CourseQuizSetting.QuizId,
                        DurationMinutes = q.CourseQuizSetting.DurationMinutes,
                        PassingScorePercentage = q.CourseQuizSetting.PassingScorePercentage,
                        ShuffleQuestions = q.CourseQuizSetting.ShuffleQuestions,
                        ShowResultsImmediately = q.CourseQuizSetting.ShowResultsImmediately,
                        AllowRetake = q.CourseQuizSetting.AllowRetake
                    } : null,
                    (SurveyQuizSettingDto?)null
                ),
                // Survey = SurveyQuizSetting
                (short)ConstantEnum.TestType.Survey => (
                    q.SurveyQuizSetting?.Title,
                    q.SurveyQuizSetting?.Description,
                    (Guid?)null,
                    (string?)null,
                    q.SurveyQuizSetting?.SurveyCode,
                    (PlacementTestQuizSettingDto?)null,
                    (CourseQuizSettingDto?)null,
                    q.SurveyQuizSetting != null ? new SurveyQuizSettingDto
                    {
                        SurveyTypeId = q.SurveyQuizSetting.SurveyTypeId,
                        SurveyCode = q.SurveyQuizSetting.SurveyCode,
                        SurveyTypeName = q.SurveyQuizSetting.SurveyTypeName,
                        Title = q.SurveyQuizSetting.Title,
                        Description = q.SurveyQuizSetting.Description
                    } : null
                ),
                _ => (null, null, null, null, null, null, null, null)
            };

            return new AdminQuizItem
            {
                QuizId = q.QuizId,
                QuizType = q.QuizType,
                QuizTypeName = quizTypeName,
                Title = title,
                Description = description,
                SubjectCode = subjectCode,
                SubjectCodeName = subjectCodeName,
                SurveyCode = surveyCode,
                TotalQuestions = q.Questions?.Count ?? 0,
                TotalStudentsTaken = studentCountDict.GetValueOrDefault(q.QuizId, 0),
                CreatedAt = q.CreatedAt,
                PlacementTestQuizSetting = placementSetting,
                CourseQuizSetting = courseSetting,
                SurveyQuizSetting = surveySetting
            };
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

