using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using QuizService.Domain.ReadModels;

namespace QuizService.Application.Applications.Admin.Queries.Tests;

public class AdminSelectPlacementTestQueryHandler(IQueryRepository<TestCollection> testRepository, IQueryRepository<StudentTestCollection> studentTestRepository) : IQueryHandler<AdminSelectPlacementTestQuery, AdminSelectPlacementTestQueryResponse>
{
    public async Task<AdminSelectPlacementTestQueryResponse> Handle(AdminSelectPlacementTestQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminSelectPlacementTestQueryResponse { Success = false };

        // Get test by TestId
        var test = await testRepository.FirstOrDefaultAsync(t => t.IsActive);
        if (test == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
            return response;
        }

        // Get all student tests for this test
        var studentTests = await studentTestRepository.ToListAsync(st => 
            st.TestId == test.TestId &&
            st.IsActive && 
            st.FinishedAt != null);

        // Map to response
        response.Response = new AdminSelectPlacementTestQueryResponseEntity
        {
            TestId = test.TestId,
            TestName = test.TestName,
            Description = test.Description,
            TotalStudentAnswered = studentTests.Select(st => st.StudentId).Distinct().Count(),
            Quizzes = test.Quizzes.Select(quiz => new AdminSelectQuizDetailResponse
            {
                QuizId = quiz.QuizId,
                Title = quiz?.PlacementTestQuizSetting?.Title,
                Description = quiz?.PlacementTestQuizSetting?.Description,
                SubjectCode = quiz?.PlacementTestQuizSetting?.SubjectCode,
                SubjectCodeName = quiz?.PlacementTestQuizSetting?.SubjectCodeName,
                TotalQuestions = quiz!.Questions.Count,
                Questions = quiz.Questions.Select(q => new AdminSelectQuestionDetailResponse
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    QuestionTypeName = q.QuestionType switch
                    {
                        (short)ConstantEnum.QuestionType.SingleChoice => "Một Lựa chọn",
                        (short)ConstantEnum.QuestionType.MultipleChoice => "Nhiều lựa chọn",
                        (short)ConstantEnum.QuestionType.TrueFalse => "Đúng / Sai",
                        _ => "Không xác định"
                    },
                    DifficultyLevel = q.DifficultyLevel,
                    Answers = q.Answers.Select(a => new AdminSelectAnswerDetailResponse
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList(),
                // StudentQuizAnswers = studentTests
                //     .SelectMany(st => st.StudentAnswers
                //         .Where(sa => quiz.Questions.Any(q => q.QuestionId == sa.QuestionId))
                //         .Select(sa => new AdminSelectStudentQuizAnswerDetailResponse
                //         {
                //             StudentQuizAnswerId = sa.StudentAnswerId,
                //             Student = new AdminSelectStudentQuizAnswerDetailResponse.StudentInformation
                //             {
                //                 StudentId = st.StudentId,
                //                 Email = st.Student.Email,
                //                 FullName = st.Student.FullName
                //             },
                //             AnsweredQuestion = new AdminSelectStudentQuizAnswerDetailResponse.AnsweredQuestionDetail
                //             {
                //                 QuestionId = sa.QuestionId,
                //                 AnswerId = sa.AnswerId,
                //                 AnswerText = sa.Answer.AnswerText
                //             }
                //         }))
                //     .ToList()
            }).ToList()
        };

        response.Success = true;
        return response;
    }
}