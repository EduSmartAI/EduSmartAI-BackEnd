using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class AnswerService : IAnswerService
{
    private readonly ICommandRepository<Answer> _commandRepository;
    private readonly IQueryRepository<AnswerCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="identityService"></param>
    public AnswerService(ICommandRepository<Answer> commandRepository, IQueryRepository<AnswerCollection> queryRepository, IUnitOfWork unitOfWork, IIdentityService identityService)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
    }
    
    /// <summary>
    /// Insert answer
    /// </summary>
    /// <param name="questionId"></param>
    /// <param name="text"></param>
    /// <param name="isCorrect"></param>
    public async Task InsertAnswerAsync(Guid questionId, string text, bool isCorrect, string email)
    {
        var answer = new Answer
        {
            QuestionId = questionId,
            AnswerText = text,
            IsCorrect = isCorrect,
        };
        await _commandRepository.AddAsync(answer, email);
    }
}