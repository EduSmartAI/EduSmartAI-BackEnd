using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.Surveys;

public class AdminSurveysSelectQueryHandler(
    IQueryRepository<QuizCollection> quizQueryRepository,
    IQueryRepository<StudentQuizCollection> studentQuizQueryRepository) 
    : IQueryHandler<AdminSurveysSelectQuery, AdminSurveysSelectResponse>
{
    public async Task<AdminSurveysSelectResponse> Handle(AdminSurveysSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminSurveysSelectResponse { Success = false };

        try
        {
            // Get all surveys (QuizType = 1)
            var allSurveys = await quizQueryRepository.ToListAsync(
                q => q.QuizType == (short)ConstantEnum.TestType.Survey && q.IsActive);

            // Apply filters in memory
            var filteredSurveys = allSurveys.AsEnumerable();

            if (request.SurveyTypeId.HasValue)
            {
                filteredSurveys = filteredSurveys.Where(q => q.SurveyQuizSetting != null 
                    && q.SurveyQuizSetting.SurveyTypeId == request.SurveyTypeId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.SurveyCode))
            {
                var surveyCodeLower = request.SurveyCode.ToLower();
                filteredSurveys = filteredSurveys.Where(q => q.SurveyQuizSetting != null 
                    && q.SurveyQuizSetting.SurveyCode.ToLower().Contains(surveyCodeLower));
            }

            if (!string.IsNullOrWhiteSpace(request.SearchTitle))
            {
                var titleLower = request.SearchTitle.ToLower();
                filteredSurveys = filteredSurveys.Where(q => q.SurveyQuizSetting != null 
                    && q.SurveyQuizSetting.Title.ToLower().Contains(titleLower));
            }

            // Get total count
            var totalCount = filteredSurveys.Count();

            // Apply pagination
            var surveys = filteredSurveys
                .OrderByDescending(q => q.CreatedAt)
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            // Get survey IDs to count students
            var surveyIds = surveys.Select(s => s.QuizId).ToList();
            
            // Count students taken for each survey
            var allStudentQuizzes = await studentQuizQueryRepository.ToListAsync(
                sq => sq.QuizType == (short)ConstantEnum.TestType.Survey && surveyIds.Contains(sq.QuizId));
            
            var studentCountDict = allStudentQuizzes
                .GroupBy(sq => sq.QuizId)
                .ToDictionary(g => g.Key, g => g.Count());

            // Map to response
            var surveyItems = surveys.Select(s => new AdminSurveyItem
            {
                SurveyId = s.QuizId,
                SurveyType = s.QuizType,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                CreatedBy = s.CreatedBy,
                UpdatedBy = s.UpdatedBy,
                TotalQuestions = s.Questions?.Count ?? 0,
                TotalStudentsTaken = studentCountDict.GetValueOrDefault(s.QuizId, 0),
                SurveyQuizSetting = s.SurveyQuizSetting != null 
                    ? new SurveyQuizSettingDto
                    {
                        SurveyTypeId = s.SurveyQuizSetting.SurveyTypeId,
                        SurveyTypeName = s.SurveyQuizSetting.SurveyTypeName,
                        SurveyCode = s.SurveyQuizSetting.SurveyCode,
                        Title = s.SurveyQuizSetting.Title,
                        Description = s.SurveyQuizSetting.Description
                    }
                    : null!,
                Questions = s.Questions!.Select(q => new QuestionDto
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    Explanation = q.Explanation,
                    QuestionType = q.QuestionType,
                    CreatedAt = q.CreatedAt,
                    UpdatedAt = q.UpdatedAt,
                    Answers = q.Answers.Select(a => new AnswerDto
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect,
                    }).ToList()
                }).ToList()
            }).ToList();

            response.Response = new AdminSurveysSelectResponseEntity
            {
                Surveys = surveyItems,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            response.Success = true;
            var message = string.Format("Lấy danh sách surveys");
            response.SetMessage(MessageId.I00001, message);
            return response;
        }
        catch (Exception e)
        {
            var errorMessage = string.Format("Lỗi khi lấy danh sách surveys: {0}", e.Message);
            response.SetMessage(MessageId.E00000, errorMessage);
            return response;
        }
    }
}

