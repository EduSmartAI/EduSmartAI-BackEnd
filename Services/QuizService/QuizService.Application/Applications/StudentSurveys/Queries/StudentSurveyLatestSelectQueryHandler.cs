using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.StudentSurveys.Queries;

public class StudentSurveyLatestSelectQueryHandler(IQueryRepository<StudentQuizCollection> studentSurveyRepository, IIdentityService identityService) : IQueryHandler<StudentSurveyLatestSelectQuery, StudentSurveyLatestSelectQueryResponse>
{
    public async Task<StudentSurveyLatestSelectQueryResponse> Handle(StudentSurveyLatestSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new StudentSurveyLatestSelectQueryResponse { Success = false };

        var studentId = identityService.GetCurrentUser()!.UserId;

        var surveys = await studentSurveyRepository
            .ToListAsync(x => x.StudentId == studentId
                              && x.QuizType == (short) ConstantEnum.TestType.Survey
                              && x.IsActive);
        
        // Get latest HABIT survey
        var latestHabitSurvey = surveys
            .Where(x => x.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        
        // Get latest INTEREST survey
        var latestInterestSurvey = surveys
            .Where(x => x.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST))
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefault();
            
        var latestSurveys = new List<StudentQuizCollection>();
        if (latestHabitSurvey != null)
            latestSurveys.Add(latestHabitSurvey);
        if (latestInterestSurvey != null)
            latestSurveys.Add(latestInterestSurvey);
            
        if (!latestSurveys.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát sinh viên");
            return response;
        }
        
        // Map to response
        response.Response = latestSurveys.Select(latestSurvey => new StudentSurveySelectDetailResponseEntity
        {
            StudentSurveyId = latestSurvey.StudentQuizId,
            SurveyId = latestSurvey.Quiz!.QuizId,
            SurveyTitle = latestSurvey.Quiz?.SurveyQuizSetting?.Title ?? string.Empty,
            SurveyDescription = latestSurvey.Quiz?.SurveyQuizSetting?.Description,
            SurveyCode = latestSurvey.Quiz?.SurveyQuizSetting?.SurveyCode,
            CreatedAt = latestSurvey.CreatedAt,
            Questions = latestSurvey.Quiz?.Questions?.Select(q => new SurveyQuestionDetailResponseEntity
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Answers = q.Answers?.Select(a => new SurveyAnswerDetailResponse
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    SelectedByStudent = latestSurvey.StudentQuizAnswers
                        .Any(sa => sa.AnswerId == a.AnswerId)
                }).ToList() ?? new List<SurveyAnswerDetailResponse>()
            }).ToList() ?? new List<SurveyQuestionDetailResponseEntity>()
        }).ToList();
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001, $"Lấy {response.Response.Count} khảo sát sinh viên mới nhất (HABIT và INTEREST)");
        return response;
    }
}