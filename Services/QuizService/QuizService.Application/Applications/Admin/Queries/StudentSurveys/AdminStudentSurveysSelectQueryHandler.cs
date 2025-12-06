using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public class AdminStudentSurveysSelectQueryHandler : IQueryHandler<AdminStudentSurveysSelectQuery, AdminStudentSurveysSelectResponse>
{
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;

    public AdminStudentSurveysSelectQueryHandler(IQueryRepository<StudentQuizCollection> studentQuizQueryRepository)
    {
        _studentQuizQueryRepository = studentQuizQueryRepository;
    }

    public async Task<AdminStudentSurveysSelectResponse> Handle(AdminStudentSurveysSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminStudentSurveysSelectResponse { Success = false };

        // Build query
        var query = await _studentQuizQueryRepository.ToListAsync(sq => sq.QuizType == (short) ConstantEnum.TestType.Survey && sq.IsActive);

        // Filter by StudentId if provided
        if (request.StudentId.HasValue)
        {
            query = query.Where(sq => sq.StudentId == request.StudentId.Value).ToList();
        }

        // Filter by SurveyId if provided
        if (request.SurveyId.HasValue)
        {
            query = query.Where(sq => sq.QuizId == request.SurveyId.Value).ToList();
        }
        
        // Filter by SurveyCode if provided
        if (!string.IsNullOrEmpty(request.SurveyCode))
        {
            query = query.Where(sq => sq.Quiz.SurveyQuizSetting != null && sq.Quiz.SurveyQuizSetting.SurveyCode == request.SurveyCode).ToList();
        }

        // Get total count
        var totalCount = query.Count();

        // Apply pagination
        var studentSurveys = query
            .OrderByDescending(sq => sq.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        if (!studentSurveys.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát nào");
            return response;
        }

        // Map to response
        var studentSurveyItems = studentSurveys.Select(sq => new AdminStudentSurveyItem
        {
            StudentQuizId = sq.StudentQuizId,
            StudentId = sq.StudentId,
            SurveyId = sq.QuizId,
            SurveyTitle = sq.Quiz?.SurveyQuizSetting?.Title!,
            SurveyCode = sq.Quiz?.SurveyQuizSetting?.SurveyCode!,
            TotalQuestions = sq.Quiz?.Questions?.Count ?? 0,
            TotalAnswers = sq.StudentQuizAnswers?.Count ?? 0,
            CreatedAt = sq.CreatedAt,
            StudentName = sq.Student.FullName,
            StudentEmail = sq.Student.Email
        }).ToList();

        response.Success = true;
        response.Response = new AdminStudentSurveysSelectResponseEntity
        {
            StudentSurveys = studentSurveyItems,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát của học sinh");
        
        return response;
    }
}

