using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuizService : IQuizService
{
    private readonly ICommandRepository<Quiz> _commandRepository;
    private readonly IQueryRepository<QuizCollection> _queryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    public QuizService(ICommandRepository<Quiz> commandRepository, IQueryRepository<QuizCollection> queryRepository, IIdentityService identityService, IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Insert quiz
    /// </summary>
    /// <param name="testId"></param>
    /// <param name="title"></param>
    /// <param name="description"></param>
    /// <param name="subjectCode"></param>
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
        };
        
        await _commandRepository.AddAsync(quiz, userEmail);
        _unitOfWork.Store(QuizCollection.FromWriteModel(quiz));
        return quiz.QuizId;
    }
}