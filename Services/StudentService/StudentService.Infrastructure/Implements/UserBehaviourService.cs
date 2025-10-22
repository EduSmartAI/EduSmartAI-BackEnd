using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.UserBehaviours.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class UserBehaviourService : IUserBehaviourService
{
    private readonly ICommandRepository<UserBehaviour> _commandRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    public UserBehaviourService(
        ICommandRepository<UserBehaviour> commandRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserBehaviourInsertResponse> InsertUserBehaviourAsync(
        UserBehaviourInsertCommand request, 
        CancellationToken cancellationToken)
    {
        var response = new UserBehaviourInsertResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var currentUser = _identityService.GetCurrentUser();

            // Create new user behaviour
            var userBehaviour = new UserBehaviour
            {
                Id = Guid.NewGuid(),
                StudentId = currentUser!.UserId,
                ActionType = request.ActionType.ToString(),
                TargetId = request.TargetId,
                TargetType = request.TargetType.ToString(),
                Metadata = request.Metadata
            };

            await _commandRepository.AddAsync(userBehaviour, currentUser.Email);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Store in read model (Marten)
            var userBehaviourCollection = UserBehaviourCollection.FromWriteModel(userBehaviour);
            _unitOfWork.Store(userBehaviourCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync(CacheKey.UserBehaviours(currentUser!.UserId));

            // Set response
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Ghi nhận hành vi người dùng");
            return true;
        }, cancellationToken);

        return response;
    }
}

