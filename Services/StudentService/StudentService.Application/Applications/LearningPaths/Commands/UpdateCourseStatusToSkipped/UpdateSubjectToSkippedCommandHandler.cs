using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;

public class UpdateSubjectToSkippedCommandHandler : ICommandHandler<UpdateSubjectToSkippedCommand, UpdateSubjectToSkippedCommandResponse>
{
    private readonly IIdentityService _identityService;
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<Domain.WriteModels.LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeCommandRepository;
    private readonly ILearningPathService _learningPathService;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSubjectToSkippedCommandHandler(IIdentityService identityService, IUnitOfWork unitOfWork, ICommandRepository<LearningPath> learningPathCommandRepository, ICommandRepository<Domain.WriteModels.LearningPathCourse> learningPathCourseCommandRepository, ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeCommandRepository, ILearningPathService learningPathService)
    {
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _learningPathSubjectCodeCommandRepository = learningPathSubjectCodeCommandRepository;
        _learningPathService = learningPathService;
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
            
        // Load subject codes separately for all majors
        var majorIds = learningPaths
            .Where(lp => lp != null)
            .SelectMany(lp => lp!.LearningPathMajors ?? Enumerable.Empty<LearningPathMajor>())
            .Where(m => m != null && m.IsActive && (m.Type == (short)ConstantEnum.LearningPathMajor.Internal || m.Type == (short)ConstantEnum.LearningPathMajor.Basic))
            .Select(m => m!.LearningPathMajorId)
            .ToList();
            
        var subjectCodes = await _learningPathSubjectCodeCommandRepository
            .Find(sc => majorIds.Contains(sc.LearningPathMajorId) && sc.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken: cancellationToken);
            
        // Attach subject codes to their majors
        foreach (var learningPath in learningPaths)
        {
            if (learningPath?.LearningPathMajors == null) continue;
            foreach (var major in learningPath.LearningPathMajors.Where(m => m != null && m.IsActive))
            {
                if (major == null) continue;
                major.LearningPathSubjectCodes = subjectCodes
                    .Where(sc => sc != null && sc.LearningPathMajorId == major.LearningPathMajorId)
                    .Cast<LearningPathSubjectCode>()
                    .ToList();
            }
        }
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var updatedCourseIds = new List<Guid>();
            var updatedSubjectCodeIds = new List<Guid>();
            var skippedStatusDescription = ConstantEnum.SubjectImprovementStatus.Skipped.GetDescription();
            
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
                        // Update courses
                        var learningPathCoursesToSkip = learningPathMajor.LearningPathCourses
                            .Where(c => c.IsActive && c.SubjectCode == subjectCode)
                            .ToList();
                        foreach (var course in learningPathCoursesToSkip)
                        {
                            if (course.Status != (short)ConstantEnum.SubjectImprovementStatus.Skipped)
                            {
                                course.Status = (short)ConstantEnum.SubjectImprovementStatus.Skipped;
                                updatedCourseIds.Add(course.LearningPathCourseId);
                                _learningPathCourseCommandRepository.Update(course);
                            }
                        }
                        
                        // Update subject codes
                        var learningPathSubjectCodesToSkip = learningPathMajor.LearningPathSubjectCodes
                            .Where(sc => sc.IsActive && sc.SubjectCode == subjectCode)
                            .ToList();
                        foreach (var subjectCodeEntity in learningPathSubjectCodesToSkip)
                        {
                            if (subjectCodeEntity.Status != skippedStatusDescription)
                            {
                                subjectCodeEntity.Status = skippedStatusDescription;
                                updatedSubjectCodeIds.Add(subjectCodeEntity.LearningPathSubjectCodeId);
                                _learningPathSubjectCodeCommandRepository.Update(subjectCodeEntity);
                            }
                        }
                    }
                }
            }
            if (!updatedCourseIds.Any() && !updatedSubjectCodeIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Tất cả khóa học và môn học có mã môn này đã được skip trước đó");
                return false;
            }
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

            // 6. Sync read model using UpdateStatusLearningPathReadModelByIdAndSortPosition
            var pathIds = learningPaths.Where(p => p != null).Select(p => p!.PathId).Distinct().ToList();
            foreach (var pathId in pathIds)
            {
                var syncRequest = new UpdateReadModelLearningPathCommand
                {
                    LearningPathId = pathId
                };
                await _learningPathService.UpdateStatusLearningPathReadModelByIdAndSortPosition(syncRequest, cancellationToken);
            }

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật trạng thái khóa học và môn học");
            return true;
        }, cancellationToken);

        return response;
    }
}