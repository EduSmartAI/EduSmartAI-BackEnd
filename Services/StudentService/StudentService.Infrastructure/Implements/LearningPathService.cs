using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class LearningPathService : ILearningPathService
{
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    //private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalCommandRepository"></param>
    /// <param name="learningGoalQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="identityService"></param>
    public LearningPathService(ICommandRepository<LearningPath> learningPathCommandRepository, IUnitOfWork unitOfWork, IIdentityService identityService)
    {
        _learningPathCommandRepository = learningPathCommandRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
    }

    public Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}