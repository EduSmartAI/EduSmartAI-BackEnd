using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateSubjectToSkippedCommandHandler : ICommandHandler<UpdateSubjectToSkippedCommand, UpdateSubjectToSkippedCommandResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<Domain.WriteModels.LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly IQueryRepository<LearningPathCollection> _learningPathQueryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSubjectToSkippedCommandHandler(IIdentityService identityService, IUnitOfWork unitOfWork, ICommandRepository<LearningPath> learningPathCommandRepository, IQueryRepository<LearningPathCollection> learningPathQueryRepository, ICommandRepository<Domain.WriteModels.LearningPathCourse> learningPathCourseCommandRepository)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
    }

    public async Task<UpdateSubjectToSkippedCommandResponse> Handle(UpdateSubjectToSkippedCommand request, CancellationToken cancellationToken)
    {
        var response = new UpdateSubjectToSkippedCommandResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser()!;

        // 1. Find learning path of student
        var learningPaths = await _learningPathCommandRepository
            .Find(predicate: x => x.StudentId == currentUser.UserId && x.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken,
                include: x => x.Include(lp => lp.LearningPathMajors)
                    .ThenInclude(m => m.LearningPathCourses))
            .ToListAsync(cancellationToken: cancellationToken);

        if (!learningPaths.Any())
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy lộ trình học của bạn");
            return response;
        }
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var updatedCourseIds = new List<Guid>();
            foreach (var learningPath in learningPaths)
            {
                var learningPathMajors = learningPath!
                    .LearningPathMajors
                    .Where(x => x.IsActive 
                    && (x.Type == (short) ConstantEnum.LearningPathMajor.Internal) || 
                    x.Type == (short) ConstantEnum.LearningPathMajor.Basic).ToList();
                foreach (var learningPathMajor in learningPathMajors)
                {
                    // Loop through all SubjectCodes in the request
                    foreach (var subjectCode in request.SubjectCode)
                    {
                        var learningPathCoursesToSkip = learningPathMajor.LearningPathCourses
                            .Where(c => c.IsActive && c.SubjectCode == subjectCode)
                            .ToList();
                        foreach (var course in learningPathCoursesToSkip)
                        {
                            if (course.Status != (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped)
                            {
                                course.Status = (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                                updatedCourseIds.Add(course.LearningPathCourseId);
                                _learningPathCourseCommandRepository.Update(course);
                            }
                        }
                    }
                }
            }
            if (!updatedCourseIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Tất cả khóa học có mã môn này đã được skip trước đó");
                return false;
            }
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // 6. Update read model
            // Extract PathIds to a simple list to avoid complex LINQ expression
            var pathIds = learningPaths.Select(p => p.PathId).ToList();
            
            // Query each PathId individually since Marten cannot handle complex Contains with Select
            var learningPathCollections = new List<LearningPathCollection>();
            foreach (var pathId in pathIds)
            {
                var pathCollection = await _learningPathQueryRepository
                    .FirstOrDefaultAsync(pc => pc.PathId == pathId);
                if (pathCollection != null)
                {
                    learningPathCollections.Add(pathCollection);
                }
            }

            foreach (var learningPathCollection in learningPathCollections)
            {
                var learningPathMajors = learningPathCollection!
                    .LearningPathMajors
                    .Where(x => x.IsActive 
                                && (x.Type == (short) ConstantEnum.LearningPathMajor.Internal) || 
                                x.Type == (short) ConstantEnum.LearningPathMajor.Basic).ToList();

                foreach (var major in learningPathMajors)
                {
                    // Loop through all SubjectCodes in the request
                    foreach (var subjectCode in request.SubjectCode)
                    {
                        var learningPathCoursesToSkip = major.LearningPathCourses
                            .Where(c => c.IsActive && c.SubjectCode == subjectCode)
                            .ToList();
                        foreach (var course in learningPathCoursesToSkip)
                        {
                            if (updatedCourseIds.Contains(course.LearningPathCourseId))
                            {
                                course.Status = (short) ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                            }
                        }
                    }
                }

                _unitOfWork.Store(learningPathCollection);
                await _unitOfWork.SessionSaveChangesAsync();

                var lpIdStr = learningPathCollection.PathId.ToString("D");
                await _unitOfWork.CacheRemoveAsync($"learning_path:select:{currentUser.UserId}:{lpIdStr}");
                await _unitOfWork.CacheRemoveAsync($"learning_path:{lpIdStr}");
                await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{lpIdStr}");
            }

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật trạng thái khóa học");
            return true;
        }, cancellationToken);

        return response;
    }
}