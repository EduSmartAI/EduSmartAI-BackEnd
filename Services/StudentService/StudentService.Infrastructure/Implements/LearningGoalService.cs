using BaseService.Application.Common;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService.LearningGoalSelectsEvents;
using StudentService.Application.Applications.LearningGoals.Commands;
using StudentService.Application.Applications.LearningGoals.Queries;
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
                LearningGoalType = (short) request.LearningGoalType,
            };
            await _learningGoalCommandRepository.AddAsync(learningGoal, currentUserEmail);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            _unitOfWork.Store(LearningGoalCollection.FromWriteModel(learningGoal));
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningGoalSelects());
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Update existing learning goal
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningGoalUpdateResponse> UpdateLearningGoalAsync(LearningGoalUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningGoalUpdateResponse { Success = false };
        
        var currentUserEmail = _identityService.GetCurrentUser()!.Email;
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check if learning goal exists
            var learningGoal = await _learningGoalCommandRepository.FirstOrDefaultAsync(x => x.GoalId == request.GoalId, cancellationToken);
            if (learningGoal == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy mục tiêu học tập");
                return false;
            }
            
            // Update learning goal
            learningGoal.GoalName = request.GoalName;
            learningGoal.Description = request.Description;
            learningGoal.LearningGoalType = (short) request.LearningGoalType;
            
            _learningGoalCommandRepository.Update(learningGoal);
            await _unitOfWork.SaveChangesAsync(currentUserEmail, cancellationToken);

            // Update read model
            var learningGoalCollection = await _learningGoalQueryRepository.FirstOrDefaultAsync(x => x.GoalId == request.GoalId);
            if (learningGoalCollection != null)
            {
                learningGoalCollection.GoalName = request.GoalName;
                learningGoalCollection.Description = request.Description;
                learningGoalCollection.LearningGoalType = (short) request.LearningGoalType;
                learningGoalCollection.UpdatedAt = learningGoal.UpdatedAt;
                learningGoalCollection.UpdatedBy = learningGoal.UpdatedBy;
                
                _unitOfWork.Store(learningGoalCollection);
                await _unitOfWork.SessionSaveChangesAsync();
            }
            
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningGoalSelects());
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Delete learning goal (logical delete)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningGoalDeleteResponse> DeleteLearningGoalAsync(LearningGoalDeleteCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningGoalDeleteResponse { Success = false };
        
        var currentUserEmail = _identityService.GetCurrentUser()!.Email;
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check if learning goal exists
            var learningGoal = await _learningGoalCommandRepository.FirstOrDefaultAsync(x => x.GoalId == request.GoalId, cancellationToken);
            if (learningGoal == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy mục tiêu học tập");
                return false;
            }
            
            // Logical delete
            _learningGoalCommandRepository.Update(learningGoal);
            await _unitOfWork.SaveChangesAsync(currentUserEmail, cancellationToken, needLogicalDelete: true);

            // Delete from read model
            var learningGoalCollection = await _learningGoalQueryRepository.FirstOrDefaultAsync(x => x.GoalId == request.GoalId);
            if (learningGoalCollection != null)
            {
                _unitOfWork.Delete(learningGoalCollection);
                await _unitOfWork.SessionSaveChangesAsync();
            }
            
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningGoalSelects());
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xóa mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select learning goals
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<LearningGoalSelectsEventResponse> SelectLearningGoalsAsync(LearningGoalSelectsQuery request)
    {
        var response = new LearningGoalSelectsEventResponse { Success = false};
        
        string cacheKey = CacheKey.LearningGoalSelects();
        
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
        
        var responseEntity = majors.Select(x => new LearningGoalSelectsEventResponseEntity
        {
            LearningGoalId = x.GoalId,
            LearningGoalName = x.GoalName,
            LearningGoalType = x.LearningGoalType
        }).ToList();
        
        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy danh sách mục tiêu học tập");
        return response;
    }

    /// <summary>
    /// Select learning goals for admin with pagination
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<AdminLearningGoalsSelectResponse> SelectAdminLearningGoalsAsync(AdminLearningGoalsSelectQuery request)
    {
        var response = new AdminLearningGoalsSelectResponse { Success = false };
        
        // Build query
        var query = await _learningGoalQueryRepository.ToListAsync();
        
        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x => x.GoalName.Contains(request.SearchTerm) || (x.Description != null && x.Description.Contains(request.SearchTerm))).ToList();
        }
        
        if (request.LearningGoalType.HasValue)
        {
            query = query.Where(x => x.LearningGoalType == request.LearningGoalType.Value).ToList();
        }
        
        // Get total count
        var totalCount = query.Count;
        
        // Apply pagination
        var learningGoals = query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();
        
        if (!learningGoals.Any() && request.PageNumber == 1)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy mục tiêu học tập nào");
            return response;
        }
        
        // Map to response
        var items = learningGoals.Select(x => new AdminLearningGoalItem
        {
            GoalId = x.GoalId,
            GoalName = x.GoalName,
            Description = x.Description,
            LearningGoalType = x.LearningGoalType,
            CreatedAt = x.CreatedAt,
        }).ToList();
        
        response.Response = new PagedResult<AdminLearningGoalItem>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        
        response.Success = true;
        response.SetMessage(MessageId.I00001, "Lấy danh sách mục tiêu học tập");
        return response;
    }
}