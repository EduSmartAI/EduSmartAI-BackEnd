using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using LearningPathMajor = StudentService.Domain.WriteModels.LearningPathMajor;

namespace StudentService.Infrastructure.Implements;

public class LearningPathService : ILearningPathService
{
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<LearningPathMajor> _learningPathMajorCommandRepository;
    private readonly ICommandRepository<LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly IRequestClient<CoursesSelectEvent> _requestClientCoursesSelectEvent;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="learningPathMajorCommandRepository"></param>
    /// <param name="learningPathCommandRepository"></param>
    /// <param name="learningPathCourseCommandRepository"></param>
    /// <param name="requestClientCoursesSelectEvent"></param>
    public LearningPathService(IUnitOfWork unitOfWork,
        ICommandRepository<LearningPathMajor> learningPathMajorCommandRepository,
        ICommandRepository<LearningPath> learningPathCommandRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseCommandRepository, 
        IRequestClient<CoursesSelectEvent> requestClientCoursesSelectEvent)
    {
        _unitOfWork = unitOfWork;
        _learningPathMajorCommandRepository = learningPathMajorCommandRepository;
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _requestClientCoursesSelectEvent = requestClientCoursesSelectEvent;
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
            
            await _learningPathCommandRepository.AddAsync(learningPath, request.StudentEmail);
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

    public async Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(
        InsertLearningPathsMajorCommand request, CancellationToken cancellationToken)
    {
        var response = new InsertLearningPathsMajorResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1) Insert Major
            var major = new LearningPathMajor
            {
                LearningPathMajorId = Guid.NewGuid(),
                PathId = request.PathId,
                MajorCode = request.MajorCode.Trim(),
                Reason = request.Reason,
                Type = (short) ConstantEnum.LearningPathMajor.External,
            };

            await _learningPathMajorCommandRepository.AddAsync(major, request.CurrentUserEmail!);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 2) Insert Courses
            if (request.Courses != null)
            {
                foreach (var step in request.Courses)
                {
                    var stepOrder = step.Order > 0 ? step.Order : (int?)null;
                    var courses = step.SuggestedCourses;

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

    /// <summary>
    /// Insert learning path major internal
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="Exception"></exception>
    /// <returns></returns>
    public async Task<LearningPathMajorInternalInsertResponse> InsertLearningPathMajorAsync(LearningPathMajorInsertCommand request, CancellationToken cancellationToken = default)
    {
        var response = new LearningPathMajorInternalInsertResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(
                x => x.PathId == request.LearningPathId && x.IsActive, cancellationToken: cancellationToken);
            if (learningPath == null)
            {
                throw new Exception("Lộ trình học tập không tồn tại");
            }
            
            // Send message to CourseService to get courses response
            var coursesSelectEventRequest = new CoursesSelectEvent
            {
                MajorCodes = request.Majors.Select(x => x.MajorCode).ToList(),
                SemesterId = request.SemesterId,
                LimitTime = request.LimitTime * 60,
                StudentLevel = request.StudentLevel
            };
            var courseSelectEvent = await _requestClientCoursesSelectEvent.GetResponse<CoursesSelectEventResponse>(coursesSelectEventRequest, cancellationToken);
            
            // Insert new learning path major
            var learningPathMajors = request.Majors.Select(x =>
            {
                var matchedCourses = courseSelectEvent
                    .Message
                    .Response
                    .FirstOrDefault(r => r.MajorCode == x.MajorCode);

                return new LearningPathMajor
                {
                    LearningPathMajorId = Guid.NewGuid(),
                    PathId = request.LearningPathId,
                    MajorCode = x.MajorCode,
                    Reason = x.Reason,
                    Type = x.MajorCode == "SE" 
                        ? (short) ConstantEnum.LearningPathMajor.Basic 
                        : request.MajorType,
                    LearningPathCourses = matchedCourses?.CourseCodeIds
                        .Select(courseId => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            InternalCourseId = courseId
                        }).ToList() ?? new List<LearningPathCourse>()
                };
            }).ToList();

            // Insert majors first
            await _learningPathMajorCommandRepository.AddRangeAsync(learningPathMajors);
            
            // Insert courses separately
            var allCourses = learningPathMajors
                .SelectMany(m => m.LearningPathCourses.Select(c =>
                {
                    c.LearningPathMajorId = m.LearningPathMajorId; // Set foreign key
                    return c;
                }))
                .ToList();
            
            if (allCourses.Any())
            {
                await _learningPathCourseCommandRepository.AddRangeAsync(allCourses);
            }
            
            await _unitOfWork.SaveChangesAsync(learningPath.CreatedBy, cancellationToken);

            foreach (var learningPathMajorCollection in learningPathMajors.Select(LearningPathMajorCollection.FromWriteModel).ToList())
            {
                _unitOfWork.Store(learningPathMajorCollection);
            }
            await _unitOfWork.SessionSaveChangesAsync();

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm chuyên ngành vào lộ trình học tập");
            return true;
        }, cancellationToken);

        return response;
    }
}