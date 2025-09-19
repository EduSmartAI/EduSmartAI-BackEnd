using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.SubjectSelectEvents;
using MassTransit;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class TestService : ITestService
{
    private readonly ICommandRepository<Test> _commandRepository;
    private readonly IQueryRepository<TestCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQuizService _quizService;
    private readonly IQuestionService _questionService;
    private readonly IAnswerService _answerService;
    private readonly IRequestClient<SubjectSelectsEvent> _requestSubjectSelectClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="quizService"></param>
    /// <param name="questionService"></param>
    /// <param name="answerService"></param>
    /// <param name="requestSubjectSelectClient"></param>
    public TestService(ICommandRepository<Test> commandRepository, IQueryRepository<TestCollection> queryRepository,
        IIdentityService identityService, IUnitOfWork unitOfWork, IQuizService quizService,
        IQuestionService questionService, IAnswerService answerService,
        IRequestClient<SubjectSelectsEvent> requestSubjectSelectClient)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _quizService = quizService;
        _questionService = questionService;
        _answerService = answerService;
        _requestSubjectSelectClient = requestSubjectSelectClient;
    }

    /// <summary>
    /// Insert test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestInsertResponse> InsertTestAsync(TestInsertCommand request,
        CancellationToken cancellationToken)
    {
        var response = new TestInsertResponse { Success = false };

        var currentEmail = _identityService.GetCurrentUser()!.Email;

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new test
            var newTest = new Test
            {
                TestId = Guid.NewGuid(),
                TestName = request.TestName,
                Description = request.Description,
            };

            await _commandRepository.AddAsync(newTest, currentEmail);
            
            var subjectIds = request.Quizzes.Select(q => q.SubjectCode).Distinct().ToList();
            
            var subjectSelectEvent = new SubjectSelectsEvent
            {
                SubjectIds = subjectIds,
            };

            var messageResponse = await _requestSubjectSelectClient.GetResponse<SubjectSelectsEventResponse>(subjectSelectEvent, cancellationToken);
            if (!messageResponse.Message.Success)
            {
                response.MessageId = messageResponse.Message.MessageId;
                response.Message = messageResponse.Message.Message;
                return false;
            }

            // Create subject mapping dictionary
            var subjectMapping = messageResponse.Message.Response
                .ToDictionary(s => s.SubjectId, s => s.SubjectName);

            foreach (var quiz in request.Quizzes)
            {
                // Insert new quizzes
                var quizId = await _quizService.InsertQuizAsync(newTest.TestId, quiz.Title, quiz.Description, quiz.SubjectCode, currentEmail);
                foreach (var question in quiz.Questions)
                {
                    // Insert new questions
                    var questionId = await _questionService.InsertQuestionAsync(quizId, question.QuestionText, question.Explanation, currentEmail, question.DifficultyLevel, question.QuestionType);
                    foreach (var answer in question.Answers)
                    {
                        // Insert new answers
                        await _answerService.InsertAnswerAsync(questionId, answer.AnswerText, answer.IsCorrect, currentEmail);
                    }
                }
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _unitOfWork.Store(TestCollection.FromWriteModel(newTest, subjectMapping));
            
            foreach (var quiz in newTest.Quizzes)
            {
                foreach (var question in quiz.Questions)
                {
                    foreach (var answer in question.Answers)
                    {
                        _unitOfWork.Store(AnswerCollection.FromWriteModel(answer));
                    }

                    _unitOfWork.Store(QuestionCollection.FromWriteModel(question));
                }

                // Get subject name from mapping, fallback to empty string if not found
                var subjectName = quiz.SubjectCode.HasValue && subjectMapping.ContainsKey(quiz.SubjectCode.Value) 
                    ? subjectMapping[quiz.SubjectCode.Value] 
                    : string.Empty;

                _unitOfWork.Store(QuizCollection.FromWriteModel(quiz, subjectName));
            }
            
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm bài kiểm tra");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<TestSelectResponse> SelectTestAsync(TestSelectQuery request)
    {
        var response = new TestSelectResponse { Success = false };

        string cacheKey = "test:id";

        // Get test from cache or database
        var test = await _queryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _queryRepository.FirstOrDefaultAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (test == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
            return response;
        }

        // Build quizzes with full details
        var quizzes = test.Quizzes
            .Where(x => request.QuizId == null || !request.QuizId.Any() || request.QuizId.Contains(x.QuizId))
            .Select(q => new QuizzDetailResponse
            {
                QuizId = q.QuizId,
                Title = q.Title,
                Description = q.Description,
                SubjectCode = q.SubjectCode,
                SubjectCodeName = q.SubjectCodeName,
                TotalQuestions = q.Questions.Count,
                Questions = q.Questions.Select(ques => new QuestionDetailResponse
                {
                    QuestionId = ques.QuestionId,
                    QuestionText = ques.QuestionText,
                    QuestionType = ques.QuestionType,
                    DifficultyLevel = ques.DifficultyLevel,
                    Answers = ques.Answers.Select(a => new AnswerDetailResponse
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText
                    }).ToList()
                }).ToList()
            }).ToList();

        var responseEntity = new TestSelectResponseEntity
        {
            TestId = test.TestId,
            TestName = test.TestName,
            Description = test.Description,
            Quizzes = quizzes
        };

        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài test");
        return response;
    }
}