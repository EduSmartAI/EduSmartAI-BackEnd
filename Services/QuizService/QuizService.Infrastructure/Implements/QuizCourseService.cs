using System.Text.Json;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MassTransit.Initializers;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Consumers;
using QuizService.Application.Applications.QuizCourses.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizCourseService : IQuizCourseService
{
    private readonly ICommandRepository<Quiz> _quizCommandRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxCommandRepository;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizCommandRepository"></param>
    /// <param name="quizQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="outboxCommandRepository"></param>
    public QuizCourseService(ICommandRepository<Quiz> quizCommandRepository, IQueryRepository<QuizCollection> quizQueryRepository, IUnitOfWork unitOfWork, ICommandRepository<OutboxMessage> outboxCommandRepository)
    {
        _quizCommandRepository = quizCommandRepository;
        _quizQueryRepository = quizQueryRepository;
        _unitOfWork = unitOfWork;
        _outboxCommandRepository = outboxCommandRepository;
    }

    /// <summary>
    /// Insert new quiz for course
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<QuizCourseInsertResponse> InsertQuizCourseAsync(QuizCourseInsertCommand request)
    {
        var response = new QuizCourseInsertResponse { Success = false };
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var newQuiz = new Quiz
            {
                Title = request.Title,
                Description = request.Description,
                QuizType = (short) ConstantEnum.TestType.Exam,
                CourseQuizSetting = new CourseQuizSetting
                {
                    DurationMinutes = request.DurationMinutes,
                    PassingScorePercentage = request.PassingScorePercentage,
                    ShuffleQuestions = request.ShuffleQuestions,
                    ShowResultsImmediately = request.ShowResultsImmediately,
                    AllowRetake = request.AllowRetake
                },
                Questions = request.Questions.Select(x => new Question
                {
                    QuestionText = x.QuestionText,
                    Explanation = x.Explanation,
                    QuestionType = x.QuestionType,
                    Answers = x.Answers.Select(x => new Answer
                    {
                        AnswerText = x.AnswerText,
                        IsCorrect = x.IsCorrect
                    }).ToList()
                }).ToList(),
            };
            
            // Save to database
            await _quizCommandRepository.AddAsync(newQuiz);
            await _unitOfWork.SaveChangesAsync(request.UserEmail, CancellationToken.None);
            
            // Publish event to read model
            var @quizCourseCollectionInsertEvent = new QuizCourseCollectionInsertEvent
            {
                Quiz = QuizCollection.FromWriteModel(newQuiz)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(QuizCourseCollectionInsertEvent),
                Content = JsonSerializer.Serialize(@quizCourseCollectionInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(request.UserEmail, CancellationToken.None);
            
            // True
            response.Success = true;
            response.Response = new QuizCourseInsertResponseEntity { QuizId = newQuiz.QuizId };
            response.SetMessage(MessageId.I00001, "Thêm câu hỏi cho khoá học");
            return true;
        });
        return response;
    }

    /// <summary>
    /// Select quiz for course
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<QuizCourseSelectQueryResponse> SelectCourseQuiz(QuizCourseSelectQuery request)
    {
        var response = new QuizCourseSelectQueryResponse { Success = false };
        
        var cacheKey = $"QuizCourse_{request.QuizId}";

        var quizSelect = await _quizQueryRepository.GetOrSetAsync(
            cacheKey,
            async () => await _quizQueryRepository.FirstOrDefaultAsync(q => q.QuizId == request.QuizId && q.IsActive),
            TimeSpan.FromMinutes(10))
            .Select(x => new QuizCourseSelectQueryResponseEntity
            {
                QuizId = x.QuizId,
                Title = x.Title,
                Description = x.Description,
                DurationMinutes = x.CourseQuizSetting!.DurationMinutes,
                PassingScorePercentage = x.CourseQuizSetting.PassingScorePercentage,
                ShuffleQuestions = x.CourseQuizSetting.ShuffleQuestions ?? false,
                ShowResultsImmediately = x.CourseQuizSetting.ShowResultsImmediately ?? false,
                AllowRetake = x.CourseQuizSetting.AllowRetake ?? false,
                TotalQuestions = x.Questions.Count(q => q.IsActive),
                Questions = x.Questions.Where(q => q.IsActive).Select(q => new QuestionDetailResponse
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Answers = q.Answers.Where(a => a.IsActive).Select(a => new AnswerDetailResponse
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText
                    }).ToList()
                }).ToList()
            });
        if (quizSelect == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài quiz");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = quizSelect;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài quiz");
        return response;
    }
}