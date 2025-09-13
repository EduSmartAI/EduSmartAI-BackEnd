using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using MassTransit.Initializers;
using QuizService.Application.Applications.Quizzes.Queries;
using QuizService.Application.Applications.Tests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizService : IQuizService
{
    private readonly ICommandRepository<Quiz> _commandRepository;
    private readonly IQueryRepository<QuizCollection> _queryRepository;
    private readonly IQueryRepository<TestCollection> _testQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="testQueryRepository"></param>
    public QuizService(ICommandRepository<Quiz> commandRepository, IQueryRepository<QuizCollection> queryRepository,
        IIdentityService identityService, IUnitOfWork unitOfWork, IQueryRepository<TestCollection> testQueryRepository)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _testQueryRepository = testQueryRepository;
    }

    /// <summary>
    /// Insert quiz
    /// </summary>
    /// <param name="testId"></param>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="subjectCode"></param>
    /// <returns></returns>
    public async Task<Guid> InsertQuizAsync(Guid testId, string title, string? description, Guid subjectCode,
        string userEmail)
    {
        var quiz = new Quiz
        {
            QuizId = Guid.NewGuid(),
            TestId = testId,
            Title = title,
            Description = description,
            SubjectCode = subjectCode,
        };

        await _commandRepository.AddAsync(quiz, userEmail);
        return quiz.QuizId;
    }

    /// <summary>
    /// Select quizzes
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<QuizSelectsResponse> SelectQuizzesAsync(QuizSelectsQuery request)
    {
        var response = new QuizSelectsResponse { Success = false };

        string cacheKey = "quiz:list";

        // Get quizzes from cache or database
        var test = await _testQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _testQueryRepository.FirstOrDefaultAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10));
        var quizzes = test.Quizzes
            .Select(q => new QuizSelectsResponseEntity
            {
                QuizId = q.QuizId,
                Title = q.Title,
                Description = q.Description,
                SubjectCode = q.SubjectCode,
            }).ToList();

        // True
        response.Success = true;
        response.Response = quizzes;
        response.SetMessage(MessageId.I00001, "Lấy danh sách bài quiz");
        return response;
    }
}