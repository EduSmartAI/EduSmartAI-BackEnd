using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.Quizzes.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizService : IQuizService
{
    private readonly ICommandRepository<Quiz> _commandRepository;
    private readonly ICommandRepository<SurveyType> _commandSurveyTypeRepository;
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
    /// <param name="commandSurveyTypeRepository"></param>
    public QuizService(ICommandRepository<Quiz> commandRepository, IQueryRepository<QuizCollection> queryRepository,
        IIdentityService identityService, IUnitOfWork unitOfWork, IQueryRepository<TestCollection> testQueryRepository,
        ICommandRepository<SurveyType> commandSurveyTypeRepository)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _testQueryRepository = testQueryRepository;
        _commandSurveyTypeRepository = commandSurveyTypeRepository;
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
                // If not in cache, get from the database
                return await _testQueryRepository.FirstOrDefaultAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10));
        var quizzes = test?.Quizzes
            .Where(x => x.IsActive && x.QuizType == (short) ConstantEnum.TestType.Quiz)
            .Select(q => new QuizSelectsResponseEntity
            {
                QuizId = q.QuizId,
                Title = q.PlacementTestQuizSetting!.Title,
                Description = q.PlacementTestQuizSetting.Description,
                SubjectCode = q.PlacementTestQuizSetting!.SubjectCode,
                SubjectCodeName = q.PlacementTestQuizSetting.SubjectCodeName,
                TotalQuestions = q.Questions.Count,
            }).ToList();
        if (quizzes == null || !quizzes.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài quiz");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = quizzes;
        response.SetMessage(MessageId.I00001, "Lấy danh sách bài quiz");
        return response;
    }
}