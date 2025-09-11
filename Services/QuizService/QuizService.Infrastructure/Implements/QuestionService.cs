using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuestionService : IQuestionService
{
    private readonly ICommandRepository<Question> _commandRepository;
    private readonly IQueryRepository<QuestionCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    public QuestionService(ICommandRepository<Question> commandRepository, IQueryRepository<QuestionCollection> queryRepository, IIdentityService identityService, IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Insert question
    /// </summary>
    /// <param name="quizId"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<Guid> InsertQuestionAsync(Guid quizId, string text)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid(),
            QuizId = quizId,
            QuestionText = text,
        };
        
        await _commandRepository.AddAsync(question);
        return question.QuestionId;
    }
}