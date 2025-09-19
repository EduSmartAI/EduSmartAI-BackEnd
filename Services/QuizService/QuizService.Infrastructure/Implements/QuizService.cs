using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.Quizzes.Queries;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;
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
    /// <param name="userEmail"></param>
    /// <returns></returns>
    public async Task<Guid> InsertQuizAsync(Guid testId, string title, string? description, Guid subjectCode, string userEmail)
    {
        var quiz = new Quiz
        {
            QuizId = Guid.NewGuid(),
            TestId = testId,
            Title = title,
            Description = description,
            SubjectCode = subjectCode,
            QuizType = (byte) ConstantEnum.TestType.Quiz
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
                // If not in cache, get from the database
                return await _testQueryRepository.FirstOrDefaultAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10));
        var quizzes = test?.Quizzes
            .Where(x => x.IsActive && x.QuizType == (short) ConstantEnum.TestType.Quiz)
            .Select(q => new QuizSelectsResponseEntity
            {
                QuizId = q.QuizId,
                Title = q.Title,
                Description = q.Description,
                SubjectCode = q.SubjectCode,
                SubjectCodeName = q.SubjectCodeName, // Set if available
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

    /// <summary>
    /// Insert new survey
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SurveyInsertResponse> InsertSurveyAsync(SurveyInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new SurveyInsertResponse { Success = false };
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () => 
        {
            var userEmail = _identityService.GetCurrentUser()!.Email;
            
            // Insert new survey
            var survey = new Quiz
            {
                QuizType = ((short)ConstantEnum.TestType.Survey),
                Title = request.Title,
                Description = request.Description,
                Questions = request.Questions.Select(q => new Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Answers = q.Answers != null
                        ? q.Answers.Select(a => new Answer
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                        }).ToList()
                        : []
                }).ToList()
            };
            
            // Save to database
            await _commandRepository.AddAsync(survey);
            await _unitOfWork.SaveChangesAsync(userEmail, cancellationToken);
            
            _unitOfWork.Store(QuizCollection.FromWriteModel(survey, string.Empty));
            await _unitOfWork.SessionSaveChangesAsync();

            // Remove cache
            await _unitOfWork.CacheRemoveAsync("survey:list");
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm khảo sát mới");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Select survey detail
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<SurveyDetailSelectResponse> SelectSurveyDetailAsync(SurveyDetailSelectQuery request)
    {
        var response = new SurveyDetailSelectResponse { Success = false };
        
        string cacheKey = $"survey:{request.SurveyId}";
        
        // Get surveys from cache or database
        var pagedResult = await _queryRepository.GetOrSetPagedAsync(
            // Cache key
            cacheKey,
            // If not in cache, get from database
            async () => await _queryRepository.PagedAsync(
                request.PageIndex,
                request.PageSize,
                x => x.QuizId == request.SurveyId && 
                     x.IsActive &&
                     x.QuizType == (short) ConstantEnum.TestType.Survey
            ),
            // Cache duration
            TimeSpan.FromMinutes(10)
        );
        
        // Map to response entity
        var mappedItems = pagedResult.Items.Select(entity => new SurveyDetailSelectResponseEntity
        {
            SurveyId = entity.QuizId,
            Title = entity.Title,
            Description = entity.Description,
            Questions = entity.Questions.Select(q => new QuestionSurveySelects
            {
                QuestionId = q.QuestionId,
                QuestionText = q.QuestionText,
                QuestionType = q.QuestionType,
                Answers = q.Answers.Select(a => new AnswerSurveySelects
                {
                    AnswerId = a.AnswerId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect
                }).ToList()
            }).ToList()
        }).ToList();
        
        // Prepare paginated result
        var paginatedResult = new PagedResult<SurveyDetailSelectResponseEntity>
        {
            Items = mappedItems,
            TotalCount = pagedResult.TotalCount,
            PageSize = pagedResult.PageSize,
        };
        
        // True
        response.Success = true;
        response.Response = paginatedResult;
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát");
        return response;
    }

    /// <summary>
    /// Select surveys
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<SurveySelectsResponse> SelectSurveyAsync(SurveySelectsQuery request)
    {
        var response = new SurveySelectsResponse { Success = false };
        
        string cacheKey = "survey:list";
        
        // Get surveys from cache or database
        var quizList = await _queryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                return await _queryRepository.ToListAsync(x => x.IsActive && x.QuizType == (short)ConstantEnum.TestType.Survey);
            },
            TimeSpan.FromMinutes(10)
        );
        if (!quizList.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy khảo sát");
            return response;
        }
        
        // Map to response entity
        var surveys = quizList.Select(x => new SurveySelectsResponseEntity
        {
            SurveyId = x.QuizId,
            Title = x.Title,
            Description = x.Description
        }).ToList();

        // True
        response.Success = true;
        response.Response = surveys;
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát");
        return response;
    }
}