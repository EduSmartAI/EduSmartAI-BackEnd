using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queris;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class LearningGoalService : ILearningGoalService
{
    private readonly ICommandRepository<LearningGoal> _learningGoalCommandRepository;
    private readonly IQueryRepository<LearningGoalCollection> _learningGoalQueryRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalCommandRepository"></param>
    /// <param name="learningGoalQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="identityService"></param>
    public LearningGoalService(ICommandRepository<LearningGoal> learningGoalCommandRepository, IQueryRepository<LearningGoalCollection> learningGoalQueryRepository, IUnitOfWork unitOfWork, IIdentityService identityService)
    {
        _learningGoalCommandRepository = learningGoalCommandRepository;
        _learningGoalQueryRepository = learningGoalQueryRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
    }

    /// <summary>
    /// Insert new learning goal
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningGoalInsertResponse> InsertLearningGoalAsync(LearningGoalInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningGoalInsertResponse { Success = false};
        
        var currentUserEmail = _identityService.GetCurrentUser()!.Email;
        
        // Check if learning goal already exists
        var learningGoalExist = await _learningGoalCommandRepository.FirstOrDefaultAsync(x => x.GoalName == request.GoalName, cancellationToken);
        if (learningGoalExist != null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.LearningGoalExists);
            return response;
        }
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new learning goal
            var learningGoal = new LearningGoal
            {
                GoalName = request.GoalName,
                Description = request.Description,
            };
            await _learningGoalCommandRepository.AddAsync(learningGoal, currentUserEmail);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _unitOfWork.Store(LearningGoalCollection.FromWriteModel(learningGoal));
            await _unitOfWork.SessionSaveChangesAsync();
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select learning goals
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<LearningGoalsSelectResponse> SelectLearningGoalsAsync(LearningGoalsSelectQuery request)
    {
        var response = new LearningGoalsSelectResponse { Success = false};
        
        string cacheKey = "learning_goals:all";
        
        // Get majors from cache or database
        var majors = await _learningGoalQueryRepository.GetOrSetListAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _learningGoalQueryRepository.ToListAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (!majors.Any())
        {
            response.SetMessage(MessageId.E00000, CommonMessages.MajorsNotFound);
            return response;
        }
        
        var responseEntity = majors.Select(x => new LearningGoalsSelectResponseEntity
        {
            GoalId = x.GoalId,
            GoalName = x.GoalName,
            Description = x.Description
        }).ToList();
        
        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy danh sách mục tiêu học tập");
        return response;
    }
}