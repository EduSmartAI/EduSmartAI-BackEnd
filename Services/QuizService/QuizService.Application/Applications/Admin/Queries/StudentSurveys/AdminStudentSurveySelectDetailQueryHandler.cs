using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.StudentSurveys;

public class AdminStudentSurveySelectDetailQueryHandler : IQueryHandler<AdminStudentSurveySelectDetailQuery, AdminStudentSurveySelectDetailResponse>
{
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;

    public AdminStudentSurveySelectDetailQueryHandler(IQueryRepository<StudentQuizCollection> studentQuizQueryRepository)
    {
        _studentQuizQueryRepository = studentQuizQueryRepository;
    }

    public async Task<AdminStudentSurveySelectDetailResponse> Handle(AdminStudentSurveySelectDetailQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminStudentSurveySelectDetailResponse { Success = false };

        // Get student survey with all related data
        var studentSurvey = await _studentQuizQueryRepository.FirstOrDefaultAsync(
            sq => sq.StudentQuizId == request.StudentQuizId && 
                  sq.QuizType == (short)ConstantEnum.TestType.Survey && 
                  sq.IsActive);

        if (studentSurvey == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát");
            return response;
        }

        // Map question results
        var questionResults = new List<SurveyQuestionResultResponseEntity>();

        foreach (var question in studentSurvey.Quiz.Questions.Where(q => q.IsActive).OrderBy(q => q.CreatedAt))
        {
            var studentAnswers = studentSurvey.StudentQuizAnswers
                .Where(sa => sa.QuestionId == question.QuestionId)
                .Select(sa => sa.AnswerId)
                .ToHashSet();

            var answers = question.Answers
                .Where(a => a.IsActive)
                .Select(a => new AdminSurveyAnswerDetailResponse
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    SelectedByStudent = studentAnswers.Contains(a.AnswerId)
                })
                .ToList();

            questionResults.Add(new SurveyQuestionResultResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Answers = answers
            });
        }

        response.Success = true;
        response.Response = new AdminStudentSurveySelectDetailResponseEntity
        {
            StudentQuizId = studentSurvey.StudentQuizId,
            StudentId = studentSurvey.StudentId,
            StudentName = studentSurvey.Student!.FullName,
            StudentEmail = studentSurvey.Student!.Email,
            SurveyId = studentSurvey.QuizId,
            SurveyTitle = studentSurvey.Quiz?.SurveyQuizSetting?.Title ?? "N/A",
            SurveyDescription = studentSurvey.Quiz?.SurveyQuizSetting?.Description,
            SurveyCode = studentSurvey.Quiz?.SurveyQuizSetting?.SurveyCode ?? "N/A",
            CreatedAt = studentSurvey.CreatedAt,
            QuestionResults = questionResults
        };
        response.SetMessage(MessageId.I00001, "Lấy chi tiết khảo sát của sinh viên thành công");
        return response;
    }
}

