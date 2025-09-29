using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Infrastructure.Implements;

public class LearningPathService : ILearningPathService
{
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<LearningPathMajor> _learningPathMajorCommandRepository;
    private readonly ICommandRepository<LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="learningGoalCommandRepository"></param>
    /// <param name="learningGoalQueryRepository"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="identityService"></param>
    public LearningPathService(
        ICommandRepository<LearningPath> learningPathCommandRepository,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ICommandRepository<LearningPathMajor> learningPathMajorCommandRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseCommandRepository
        )
    {
        _learningPathCommandRepository = learningPathCommandRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _learningPathMajorCommandRepository = learningPathMajorCommandRepository;
    }

    public async Task<LearningPathInsertResponse> InsertLearningPathAsync(LearningPathInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningPathInsertResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new learning path
            var learningPath = new LearningPath
            {
                PathId = request.PathId,
                PathName = request.PathName,
                StudentId = request.StudentId,
            };
            await _learningPathCommandRepository.AddAsync(learningPath, request.CurrentUserEmail);
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
    /// <summary>
    /// Insert External Major and Insert Their Course
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(
        InsertLearningPathsMajorCommand request,
        CancellationToken cancellationToken)
    {
        var response = new InsertLearningPathsMajorResponse { Success = false };

        // Validate
        if (request.PathId == Guid.Empty || string.IsNullOrWhiteSpace(request.MajorCode))
        {
            response.Success = false;
            response.SetMessage(MessageId.E00000, "Thiếu PathId hoặc MajorCode");
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1) Insert Major
            var major = new LearningPathMajor
            {
                LearningPathMajorId = Guid.NewGuid(),
                PathId = request.PathId,
                MajorCode = request.MajorCode.Trim(),
                Reason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason,
                IsActive = true,
                Type = (short)LearningPathMajorEnum.External,
            };

            await _learningPathMajorCommandRepository.AddAsync(major, request.CurrentUserEmail!);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 2) Insert Courses
            if (request.Courses != null)
            {
                foreach (var step in request.Courses)
                {
                    var stepOrder = step.Order > 0 ? step.Order : (int?)null;
                    var courses = step.SuggestedCourses ?? [];

                    foreach (var sc in courses)
                    {
                        var course = new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            LearningPathMajorId = major.LearningPathMajorId,
                            Position = stepOrder,
                            StepName = step.Title,
                            ExternalCourseLink = sc.Link,
                            ExternalCourseReason = sc.Reason,
                            ExternalCourseDuration = sc.Duration,
                            ExternalCourseLevel = sc.Level,
                            ExternalCourseProvider = sc.Provider
                        };

                        await _learningPathCourseCommandRepository.AddAsync(course, request.CurrentUserEmail!);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            // 3) Update read-model / cache (eventual consistency)
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync($"learning_path:{request.PathId}");
            await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{request.PathId}");

            // 4) Done
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm ngành và học phần vào lộ trình");

            return true;
        }, cancellationToken);

        return response;
    }
}