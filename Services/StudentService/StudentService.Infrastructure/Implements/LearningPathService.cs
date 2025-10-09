using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoInternalCourse;
using MapsterMapper;
using MassTransit;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.InsertInternal;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
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
    public LearningPathService(IUnitOfWork unitOfWork,
        ICommandRepository<LearningPathMajor> learningPathMajorCommandRepository,
        ICommandRepository<LearningPath> learningPathCommandRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseCommandRepository,
        IRequestClient<CoursesSelectEvent> requestClientCoursesSelectEvent,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IIdentityService identityService,
        IRequestClient<GetInfoInternalCourseEvents> requestClient,
        IMapper mapper)
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
                        await _learningPathCourseCommandRepository.AddAsync(course, request.CurrentUserEmail!);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            major.LearningPathCourses = newCourses;
            // 3) Update read-model / cache (eventual consistency)
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            var lpRead = await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == request.PathId && x.IsActive
            );
            if (lpRead != null)
            {
                lpRead.LearningPathMajors.Add(LearningPathMajorCollection.FromWriteModel(major));
                _unitOfWork.Store(lpRead);
            }
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
                    Type = request.MajorType,
                    LearningPathCourses = matchedCourses?.CourseCodeIds
                        .Select(courseId => new LearningPathCourse
                        {
                            LearningPathCourseId = Guid.NewGuid(), // Add this line to generate ID
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
    /// <summary>
    /// Get Learning Path By Id
    /// </summary>
    /// <param name="query"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<LearningPathSelectResponse> GetLearningPathById(
        LearningPathSelectsQuery query, CancellationToken cancellationToken = default)
    {
        var res = new LearningPathSelectResponse { Success = false };

        var cacheKey = $"learning_path:select:{query.LearningPathId:D}";
        var readModel = await _learningPathQueryRepository.GetOrSetAsync(
            cacheKey,
            async () => await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == query.LearningPathId && x.IsActive
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
            .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Basic)
            .SelectMany(m => m.LearningPathCourses)
            .Where(c => c.InternalCourseId.HasValue)
            .Select(c => c.InternalCourseId!.Value)
            .Distinct()
            .ToList();

        var internalIds = readModel.LearningPathMajors
            .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
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

        var internalInfos = (IEnumerable<InternalCourseInfoDto>)(resultInternal.Message.Response ?? Array.Empty<InternalCourseInfoDto>());
        var basicInfos = (IEnumerable<InternalCourseInfoDto>)(resultBasic.Message.Response ?? Array.Empty<InternalCourseInfoDto>());

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
            .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Basic)
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
            .Where(m => m.Type == (short)ConstantEnum.LearningPathMajor.Internal)
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
    /// Insert Internal and Major for editting DB
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<InsertInternalLearningPathResponse> InsertInternalMajorAndCourse(InsertInternalLearningPathCommand request, CancellationToken cancellationToken)
    {
        var response = new InsertInternalLearningPathResponse { Success = false };
        var currentUserEmail = _identityService.GetCurrentUser()!.Email;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var learningPath = await _learningPathCommandRepository.FirstOrDefaultAsync(
                x => x.PathId == request.PathId && x.IsActive, cancellationToken: cancellationToken);
            if (learningPath == null)
            {
                throw new Exception("Lộ trình học tập không tồn tại");
            }
            var major = new LearningPathMajor
            {
                LearningPathMajorId = Guid.NewGuid(),
                PathId = request.PathId,
                MajorCode = "BASE",
                Reason = string.Empty,
                Type = (short)(
                    request.MajorType is (int)ConstantEnum.LearningPathMajor.Basic
                                     or (int)ConstantEnum.LearningPathMajor.Internal
                        ? request.MajorType
                        : 0
                )
            };

            await _learningPathMajorCommandRepository.AddAsync(major, currentUserEmail);

            var newCourses = new List<LearningPathCourse>();
            if (request.Courses?.Any() == true)
            {
                newCourses = request.Courses.Select(c => new LearningPathCourse
                {
                    LearningPathCourseId = Guid.NewGuid(),
                    LearningPathMajorId = major.LearningPathMajorId,
                    InternalCourseId = c.courseId
                }).ToList();

                await _learningPathCourseCommandRepository.AddRangeAsync(newCourses, currentUserEmail);
            }

            // Save to db and update collection
            await _unitOfWork.SaveChangesAsync(currentUserEmail, cancellationToken);
            major.LearningPathCourses = newCourses;
            _unitOfWork.Store(LearningPathMajorCollection.FromWriteModel(major));
            var lpRead = await _learningPathQueryRepository.FirstOrDefaultAsync(
                x => x.PathId == request.PathId && x.IsActive
            );
            if (lpRead != null)
            {
                lpRead.LearningPathMajors.Add(LearningPathMajorCollection.FromWriteModel(major));
                _unitOfWork.Store(lpRead);
            }
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync($"learning_path:select:{request.PathId}");
            await _unitOfWork.CacheRemoveAsync($"learning_path_major:list:{request.PathId}");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm chuyên ngành vào lộ trình học tập");
            return true;
        }, cancellationToken);
        return response;
    }
}