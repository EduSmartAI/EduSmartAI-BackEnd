using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using BuildingBlocks.Pagination;
using MapsterMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateLearningPathStatus;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertBatchLearningPathsMajor;
using StudentService.Application.Applications.LearningPathsMajor.Commands.InsertLearningPathsMajor;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements;

public class LearningPathService : ILearningPathService
{
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<LearningPathMajor> _learningPathMajorCommandRepository;
    private readonly ICommandRepository<LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeCommandRepository;
    private readonly IRequestClient<CoursesSelectEvent> _requestClientCoursesSelectEvent;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryRepository<LearningPathCollection> _learningPathQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<GetInfoInternalCourseEvents> _requestClient;
    private readonly IRequestClient<CourseSelectsBySubjectCodeEvent> _requestClientCourseSelectsBySubjectCodeEvent;
    private readonly IMapper _mapper;
    private readonly ILearningPathRealtimeNotifier _learningPathRealtimeNotifier;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="unitOfWork"></param>
    /// <param name="learningPathMajorCommandRepository"></param>
    /// <param name="learningPathCommandRepository"></param>
    /// <param name="learningPathCourseCommandRepository"></param>
    /// <param name="requestClientCoursesSelectEvent"></param>
    /// <param name="learningPathQueryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="requestClient"></param>
    /// <param name="mapper"></param>
    /// <param name="requestClientCourseSelectsBySubjectCodeEvent"></param>
    public LearningPathService(IUnitOfWork unitOfWork,
        ICommandRepository<LearningPathMajor> learningPathMajorCommandRepository,
        ICommandRepository<LearningPath> learningPathCommandRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseCommandRepository,
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeCommandRepository,
        IRequestClient<CoursesSelectEvent> requestClientCoursesSelectEvent,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IIdentityService identityService,
        IRequestClient<GetInfoInternalCourseEvents> requestClient,
        IMapper mapper,
        IRequestClient<CourseSelectsBySubjectCodeEvent> requestClientCourseSelectsBySubjectCodeEvent,
        ILearningPathRealtimeNotifier learningPathRealtimeNotifier)
    {
        _unitOfWork = unitOfWork;
        _learningPathMajorCommandRepository = learningPathMajorCommandRepository;
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _learningPathSubjectCodeCommandRepository = learningPathSubjectCodeCommandRepository;
        _requestClientCoursesSelectEvent = requestClientCoursesSelectEvent;
        _learningPathQueryRepository = learningPathQueryRepository;
        _identityService = identityService;
        _requestClient = requestClient;
        _mapper = mapper;
        _requestClientCourseSelectsBySubjectCodeEvent = requestClientCourseSelectsBySubjectCodeEvent;
        _learningPathRealtimeNotifier = learningPathRealtimeNotifier;
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
                Status = (short) ConstantEnum.LearningPathStatus.Generating,
            };

            await _learningPathCommandRepository.AddAsync(learningPath, request.StudentEmail);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _unitOfWork.Store(LearningPathCollection.FromWriteModel(learningPath));
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningGoalSelects());

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm mục tiêu học tập");
            return true;
        }, cancellationToken);
        return response;
    }

    public async Task<LearningPathRenameResponse> RenameLearningPathAsync(LearningPathRenameCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningPathRenameResponse { Success = false };

        if (request.LearningPathId == Guid.Empty)
        {
            response.SetMessage(MessageId.E00000, "Thiếu hoặc sai LearningPathId.");
            return response;
        }

        if (string.IsNullOrWhiteSpace(request.PathName))
        {
            response.SetMessage(MessageId.E00000, "Tên lộ trình không được bỏ trống.");
            return response;
        }

        var identity = _identityService.GetCurrentUser();
        var studentId = request.StudentId != Guid.Empty
            ? request.StudentId
            : identity?.UserId ?? Guid.Empty;

        if (studentId == Guid.Empty)
        {
            response.SetMessage(MessageId.E00000, "Không xác định được sinh viên hiện tại.");
            return response;
        }

        var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(
            x => x.PathId == request.LearningPathId && x.IsActive,
            cancellationToken: cancellationToken);

        if (learningPath == null)
        {
            response.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại.");
            return response;
        }

        if (learningPath.StudentId.HasValue && learningPath.StudentId != studentId)
        {
            response.SetMessage(MessageId.E00000, "Bạn không thể chỉnh sửa lộ trình của người khác.");
            return response;
        }

        var newName = request.PathName.Trim();
        var hasChanged = !string.Equals(learningPath.PathName, newName, StringComparison.Ordinal);

        if (hasChanged)
        {
            learningPath.PathName = newName;
            learningPath.UpdatedAt = DateTime.UtcNow;
            learningPath.UpdatedBy = !string.IsNullOrWhiteSpace(request.StudentEmail)
                ? request.StudentEmail
                : identity?.Email ?? learningPath.UpdatedBy;

            _learningPathCommandRepository.Update(learningPath);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        await SyncLearningPathReadModelByIdAsync(learningPath.PathId, studentId, cancellationToken);
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(learningPath.PathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(learningPath.PathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(studentId, learningPath.PathId));

        response.Success = true;
        response.Response = learningPath.PathId.ToString();
        response.SetMessage(MessageId.I00001, "Đổi tên lộ trình học tập thành công.");
        return response;
    }
    /// <summary>
    /// Insert external and major to Learning Path
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<InsertLearningPathsMajorResponse> InsertLearningPathMajorCourseAsync(
        InsertLearningPathsMajorCommand request, CancellationToken cancellationToken)
    {
        var response = new InsertLearningPathsMajorResponse { Success = false };

        var learningPathCollection = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == request.PathId && x.IsActive);
        if (learningPathCollection == null)
        {
            throw new Exception("Lộ trình học tập không tồn tại");
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1) Insert Major
            var major = new LearningPathMajor
            {
                LearningPathMajorId = Guid.NewGuid(),
                PathId = request.PathId,
                MajorCode = request.MajorCode.Trim(),
                Reason = request.Reason,
                Type = (short)ConstantEnum.LearningPathMajor.External,
            };

            await _learningPathMajorCommandRepository.AddAsync(major, request.CurrentUserEmail!);

            // 2) Insert Courses
            var newCourses = new List<LearningPathCourse>();
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
                            Status = (short)ConstantEnum.StudentLearningPathCourseStatus.NotStarted,
                            Position = stepOrder,
                            StepName = step.Title,
                            ExternalCourseLink = sc.Link,
                            ExternalCourseReason = sc.Reason,
                            ExternalCourseDuration = sc.Duration,
                            ExternalCourseLevel = sc.Level,
                            ExternalCourseProvider = sc.Provider
                        };
                        newCourses.Add(course);
                        await _learningPathCourseCommandRepository.AddAsync(course);
                    }
                }

                await _unitOfWork.SaveChangesAsync(request.CurrentUserEmail!, cancellationToken);
            }
            major.LearningPathCourses = newCourses;

            // 3) Update read-model / cache (eventual consistency)
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));

            learningPathCollection.LearningPathMajors.Add(LearningPathMajorCollection.FromWriteModel(major));
            _unitOfWork.Store(learningPathCollection);

            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(request.PathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(request.PathId));

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
            var learningPath = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == request.LearningPathId && x.IsActive);
            if (learningPath == null)
            {
                throw new Exception($"Lộ trình học tập không tồn tại");
            }

            // Send message to CourseService to get courses response
            var coursesSelectEventRequest = new CoursesSelectEvent
            {
                MajorCodes = request.Majors.Select(x => x.MajorCode).ToList(),
                SemesterId = request.SemesterId,
                LimitTime = request.LimitTime * 60,
                StudentLevel = request.StudentLevel,
                StudentPassedSubjects = request.StudentPassedSubjects,
                CourseImproves = request.CourseImproves?.Select(x => new BuildingBlocks.Messaging.Events.QuizService.CourseImprove
                {
                    SubjectCode = x.SubjectCode,
                    SubjectPrerequisiteCode = x.SubjectPrerequisiteCode,
                    Level = x.Level
                }).ToList(),
                StudentTranscriptSelectEvent = request.StudentTranscripts
            };
            var courseSelectEvent = await _requestClientCoursesSelectEvent.GetResponse<CoursesSelectEventResponse>(coursesSelectEventRequest, cancellationToken);

            // Check if response contains "SE" major
            var hasSeInResponse = courseSelectEvent.Message.Response.Any(r => r.MajorCode == "SE");
            var hasSeInRequest = request.Majors.Any(m => m.MajorCode == "SE");

            // Insert new learning path major from request
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
                    LearningPathCourses = matchedCourses?.Courses
                        .Select(course => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            InternalCourseId = course.CourseId,
                            Status = (short) ConstantEnum.StudentLearningPathCourseStatus.NotStarted,
                            SubjectCode = course.SubjectCode,
                        }).ToList() ?? new List<LearningPathCourse>()
                };
            }).ToList();

            // If SE exists in response but not in request, add it automatically with Type = Basic
            if (hasSeInResponse && !hasSeInRequest)
            {
                var seCourses = courseSelectEvent.Message.Response.FirstOrDefault(r => r.MajorCode == "SE");

                var seMajor = new LearningPathMajor
                {
                    LearningPathMajorId = Guid.NewGuid(),
                    PathId = request.LearningPathId,
                    MajorCode = "SE",
                    Reason = "Chuyên ngành cơ bản cho các sinh viên dưới kỳ 4 theo học Software Engineering",
                    Type = (short) ConstantEnum.LearningPathMajor.Basic,
                    LearningPathCourses = seCourses!.Courses
                        .Select(course => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            InternalCourseId = course.CourseId,
                            Status = (short)ConstantEnum.StudentLearningPathCourseStatus.NotStarted,
                            SubjectCode = course.SubjectCode
                        }).ToList()
                };

                learningPathMajors.Add(seMajor);
            }

            // Insert majors first
            await _learningPathMajorCommandRepository.AddRangeAsync(learningPathMajors);

            // Insert courses separately
            var allCourses = learningPathMajors
                .SelectMany(m => m.LearningPathCourses.Select(c =>
                {
                    c.LearningPathMajorId = m.LearningPathMajorId;
                    return c;
                }))
                .ToList();

            if (allCourses.Any())
            {
                await _learningPathCourseCommandRepository.AddRangeAsync(allCourses);
            }
            
            await _unitOfWork.SaveChangesAsync(learningPath.CreatedBy, cancellationToken);

            // Ensure LearningPathCourses are properly set BEFORE storing to read-model
            foreach (var major in learningPathMajors)
            {
                major.LearningPathCourses = allCourses
                    .Where(c => c.LearningPathMajorId == major.LearningPathMajorId)
                    .ToList();
                if (major.MajorCode == request.StudentMajor.MajorCode)
                {
                    response.StudentMajorId = major.LearningPathMajorId;
                }
            }

            foreach (var major in learningPathMajors)
            {
                _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            }

            learningPath.LearningPathMajors.AddRange(learningPathMajors.Select(LearningPathMajorCollection.FromWriteModel));

            _unitOfWork.Store(learningPath);

            await _unitOfWork.SessionSaveChangesAsync();
            await PublishLearningPathSnapshotAsync(learningPath.PathId, learningPath.StudentId, cancellationToken);

            // Create dictionary to map MajorCode to MajorName from courseSelectEvent
            var majorNameDictionary = courseSelectEvent.Message.Response
                .ToDictionary(r => r.MajorCode, r => r.MajorName);

            response.Majors = learningPathMajors.Select(x => new MajorInternalInsertResponse
            {
                LearningPathMajorId = x.LearningPathMajorId,
                MajorCode = x.MajorCode,
                MajorName = majorNameDictionary.TryGetValue(x.MajorCode, out var majorName) ? majorName : x.MajorCode,
            }).ToList();
            response.StudentCurriculums = courseSelectEvent.Message.StudentCurriculums;
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm chuyên ngành vào lộ trình học tập");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Insert batch learning path majors with courses (External majors)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<InsertBatchLearningPathsMajorResponse> InsertBatchLearningPathMajorCourseAsync(InsertBatchLearningPathsMajorCommand request, CancellationToken cancellationToken)
    {
        var response = new InsertBatchLearningPathsMajorResponse { Success = false };

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var learningPath = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == request.PathId && x.IsActive);
            if (learningPath == null)
            {
                response.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại");
                return false;
            }

            var allMajors = new List<LearningPathMajor>();
            var allCourses = new List<LearningPathCourse>();

            // Process each major
            foreach (var majorItem in request.Majors)
            {
                var major = new LearningPathMajor
                {
                    LearningPathMajorId = Guid.NewGuid(),
                    PathId = request.PathId,
                    MajorCode = majorItem.MajorCode.Trim(),
                    Reason = majorItem.Reason,
                    Type = (short) ConstantEnum.LearningPathMajor.External,
                };

                allMajors.Add(major);

                // Process courses for this major
                if (majorItem.Steps != null && majorItem.Steps.Any())
                {
                    foreach (var step in majorItem.Steps)
                    {
                        var stepOrder = step.Order > 0 ? step.Order : (int?)null;
                        var courses = step.SuggestedCourses;

                        foreach (var sc in courses)
                        {
                            var course = new LearningPathCourse
                            {
                                LearningPathCourseId = Guid.NewGuid(),
                                LearningPathMajorId = major.LearningPathMajorId,
                                Status = (short)ConstantEnum.StudentLearningPathCourseStatus.NotStarted,
                                Position = stepOrder,
                                StepName = step.Title,
                                ExternalCourseLink = sc.Link,
                                ExternalCourseReason = sc.Reason,
                                ExternalCourseDuration = sc.Duration,
                                ExternalCourseLevel = sc.Level,
                                ExternalCourseProvider = sc.Provider
                            };

                            allCourses.Add(course);
                        }
                    }
                }
            }

            // Insert all majors at once
            if (allMajors.Any())
            {
                await _learningPathMajorCommandRepository.AddRangeAsync(allMajors);
            }

            // Insert all courses at once
            if (allCourses.Any())
            {
                await _learningPathCourseCommandRepository.AddRangeAsync(allCourses);
            }

            await _unitOfWork.SaveChangesAsync(request.CurrentUserEmail, cancellationToken);

            foreach (var major in allMajors)
            {
                major.LearningPathCourses = allCourses
                    .Where(c => c.LearningPathMajorId == major.LearningPathMajorId)
                    .ToList();
            }

            foreach (var major in allMajors)
            {
                _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            }

            learningPath.LearningPathMajors.AddRange(allMajors.Select(LearningPathMajorCollection.FromWriteModel));

            _unitOfWork.Store(learningPath);

            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(request.PathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(request.PathId));

            response.Success = true;
            response.Response = $"Đã thêm {allMajors.Count} chuyên ngành với {allCourses.Count} khóa học";
            response.SetMessage(MessageId.I00001, "Thêm hàng loạt chuyên ngành và khóa học vào lộ trình");
            return true;
        }, cancellationToken);

        return response;
    }

    public async Task<bool> UpdateLearningPathStatusAsync(Guid learningPathId, CancellationToken contextCancellationToken)
    {
        var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(x => x.PathId == learningPathId && x.IsActive, cancellationToken: contextCancellationToken);
        if (learningPath == null)
        {
            throw new Exception($"Lộ trình học tập không tồn tại");
        }

        learningPath.Status = (short)ConstantEnum.LearningPathStatus.Choosing;

        _learningPathCommandRepository.Update(learningPath);
        await _unitOfWork.SaveChangesAsync(learningPath.CreatedBy, contextCancellationToken);

        var learningPathRead = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == learningPathId && x.IsActive);
        learningPathRead!.Status = learningPath.Status;

        _unitOfWork.Store(learningPathRead);
        await _unitOfWork.SessionSaveChangesAsync();
        await PublishLearningPathSnapshotAsync(learningPathId, learningPath.StudentId, contextCancellationToken);

        return true;
    }
    /// <summary>
    /// Determines the aggregate status of a group of courses based on individual course statuses.
    /// Priority: Completed > InProgress > Skipped > NotStarted.
    /// </summary>
    /// <param name="courses"></param>
    /// <returns></returns>
    private static short AggregateGroupStatus(IEnumerable<CourseItemDto> courses)
    {
        if (courses.Any(x => x.Status == (short)ConstantEnum.StudentLearningPathCourseStatus.Completed))
            return (short)ConstantEnum.StudentLearningPathCourseStatus.Completed;

        if (courses.Any(x => x.Status == (short)ConstantEnum.StudentLearningPathCourseStatus.InProgress))
            return (short)ConstantEnum.StudentLearningPathCourseStatus.InProgress;

        if (courses.Any() && courses.All(x => x.Status == (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped))
            return (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;

        return (short)ConstantEnum.StudentLearningPathCourseStatus.NotStarted;
    }
    /// <summary>
    /// Fetch course
    /// </summary>
    /// <param name="courseIds"></param>
    /// <param name="userId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<Dictionary<Guid, InternalCourseInfoDto>> FetchCourseInfoAsync(
        List<Guid> courseIds,
        Guid userId,
        CancellationToken cancellationToken)
    {
        if (!courseIds.Any())
        {
            return new Dictionary<Guid, InternalCourseInfoDto>();
        }

        var request = new GetInfoInternalCourseEvents(courseIds, userId);
        var response = await _requestClient.GetResponse<GetInfoInternalCourseResponse>(request, cancellationToken);

        return (response.Message.Response ?? Enumerable.Empty<InternalCourseInfoDto>())
            .Where(x => x.CourseId.HasValue)
            .GroupBy(x => x.CourseId!.Value)
            .ToDictionary(g => g.Key, g => g.First());
    }
    /// <summary>
    /// Collect courseId to list
    /// </summary>
    /// <param name="subjectCodes"></param>
    /// <param name="fallbackCourses"></param>
    /// <returns></returns>
    private static List<Guid> CollectInternalCourseIds(
        IEnumerable<LearningPathSubjectCodeCollection> subjectCodes,
        IEnumerable<LearningPathCourseCollection> fallbackCourses)
    {
        return subjectCodes
            .SelectMany(sc => sc.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>())
            .Concat(fallbackCourses ?? Enumerable.Empty<LearningPathCourseCollection>())
            .Where(c => c.InternalCourseId.HasValue)
            .Select(c => c.InternalCourseId!.Value)
            .Distinct()
            .ToList();
    }

    private static List<LearningPathSubjectCodeCollection> ExtractSubjectCodesWithCourses(
        IEnumerable<LearningPathMajorCollection> majors)
    {
        var result = new List<LearningPathSubjectCodeCollection>();

        foreach (var major in majors ?? Enumerable.Empty<LearningPathMajorCollection>())
        {
            if (major == null)
            {
                continue;
            }

            var majorCourses = (major.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>())
                .Where(c => c.IsActive)
                .ToList();

            foreach (var subject in major.LearningPathSubjectCodes ?? Enumerable.Empty<LearningPathSubjectCodeCollection>())
            {
                if (subject == null || !subject.IsActive)
                {
                    continue;
                }

                var subjectCourses = subject.LearningPathCourses ?? new List<LearningPathCourseCollection>();
                if (!subjectCourses.Any())
                {
                    subjectCourses = majorCourses
                        .Where(c => c.LearningPathSubjectCodeId.HasValue &&
                                    c.LearningPathSubjectCodeId.Value == subject.LearningPathSubjectCodeId)
                        .ToList();

                    if (!subjectCourses.Any() && !string.IsNullOrWhiteSpace(subject.SubjectCode))
                    {
                        subjectCourses = majorCourses
                            .Where(c => !string.IsNullOrWhiteSpace(c.SubjectCode) &&
                                        string.Equals(c.SubjectCode, subject.SubjectCode, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }
                }

                subject.LearningPathCourses = subjectCourses;
                result.Add(subject);
            }
        }

        return result;
    }

    private List<CourseGroupDto> BuildCourseGroupsFromSubjectCodes(
        IEnumerable<LearningPathSubjectCodeCollection> subjectCodes,
        IDictionary<Guid, InternalCourseInfoDto> infoLookup)
    {
        var groups = new List<CourseGroupDto>();

        foreach (var subject in subjectCodes ?? Enumerable.Empty<LearningPathSubjectCodeCollection>())
        {
            var courseItems = new List<CourseItemDto>();
            var courses = subject.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>();

            foreach (var course in courses)
            {
                var dto = MapCourseItem(course, infoLookup, subject.SubjectCode);
                if (dto != null)
                {
                    courseItems.Add(dto);
                }
            }

            var ordered = courseItems.OrderBy(x => x.SemesterPosition).ToList();
            groups.Add(new CourseGroupDto
            {
                SubjectCode = string.IsNullOrWhiteSpace(subject.SubjectCode) ? "UNKNOWN" : subject.SubjectCode,
                AnalysisMarkdown = subject.AnalysisMarkdown,
                Status = AggregateGroupStatus(ordered),
                Courses = ordered
            });
        }

        return groups;
    }

    private List<CourseItemDto> BuildCourseItemsFromCourses(
        IEnumerable<LearningPathCourseCollection> courses,
        IDictionary<Guid, InternalCourseInfoDto> infoLookup)
    {
        var result = new List<CourseItemDto>();
        foreach (var course in courses ?? Enumerable.Empty<LearningPathCourseCollection>())
        {
            var dto = MapCourseItem(course, infoLookup, course.SubjectCode);
            if (dto != null)
            {
                result.Add(dto);
            }
        }

        return result;
    }

    private List<CourseGroupDto> BuildCourseGroupsFromItems(List<CourseItemDto> items)
    {
        if (items == null || items.Count == 0)
        {
            return new List<CourseGroupDto>();
        }

        return items
            .GroupBy(ci => string.IsNullOrWhiteSpace(ci.SubjectCode) ? "UNKNOWN" : ci.SubjectCode)
            .Select(g =>
            {
                var courses = g.OrderBy(x => x.SemesterPosition).ToList();
                return new CourseGroupDto
                {
                    SubjectCode = g.Key,
                    AnalysisMarkdown = null,
                    Status = AggregateGroupStatus(courses),
                    Courses = courses
                };
            })
            .OrderBy(g => g.Courses.Min(x => x.SemesterPosition))
            .ThenBy(g => g.SubjectCode)
            .ToList();
    }
    /// <summary>
    /// Map data 
    /// </summary>
    /// <param name="course"></param>
    /// <param name="infoLookup"></param>
    /// <param name="fallbackSubjectCode"></param>
    /// <returns></returns>
    private CourseItemDto? MapCourseItem(
        LearningPathCourseCollection course,
        IDictionary<Guid, InternalCourseInfoDto> infoLookup,
        string? fallbackSubjectCode)
    {
        if (!course.InternalCourseId.HasValue)
        {
            return null;
        }

        infoLookup.TryGetValue(course.InternalCourseId.Value, out var info);
        var dto = _mapper.Map<CourseItemDto>((course, info));

        var subjectCode = !string.IsNullOrWhiteSpace(fallbackSubjectCode)
            ? fallbackSubjectCode!
            : dto.SubjectCode;

        dto.SubjectCode = string.IsNullOrWhiteSpace(subjectCode) ? "UNKNOWN" : subjectCode!;
        return dto;
    }

    private async Task PublishLearningPathSnapshotAsync(Guid pathId, Guid? studentId, CancellationToken cancellationToken)
    {
        if (pathId == Guid.Empty || !studentId.HasValue || studentId == Guid.Empty)
        {
            return;
        }

        var snapshot = await GetLearningPathById(
            new LearningPathSelectsQuery { LearningPathId = pathId },
            studentId.Value,
            cancellationToken);

        if (snapshot.Success)
        {
            await _learningPathRealtimeNotifier.PublishAsync(pathId, snapshot, cancellationToken);
        }
    }

    /// <summary>
    /// Calculation completion of learning path
    /// </summary>
    /// <param name="readModel"></param>
    /// <returns></returns>
    private static decimal CalculateCompletionPercentFromGroups(LearningPathSelectDto dto)
    {
        var allGroups = new List<CourseGroupDto>();
        if (dto.BasicLearningPath?.CourseGroups != null)
            allGroups.AddRange(dto.BasicLearningPath.CourseGroups);

        if (dto.InternalLearningPath != null)
            allGroups.AddRange(dto.InternalLearningPath.SelectMany(m => m.MajorCourseGroups ?? new List<CourseGroupDto>()));
        var mergedBySubject = allGroups
            .GroupBy(g => string.IsNullOrWhiteSpace(g.SubjectCode) ? "UNKNOWN" : g.SubjectCode)
            .Select(g => new
            {
                Subject = g.Key,
                Courses = g.SelectMany(x => x.Courses ?? new List<CourseItemDto>())
                           .Where(c => c.Status != (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped)
                           .ToList()
            })
            .Where(x => x.Courses.Count > 0)
            .Select(x => new
            {
                x.Subject,
                Status = AggregateGroupStatus(x.Courses)
            })
            .ToList();

        var totalSubjects = mergedBySubject.Count;
        if (totalSubjects == 0) return 0m;

        var completedSubjects = mergedBySubject.Count(x =>
            x.Status == (short)ConstantEnum.StudentLearningPathCourseStatus.Completed);

        var percent = (decimal)completedSubjects * 100m / totalSubjects;
        return Math.Round(percent, 2, MidpointRounding.AwayFromZero);
    }
    /// <summary>
    /// Get Learning Path By Id
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningPathSelectResponse> GetLearningPathById(LearningPathSelectsQuery query, Guid userId, CancellationToken cancellationToken = default)
    {
        var res = new LearningPathSelectResponse { Success = false };
        if (query.LearningPathId == Guid.Empty)
        {
            res.SetMessage(MessageId.E00000, "Thiếu hoặc sai LearningPathId.");
            return res;
        }
        var cacheKey = CacheKey.LearningPathSelect(userId, query.LearningPathId);
        var readModel = await _learningPathQueryRepository.GetOrSetAsync(
            cacheKey,
            async () => await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == query.LearningPathId && x.StudentId == userId && x.IsActive
            ),
            expiry: TimeSpan.FromMinutes(1)
        );

        if (readModel == null)
        {
            res.SetMessage(MessageId.E00000, "Không tìm thấy lộ trình học tập.");
            return res;
        }

        // Map readmodel to dto
        var dto = _mapper.Map<LearningPathSelectDto>(readModel);

        var basicMajors = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Basic)
            .ToList();
        var internalMajorsRead = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
            .ToList();

        var basicSubjectCodes = ExtractSubjectCodesWithCourses(basicMajors);
        var internalSubjectCodes = ExtractSubjectCodesWithCourses(internalMajorsRead);

        var basicCourses = basicMajors
            .SelectMany(m => m.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>())
            .ToList();
        var internalCourses = internalMajorsRead
            .SelectMany(m => m.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>())
            .ToList();

        var basicIds = CollectInternalCourseIds(basicSubjectCodes, basicCourses);
        var internalIds = CollectInternalCourseIds(internalSubjectCodes, internalCourses);

        var fetchBasicTask = FetchCourseInfoAsync(basicIds, userId, cancellationToken);
        var fetchInternalTask = FetchCourseInfoAsync(internalIds, userId, cancellationToken);

        await Task.WhenAll(fetchBasicTask, fetchInternalTask);

        var dictBasic = await fetchBasicTask;
        var dictInternal = await fetchInternalTask;

        // ===== 4) Basic: build groups from subject-code collection =====
        var basicGroups = BuildCourseGroupsFromSubjectCodes(basicSubjectCodes, dictBasic);
        if (!basicGroups.Any())
        {
            var fallbackBasicItems = BuildCourseItemsFromCourses(basicCourses, dictBasic);
            basicGroups = BuildCourseGroupsFromItems(fallbackBasicItems);
        }

        dto.BasicLearningPath ??= new BasicLearningPathDto();
        dto.BasicLearningPath.CourseGroups = basicGroups;

        // ===== 5) Internal: mỗi major sử dụng subject code collection =====
        dto.InternalLearningPath = internalMajorsRead
            .Select(m =>
            {
                var majorDto = _mapper.Map<InternalLearningPathDto>(m);
                var subjectCodes = (m.LearningPathSubjectCodes ?? Enumerable.Empty<LearningPathSubjectCodeCollection>())
                    .Where(sc => sc.IsActive)
                    .ToList();

                var courseGroups = BuildCourseGroupsFromSubjectCodes(subjectCodes, dictInternal);
                if (!courseGroups.Any())
                {
                    var fallbackItems = BuildCourseItemsFromCourses(m.LearningPathCourses ?? Enumerable.Empty<LearningPathCourseCollection>(), dictInternal);
                    courseGroups = BuildCourseGroupsFromItems(fallbackItems);
                }

                majorDto.MajorCourseGroups = courseGroups;
                return majorDto;
            })
            .ToList();
        dto.CompletionPercent = CalculateCompletionPercentFromGroups(dto);

        // Done
        res.Response = dto;
        res.Success = true;
        res.SetMessage(MessageId.I00000, "Lấy chi tiết lộ trình học tập");
        return res;
    }

    /// <summary>
    /// Update learning path courses - set selected courses as active and others as inactive
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningPathCourseUpdateResponse> UpdateLearningPathCoursesAsync(LearningPathCourseUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new LearningPathCourseUpdateResponse { Success = false };

        var userEmail = _identityService.GetCurrentUser()!.Email;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1. Validate learning path exists and belongs to current user
            var currentUser = _identityService.GetCurrentUser();

            var learningPath = await _learningPathCommandRepository
                .Find(x => x.PathId == request.PathId
                           && x.StudentId == currentUser!.UserId
                           && x.IsActive,
                      cancellationToken: cancellationToken)
                .FirstOrDefaultAsync(cancellationToken);

            if (learningPath == null)
            {
                response.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại hoặc không thuộc về bạn");
                return false;
            }

            // 2. Get all courses in this learning path
            var allCourses = await _learningPathCourseCommandRepository
                .Find(c => c.LearningPathMajor.PathId == request.PathId,
                      cancellationToken: cancellationToken)
                .ToListAsync(cancellationToken);

            if (!allCourses.Any())
            {
                response.SetMessage(MessageId.E00000, "Lộ trình học tập chưa có khóa học nào");
                return false;
            }

            // 3. Validate all selected course IDs exist in this learning path
            var validCourseIds = allCourses.Select(c => c.LearningPathCourseId).ToHashSet();
            var invalidIds = request.SelectedCourseIds
                .Where(id => !validCourseIds.Contains(id))
                .ToList();

            if (invalidIds.Any())
            {
                response.SetMessage(MessageId.E00000, $"Các khóa học sau không thuộc lộ trình này: {string.Join(", ", invalidIds)}");
                return false;
            }

            // 4. Find courses that are NOT selected by student (courses to deactivate)
            var coursesToDeactivate = allCourses
                .Where(c => request.SelectedCourseIds.Contains(c.LearningPathCourseId))
                .ToList();

            // 5. Mark courses for logical delete by updating them
            foreach (var course in coursesToDeactivate)
            {
                _learningPathCourseCommandRepository.Update(course);
            }

            // 6. Save changes with logical delete enabled (IsActive will be set to false automatically)
            await _unitOfWork.SaveChangesAsync(userEmail, cancellationToken, needLogicalDelete: true);

            // 7. Count activated and deactivated courses
            var deactivatedCount = coursesToDeactivate.Count;
            var activatedCount = request.SelectedCourseIds.Count;

            // 8. Update learning path status to Choosing (user has made their choice)
            if (learningPath.Status == (short)ConstantEnum.LearningPathStatus.Choosing)
            {
                learningPath.Status = (short)ConstantEnum.LearningPathStatus.Generating;
                _learningPathCommandRepository.Update(learningPath);

                // Save learning path status update
                await _unitOfWork.SaveChangesAsync(userEmail, cancellationToken);
            }

            // 10. Clear cache
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(request.PathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(currentUser!.UserId, request.PathId));

            response.Success = true;
            response.SetMessage(MessageId.I00001, $"Đã cập nhật lộ trình: {activatedCount} khóa học được kích hoạt, {deactivatedCount} khóa học bị xóa");
            return true;
        }, cancellationToken);

        return response;
    }
    /// <summary>
    /// Update status Learning path use for choosing major
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UpdateStatusLearningPathResponse> UpdateStatusLearningPathByIdAndSortPosition(UpdateStatusLearningPathCommand request, CancellationToken cancellationToken)
    {
        var res = new UpdateStatusLearningPathResponse { Success = false };
        var currentUser = _identityService.GetCurrentUser()!;
        var currentUserId = currentUser.UserId;
        var currentUserEmail = currentUser.Email;

        if (request.LearningPathId == Guid.Empty)
        {
            res.SetMessage(MessageId.E00000, "Thiếu hoặc sai LearningPathId.");
            return res;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1) Lấy LearningPath của chính user hiện tại
            var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(
                x => x.PathId == request.LearningPathId
                     && x.StudentId == currentUserId
                     && x.IsActive,
                cancellationToken);

            if (learningPath == null)
            {
                res.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại.");
                return false;
            }

            var allInternalMajors = await _learningPathMajorCommandRepository
                .Find(m => m.PathId == request.LearningPathId && m.Type == (short)ConstantEnum.LearningPathMajor.Internal, isTracking: true, cancellationToken: cancellationToken,
                       m => m.LearningPathCourses)
                .ToListAsync(cancellationToken);

            // 3) Chuẩn hoá danh sách & map vị trí
            var internalOrder = (request.InternalMajorIds)
                .Where(id => id != Guid.Empty).Distinct().ToList();

            var internalPosMap = internalOrder
                .Select((id, idx) => new { id, pos = idx + 1 })
                .ToDictionary(x => x.id, x => x.pos);

            // Tập id được phép hoạt động (bất kỳ major nào ngoài tập này sẽ bị inactive)
            var activeSet = new HashSet<Guid>(internalOrder);

            // 4) Cập nhật toàn bộ majors theo rule "không có trong request => IsActive = false"

            var activeMajorIds = new List<Guid>();
            var deactiveMajorIds = new List<Guid>();

            foreach (var m in allInternalMajors)
            {
                var inRequest = activeSet.Contains(m.LearningPathMajorId);

                if (inRequest)
                {
                    var posInt = internalPosMap[m.LearningPathMajorId];
                    m.PositionIndex = posInt;
                    _learningPathMajorCommandRepository.Update(m, currentUserEmail, needLogicalDelete: false);
                    activeMajorIds.Add(m.LearningPathMajorId);
                }
                else
                {
                    m.PositionIndex = null;
                    _learningPathMajorCommandRepository.Update(m, currentUserEmail, needLogicalDelete: true);
                    deactiveMajorIds.Add(m.LearningPathMajorId);
                }
            }

            // 5) Xóa các subject codes trùng lặp trong các majors bị deactive
            if (activeMajorIds.Any() && deactiveMajorIds.Any())
            {
                // Lấy tất cả subject codes từ các majors được active
                var activeSubjectCodes = await _learningPathSubjectCodeCommandRepository
                    .Find(sc => activeMajorIds.Contains(sc.LearningPathMajorId) && sc.IsActive,
                        isTracking: false, cancellationToken: cancellationToken)
                    .Select(sc => sc.SubjectCode)
                    .Distinct()
                    .ToListAsync(cancellationToken);

                // Lấy các subject codes trùng lặp trong các majors bị deactive
                var duplicateSubjectCodesInDeactiveMajors = await _learningPathSubjectCodeCommandRepository
                    .Find(sc => deactiveMajorIds.Contains(sc.LearningPathMajorId) 
                        && sc.IsActive 
                        && activeSubjectCodes.Contains(sc.SubjectCode),
                        isTracking: true, cancellationToken: cancellationToken)
                    .ToListAsync(cancellationToken);

                // Soft delete các subject codes trùng
                foreach (var subjectCode in duplicateSubjectCodesInDeactiveMajors)
                {
                    if (subjectCode != null)
                    {
                        _learningPathSubjectCodeCommandRepository.Update(subjectCode, currentUserEmail, needLogicalDelete: true);
                    }
                }
            }

            // Update write-model
            learningPath.Status = (short)ConstantEnum.LearningPathStatus.InProgress;
            _learningPathCommandRepository.Update(learningPath, currentUserEmail);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Get read model
            var lpRead = await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == request.LearningPathId && x.IsActive);

            // Rebuild danh sách majors list from write-model
            var freshMajors = await _learningPathMajorCommandRepository
                .Find(m => m.PathId == request.LearningPathId, isTracking: false, cancellationToken: cancellationToken,
                       m => m.LearningPathCourses)
                .ToListAsync(cancellationToken);

            // Update lpRead: status + embed list
            if (lpRead != null)
            {
                lpRead.Status = (short)ConstantEnum.LearningPathStatus.InProgress;
                lpRead.LearningPathMajors = freshMajors
                   .OfType<LearningPathMajor>()
                   .OrderBy(m => m.PositionIndex ?? int.MaxValue)
                   .Select(m => LearningPathMajorCollection.FromWriteModel(m))
                   .ToList();

                _unitOfWork.Store(lpRead);
            }

            foreach (var m in freshMajors)
            {
                _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(m!));
            }

            await _unitOfWork.SessionSaveChangesAsync();

            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(currentUserId, request.LearningPathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(request.LearningPathId));

            res.Success = true;
            res.SetMessage(MessageId.I00001, "Cập nhật trạng thái & kích hoạt/vô hiệu chuyên ngành theo request thành công.");
            return true;
        }, cancellationToken);

        return res;
    }
    /// <summary>
    /// Update readModel when edit data in db
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UpdateReadModelLearningPathResponse> UpdateStatusLearningPathReadModelByIdAndSortPosition(
        UpdateReadModelLearningPathCommand request,
        CancellationToken cancellationToken)
    {
        var res = new UpdateReadModelLearningPathResponse { Success = false };

        if (request.LearningPathId == Guid.Empty)
        {
            res.SetMessage(MessageId.E00000, "Thiếu hoặc sai LearningPathId.");
            return res;
        }

        try
        {
            // 1) Lấy write-model gốc
            var lpWrite = await _learningPathCommandRepository.FirstOrDefaultAsync(
                x => x.PathId == request.LearningPathId && x.IsActive
            );
            if (lpWrite == null)
            {
                res.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại.");
                return res;
            }

            // 2) Lấy majors + courses từ write-model
            var majorsWrite = await _learningPathMajorCommandRepository
                .Find(m => m.PathId == request.LearningPathId,
                      isTracking: false,
                      cancellationToken: cancellationToken,
                      m => m.LearningPathCourses,
                      m => m.LearningPathSubjectCodes)
                .ToListAsync(cancellationToken);

            majorsWrite = majorsWrite.OfType<LearningPathMajor>().ToList();
            lpWrite.LearningPathMajors = majorsWrite;

            // 3) Lấy document read-model hiện có (giữ nguyên identity doc)
        var lpRead = await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == request.LearningPathId
            );

            // Chuẩn bị danh sách major read-model đã sort
            var majorsRead = majorsWrite
                .OrderBy(m => m.PositionIndex ?? int.MaxValue)
                .Select(LearningPathMajorCollection.FromWriteModel)
                .ToList();

        LearningPathCollection? currentReadModel = null;

            if (lpRead != null)
            {
                // 4a) UPDATE IN-PLACE: cập nhật thẳng object đang có
                lpRead.PathName = lpWrite.PathName;
                lpRead.CreatedAt = lpWrite.CreatedAt;
                lpRead.UpdatedAt = lpWrite.UpdatedAt;
                lpRead.CreatedBy = lpWrite.CreatedBy;
                lpRead.UpdatedBy = lpWrite.UpdatedBy;
                lpRead.IsActive = lpWrite.IsActive;
                lpRead.StudentId = lpWrite.StudentId;
                lpRead.Status = lpWrite.Status;
                lpRead.SummaryFeedback = lpWrite.SummaryFeedback;
                lpRead.HabitAndInterestAnalysis = lpWrite.HabitAndInterestAnalysis;
                lpRead.Personality = lpWrite.Personality;
                lpRead.LearningAbility = lpWrite.LearningAbility;
                lpRead.LearningPathMajors = majorsRead;

                _unitOfWork.Store(lpRead); // upsert đúng document hiện tại
            currentReadModel = lpRead;
            }
            else
            {
                // 4b) Không có thì map mới từ write-model (fallback)
                var lpReadNew = LearningPathCollection.FromWriteModel(lpWrite);
                // Bảo đảm majors đã sort theo PositionIndex
                lpReadNew.LearningPathMajors = majorsRead;

                _unitOfWork.Store(lpReadNew);
            currentReadModel = lpReadNew;
            }

            // (Tuỳ nhu cầu) Nếu không cần query majors rời rạc thì có thể bỏ store từng major để tránh duplicate.
            // Giữ nguyên như cũ nếu app đang đọc theo collection majors riêng.
            foreach (var m in majorsWrite)
            {
                _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(m));
            }

            await _unitOfWork.SessionSaveChangesAsync();

            // 5) Xoá cache liên quan (bổ sung xoá key 'select' theo cách GetLearningPathById đang dùng)
            var lpIdStr = request.LearningPathId.ToString("D");
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(request.LearningPathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(request.LearningPathId));

            var currentUserId = _identityService.GetCurrentUser()?.UserId;
            if (currentUserId != null)
            {
                await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(currentUserId.Value, request.LearningPathId));
            }

            res.Success = true;
            res.Response = lpIdStr;
            res.SetMessage(MessageId.I00001, "Đồng bộ lại read-model cho LearningPath thành công.");

            var studentIdForNotification = lpWrite.StudentId ?? currentReadModel?.StudentId;
            await PublishLearningPathSnapshotAsync(request.LearningPathId, studentIdForNotification, cancellationToken);

            return res;
        }
        catch (Exception ex)
        {
            throw;
        }
    }
    /// <summary>
    /// Get All LearningPath
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SelectAllLearningPathResponse> GetAllLearningPath(SelectAllLearningPathQuery query, Guid userId, CancellationToken cancellationToken = default)
    {
        var res = new SelectAllLearningPathResponse { Success = false };


        if (userId == Guid.Empty)
        {
            res.SetMessage(MessageId.E00000, "Không xác định được người dùng hiện tại.");
            return res;
        }

        // Pagination: 0-based (request) -> 1-based (repo)
        var pageIndex0 = query.Pagination?.PageIndex ?? 0;
        var pageSize = query.Pagination?.PageSize ?? 10;
        if (pageIndex0 < 0) pageIndex0 = 0;
        if (pageSize <= 0) pageSize = 10;

        var pageNumber1 = pageIndex0 + 1;
        var cacheKey = CacheKey.LearningPathPaged(userId, pageNumber1, pageSize);

        var pagedRead = await _learningPathQueryRepository.GetOrSetPagedAsync(
            cacheKey,
            async () => await _learningPathQueryRepository.PagedAsync(
                pageNumber: pageNumber1,
                pageSize: pageSize,
                predicate: x => x.IsActive && x.StudentId == userId
            ),
            expiry: TimeSpan.FromSeconds(45)
        );

        // Map to DTO
        var dtoItems = _mapper.Map<List<LearningPathSelectAllDto>>(pagedRead.Items);

        // Done
        res.Response = new PaginatedResult<LearningPathSelectAllDto>(
            pageIndex: pageIndex0,
            pageSize: pageSize,
            totalCount: pagedRead.TotalCount,
            data: dtoItems
        );

        res.Success = true;
        res.SetMessage(MessageId.I00000, "Lấy danh sách lộ trình học tập (cache + paging từ repo).");
        return res;
    }

    /// <summary>
    /// Update course status to Skipped
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<UpdateCourseStatusToSkippedResponse> UpdateCourseStatusToSkippedAsync(UpdateCourseStatusToSkippedCommand request, Guid userId, string email, CancellationToken cancellationToken)
    {
        var response = new UpdateCourseStatusToSkippedResponse { Success = false };

        if (userId == Guid.Empty)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // 1. Tìm LearningPathCourse từ request để lấy SubjectCode
            var learningPathCourse = await _learningPathCourseCommandRepository
                .Find(x => x.InternalCourseId == request.CourseId && x.IsActive)
                .FirstOrDefaultAsync(cancellationToken);

            if (learningPathCourse == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy khóa học trong lộ trình học của bạn");
                return false;
            }

            // if (string.IsNullOrWhiteSpace(learningPathCourse.SubjectCode))
            // {
            //     response.SetMessage(MessageId.E00000, "Khóa học không có mã môn học");
            //     return false;
            // }

            // var subjectCodeToSkip = learningPathCourse.SubjectCode;

            // 2. Lấy tất cả PathId của user
            var studentPathIds = await _learningPathCommandRepository
                .Find(lp => lp.StudentId == userId && lp.IsActive, isTracking: false, cancellationToken)
                .Select(lp => lp.PathId)
                .ToListAsync(cancellationToken);

            if (!studentPathIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy lộ trình học của bạn");
                return false;
            }

            // 3. Lấy tất cả LearningPathMajorId thuộc các PathId đó
            var studentMajorIds = await _learningPathMajorCommandRepository
                .Find(m => studentPathIds.Contains(m.PathId) && m.IsActive, isTracking: false, cancellationToken)
                .Select(m => m.LearningPathMajorId)
                .ToListAsync(cancellationToken);

            if (!studentMajorIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy chuyên ngành trong lộ trình học của bạn");
                return false;
            }

            // 4. Tìm tất cả LearningPathCourse có cùng SubjectCode của user
            var coursesToUpdate = await _learningPathCourseCommandRepository
                .Find(c => studentMajorIds.Contains(c.LearningPathMajorId)
                          // && c.SubjectCode == subjectCodeToSkip
                          && c.IsActive,
                      isTracking: true,
                      cancellationToken)
                .ToListAsync(cancellationToken);

            if (!coursesToUpdate.Any())
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy khóa học nào cần cập nhật");
                return false;
            }

            // 5. Update status thành Skipped
            var updatedCourseIds = new List<Guid>();
            foreach (var course in coursesToUpdate)
            {
                if (course!.Status != (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped)
                {
                    course.Status = (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                    updatedCourseIds.Add(course.LearningPathCourseId);
                }
            }

            if (!updatedCourseIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Tất cả khóa học có mã môn này đã được skip trước đó");
                return false;
            }

            _learningPathCourseCommandRepository.UpdateRange(coursesToUpdate);
            await _unitOfWork.SaveChangesAsync(email, cancellationToken);

            // 6. Update read model
            var learningPathCollections = await _learningPathQueryRepository
                .ToListAsync(x => studentPathIds.Contains(x.PathId));

            foreach (var learningPathCollection in learningPathCollections)
            {
                var hasChanges = false;

                foreach (var major in learningPathCollection.LearningPathMajors)
                {
                    foreach (var course in major.LearningPathCourses)
                    {
                        if (updatedCourseIds.Contains(course.LearningPathCourseId))
                        {
                            course.Status = (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                            hasChanges = true;
                        }
                    }
                }

                if (hasChanges)
                {
                    _unitOfWork.Store(learningPathCollection);
                    await _unitOfWork.SessionSaveChangesAsync();

                    var lpIdStr = learningPathCollection.PathId.ToString("D");
                    await _unitOfWork.CacheRemoveAsync($"learning_path:select:{userId}:{lpIdStr}");
                    await _unitOfWork.CacheRemoveAsync($"learning_path:{lpIdStr}");
                    await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{lpIdStr}");
                }
            }

            response.Success = true;
            // response.Response = $"Đã cập nhật {updatedCourseIds.Count} khóa học có mã môn '{subjectCodeToSkip}' thành trạng thái Skipped";
            response.SetMessage(MessageId.I00001, "Cập nhật trạng thái khóa học");
            return true;
        }, cancellationToken);

        return response;
    }

    public async Task<UpdateCourseStatusToSkippedResponse> UpdateCourseStatusToSkippedBySubjectAsync(
        Guid userId,
        Guid learningPathId,
        string subjectCode,
        string email,
        CancellationToken cancellationToken)
    {
        var response = new UpdateCourseStatusToSkippedResponse { Success = false };

        if (userId == Guid.Empty || learningPathId == Guid.Empty)
        {
            response.SetMessage(MessageId.E00000, "Thiếu thông tin người dùng hoặc lộ trình.");
            return response;
        }

        if (string.IsNullOrWhiteSpace(subjectCode))
        {
            response.SetMessage(MessageId.E00000, "Thiếu mã môn học cần skip.");
            return response;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(
                x => x.PathId == learningPathId && x.StudentId == userId && x.IsActive,
                cancellationToken: cancellationToken);

            if (learningPath == null)
            {
                response.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại hoặc không thuộc về bạn");
                return false;
            }

            var majorIds = await _learningPathMajorCommandRepository
                .Find(m => m.PathId == learningPathId && m.IsActive, isTracking: false, cancellationToken)
                .Select(m => m.LearningPathMajorId)
                .ToListAsync(cancellationToken);

            if (!majorIds.Any())
            {
                response.SetMessage(MessageId.E00000, "Lộ trình học tập chưa có chuyên ngành hợp lệ");
                return false;
            }

            var courses = await _learningPathCourseCommandRepository
                .Find(c => majorIds.Contains(c.LearningPathMajorId) && c.IsActive,
                      isTracking: true,
                      cancellationToken)
                .ToListAsync(cancellationToken);

            var coursesToUpdate = courses
                // .Where(c => !string.IsNullOrWhiteSpace(c.SubjectCode) &&
                //             string.Equals(c.SubjectCode, subjectCode, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!coursesToUpdate.Any())
            {
                response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học nào có mã môn '{subjectCode}' trong lộ trình này");
                return false;
            }

            var updatedCourseIds = new List<Guid>();
            foreach (var course in coursesToUpdate)
            {
                if (course.Status != (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped)
                {
                    course.Status = (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                    updatedCourseIds.Add(course.LearningPathCourseId);
                }
            }

            if (!updatedCourseIds.Any())
            {
                response.SetMessage(MessageId.E00000, $"Các môn mã '{subjectCode}' đã được skip trước đó");
                return false;
            }

            _learningPathCourseCommandRepository.UpdateRange(coursesToUpdate);
            await _unitOfWork.SaveChangesAsync(email, cancellationToken);

            var learningPathCollection = await _learningPathQueryRepository
                .FirstOrDefaultAsync(x => x.PathId == learningPathId && x.IsActive);

            if (learningPathCollection != null)
            {
                var hasChanges = false;

                foreach (var major in learningPathCollection.LearningPathMajors)
                {
                    foreach (var course in major.LearningPathCourses)
                    {
                        if (updatedCourseIds.Contains(course.LearningPathCourseId))
                        {
                            course.Status = (short)ConstantEnum.StudentLearningPathCourseStatus.Skipped;
                            hasChanges = true;
                        }
                    }
                }

                if (hasChanges)
                {
                    _unitOfWork.Store(learningPathCollection);
                    await _unitOfWork.SessionSaveChangesAsync();
                }
            }

            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(learningPathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(learningPathId));
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(userId, learningPathId));

            response.Success = true;
            response.Response = $"Đã cập nhật các môn có mã '{subjectCode.ToUpperInvariant()}' thành trạng thái Skipped";
            response.SetMessage(MessageId.I00001, "Cập nhật trạng thái khóa học");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Update course status for user
    /// </summary>
    /// <param name="userId"></param>
    /// <param name="courseId"></param>
    /// <param name="status"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task UpdateCourseStatusForUserAsync(Guid userId, Guid courseId, short status, CancellationToken cancellationToken = default)
    {
        // 1. Lấy tất cả learning path của user
        var pathIds = await _learningPathCommandRepository
            .Find(lp => lp.StudentId == userId && lp.IsActive, isTracking: false, cancellationToken)
            .Select(lp => lp.PathId)
            .ToListAsync(cancellationToken);

        if (!pathIds.Any())
            return;

        // 2. Lấy tất cả majors thuộc các paths đó
        var learningPathMajorIds = await _learningPathMajorCommandRepository
            .Find(m => pathIds.Contains(m.PathId) && m.IsActive, isTracking: false, cancellationToken)
            .Select(m => m.LearningPathMajorId)
            .ToListAsync(cancellationToken);

        if (!learningPathMajorIds.Any())
            return;

        // 3. Lấy tất cả LearningPathCourse chứa courseId này
        var coursesToUpdate = await _learningPathCourseCommandRepository
            .Find(c => c.InternalCourseId == courseId
                       && c.IsActive
                       && learningPathMajorIds.Contains(c.LearningPathMajorId),
                   isTracking: true,
                   cancellationToken)
            .ToListAsync(cancellationToken);

        if (!coursesToUpdate.Any())
            return;

        // 4. Update status trong write model
        foreach (var lpCourse in coursesToUpdate)
        {
            lpCourse.Status = status;
            lpCourse.UpdatedAt = DateTime.UtcNow;
        }

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Update range
            _learningPathCourseCommandRepository.UpdateRange(coursesToUpdate);

            // Save changes vào Postgres
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // 5. Cập nhật read model Marten cho các learning path bị ảnh hưởng
            var affectedPathIds = await _learningPathMajorCommandRepository
                .Find(m => learningPathMajorIds.Contains(m.LearningPathMajorId), isTracking: false, cancellationToken)
                .Where(m => coursesToUpdate.Select(c => c.LearningPathMajorId).Contains(m.LearningPathMajorId))
                .Select(m => m.PathId)
                .Distinct()
                .ToListAsync(cancellationToken);

            foreach (var pathId in affectedPathIds)
            {
                await CheckAndUpdateLearningPathStatusAsync(pathId, cancellationToken);
            }

            return true;
        }, cancellationToken);
    }

    /// <summary>
    /// Update Learning Path Status (InProgress, Closed, Paused)
    /// </summary>
    /// If the new status is InProgress, other learning paths of the user will be set to Paused.
    /// <param name="learningPathId"></param>
    /// <param name="newStatus"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<UpdateLearningPathStatusResponse> UpdateLearningPathStatusAsync(Guid learningPathId, ConstantEnum.LearningPathStatus newStatus, CancellationToken ct = default)
    {
        var response = new UpdateLearningPathStatusResponse { Success = false };
        var currentUser = _identityService.GetCurrentUser();

        if (currentUser == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy thông tin người dùng");
            return response;
        }

        if (newStatus != ConstantEnum.LearningPathStatus.InProgress && newStatus != ConstantEnum.LearningPathStatus.Closed && newStatus != ConstantEnum.LearningPathStatus.Paused)
        {
            response.SetMessage(MessageId.E00000, "Trạng thái lộ trình học tập không hợp lệ");
            return response;
        }

        var lp = await _learningPathCommandRepository
                                .Find(x => x.PathId == learningPathId && x.IsActive,
                                      isTracking: true,
                                      cancellationToken: ct)
                                .FirstOrDefaultAsync(ct);

        if (lp == null)
        {
            response.SetMessage(MessageId.E00000, "Lộ trình học tập không tồn tại");
            return response;
        }

        if (lp.Status == (short)newStatus)
        {
            response.SetMessage(MessageId.E00000, "Trạng thái lộ trình học tập không thay đổi");
            return response;
        }


        lp.Status = (short)newStatus;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {

            if (newStatus == ConstantEnum.LearningPathStatus.InProgress)
            {
                var otherPaths = await _learningPathCommandRepository
                    .Find(x => x.StudentId == currentUser.UserId
                               && x.PathId != learningPathId
                               && x.IsActive,
                          isTracking: true,
                          cancellationToken: ct)
                    .ToListAsync(ct);

                foreach (var p in otherPaths)
                {
                    p.Status = (short)ConstantEnum.LearningPathStatus.Paused;
                    p.UpdatedAt = DateTime.UtcNow;
                    _learningPathCommandRepository.Update(p);
                }
            }

            _learningPathCommandRepository.Update(lp);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, ct);

            // Cập nhật read model
            await SyncLearningPathReadModelByIdAsync(learningPathId, currentUser.UserId, ct);

            // Các LP khác (nếu có)
            if (newStatus == ConstantEnum.LearningPathStatus.InProgress)
            {
                var otherPaths = await _learningPathCommandRepository
                    .Find(x => x.StudentId == currentUser.UserId
                               && x.PathId != learningPathId
                               && x.IsActive,
                          isTracking: false,
                          cancellationToken: ct)
                    .Select(x => x.PathId)
                    .ToListAsync(ct);

                foreach (var otherId in otherPaths)
                {
                    await SyncLearningPathReadModelByIdAsync(otherId, currentUser.UserId, ct);
                }
            }

            return true;
        }, ct);

        // Clear cache cho LP chính
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(learningPathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(learningPathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(currentUser.UserId, learningPathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathPaged(currentUser.UserId, 1, 20));


        response.Success = true;
        response.Response = true;
        response.SetMessage(MessageId.I00001, "Cập nhật trạng thái lộ trình học tập");
        return response;
    }


    #region Private Helpers Methods

    /// <summary>
    /// Sync LearningPath Read Model từ Write Model theo Id
    /// </summary>
    /// <param name="learningPathId"></param>
    /// <param name="studentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task SyncLearningPathReadModelByIdAsync(Guid learningPathId, Guid? studentId, CancellationToken cancellationToken)
    {
        // 1) Lấy write-model gốc
        var lpWrite = await _learningPathCommandRepository
                                .Find(x => x.PathId == learningPathId && x.IsActive,
                                      isTracking: false,
                                      cancellationToken: cancellationToken)
                                .FirstOrDefaultAsync(cancellationToken);
        if (lpWrite == null)
        {
            return; // hoặc log warning, tuỳ quy ước
        }

        // 2) Lấy majors + courses từ write-model
        var majorsWrite = await _learningPathMajorCommandRepository
            .Find(m => m.PathId == learningPathId,
                  isTracking: false,
                  cancellationToken: cancellationToken,
                  m => m.LearningPathCourses)
            .ToListAsync(cancellationToken);

        majorsWrite = majorsWrite.OfType<LearningPathMajor>().ToList();
        lpWrite.LearningPathMajors = majorsWrite;

        // 3) Lấy document read-model hiện có (giữ nguyên identity doc)
        var lpRead = await _learningPathQueryRepository.FirstOrDefaultAsync(
            x => x.PathId == learningPathId
        );

        // Chuẩn bị danh sách major read-model đã sort
        var majorsRead = majorsWrite
            .OrderBy(m => m.PositionIndex ?? int.MaxValue)
            .Select(LearningPathMajorCollection.FromWriteModel)
            .ToList();

        if (lpRead != null)
        {
            // 4a) UPDATE IN-PLACE: cập nhật thẳng object đang có
            lpRead.PathName = lpWrite.PathName;
            lpRead.CreatedAt = lpWrite.CreatedAt;
            lpRead.UpdatedAt = lpWrite.UpdatedAt;
            lpRead.CreatedBy = lpWrite.CreatedBy;
            lpRead.UpdatedBy = lpWrite.UpdatedBy;
            lpRead.IsActive = lpWrite.IsActive;
            lpRead.StudentId = lpWrite.StudentId;
            lpRead.Status = lpWrite.Status;
            lpRead.LearningPathMajors = majorsRead;

            _unitOfWork.Store(lpRead); // upsert đúng document hiện tại
        }
        else
        {
            // 4b) Không có thì map mới từ write-model (fallback)
            var lpReadNew = LearningPathCollection.FromWriteModel(lpWrite);
            // Bảo đảm majors đã sort theo PositionIndex
            lpReadNew.LearningPathMajors = majorsRead;

            _unitOfWork.Store(lpReadNew);
        }

        // Nếu app của bạn đang sử dụng major-collection riêng → giữ như mẫu
        foreach (var m in majorsWrite)
        {
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(m));
        }

        await _unitOfWork.SessionSaveChangesAsync();

        // 5) Xoá cache liên quan (y chang mẫu, chỉ khác là dùng studentId truyền vào)
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPath(learningPathId));
        await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathMajorList(learningPathId));

        // Ở hàm mẫu lấy currentUser từ token, nhưng ở đây userId đã có trong event
        if (studentId.HasValue)
        {
            await _unitOfWork.CacheRemoveAsync(CacheKey.LearningPathSelect(studentId.Value, learningPathId));
        }
    }

    /// <summary>
    /// Check và update status của Learning Path
    /// </summary>
    /// Một SubjectCode được coi là completed khi:
    /// Có ít nhất 1 LearningPathCourse thuộc SubjectCode đó có Status = Completed.
    /// Một Learning Path hoàn thành khi:
    /// TẤT CẢ SubjectCodes trong LearningPath đều completed.
    /// Mỗi môn có thể có 1–N LearningPathCourse (InternalCourseId khác nhau nhưng cùng SubjectCode).
    /// Sinh viên chỉ cần hoàn thành 1 course của SubjectCode đó → xem như đã hoàn thành môn đó.
    /// <param name="pathId"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    private async Task CheckAndUpdateLearningPathStatusAsync(Guid pathId, CancellationToken ct)
    {
        // Load all majors (+ courses) trong Learning Path
        var majors = await _learningPathMajorCommandRepository
            .Find(m => m.PathId == pathId && m.IsActive,
                        isTracking: false,
                        ct,
                        m => m.LearningPathCourses)
            .ToListAsync(ct);

        if (!majors.Any())
            return;

        // Lấy tất cả SubjectCodes trong LP
        var subjectGroups = majors
            .SelectMany(m => m.LearningPathCourses)
            .Where(c => c.IsActive)
            //             && c.SubjectCode != null)
            // .GroupBy(c => c.SubjectCode)
            // Để đại để fix lỗi
            .GroupBy(c => c.Status)
            .ToList();

        // Check: SubjectCode nào được coi completed?
        bool AllSubjectsCompleted = subjectGroups.All(group =>
            group.Any(c => c.Status == (short)ConstantEnum.CourseProgressStatus.Completed)
        );

        // Load LearningPath write-model
        var learningPath = await _learningPathCommandRepository
                                        .Find(lp => lp.PathId == pathId,
                                              isTracking: true,
                                              cancellationToken: ct)
                                        .FirstOrDefaultAsync(ct);


        if (learningPath == null)
            return;

        // Nếu tất cả môn đã complete → set LearningPath = Completed
        if (AllSubjectsCompleted &&
            learningPath.Status != (short)ConstantEnum.LearningPathStatus.Completed)
        {
            learningPath.Status = (short)ConstantEnum.LearningPathStatus.Completed;
            learningPath.UpdatedAt = DateTime.UtcNow;

            _learningPathCommandRepository.Update(learningPath);
            await _unitOfWork.SaveChangesAsync(ct);

            // Sync readmodel cho LP
            await SyncLearningPathReadModelByIdAsync(pathId, learningPath.StudentId, ct);
        }
    }


    #endregion

}