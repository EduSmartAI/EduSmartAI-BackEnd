using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
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

    public async Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningPathInsertResponse { Success = false };

        //var currentUserEmail = _identityService.GetCurrentUser()!.Email;

        //var studentId = _identityService.GetCurrentUser()!.UserId;


        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new learning path
            var learningPath = new LearningPath
            {
                PathId = request.PathId,
                PathName = "Lộ trình FullStack",
                StudentId = Guid.Parse("dc83f7bf-970c-4e5d-92d7-562e1186d352"),
            };
            await _learningPathCommandRepository.AddAsync(learningPath, "pafevi5206@ishense.com");
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _unitOfWork.Store(LearningPathCollection.FromWriteModel(learningPath));
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync("learning_goals:all");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }
}