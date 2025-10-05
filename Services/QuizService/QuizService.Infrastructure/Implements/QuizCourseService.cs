using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
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
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly IQueryRepository<StudentQuizCollection>_studentQuizQueryRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="quizCommandRepository"></param>
    /// <param name="quizQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="outboxCommandRepository"></param>
    public QuizCourseService(ICommandRepository<Quiz> quizCommandRepository, IQueryRepository<QuizCollection> quizQueryRepository, IUnitOfWork unitOfWork, ICommandRepository<OutboxMessage> outboxCommandRepository, IIdentityService identityService, ICommandRepository<StudentQuiz> studentQuizCommandRepository, IQueryRepository<StudentQuizCollection> studentQuizQueryRepository)
    {
        _quizCommandRepository = quizCommandRepository;
        _quizQueryRepository = quizQueryRepository;
        _unitOfWork = unitOfWork;
        _outboxCommandRepository = outboxCommandRepository;
        _identityService = identityService;
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
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
            async () => await _quizQueryRepository.FirstOrDefaultAsync(q => q.QuizId == request.QuizId && q.QuizType == (short) ConstantEnum.TestType.Exam && q.IsActive),
            TimeSpan.FromMinutes(10))
            .Select(x => new QuizCourseSelectQueryResponseEntity
            {
                QuizId = x.QuizId,
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
                    Explanation = q.Explanation ?? string.Empty,
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

    /// <summary>
    /// Insert student quiz course
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentQuizCourseInsertResponse> InsertStudentQuizCourseAsync(StudentQuizCourseInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentQuizCourseInsertResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check quiz exist
            var quizCollectionExist = await _quizQueryRepository.FirstOrDefaultAsync(q =>
                q.QuizId == request.QuizId && q.QuizType == (short)ConstantEnum.TestType.Exam && q.IsActive);
            if (quizCollectionExist == null)
            {
                response.SetMessage(MessageId.E00000, "Bài kiểm tra không tồn tại");
                return false;
            }

            // Insert new StudentQuiz
            var newStudentQuiz = new StudentQuiz
            {
                QuizId = request.QuizId,
                StudentId = currentUser!.UserId,
                QuizType = (short)ConstantEnum.TestType.Exam,
                StudentQuizAnswers = request.StudentQuizAnswers.Select(ans => new StudentQuizAnswer
                {
                    QuestionId = ans.QuestionId,
                    AnswerId = ans.AnswerId,
                }).ToList()
            };

            await _studentQuizCommandRepository.AddAsync(newStudentQuiz);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // Publish event to read model
            var studentQuizCourseInsertEvent = new StudentQuizCourseInsertEvent
            {
                StudentQuiz = StudentQuizCollection.FromWriteModel(newStudentQuiz, quizCollectionExist)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(StudentQuizCourseInsertEvent),
                Content = JsonSerializer.Serialize(studentQuizCourseInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };

            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Lưu kết quả làm bài course");
            return true;
        }, cancellationToken);
        return response;
    }

    public async Task<StudentCourseQuizSelectResponse> SelectStudentCourseQuizAsync(StudentCourseQuizSelectQuery request)
    {
        var response = new StudentCourseQuizSelectResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();
        
        var cacheKey = CacheKey.StudentQuizCourse(currentUser!.UserId, request.QuizId);

        // Get student quiz from cache or database
        var studentCourseQuiz = await _studentQuizQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                return await _studentQuizQueryRepository.FirstOrDefaultAsync(x => x.QuizId == request.QuizId && x.StudentId == currentUser.UserId && x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (studentCourseQuiz == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy kết quả làm bài kiểm tra");
            return response;
        }
        
        var questionResults = new List<QuestionsCourseResultSelectResponseEntity>();

        // Get question results for this quiz - including answers and whether student selected them
        foreach (var question in studentCourseQuiz.Quiz.Questions)
        {
            var answerResults = new List<StudentQuizCourseAnswerDetailResponse>();
            foreach (var answer in question.Answers)
            {
                var selectedByStudent = studentCourseQuiz.StudentQuizAnswers.Any(sa => sa.QuestionId == question.QuestionId && sa.AnswerId == answer.AnswerId);
                answerResults.Add(new StudentQuizCourseAnswerDetailResponse
                {
                    AnswerId = answer.AnswerId,
                    IsCorrectAnswer = answer.IsCorrect,
                    SelectedByStudent = selectedByStudent,
                    AnswerText = answer.AnswerText
                });
            }
            questionResults.Add(new QuestionsCourseResultSelectResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Answers = answerResults
            });
        }
            
        // Caculate total correct answers
        var answeredQuestionIds = studentCourseQuiz.StudentQuizAnswers
            .Where(sa => studentCourseQuiz.Quiz.Questions.Any(q => q.QuestionId == sa.QuestionId))
            .Select(sa => sa.QuestionId)
            .Distinct()
            .ToHashSet();
                
        // Count correct answers
        var totalCorrectAnswers = studentCourseQuiz.Quiz.Questions
            .Where(q => answeredQuestionIds.Contains(q.QuestionId))
            .Count(q => studentCourseQuiz.StudentQuizAnswers.Any(sa =>
                sa.QuestionId == q.QuestionId &&
                q.Answers.Any(a => a.AnswerId == sa.AnswerId && a.IsCorrect)
            ));
        
        response.Response = new StudentCourseQuizSelectResponseEntity
        {
            QuizId = studentCourseQuiz.QuizId,
            TotalQuestions = studentCourseQuiz.Quiz.Questions.Count(q => q.IsActive),
            QuestionResults = questionResults,
            TotalCorrectAnswers = totalCorrectAnswers,
        };
        response.SetMessage(MessageId.I00001, "Lấy kết quả làm bài kiểm tra trong khoá học của sinh viên");
        return response;
    }
}