using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Surveys.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizSurveyService : IQuizSurveyService
{
    private readonly ICommandRepository<Quiz> _commandRepository;
    private readonly ICommandRepository<SurveyType> _commandSurveyTypeRepository;
    private readonly IQueryRepository<QuizCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public QuizSurveyService(ICommandRepository<Quiz> commandRepository,
        IQueryRepository<QuizCollection> queryRepository, 
        ICommandRepository<SurveyType> commandSurveyTypeRepository, 
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _commandSurveyTypeRepository = commandSurveyTypeRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
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

            var surveyTypeExist = await _commandSurveyTypeRepository.FirstOrDefaultAsync(x => x.SurveyCode == request.SurveyCode, cancellationToken: cancellationToken);
            if (surveyTypeExist == null)
            {
                response.SetMessage(MessageId.E00000, "Mã khảo sát không tồn tại");
                return false;
            }

             // Insert new survey
            var survey = new Quiz
            {
                QuizType = (short) ConstantEnum.TestType.Survey, 
                SurveyQuizSetting = new SurveyQuizSetting
                {
                    SurveyTypeId = surveyTypeExist.SurveyTypeId,
                    Title = request.Title,
                    Description = request.Description,
                },
                
                Questions = request.Questions.Select(q => new Question
                {
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Answers =  q.Answers.Select(a => new Answer
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                            AnswerRules = a.AnswerRules.Select(r => new AnswerRule
                            {
                                NumericMin = r.NumericMin,
                                NumericMax = r.NumericMax,
                                Unit = r.Unit.ToString(),
                                MappedField = r.MappedField,
                                Formula = r.Formula
                            }).ToList()
                        }).ToList()
                }).ToList()
            };
            
            // Save to database
            await _commandRepository.AddAsync(survey);
            await _unitOfWork.SaveChangesAsync(userEmail, cancellationToken);
            
            // Store to read model
            _unitOfWork.Store(QuizCollection.FromWriteModel(survey, surveyTypeExist));
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
            Title = entity.SurveyQuizSetting!.Title,
            Description = entity.SurveyQuizSetting.Description,
            SurveyCode = entity.SurveyQuizSetting!.SurveyCode,
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
                return await _queryRepository.ToListAsync(x => x.IsActive && x.QuizType == (short) ConstantEnum.TestType.Survey);
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
            Title = x.SurveyQuizSetting!.Title,
            Description = x.SurveyQuizSetting.Description,
            SurveyCode = x.SurveyQuizSetting!.SurveyCode
        }).ToList();

        // True
        response.Success = true;
        response.Response = surveys;
        response.SetMessage(MessageId.I00001, "Lấy danh sách khảo sát");
        return response;
    }
}