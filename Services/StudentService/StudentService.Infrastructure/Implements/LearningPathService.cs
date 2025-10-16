using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using BuildingBlocks.Pagination;
using MapsterMapper;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
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
    private readonly IQueryRepository<LearningPathCourseCollection> _learningPathCourseQueryRepository;
    private readonly IRequestClient<CoursesSelectEvent> _requestClientCoursesSelectEvent;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IQueryRepository<LearningPathCollection> _learningPathQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<GetInfoInternalCourseEvents> _requestClient;
    private readonly IMapper _mapper;

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
    /// <param name="learningPathCourseQueryRepository"></param>
    public LearningPathService(IUnitOfWork unitOfWork,
        ICommandRepository<LearningPathMajor> learningPathMajorCommandRepository,
        ICommandRepository<LearningPath> learningPathCommandRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseCommandRepository,
        IRequestClient<CoursesSelectEvent> requestClientCoursesSelectEvent,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IIdentityService identityService,
        IRequestClient<GetInfoInternalCourseEvents> requestClient,
        IMapper mapper, IQueryRepository<LearningPathCourseCollection> learningPathCourseQueryRepository)
    {
        _unitOfWork = unitOfWork;
        _learningPathMajorCommandRepository = learningPathMajorCommandRepository;
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _requestClientCoursesSelectEvent = requestClientCoursesSelectEvent;
        _learningPathQueryRepository = learningPathQueryRepository;
        _identityService = identityService;
        _requestClient = requestClient;
        _mapper = mapper;
        _learningPathCourseQueryRepository = learningPathCourseQueryRepository;
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
                Status = (short)ConstantEnum.LearningPathStatus.Generating,
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
                StudentLevel = request.StudentLevel
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
                        ? (short)ConstantEnum.LearningPathMajor.Basic
                        : request.MajorType,
                    LearningPathCourses = matchedCourses?.CourseCodeIds
                        .Select(courseId => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            InternalCourseId = courseId,
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
                    Type = (short)ConstantEnum.LearningPathMajor.Basic,
                    LearningPathCourses = seCourses!.CourseCodeIds
                        .Select(courseId => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(),
                            InternalCourseId = courseId
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
                    c.LearningPathMajorId = m.LearningPathMajorId; // Set foreign key
                    return c;
                }))
                .ToList();

            if (allCourses.Any())
            {
                await _learningPathCourseCommandRepository.AddRangeAsync(allCourses);
            }

            await _unitOfWork.SaveChangesAsync(learningPath.CreatedBy, cancellationToken);

            // ✅ FIX: Ensure LearningPathCourses are properly set BEFORE storing to read-model
            foreach (var major in learningPathMajors)
            {
                major.LearningPathCourses = allCourses
                    .Where(c => c.LearningPathMajorId == major.LearningPathMajorId)
                    .ToList();
            }

            foreach (var major in learningPathMajors)
            {
                _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            }

            learningPath.LearningPathMajors.AddRange(learningPathMajors.Select(LearningPathMajorCollection.FromWriteModel));

            _unitOfWork.Store(learningPath);

            await _unitOfWork.SessionSaveChangesAsync();

            // True
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
                    Type = (short)ConstantEnum.LearningPathMajor.External,
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

            await _unitOfWork.SaveChangesAsync(request.CurrentUserEmail!, cancellationToken);

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
            await _unitOfWork.CacheRemoveAsync($"learning_path:{request.PathId}");
            await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{request.PathId}");

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

        return true;
    }
    /// <summary>
    /// Get Learning Path By Id
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningPathSelectResponse> GetLearningPathById(LearningPathSelectsQuery query, CancellationToken cancellationToken = default)
    {
        var res = new LearningPathSelectResponse { Success = false };
        var currentUserId = _identityService.GetCurrentUser()!.UserId;
        if (query.LearningPathId == Guid.Empty)
        {
            res.SetMessage(MessageId.E00000, "Thiếu hoặc sai LearningPathId.");
            return res;
        }
        var lpId = query.LearningPathId.ToString("D");
        var cacheKey = $"learning_path:select:{currentUserId}:{lpId}";
        var readModel = await _learningPathQueryRepository.GetOrSetAsync(
            cacheKey,
            async () => await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == query.LearningPathId && x.StudentId == currentUserId && x.IsActive
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

        // Get list courseId For Basic & Internal
        var basicIds = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Basic)
            .SelectMany(m => m.LearningPathCourses)
            .Where(c => c.InternalCourseId.HasValue)
            .Select(c => c.InternalCourseId!.Value)
            .Distinct()
            .ToList();

        var internalIds = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
            .SelectMany(m => m.LearningPathCourses)
            .Where(c => c.InternalCourseId.HasValue)
            .Select(c => c.InternalCourseId!.Value)
            .Distinct()
            .ToList();

        // Call CourseService to get info
        var @eventInternal = new GetInfoInternalCourseEvents(internalIds);
        var @eventBasic = new GetInfoInternalCourseEvents(basicIds);

        var resultInternal = await _requestClient.GetResponse<GetInfoInternalCourseResponse>(@eventInternal, cancellationToken);
        var resultBasic = await _requestClient.GetResponse<GetInfoInternalCourseResponse>(@eventBasic, cancellationToken);

        var internalInfos = (IEnumerable<InternalCourseInfoDto>)(resultInternal.Message.Response);
        var basicInfos = (IEnumerable<InternalCourseInfoDto>)(resultBasic.Message.Response);

        // 3) Đưa về dict theo CourseId để tra nhanh
        var dictInternal = internalInfos
            .Where(x => x.CourseId.HasValue)
            .GroupBy(x => x.CourseId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        var dictBasic = basicInfos
            .Where(x => x.CourseId.HasValue)
            .GroupBy(x => x.CourseId!.Value)
            .ToDictionary(g => g.Key, g => g.First());

        // 4) Fill BasicLearningPath.Courses
        var basicCourses = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Basic)
            .SelectMany(m => m.LearningPathCourses)
            .Where(c => c.InternalCourseId.HasValue)
            .Select(c =>
            {
                dictBasic.TryGetValue(c.InternalCourseId!.Value, out var info);
                return _mapper.Map<CourseItemDto>((c, info));
            })
            .OrderBy(c => c.SemesterPosition)
            .ToList();

        dto.BasicLearningPath.Courses = basicCourses;

        // 5) Fill InternalLearningPath[i].MajorCourse
        var internalMajorsRead = readModel.LearningPathMajors
            .Where(m => m.IsActive && m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
            .ToList();

        // Map dto.InternalLearningPath
        dto.InternalLearningPath = internalMajorsRead
            .Select(m =>
            {
                var majorDto = _mapper.Map<InternalLearningPathDto>(m);
                majorDto.MajorCourse = (m.LearningPathCourses ?? new List<LearningPathCourseCollection>())
                    .Where(c => c.InternalCourseId.HasValue)
                    .Select(c =>
                    {
                        dictInternal.TryGetValue(c.InternalCourseId!.Value, out var info);
                        return _mapper.Map<CourseItemDto>((c, info));
                    })
                    .OrderBy(ci => ci.SemesterPosition)
                    .ToList();
                return majorDto;
            })
            .ToList();

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
                .Where(c => !request.SelectedCourseIds.Contains(c.LearningPathCourseId))
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

            // Delete learning path course from read-model
            foreach (var course in coursesToDeactivate)
            {
                var courseRead = await _learningPathCourseQueryRepository.FirstOrDefaultAsync(x => x.InternalCourseId == course.InternalCourseId && x.IsActive);
                _unitOfWork.Delete(courseRead);
            }

            // 10. Clear cache
            await _unitOfWork.CacheRemoveAsync($"learning_path:{request.PathId}");
            await _unitOfWork.CacheRemoveAsync($"learning_path:select:{currentUser!.UserId}:{request.PathId}");

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
            var internalOrder = (request.InternalMajorIds ?? [])
                .Where(id => id != Guid.Empty).Distinct().ToList();

            var internalPosMap = internalOrder
                .Select((id, idx) => new { id, pos = idx + 1 })
                .ToDictionary(x => x.id, x => x.pos);

            // Tập id được phép hoạt động (bất kỳ major nào ngoài tập này sẽ bị inactive)
            var activeSet = new HashSet<Guid>(internalOrder);

            // 4) Cập nhật toàn bộ majors theo rule "không có trong request => IsActive = false"
            var majorsChanged = new List<LearningPathMajor>();

            foreach (var m in allInternalMajors)
            {
                var inRequest = activeSet.Contains(m.LearningPathMajorId);

                if (inRequest)
                {
                    var posInt = internalPosMap[m.LearningPathMajorId];
                    m.PositionIndex = posInt;
                    _learningPathMajorCommandRepository.Update(m, currentUserEmail, needLogicalDelete: false);
                }
                else
                {
                    m.PositionIndex = null;
                    _learningPathMajorCommandRepository.Update(m, currentUserEmail, needLogicalDelete: true);
                }
                majorsChanged.Add(m);
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

            var lpIdStr = request.LearningPathId.ToString("D");
            await _unitOfWork.CacheRemoveAsync($"learning_path:select:{currentUserId}:{lpIdStr}");
            await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{request.LearningPathId}");

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
                  m => m.LearningPathCourses)
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

        // (Tuỳ nhu cầu) Nếu không cần query majors rời rạc thì có thể bỏ store từng major để tránh duplicate.
        // Giữ nguyên như cũ nếu app đang đọc theo collection majors riêng.
        foreach (var m in majorsWrite)
        {
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(m));
        }

        await _unitOfWork.SessionSaveChangesAsync();

        // 5) Xoá cache liên quan (bổ sung xoá key 'select' theo cách GetLearningPathById đang dùng)
        var lpIdStr = request.LearningPathId.ToString("D");
        await _unitOfWork.CacheRemoveAsync($"learning_path:{lpIdStr}");
        await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{lpIdStr}");

        var currentUserId = _identityService.GetCurrentUser()?.UserId;
        if (currentUserId != null)
        {
            await _unitOfWork.CacheRemoveAsync($"learning_path:select:{currentUserId}:{lpIdStr}");
        }

        res.Success = true;
        res.Response = lpIdStr;
        res.SetMessage(MessageId.I00001, "Đồng bộ lại read-model cho LearningPath thành công.");
        return res;
    }
    /// <summary>
    /// Get All LearningPath
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SelectAllLearningPathResponse> GetAllLearningPath(SelectAllLearningPathQuery query, CancellationToken cancellationToken = default)
    {
        var res = new SelectAllLearningPathResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();
        if (currentUser is null)
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
        var cacheKey = $"learning_path:paged:{currentUser.UserId}:p{pageNumber1}:s{pageSize}";

        var pagedRead = await _learningPathQueryRepository.GetOrSetPagedAsync(
            cacheKey,
            async () => await _learningPathQueryRepository.PagedAsync(
                pageNumber: pageNumber1,
                pageSize: pageSize,
                predicate: x => x.IsActive && x.StudentId == currentUser.UserId
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
}