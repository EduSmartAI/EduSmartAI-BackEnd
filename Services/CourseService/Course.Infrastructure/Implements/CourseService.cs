using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents;
using BuildingBlocks.Messaging.Events.CourseService.QuizCourseSelectEvents;
using BuildingBlocks.Pagination;
using Course.Application.Courses.Commands.CreateCourse;
using Course.Application.Courses.Commands.EnrollCourse;
using Course.Application.Courses.Commands.UpdateCourse;
using Course.Application.Courses.Commands.UpdateCourseModules;
using Course.Application.Courses.Queries.CheckEnrollment;
using Course.Application.Courses.Queries.GetCourseById;
using Course.Application.Courses.Queries.GetCourseBySlug;
using Course.Application.Courses.Queries.GetCourses;
using Course.Application.Courses.Queries.GetCourseTags;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.DTOs.CourseTagsDTO;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO;
using Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO;
using Course.Application.DTOs.ModulesDTO.ModuleMaterialDTO;
using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;
using Course.Application.DTOs.QuizDTO;
using Course.Application.DTOs.UserLessonProgressDTO;
using Course.Application.Interfaces;
using Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress;
using Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress;
using Course.Domain.Enum;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Course.Infrastructure.Caching;
using Course.Infrastructure.Extensions;
using FluentValidation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using System.Linq.Expressions;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Infrastructure.Implements
{
	public class CourseService(
		ICommandRepository<CourseEntity> _courseRepository,
		ICommandRepository<CourseStudentEnrollment> _enrollmentRepository,
		IQueryRepository<CourseStudentEnrollmentCollection> _enrollmentQueryRepository,
		ICommandRepository<Tag> _tagRepository,
		IUnitOfWork unitOfWork,
		IDatabase _cache,
		IIdentityService _identityService,
		IRequestClient<QuizCourseInsertEvent> _quizCourseClient,
		ICommandRepository<ModuleQuiz> _moduleQuizRepository,
		ICommandRepository<LessonQuiz> _lessonQuizRepository,
		IRequestClient<QuizCourseSelectEvent> _quizSelectClient,
		ICommandRepository<UserLessonProgress> _userLessonProgress,
		ICommandRepository<UserModuleProgress> _userModuleProgressQuery,
		ICommandRepository<UserCourseProgress> _userCourseProgressQuery,
		ICommandRepository<Lesson> _lessonRepository) : ICourseService
	{
		#region Service for Lecture & Guest

		/// <summary>
		/// Get all courses with pagination and optional filtering
		/// </summary>
		/// <param name="pagination"></param>
		/// <param name="query"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCoursesResponse> GetAllAsync(
			PaginationRequest pagination,
			CourseQuery? query = null,
			CancellationToken ct = default)
		{
			var response = new GetCoursesResponse() { Success = false };

			// Generate cache key based on pagination and query parameters
			var cacheKey = GenerateCacheKeyForGetAll(pagination, query);

			// Try to get from cache first
			var cached = await _cache.GetAsync<PaginatedResult<CourseDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.Message = "OK (from cache)";
				response.Response = cached;
				return response;
			}

			// Build predicate dynamic theo filter
			Expression<Func<CourseEntity, bool>>? predicate = null;

			if (query is not null)
			{
				// Start with 'true' and &=
				Expression<Func<CourseEntity, bool>> Acc(Expression<Func<CourseEntity, bool>> left,
					Expression<Func<CourseEntity, bool>> right)
					=> left is null ? right : left.AndAlso(right);

				// helpers
				Expression<Func<CourseEntity, bool>> True() => x => true;

				var pred = True();

				if (!string.IsNullOrWhiteSpace(query.Search))
				{
					var search = query.Search.Trim();
					pred = Acc(pred, x =>
						EF.Functions.ILike(x.Title ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.Description ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.ShortDescription ?? "", $"%{search}%") ||
						EF.Functions.ILike(x.Slug ?? "", $"%{search}%")
					);
				}

				if (!string.IsNullOrWhiteSpace(query.SubjectCode))
				{
					var code = query.SubjectCode.Trim();

					pred = Acc(pred, x => x.Subject != null &&
										  EF.Functions.ILike(x.Subject.SubjectCode ?? "", $"%{code}%"));
				}

				if (query.IsActive is bool isActive)
					pred = Acc(pred, x => x.IsActive == isActive);

				if (query.LectureId.HasValue)
					pred = Acc(pred, x => x.TeacherId == query.LectureId.Value);

				predicate = pred;
			}

			// Chọn orderBy mặc định theo COurse mới nhất
			Expression<Func<CourseEntity, object>> orderBy = x => x.UpdatedAt;

			var orderByDescending = true; // mặc định: mới nhất

			switch (query?.SortBy ?? CourseSortBy.Latest)
			{
				case CourseSortBy.Popular:
					orderBy = x => x.LearnerCount;
					orderByDescending = true;
					break;
				case CourseSortBy.TitleAsc:
					orderBy = x => x.Title!;
					orderByDescending = false;
					break;
				case CourseSortBy.TitleDesc:
					orderBy = x => x.Title!;
					orderByDescending = true;
					break;
				case CourseSortBy.Latest:
					orderBy = x => x.UpdatedAt;
					orderByDescending = true;
					break;
				default:
					orderByDescending = true;
					break;
			}

			// PaginationRequest đang 0-based (PageIndex). Chuyển sang 1-based để an toàn.
			var pageNumber = pagination.PageIndex + 1;
			var pageSize = pagination.PageSize;

			var page = await _courseRepository.PagedAsync(
				pageNumber: pageNumber,
				pageSize: pageSize,
				predicate: predicate,
				orderBy: orderBy,
				orderByDescending: orderByDescending,
				cancellationToken: ct,
				x => x.Subject
			// includes: nếu cần eager load, thêm tại đây: x => x.Subject, x => x.Modules ...
			);

			// Map entity -> DTO
			var items = page.Items.Select(MapToDto).ToList();

			var result = new PaginatedResult<CourseDto>(
				pageIndex: pagination.PageIndex,
				pageSize: pagination.PageSize,
				totalCount: page.TotalCount,
				data: items
			);

			// Cache the result for future requests
			await _cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));

			response.Success = true;
			response.Response = result;
			response.SetMessage(MessageId.I00001, "Lấy danh sách khóa học");

			return response;
		}

		/// <summary>
		/// Get course details by ID for guest users
		/// </summary>
		/// <param name="Id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseByIdForGuestResponse> GetCourseByIdForGuestAsync(Guid Id, CancellationToken ct = default)
		{
			var response = new GetCourseByIdForGuestResponse() { Success = false };

			var cacheKey = $"CourseDetailForGuest:{Id}";
			var cached = await _cache.GetAsync<CourseDetailForGuestDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.CourseId == Id && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với mã {Id}");
				return response;
			}

			var detail = MapCourseDetailForGuest(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by Slug for guest users
		/// </summary>
		/// <param name="Slug"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<GetCourseBySlugForGuestResponse> GetCourseBySlugForGuestAsync(string Slug, CancellationToken ct = default)
		{
			var response = new GetCourseBySlugForGuestResponse() { Success = false };

			var cacheKey = $"CourseDetailBySlugForGuest:{Slug}";
			var cached = await _cache.GetAsync<CourseDetailForGuestDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.Slug == Slug && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với Slug {Slug}");
				return response;
			}

			var detail = MapCourseDetailForGuest(entity);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho khách");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by ID for lecturer (instructor) users
		/// </summary>
		/// <param name="id"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseByIdForLectureResponse> GetCourseByIdForLectureAsync(Guid Id, CancellationToken ct = default)
		{
			var response = new GetCourseByIdForLectureResponse() { Success = false };

			var cacheKey = $"CourseDetailForLecture:{Id}";
			var cached = await _cache.GetAsync<CourseDetailForLectureDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.CourseId == Id && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với mã {Id}");
				return response;
			}

			// 1) Lấy tất cả ModuleId & LessonId còn active
			var moduleIds = entity.Modules.Where(m => m.IsActive).Select(m => m.ModuleId).ToList();
			var lessonIds = entity.Modules.SelectMany(m => m.Lessons)
										  .Where(l => l.IsActive)
										  .Select(l => l.LessonId).ToList();

			// 2) Load mapping (module->quiz, lesson->quiz)
			var moduleMaps = await _moduleQuizRepository.Find(x => moduleIds.Contains(x.ModuleId), isTracking: false, ct)
												   .Select(x => new { x.ModuleId, x.QuizId })
												   .ToListAsync(ct);

			var lessonMaps = await _lessonQuizRepository.Find(x => lessonIds.Contains(x.LessonId), isTracking: false, ct)
												   .Select(x => new { x.LessonId, x.QuizId })
												   .ToListAsync(ct);

			// 3) Gọi song song QuizService, dedupe quizIds
			var allQuizIds = moduleMaps.Select(m => m.QuizId).Concat(lessonMaps.Select(l => l.QuizId))
									   .Distinct().ToList();

			var quizTasks = allQuizIds.ToDictionary(id => id, id => FetchQuizAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?

			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			var detail = MapCourseDetailForLecture(
							entity,
							moduleQuizIdByModuleId,
							lessonQuizIdByLessonId,
							quizDict
						);
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Get course details by Slug for lecturer (instructor) users
		/// </summary>
		/// <param name="Slug"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<GetCourseBySlugForLectureResponse> GetCourseBySlugForLectureAsync(string Slug, CancellationToken ct = default)
		{
			var response = new GetCourseBySlugForLectureResponse() { Success = false };

			var cacheKey = $"CourseDetailBySlugForLecture:{Slug}";
			var cached = await _cache.GetAsync<CourseDetailForLectureDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			var baseQuery = _courseRepository
				.Find(x => x.Slug == Slug && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ct => ct.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);

			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với Slug {Slug}");
				return response;
			}

			// 1) Lấy tất cả ModuleId & LessonId còn active
			var moduleIds = entity.Modules.Where(m => m.IsActive).Select(m => m.ModuleId).ToList();
			var lessonIds = entity.Modules.SelectMany(m => m.Lessons)
										  .Where(l => l.IsActive)
										  .Select(l => l.LessonId).ToList();

			// 2) Load mapping (module->quiz, lesson->quiz)
			var moduleMaps = await _moduleQuizRepository.Find(x => moduleIds.Contains(x.ModuleId), isTracking: false, ct)
												   .Select(x => new { x.ModuleId, x.QuizId })
												   .ToListAsync(ct);

			var lessonMaps = await _lessonQuizRepository.Find(x => lessonIds.Contains(x.LessonId), isTracking: false, ct)
												   .Select(x => new { x.LessonId, x.QuizId })
												   .ToListAsync(ct);

			// 3) Gọi song song QuizService, dedupe quizIds
			var allQuizIds = moduleMaps.Select(m => m.QuizId).Concat(lessonMaps.Select(l => l.QuizId))
									   .Distinct().ToList();

			var quizTasks = allQuizIds.ToDictionary(id => id, id => FetchQuizAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?

			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			var detail = MapCourseDetailForLecture(
							entity,
							moduleQuizIdByModuleId,
							lessonQuizIdByLessonId,
							quizDict
						);

			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(10));
			var modulesCount = entity.Modules.Count(m => m.IsActive);
			var lessonsCount = entity.Modules.Sum(m => m.Lessons.Count(l => l.IsActive));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy chi tiết khóa học cho giảng viên");
			response.Response = detail;
			response.ModulesCount = modulesCount;
			response.LessonsCount = lessonsCount;
			return response;
		}

		/// <summary>
		/// Check if current user is enrolled in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CheckEnrollmentResponse> CheckEnrollmentAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new CheckEnrollmentResponse() { Success = false };

			// Get current user id from token
			var currentUser = _identityService.GetCurrentUser()!;

			var cacheKey = $"enroll:status:{currentUser.UserId}:{courseId}";

			// Check if user is enrolled in the course
			var enrollment = await _enrollmentQueryRepository.GetOrSetAsync(
				cacheKey,
				() => _enrollmentQueryRepository.FirstOrDefaultAsync(x =>
					x.CourseId == courseId &&
					x.UserId == currentUser.UserId &&
					x.IsActive),
				TimeSpan.FromMinutes(5)
			);

			if (enrollment is null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00000, "Người dùng chưa tham gia khóa học");
				response.Response = false;
				return response;
			}

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Người dùng đã tham gia khóa học");
			response.Response = true;

			return response;
		}

		/// <summary>
		/// Enroll current user in a course
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<EnrollInCourseResponse> EnrollCourseAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new EnrollInCourseResponse() { Success = false };

			// Get current user id from token
			var currentUser = _identityService.GetCurrentUser()!;

			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			// Check if user is already enrolled in the course
			var existingEnrollment = await _enrollmentRepository
				.Find(x => x.CourseId == courseId && x.UserId == currentUser.UserId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (existingEnrollment is not null)
			{
				response.SetMessage(MessageId.E11004, "Người dùng đã tham gia khóa học");
				return response;
			}

			// Check if course exists
			var course = await _courseRepository
				.Find(x => x.CourseId == courseId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (course is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với mã {courseId}");
				return response;
			}

			// Create new enrollment
			var enrollment = new CourseStudentEnrollment
			{
				EnrollmentId = Guid.NewGuid(),
				CourseId = courseId,
				UserId = currentUser.UserId,
				StartedAt = DateTime.UtcNow,
				ExpiresAt = null,
			};

			// Save to database within a transaction
			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _enrollmentRepository.AddAsync(enrollment, currentUser.Email);
							await unitOfWork.SaveChangesAsync(ct);

							unitOfWork.Store(CourseStudentEnrollmentCollection.FromWriteModel(enrollment));
							await unitOfWork.SessionSaveChangesAsync();
							return true;
						}, ct);

			// Respond success
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Người dùng đã tham gia khóa học thành công");

			return response;
		}

		/// <summary>
		/// Create course with modules + lessons
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateCourseResponse> CreateAsync(CreateCourseDto dto, CancellationToken ct = default)
		{
			var response = new CreateCourseResponse() { Success = false };

			// Get current user id
			var currentUser = _identityService.GetCurrentUser()!;

			var title = dto.Title?.Trim();

			var slug = await GenerateUniqueSlugAsync(dto.Title!, ct);

			var course = new CourseEntity
			{
				CourseId = Guid.NewGuid(),
				TeacherId = currentUser.UserId,
				SubjectId = dto.SubjectId,
				Title = title,
				ShortDescription = dto.ShortDescription,
				Description = dto.Description,
				Slug = slug,
				CourseImageUrl = dto.CourseImageUrl,
				//Status = dto.Status,          // nếu enum: dto.Status; nếu string: giữ nguyên
				LearnerCount = 0,
				DurationMinutes = dto.DurationMinutes,
				Level = dto.Level,
				Price = dto.Price,
				DealPrice = dto.DealPrice,
				CourseIntroVideoUrl = dto.CourseIntroVideoUrl,
			};

			// Pending quizzes để gửi sau khi commit (module/lesson)
			var pendingModuleQuizzes = new List<(Guid ModuleId, CreateQuizDto Quiz)>();
			var pendingLessonQuizzes = new List<(Guid ModuleId, Guid LessonId, CreateQuizDto Quiz)>();

			// 3) Mục tiêu học tập (CourseObjectives) – optional
			if (dto.Objectives is { Count: > 0 })
			{
				foreach (var (obj, idx) in dto.Objectives
							 .OrderBy(o => o.PositionIndex)
							 .Select((o, i) => (o, i)))
				{
					course.CourseObjectives.Add(new CourseObjective
					{
						ObjectiveId = Guid.NewGuid(),
						CourseId = course.CourseId,
						Content = obj.Content,
						PositionIndex = obj.PositionIndex > 0 ? obj.PositionIndex : idx,
						IsActive = true,
						CreatedBy = currentUser.Email,
						UpdatedBy = currentUser.Email
					});
				}
			}

			// 4) Yêu cầu trước khi học (CourseRequirements) – optional
			if (dto.Requirements is { Count: > 0 })
			{
				foreach (var (req, idx) in dto.Requirements
							 .OrderBy(r => r.PositionIndex)
							 .Select((r, i) => (r, i)))
				{
					course.CourseRequirements.Add(new CourseRequirement
					{
						RequirementId = Guid.NewGuid(),
						CourseId = course.CourseId,
						Content = req.Content,
						PositionIndex = req.PositionIndex > 0 ? req.PositionIndex : idx,
						IsActive = true,
						CreatedBy = currentUser.Email,
						UpdatedBy = currentUser.Email
					});
				}
			}

			// 5) Course Tags – optional
			if (dto.CourseTags is { Count: > 0 })
			{
				foreach (var courseTag in dto.CourseTags)
				{
					course.CourseTags.Add(new CourseTag
					{
						CourseId = course.CourseId,
						TagId = courseTag.TagId,
					});
				}
			}

			// 5.1) Course Audiences – optional (1-N)
			if (dto.Audiences is { Count: > 0 })
			{
				// Validate không trùng PositionIndex trong payload
				EnsureDistinct(dto.Audiences.Select(a => a.PositionIndex),
					"Audience PositionIndex must be unique within the course.");

				// Tập position đã dùng (đang rỗng vì course mới)
				var takenIdx = new HashSet<int>();

				foreach (var (aud, idx) in dto.Audiences
							 .OrderBy(a => a.PositionIndex)
							 .Select((a, i) => (a, i)))
				{
					// fallback index nếu client gửi <= 0
					var pos = aud.PositionIndex > 0 ? aud.PositionIndex : NextIndex(takenIdx);
					if (takenIdx.Contains(pos)) pos = NextIndex(takenIdx);
					takenIdx.Add(pos);

					course.CourseAudiences.Add(new CourseAudience
					{
						AudienceId = Guid.NewGuid(),      // sinh ID ngay
						CourseId = course.CourseId,
						Content = aud.Content,
						PositionIndex = pos,
						IsActive = aud.IsActive,
						CreatedBy = currentUser.Email,
						UpdatedBy = currentUser.Email
					});
				}
			}


			if (dto.Modules is { Count: > 0 })
			{
				// 6) Map Modules + Lessons (giữ thứ tự PositionIndex)
				foreach (var m in dto.Modules.OrderBy(x => x.PositionIndex))
				{
					var module = new Module
					{
						ModuleId = Guid.NewGuid(),
						CourseId = course.CourseId,
						ModuleName = m.ModuleName,
						Description = m.Description,
						PositionIndex = m.PositionIndex,
						IsCore = m.IsCore,
						DurationMinutes = m.DurationMinutes,
						Level = m.Level,
						IsActive = true,
						CreatedBy = currentUser.Email,
						UpdatedBy = currentUser.Email
					};

					// Module Objectives (optional)
					if (m.Objectives is { Count: > 0 })
					{
						foreach (var (mo, idx) in m.Objectives
									 .OrderBy(o => o.PositionIndex)
									 .Select((o, i) => (o, i)))
						{
							module.ModuleObjectives.Add(new ModuleObjective
							{
								ObjectiveId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Content = mo.Content,
								PositionIndex = mo.PositionIndex > 0 ? mo.PositionIndex : idx,
								IsActive = true,
								CreatedBy = currentUser.Email,
								UpdatedBy = currentUser.Email
							});
						}
					}

					// Lessons (required, at least 1)
					if (m.Lessons is { Count: > 0 })
					{
						foreach (var l in m.Lessons.OrderBy(x => x.PositionIndex))
						{
							var lesson = new Lesson
							{
								LessonId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = l.Title,
								VideoUrl = l.VideoUrl,
								VideoDurationSec = l.VideoDurationSec,
								PositionIndex = l.PositionIndex,
								IsActive = true,
								CreatedBy = currentUser.Email,
								UpdatedBy = currentUser.Email
							};

							module.Lessons.Add(lesson);

							if (l.LessonQuiz is not null)
								pendingLessonQuizzes.Add((module.ModuleId, lesson.LessonId, l.LessonQuiz));
						}
					}

					// Discussions (optional)
					if (m.Discussions is { Count: > 0 })
					{
						foreach (var d in m.Discussions)
						{
							module.ModuleDiscussions.Add(new ModuleDiscussion
							{
								DiscussionId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = d.Title?.Trim(),
								Description = d.Description,
								DiscussionQuestion = d.DiscussionQuestion,
								IsActive = true,
								CreatedBy = currentUser.Email,
								UpdatedBy = currentUser.Email
							});
						}
					}

					// Materials (optional)
					if (m.Materials is { Count: > 0 })
					{
						foreach (var mat in m.Materials)
						{
							module.ModuleMaterials.Add(new ModuleMaterial
							{
								MaterialId = Guid.NewGuid(),
								ModuleId = module.ModuleId,
								Title = mat.Title?.Trim(),
								Description = mat.Description,
								FileUrl = mat.FileUrl,
								IsActive = true
							});
						}
					}

					if (m.ModuleQuiz is not null)
						pendingModuleQuizzes.Add((module.ModuleId, m.ModuleQuiz));


					course.Modules.Add(module);
				}
			}

			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _courseRepository.AddAsync(course, currentUser.Email);
							await unitOfWork.SaveChangesAsync(ct);

							return true; // yêu cầu của BeginTransactionAsync: trả true để commit
						}, ct);


			// ===== Request/Response tới QuizService theo event QuizCourseInsertEvent (ModuleQuizInsertEvent/LessonQuizInsertEvent) =====

			// Module-level

			foreach (var (moduleId, quizDto) in pendingModuleQuizzes)
			{
				var payload = ToQuizCourseInsertEvent(
					currentUser.Email,
					quizDto
				);

				var resp = await _quizCourseClient.GetResponse<QuizCourseInsertEventResponse>(payload, ct);

				if (!resp.Message.Success)
					throw new InvalidOperationException($"{resp.Message.MessageId}: {resp.Message.Message}");

				var quizId = resp.Message.Response?.QuizId ?? Guid.Empty;
				if (quizId == Guid.Empty)
					throw new InvalidOperationException("QuizId is empty.");

				await _moduleQuizRepository.AddAsync(new ModuleQuiz { ModuleId = moduleId, QuizId = quizId });
				await unitOfWork.SaveChangesAsync(ct);


			}

			// Lesson-level
			foreach (var (moduleId, lessonId, quizDto) in pendingLessonQuizzes)
			{
				var payload = ToQuizCourseInsertEvent(
					currentUser.Email,
					quizDto
				);

				var resp = await _quizCourseClient.GetResponse<QuizCourseInsertEventResponse>(payload, ct);

				if (!resp.Message.Success)
					throw new InvalidOperationException($"{resp.Message.MessageId}: {resp.Message.Message}");

				var quizId = resp.Message.Response?.QuizId ?? Guid.Empty;
				if (quizId == Guid.Empty)
					throw new InvalidOperationException("QuizId is empty.");

				await _lessonQuizRepository.AddAsync(new LessonQuiz { LessonId = lessonId, QuizId = quizId });
				await unitOfWork.SaveChangesAsync(ct);

			}


			// Clear cache after successful creation
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			response.Response = course.CourseId.ToString();
			response.Success = true;
			response.SetMessage(MessageId.I00000, "Tạo khóa học thành công");

			return response;
		}

		/// <summary>
		/// Update course and its related data (objectives, requirements, audiences, course-tags)
		/// Delete Course: Set IsActive = false (soft delete)
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateCourseResponse> UpdateAsync(Guid courseId, UpdateCourseDto dto, CancellationToken ct = default)
		{
			var response = new UpdateCourseResponse() { Success = false };
			// 1. Validate PositionIndex uniqueness
			ValidateCourseDetailPositionIndexes(dto);

			// 2. Get existing course with all related data
			var existingCourse = await _courseRepository
				.Find(x => x.CourseId == courseId, isTracking: true, ct,
					x => x.CourseObjectives,
					x => x.CourseRequirements,
					x => x.CourseAudiences,
					x => x.CourseTags)
				.FirstOrDefaultAsync(ct);

			if (existingCourse is null)
			{
				response.Message = $"Course {courseId} not found";
				return response;
			}

			var currentUser = _identityService.GetCurrentUser()!;

			// 3. Update basic course properties
			existingCourse.TeacherId = dto.TeacherId;
			existingCourse.SubjectId = dto.SubjectId;
			existingCourse.Title = dto.Title?.Trim() ?? string.Empty;
			existingCourse.ShortDescription = dto.ShortDescription;
			existingCourse.Description = dto.Description;
			existingCourse.CourseImageUrl = dto.CourseImageUrl;
			existingCourse.DurationMinutes = dto.DurationMinutes;
			existingCourse.Level = dto.Level;
			existingCourse.Price = dto.Price;
			existingCourse.DealPrice = dto.DealPrice;
			existingCourse.IsActive = dto.IsActive;

			// 4. Handle slug update with uniqueness check
			if (!string.IsNullOrWhiteSpace(dto.Slug))
			{
				var newSlug = dto.Slug.Trim();
				if (newSlug != existingCourse.Slug)
				{
					existingCourse.Slug = await EnsureUniqueSlugForUpdateAsync(courseId, newSlug, ct, allowRandomSuffix: true);
				}
			}

			// 5. Update CourseObjectives
			await UpdateCourseObjectivesAsync(existingCourse, dto.Objectives, currentUser.Email);

			// 6. Update CourseRequirements
			await UpdateCourseRequirementsAsync(existingCourse, dto.Requirements, currentUser.Email);

			// 6.1 Update CourseAudiences
			await UpdateCourseAudiencesAsync(existingCourse, dto.Audiences, currentUser.Email);

			// 6.2 Update CourseTags
			await UpdateCourseTagsAsync(existingCourse, dto.CourseTags);

			try
			{
				// 7. Save changes in transaction
				await unitOfWork.BeginTransactionAsync(async () =>
				{
					_courseRepository.Update(existingCourse, currentUser.Email);
					await unitOfWork.SaveChangesAsync(ct);
					return true;
				}, ct);
			}
			catch (DbUpdateConcurrencyException)
			{
				response.SetMessage(MessageId.E11003, "Cập nhật khóa học thất bại do xung đột dữ liệu. Vui lòng thử lại.");
				return response;
			}
			catch (Exception ex)
			{
				response.SetMessage(MessageId.E00000, $"Cập nhật khóa học thất bại: {ex.Message}");
				return response;
			}


			// 8. Clear cache after successful update
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			// 9. Return updated course detail
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật khóa học thành công");
			return response;
		}

		/// <summary>
		/// Update multiple modules in a course (bulk update)
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpdateCourseModulesResponse> UpdateCourseModulesAsync(Guid courseId, UpdateCourseModulesDto dto, CancellationToken ct = default)
		{
			var response = new UpdateCourseModulesResponse() { Success = false };
			// 1. Validate PositionIndex uniqueness across all modules
			ValidateCourseModulesPositionIndexes(dto);

			// 2. Get existing course with all modules and related data
			var existingCourse = await _courseRepository
				.Find(x => x.CourseId == courseId, isTracking: true, ct,
					x => x.Modules)
				.Include(x => x.Modules)
					.ThenInclude(m => m.ModuleObjectives)
				.Include(x => x.Modules)
					.ThenInclude(m => m.Lessons)
				.Include(x => x.Modules)
					.ThenInclude(m => m.ModuleDiscussions)
				.Include(x => x.Modules)
					.ThenInclude(m => m.ModuleMaterials)
				.FirstOrDefaultAsync(ct);

			if (existingCourse is null)
			{
				response.Message = $"Course {courseId} not found";
				return response;
			}

			var currentUser = _identityService.GetCurrentUser();

			if (currentUser is null)
			{
				currentUser = new IdentityEntity
				{
					UserId = Guid.Empty,
					FullName = "system",
					Email = "system"
				};
			}

			// 3. Update modules based on payload
			await UpdateCourseModulesInternalAsync(existingCourse, dto.Modules, currentUser.Email);

			// 4. Save changes in transaction
			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_courseRepository.Update(existingCourse, currentUser.Email);
				await unitOfWork.SaveChangesAsync(ct);
				return true;
			}, ct);

			// 5. Clear cache after successful update
			await ClearGetAllCacheAsync();
			await ClearCourseTagsCacheAsync();

			// 6. Return response
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Cập nhật các module của khóa học");

			return response;
		}

		/// <summary>
		/// Get all course tags
		/// </summary>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseTagsResponse> GetCourseTagsAsync(CancellationToken ct = default)
		{
			var response = new GetCourseTagsResponse() { Success = false };

			// generate cache key for course tags
			var cacheKey = "CourseTags:GetAll";

			// get from cache first
			var cached = await _cache.GetAsync<List<CourseTagDetailsDto>>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Lấy danh sách tag của khóa học");
				response.Response = cached;
				return response;
			}

			// get from database
			var tags = await _tagRepository
				.Find(predicate: null, isTracking: false, cancellationToken: ct)
				.Select(t => new CourseTagDetailsDto(
					t.TagId,
					t.TagName
				))
				.ToListAsync(ct);

			// Cache the result for future requests
			await _cache.SetAsync(cacheKey, tags, TimeSpan.FromMinutes(10));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy danh sách tag của khóa học");
			response.Response = tags;

			return response;
		}

		#endregion

		#region Service for Student (Learner)

		/// <summary>
		/// Get course details by Id for student (learner) users
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseByIdForStudentResponse> GetCourseByIdForStudentAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new GetCourseByIdForStudentResponse() { Success = false };

			// Lấy userId từ token (soft FK, không join bảng Users)
			var currentUser = _identityService.GetCurrentUser()!;
			var userId = currentUser.UserId;

			// Cache theo user + course (nội dung kèm progress riêng từng user)
			var cacheKey = $"CourseDetailForStudent:{courseId}:{userId}";
			var cached = await _cache.GetAsync<CourseDetailForStudentDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Chi tiết khóa học cho học viên (cached)");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			// 1) Kiểm tra enrollment (must be active)
			var activeEnroll = await _enrollmentRepository
				.Find(x => x.CourseId == courseId && x.UserId == userId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);
			if (activeEnroll is null)
			{
				response.SetMessage(MessageId.E00000, "Bạn chưa đăng ký (enroll) khóa học này hoặc thời hạn học đã không còn hiệu lực.");
				return response;
			}

			// 2) Nạp course + modules + lessons (y hệt bản lecture)
			var baseQuery = _courseRepository
				.Find(x => x.CourseId == courseId && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ctg => ctg.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);
			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học {courseId}");
				return response;
			}

			// 3) Lấy mapping quiz (module/lesson) & gọi QuizService (y hệt bản lecture)
			var moduleIds = entity.Modules.Where(m => m.IsActive).Select(m => m.ModuleId).ToList();
			var lessonIds = entity.Modules.SelectMany(m => m.Lessons).Where(l => l.IsActive).Select(l => l.LessonId).ToList();

			var moduleMaps = await _moduleQuizRepository.Find(x => moduleIds.Contains(x.ModuleId), isTracking: false, ct)
				.Select(x => new { x.ModuleId, x.QuizId }).ToListAsync(ct);

			var lessonMaps = await _lessonQuizRepository.Find(x => lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new { x.LessonId, x.QuizId }).ToListAsync(ct);

			var allQuizIds = moduleMaps.Select(m => m.QuizId).Concat(lessonMaps.Select(l => l.QuizId)).Distinct().ToList();
			var quizTasks = allQuizIds.ToDictionary(id => id, id => FetchQuizAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?
			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			// 4) Tải progress bài học của user (để tick bài đã hoàn thành + resume)
			var lessonProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new LessonProgressSnap(
					x.LessonId, x.Status, x.LastPositionSec ?? 0, x.CompletedAt))
				.ToListAsync(ct);
			var progressByLessonId =
				lessonProgress.ToDictionary(x => x.LessonId, x => x);

			// 5) Tải module/course progress snapshot (nếu đã có từ trigger); nếu chưa có sẽ fallback tự tính
			// module snapshot
			var moduleProgress = await _userModuleProgressQuery
				.Find(x => x.UserId == userId && moduleIds.Contains(x.ModuleId), isTracking: false, ct)
				.Select(x => new ModuleProgressSnap(
					x.ModuleId, x.LessonsTotal, x.LessonsCompleted, (x.PercentCompleted ?? 0m),
					x.Status, x.StartedAt, x.CompletedAt))
				.ToListAsync(ct);
			var moduleProgressById = moduleProgress.ToDictionary(x => x.ModuleId, x => x);

			// course snapshot
			var courseProgress = await _userCourseProgressQuery
				.Find(x => x.UserId == userId && x.CourseId == courseId, isTracking: false, ct)
				.Select(x => new CourseProgressSnap(
					x.LessonsTotal, x.LessonsCompleted, (x.PercentCompleted ?? 0m),
					x.Status, x.StartedAt, x.CompletedAt))
				.FirstOrDefaultAsync(ct);

			// 6) Map sang DTO Student (tick bài, % module, % course chỉ tính core)
			var detail = MapCourseDetailForStudent(
				entity,
				moduleQuizIdByModuleId,
				lessonQuizIdByLessonId,
				quizDict,
				progressByLessonId,
				moduleProgressById,
				courseProgress,
				preferCoreForCourse: true // % course chỉ tính modules IsCore = true
			);

			// 8) Cache ngắn
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(5));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Chi tiết khóa học cho học viên");
			response.Response = detail;
			response.ModulesCount = detail.Modules.Count;
			response.LessonsCount = detail.Modules.Sum(m => m.Lessons.Count);
			return response;
		}

		/// <summary>
		/// Get course details by Slug for student (learner) users
		/// </summary>
		/// <param name="courseSlug"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetCourseBySlugForStudentResponse> GetCourseBySlugForStudentAsync(string courseSlug, CancellationToken ct = default)
		{
			var response = new GetCourseBySlugForStudentResponse() { Success = false };

			// Lấy userId từ token (soft FK, không join bảng Users)
			var currentUser = _identityService.GetCurrentUser()!;
			var userId = currentUser.UserId;

			// Cache theo user + course (nội dung kèm progress riêng từng user)
			var cacheKey = $"CourseDetailForStudent:{courseSlug}:{userId}";
			var cached = await _cache.GetAsync<CourseDetailForStudentDto>(cacheKey);
			if (cached is not null)
			{
				response.Success = true;
				response.SetMessage(MessageId.I00001, "Chi tiết khóa học cho học viên (cached)");
				response.Response = cached;
				response.ModulesCount = cached.Modules.Count;
				response.LessonsCount = cached.Modules.Sum(m => m.Lessons.Count);
				return response;
			}

			// Tìm courseId từ slug
			var courseEntity = await _courseRepository
				.Find(x => x.Slug == courseSlug && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);

			if (courseEntity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học với slug '{courseSlug}'");
				return response;
			}

			var courseId = courseEntity?.CourseId;

			// 1) Kiểm tra enrollment (must be active)
			var activeEnroll = await _enrollmentRepository
			.Find(x => x.CourseId == courseId && x.UserId == userId && x.IsActive, isTracking: false, ct)
			.FirstOrDefaultAsync(ct);
			if (activeEnroll is null)
			{
				response.SetMessage(MessageId.E00000, "Bạn chưa đăng ký (enroll) khóa học này hoặc thời hạn học đã không còn hiệu lực.");
				return response;
			}

			// 2) Nạp course + modules + lessons (y hệt bản lecture)
			var baseQuery = _courseRepository
				.Find(x => x.CourseId == courseId && x.IsActive, isTracking: false, ct)
				.Cast<CourseEntity>()
				.Include(x => x.Subject)
				.Include(x => x.CourseObjectives.Where(o => o.IsActive))
				.Include(x => x.CourseRequirements.Where(r => r.IsActive))
				.Include(x => x.CourseComments.Where(c => c.IsActive))
				.Include(x => x.CourseTags).ThenInclude(ctg => ctg.Tag)
				.Include(x => x.CourseRatings)
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleObjectives.Where(o => o.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleDiscussions.Where(d => d.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.ModuleMaterials.Where(mat => mat.IsActive))
				.Include(x => x.Modules.Where(m => m.IsActive)).ThenInclude(m => m.Lessons.Where(l => l.IsActive));

			var entity = await baseQuery.FirstOrDefaultAsync(ct);
			if (entity is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy khóa học {courseId}");
				return response;
			}

			// 3) Lấy mapping quiz (module/lesson) & gọi QuizService (y hệt bản lecture)
			var moduleIds = entity.Modules.Where(m => m.IsActive).Select(m => m.ModuleId).ToList();
			var lessonIds = entity.Modules.SelectMany(m => m.Lessons).Where(l => l.IsActive).Select(l => l.LessonId).ToList();

			var moduleMaps = await _moduleQuizRepository.Find(x => moduleIds.Contains(x.ModuleId), isTracking: false, ct)
				.Select(x => new { x.ModuleId, x.QuizId }).ToListAsync(ct);

			var lessonMaps = await _lessonQuizRepository.Find(x => lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new { x.LessonId, x.QuizId }).ToListAsync(ct);

			var allQuizIds = moduleMaps.Select(m => m.QuizId).Concat(lessonMaps.Select(l => l.QuizId)).Distinct().ToList();
			var quizTasks = allQuizIds.ToDictionary(id => id, id => FetchQuizAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?
			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			// 4) Tải progress bài học của user (để tick bài đã hoàn thành + resume)
			var lessonProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new LessonProgressSnap(
					x.LessonId, x.Status, x.LastPositionSec ?? 0, x.CompletedAt))
				.ToListAsync(ct);
			var progressByLessonId =
				lessonProgress.ToDictionary(x => x.LessonId, x => x);

			// 5) Tải module/course progress snapshot (nếu đã có từ trigger); nếu chưa có sẽ fallback tự tính
			// module snapshot
			var moduleProgress = await _userModuleProgressQuery
				.Find(x => x.UserId == userId && moduleIds.Contains(x.ModuleId), isTracking: false, ct)
				.Select(x => new ModuleProgressSnap(
					x.ModuleId, x.LessonsTotal, x.LessonsCompleted, (x.PercentCompleted ?? 0m),
					x.Status, x.StartedAt, x.CompletedAt))
				.ToListAsync(ct);
			var moduleProgressById = moduleProgress.ToDictionary(x => x.ModuleId, x => x);

			// course snapshot
			var courseProgress = await _userCourseProgressQuery
				.Find(x => x.UserId == userId && x.CourseId == courseId, isTracking: false, ct)
				.Select(x => new CourseProgressSnap(
					x.LessonsTotal, x.LessonsCompleted, (x.PercentCompleted ?? 0m),
					x.Status, x.StartedAt, x.CompletedAt))
				.FirstOrDefaultAsync(ct);

			// 6) Map sang DTO Student (tick bài, % module, % course chỉ tính core)
			var detail = MapCourseDetailForStudent(
				entity,
				moduleQuizIdByModuleId,
				lessonQuizIdByLessonId,
				quizDict,
				progressByLessonId,
				moduleProgressById,
				courseProgress,
				preferCoreForCourse: true // % course chỉ tính modules IsCore = true
			);

			// 8) Cache ngắn
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromMinutes(5));

			response.Success = true;
			response.SetMessage(MessageId.I00001, "Chi tiết khóa học cho học viên");
			response.Response = detail;
			response.ModulesCount = detail.Modules.Count;
			response.LessonsCount = detail.Modules.Sum(m => m.Lessons.Count);
			return response;
		}

		/// <summary>
		/// Create user lesson progress
		/// Call khi người dùng đã bắt đầu học 1 bài (lần đầu xem video)
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<CreateUserLessonProgressResponse> CreateUserLessonProgressAsync(CreateUserLessonProgressDto dto, CancellationToken ct = default)
		{
			var response = new CreateUserLessonProgressResponse() { Success = false };
			var currentUser = _identityService.GetCurrentUser()!;
			var userId = currentUser.UserId;

			// Validate lesson exists and is active
			var lesson = await _lessonRepository
				.Find(x => x.LessonId == dto.LessonId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);
			if (lesson is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy bài học {dto.LessonId}");
				return response;
			}
			// Check if progress already exists
			var existingProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && x.LessonId == dto.LessonId, isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			if (existingProgress is not null)
			{
				response.SetMessage(MessageId.E00000, "Đã tồn tại tiến độ học cho bài học này.");
				return response;
			}

			var now = DateTime.UtcNow;

			// Create new progress record
			var progress = new UserLessonProgress
			{
				UserId = userId,
				LessonId = dto.LessonId,
				Status = (short)LessonStatus.NotStarted,
				CreatedAt = now,
			};


			await unitOfWork.BeginTransactionAsync(async () =>
			{
				await _userLessonProgress.AddAsync(progress);
				await unitOfWork.SaveChangesAsync(ct);

				unitOfWork.Store(UserLessonProgressCollection.FromWriteModel(progress));
				await unitOfWork.SessionSaveChangesAsync();
				return true;
			}, ct);

			// Clear relevant caches
			await ClearCourseDetailForStudentCacheAsync();

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00000, "Tạo tiến độ học bài học thành công.");
			return response;
		}

		public async Task<UpdateUserLessonProgressResponse> UpdateUserLessonProgressAsync(UpdateUserLessonProgressDto dto, CancellationToken ct = default)
		{
			var response = new UpdateUserLessonProgressResponse() { Success = false };

			// Get current user
			var currentUser = _identityService.GetCurrentUser()!;
			var userId = currentUser.UserId;

			// Validate lesson exists and is active
			var lesson = await _lessonRepository
				.Find(x => x.LessonId == dto.LessonId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);
			if (lesson is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy bài học {dto.LessonId}");
				return response;
			}

			// Check if progress already exists
			var existingProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && x.LessonId == dto.LessonId, isTracking: true, ct)
				.FirstOrDefaultAsync(ct);
			if (existingProgress is null)
			{
				response.SetMessage(MessageId.E00000, "Không tìm thấy tiến độ học cho bài học này.");
				return response;
			}
			var now = DateTime.UtcNow;

			// Update progress record
			existingProgress.Status = dto.Status;
			existingProgress.LastPositionSec = dto.LastPositionSec;
			existingProgress.DurationWatchedSec = dto.DurationWatchedSec;
			if (dto.Status == (short)LessonStatus.Completed)
			{
				existingProgress.CompletedAt = now;
			}
			else if (dto.Status == (short)LessonStatus.InProgress && existingProgress.CompletedAt.HasValue)
			{
				// Nếu chuyển từ Completed về InProgress thì xóa CompletedAt
				existingProgress.CompletedAt = null;
			}

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_userLessonProgress.Update(existingProgress);
				await unitOfWork.SaveChangesAsync(ct);

				unitOfWork.Store(UserLessonProgressCollection.FromWriteModel(existingProgress));
				await unitOfWork.SessionSaveChangesAsync();
				return true;
			}, ct);

			// Clear relevant caches
			await ClearCourseDetailForStudentCacheAsync();

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Cập nhật tiến độ học bài học thành công.");
			return response;
		}

		#endregion

		#region Private Helper Methods

		/// <summary>
		/// Generate cache key for GetAllAsync method based on pagination and query parameters
		/// </summary>
		/// <param name="pagination"></param>
		/// <param name="query"></param>
		/// <returns></returns>
		private static string GenerateCacheKeyForGetAll(PaginationRequest pagination, CourseQuery? query)
		{
			var keyParts = new List<string> { "Courses:GetAll" };

			// Add pagination parameters
			keyParts.Add($"PageIndex:{pagination.PageIndex}");
			keyParts.Add($"PageSize:{pagination.PageSize}");

			// Add query parameters if present
			if (query is not null)
			{
				if (!string.IsNullOrWhiteSpace(query.Search))
					keyParts.Add($"Search:{query.Search.Trim().ToLowerInvariant()}");

				if (!string.IsNullOrWhiteSpace(query.SubjectCode))
					keyParts.Add($"SubjectCode:{query.SubjectCode.Trim().ToLowerInvariant()}");

				if (query.IsActive.HasValue)
					keyParts.Add($"IsActive:{query.IsActive.Value}");

				if (query.LectureId.HasValue)
					keyParts.Add($"TeacherId:{query.LectureId.Value}");

				keyParts.Add($"SortBy:{query.SortBy}");
			}

			return string.Join(":", keyParts);
		}

		/// <summary>
		/// Clear cache for GetAllAsync method when course data changes
		/// </summary>
		/// <returns></returns>
		private async Task ClearGetAllCacheAsync()
		{
			// Get all cache keys that start with "Courses:GetAll"
			var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: "Courses:GetAll*");

			if (keys.Any())
			{
				await _cache.KeyDeleteAsync(keys.ToArray());
			}
		}

		/// <summary>
		/// Clear cache for GetCourseByIdForStudentAsync and GetCourseBySlugForStudentAsync methods when user progress or enrollment changes
		/// </summary>
		/// <returns></returns>
		private async Task ClearCourseDetailForStudentCacheAsync()
		{
			var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: "CourseDetailForStudent*");
			if (keys.Any())
			{
				await _cache.KeyDeleteAsync(keys.ToArray());
			}
		}

		/// <summary>
		/// Clear cache for GetCourseTagsAsync method when tag data changes
		/// </summary>
		/// <returns></returns>
		private async Task ClearCourseTagsCacheAsync()
		{
			// Get all cache keys that start with "CourseTags:"
			var server = _cache.Multiplexer.GetServer(_cache.Multiplexer.GetEndPoints().FirstOrDefault()!);
			var keys = server.Keys(pattern: "CourseTags:*");

			if (keys.Any())
			{
				await _cache.KeyDeleteAsync(keys.ToArray());
			}
		}

		/// <summary>
		/// Generate slug from title
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		private static string ToSlug(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];
			var s = input.ToLowerInvariant().Trim();
			s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
			s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
			s = System.Text.RegularExpressions.Regex.Replace(s, "-{2,}", "-").Trim('-');
			return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("n")[..8] : s;
		}

		/// <summary>
		/// Generate a unique slug by appending a random suffix if necessary
		/// </summary>
		/// <param name="title"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		private async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct)
		{
			var baseSlug = ToSlug(title);
			var slug = baseSlug;

			while (await _courseRepository.Find(c => c.Slug == slug).AnyAsync(ct))
			{
				// lấy 6 ký tự ngẫu nhiên từ Guid
				var suffix = Guid.NewGuid().ToString("N")[..6];
				slug = $"{baseSlug}-{suffix}";
			}

			return slug;
		}

		// Đảm bảo slug unique khi UPDATE (bỏ qua chính course hiện tại).
		// Nếu allowRandomSuffix=true, khi trùng sẽ gắn thêm -xxxxxx (6 hex) để tránh loop nhiều lần.
		private async Task<string> EnsureUniqueSlugForUpdateAsync(Guid courseId, string candidate, CancellationToken ct, bool allowRandomSuffix = false)
		{
			var slug = ToSlug(candidate);

			// Nếu slug đã thuộc về chính course này -> ok
			var ownedByCurrent = await _courseRepository
				.Find(c => c.CourseId == courseId && c.Slug == slug)
				.AnyAsync(ct);
			if (ownedByCurrent) return slug;

			var exists = await _courseRepository
				.Find(c => c.Slug == slug && c.CourseId != courseId)
				.AnyAsync(ct);

			if (!exists) return slug;

			if (allowRandomSuffix)
			{
				var suffix = Guid.NewGuid().ToString("N")[..6];
				return await EnsureUniqueSlugForUpdateAsync(courseId, $"{slug}-{suffix}", ct, allowRandomSuffix: false);
			}
			else
			{
				// fallback: tăng dần -1, -2 ... (hiếm khi cần)
				var baseSlug = slug;
				var i = 1;
				while (await _courseRepository.Find(c => c.Slug == slug && c.CourseId != courseId).AnyAsync(ct))
				{
					slug = $"{baseSlug}-{i}";
					i++;
				}
				return slug;
			}
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDetailForGuestDto MapCourseDetailForGuest(CourseEntity e)
		{
			var modules = e.Modules
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailDto<GuestLessonDetailDto>(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(
							o.ObjectiveId,
							o.Content,
							o.PositionIndex,
							o.IsActive
						)).ToList(),
					m.Lessons
						.OrderBy(l => l.PositionIndex)
						.Select(l => new GuestLessonDetailDto(
							l.LessonId,
							l.Title,
							l.PositionIndex,
							l.IsActive))
						.ToList()
				)).ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForGuestDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDetailForLectureDto MapCourseDetailForLecture(
			CourseEntity e,
			IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId,     // moduleId -> quizId
			IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId,     // lessonId -> quizId
			IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId         // quizId -> QuizOutDto
		)
		{
			var modules = e.Modules
			.Where(m => m.IsActive)
			.OrderBy(m => m.PositionIndex)
			.Select(m =>
			{
				// Lấy quiz cho module (nếu có)
				QuizOutDto? moduleQuiz = null;
				if (moduleQuizIdByModuleId is not null &&
					moduleQuizIdByModuleId.TryGetValue(m.ModuleId, out var qid) &&
					quizByQuizId is not null &&
					quizByQuizId.TryGetValue(qid, out var qdto))
				{
					moduleQuiz = qdto;
				}

				var lessons = m.Lessons
					.Where(l => l.IsActive)
					.OrderBy(l => l.PositionIndex)
					.Select(l =>
					{
						QuizOutDto? lessonQuiz = null;
						if (lessonQuizIdByLessonId is not null &&
							lessonQuizIdByLessonId.TryGetValue(l.LessonId, out var lqid) &&
							quizByQuizId is not null &&
							quizByQuizId.TryGetValue(lqid, out var lqdto))
						{
							lessonQuiz = lqdto;
						}

						return new LectureLessonDetailDto(
							l.LessonId,
							l.Title,
							l.VideoUrl,
							l.VideoDurationSec,
							l.PositionIndex,
							l.IsActive,
							lessonQuiz // NEW
						);
					})
					.ToList();

				return new ModuleDetailForLectureDto(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives.Where(o => o.IsActive)
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
						.ToList(),
					m.ModuleDiscussions.Where(d => d.IsActive)
						.Select(d => new ModuleDiscussionDetailDto(d.DiscussionId, d.Title, d.Description, d.DiscussionQuestion, d.CreatedAt, d.UpdatedAt))
						.ToList(),
					m.ModuleMaterials.Where(mat => mat.IsActive)
						.Select(mat => new ModuleMaterialDetailDto(mat.MaterialId, mat.Title, mat.Description, mat.FileUrl, mat.CreatedAt, mat.UpdatedAt))
						.ToList(),
					lessons,
					moduleQuiz // NEW
				);
			})
			.ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForLectureDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.Modules.SelectMany(m => m.Lessons)
					.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoUrl ?? string.Empty,
				e.Modules.SelectMany(m => m.Lessons)
				.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoDurationSec ?? 0,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		private static QuizOutDto ToQuizOutDto(QuizCourseSelectEventResponseEntity q)
		{
			return new QuizOutDto(
				new QuizSettingsOutDto(
					q.DurationMinutes,
					q.PassingScorePercentage,
					q.ShuffleQuestions,
					q.ShowResultsImmediately,
					q.AllowRetake
				),
				q.Questions.Select(qq => new QuizQuestionOutDto(
					qq.QuestionId,
					qq.QuestionText,
					qq.Explanation,
					qq.QuestionType,
					qq.Answers.Select(a => new QuizAnswerOutDto(a.AnswerId, a.AnswerText)).ToList()
				)).ToList()
			);
		}

		private async Task<QuizOutDto?> FetchQuizAsync(Guid quizId, CancellationToken ct)
		{
			var res = await _quizSelectClient.GetResponse<QuizCourseSelectEventResponse>(
				new QuizCourseSelectEvent { QuizId = quizId }, ct);

			if (!res.Message.Success || res.Message.Response is null)
				return null;

			return ToQuizOutDto(res.Message.Response);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		private static CourseDto MapToDto(CourseEntity e) => new(
			CourseId: e.CourseId,
			TeacherId: e.TeacherId,
			SubjectId: e.SubjectId,
			SubjectCode: e.Subject?.SubjectCode ?? string.Empty,
			Title: e.Title ?? string.Empty,
			ShortDescription: e.ShortDescription,
			Description: e.Description,
			Slug: e.Slug,
			CourseImageUrl: e.CourseImageUrl,
			LearnerCount: e.LearnerCount,
			DurationMinutes: e.DurationMinutes,
			DurationHours: e.DurationHours,
			Level: e.Level,
			Price: e.Price,
			DealPrice: e.DealPrice,
			IsActive: e.IsActive,
			CreatedAt: e.CreatedAt,
			UpdatedAt: e.UpdatedAt
		);

		/// <summary>
		/// Validate PositionIndex uniqueness across all modules in UpdateCourseModulesDto
		/// </summary>
		/// <param name="dto"></param>
		/// <exception cref="ValidationException"></exception>
		private static void ValidateCourseDetailPositionIndexes(UpdateCourseDto dto)
		{
			// Course Objectives (chỉ active)
			if (dto.Objectives is { Count: > 0 })
			{
				var activeIdx = dto.Objectives
					.Where(o => o.IsActive)
					.Select(o => o.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Objective PositionIndex must be unique among active objectives.");

				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Objective PositionIndex must be > 0 for active objectives.");
			}

			// Course Requirements (chỉ active)
			if (dto.Requirements is { Count: > 0 })
			{
				var activeIdx = dto.Requirements
					.Where(r => r.IsActive)
					.Select(r => r.PositionIndex)
					.ToList();

				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Requirement PositionIndex must be unique among active requirements.");

				if (activeIdx.Any(i => i < 0))
					throw new ValidationException("Requirement PositionIndex must be >= 0 for active requirements.");
			}

			// Course Audiences (chỉ active)
			if (dto.Audiences is { Count: > 0 })
			{
				var activeIdx = dto.Audiences
					.Where(a => a.IsActive)
					.Select(a => a.PositionIndex)
					.ToList();
				if (activeIdx.Count != activeIdx.Distinct().Count())
					throw new ValidationException("Audience PositionIndex must be unique among active audiences.");
				if (activeIdx.Any(i => i <= 0))
					throw new ValidationException("Audience PositionIndex must be > 0 for active audiences.");
			}
		}

		/// <summary>
		/// Update CourseObjectives based on payload
		/// </summary>
		private Task UpdateCourseObjectivesAsync(CourseEntity course, List<UpdateCourseObjectiveDto>? objectives, string actor)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in course.CourseObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = course.CourseObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing objectives or create new ones
			foreach (var objDto in objectives)
			{
				if (objDto.ObjectiveId.HasValue && existingObjectives.TryGetValue(objDto.ObjectiveId.Value, out var existing))
				{
					// Update existing objective
					existing.Content = objDto.Content;
					existing.PositionIndex = objDto.PositionIndex;
					existing.IsActive = objDto.IsActive;
				}
				else
				{
					// Create new objective - let EF generate the ID
					var newObjective = new CourseObjective
					{
						CourseId = course.CourseId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					course.CourseObjectives.Add(newObjective);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update CourseRequirements based on payload
		/// </summary>
		private Task UpdateCourseRequirementsAsync(CourseEntity course, List<UpdateCourseRequirementDto>? requirements, string actor)
		{
			if (requirements is null || requirements.Count == 0)
			{
				// Mark all existing requirements as inactive (soft delete)
				foreach (var req in course.CourseRequirements.Where(r => r.IsActive))
				{
					req.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingRequirements = course.CourseRequirements.ToDictionary(r => r.RequirementId, r => r);
			var payloadRequirementIds = requirements.Where(r => r.RequirementId.HasValue).Select(r => r.RequirementId!.Value).ToHashSet();

			// 1. Mark requirements not in payload as inactive (soft delete)
			foreach (var existing in existingRequirements.Values.Where(r => r.IsActive && !payloadRequirementIds.Contains(r.RequirementId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing requirements or create new ones
			foreach (var reqDto in requirements)
			{
				if (reqDto.RequirementId.HasValue && existingRequirements.TryGetValue(reqDto.RequirementId.Value, out var existing))
				{
					// Update existing requirement
					existing.Content = reqDto.Content;
					existing.PositionIndex = reqDto.PositionIndex;
					existing.IsActive = reqDto.IsActive;
				}
				else
				{
					// Create new requirement - let EF generate the ID
					var newRequirement = new CourseRequirement
					{
						CourseId = course.CourseId,
						Content = reqDto.Content,
						PositionIndex = reqDto.PositionIndex,
						IsActive = reqDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					course.CourseRequirements.Add(newRequirement);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update CourseAudiences based on payload
		/// </summary>
		/// <param name="course"></param>
		/// <param name="audiences"></param>
		/// <param name="actor"></param>
		/// <returns></returns>
		private Task UpdateCourseAudiencesAsync(CourseEntity course, List<UpdateCourseAudienceDto>? audiences, string actor)
		{
			if (audiences is null || audiences.Count == 0)
			{
				// Mark all existing audiences as inactive (soft delete)
				foreach (var aud in course.CourseAudiences.Where(a => a.IsActive))
				{
					aud.IsActive = false;
				}
				return Task.CompletedTask;
			}
			var now = DateTime.UtcNow;
			var existingAudiences = course.CourseAudiences.ToDictionary(a => a.AudienceId, a => a);
			var payloadAudienceIds = audiences.Where(a => a.AudienceId.HasValue).Select(a => a.AudienceId!.Value).ToHashSet();

			// 1. Mark audiences not in payload as inactive (soft delete)
			foreach (var existing in existingAudiences.Values.Where(a => a.IsActive && !payloadAudienceIds.Contains(a.AudienceId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing audiences or create new ones
			foreach (var audDto in audiences)
			{
				if (audDto.AudienceId.HasValue && existingAudiences.TryGetValue(audDto.AudienceId.Value, out var existing))
				{
					// Update existing audience
					existing.Content = audDto.Content;
					existing.PositionIndex = audDto.PositionIndex;
					existing.IsActive = audDto.IsActive;
				}
				else
				{
					// Create new audience - let EF generate the ID
					var newAudience = new CourseAudience
					{
						CourseId = course.CourseId,
						Content = audDto.Content,
						PositionIndex = audDto.PositionIndex,
						IsActive = audDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					course.CourseAudiences.Add(newAudience);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Update CourseTags based on payload
		/// </summary>
		/// <param name="course"></param>
		/// <param name="courseTags"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		/// <exception cref="ValidationException"></exception>
		private async Task UpdateCourseTagsAsync(
			CourseEntity course,
			List<UpdateCourseTagDto>? courseTags,
			CancellationToken ct = default)
		{
			// Nếu payload rỗng hoặc không có => XÓA HẾT (hard delete)
			if (courseTags is null || courseTags.Count == 0)
			{
				if (course.CourseTags.Count > 0)
				{
					course.CourseTags.Clear(); // EF sẽ xóa các hàng ở bảng CourseTags (nếu cấu hình đúng)
				}
				return;
			}

			// 1) Chuẩn hóa payload: loại trùng và bỏ TagId <= 0
			var payloadTagIds = courseTags
				.Select(t => t.TagId)
				.Where(id => id > 0)
				.Distinct()
				.ToHashSet();

			if (payloadTagIds.Count == 0)
			{
				// Không còn tag hợp lệ => xóa hết
				course.CourseTags.Clear();
				return;
			}

			// 2) Validate các TagId có tồn tại trong bảng Tag
			var existedTagIds = await _tagRepository
				.Find(t => payloadTagIds.Contains(t.TagId), isTracking: false, ct)
				.Select(t => t.TagId)
				.ToListAsync(ct);

			var notFound = payloadTagIds.Except(existedTagIds).ToList();
			if (notFound.Count > 0)
				throw new ValidationException($"TagId không tồn tại: {string.Join(", ", notFound)}");

			// 3) Tập hiện tại trong course
			var currentTagIds = course.CourseTags.Select(ctg => ctg.TagId).ToHashSet();

			// 4) Tính phần cần xóa và cần thêm
			var toRemove = currentTagIds.Except(payloadTagIds).ToList();
			var toAdd = payloadTagIds.Except(currentTagIds).ToList();

			// 5) Hard delete: gỡ các CourseTag không còn trong payload
			if (toRemove.Count > 0)
			{
				// Lấy các entity tương ứng để Remove
				var removeEntities = course.CourseTags.Where(ctg => toRemove.Contains(ctg.TagId)).ToList();
				foreach (var rm in removeEntities)
					course.CourseTags.Remove(rm); // EF sẽ xóa bản ghi join
			}

			// 6) Thêm mới những TagId chưa có
			if (toAdd.Count > 0)
			{
				var now = DateTime.UtcNow;
				foreach (var tagId in toAdd)
				{
					course.CourseTags.Add(new CourseTag
					{
						CourseId = course.CourseId,
						TagId = tagId,
						CreatedAt = now
					});
				}
			}
		}


		/// <summary>
		/// Validate PositionIndex uniqueness across all modules in course
		/// </summary>
		private static void ValidateCourseModulesPositionIndexes(UpdateCourseModulesDto dto)
		{
			if (dto.Modules is null || dto.Modules.Count == 0)
				return;

			// Validate module PositionIndex uniqueness
			var moduleIndexes = dto.Modules
				.Where(m => m.IsActive)
				.Select(m => m.PositionIndex)
				.ToList();

			if (moduleIndexes.Count != moduleIndexes.Distinct().Count())
				throw new ValidationException("Module PositionIndex must be unique within the course.");

			if (moduleIndexes.Any(i => i <= 0))
				throw new ValidationException("Module PositionIndex must be > 0 for active modules.");

			// Validate objectives and lessons within each module
			foreach (var module in dto.Modules.Where(m => m.IsActive))
			{
				// Module Objectives
				if (module.Objectives is { Count: > 0 })
				{
					var activeObjIdx = module.Objectives
						.Where(o => o.IsActive)
						.Select(o => o.PositionIndex)
						.ToList();

					if (activeObjIdx.Count != activeObjIdx.Distinct().Count())
						throw new ValidationException($"Module '{module.ModuleName}' objectives' PositionIndex must be unique.");

					if (activeObjIdx.Any(i => i <= 0))
						throw new ValidationException($"Module '{module.ModuleName}' objectives' PositionIndex must be > 0 for active objectives.");
				}

				// Lessons
				if (module.Lessons is { Count: > 0 })
				{
					var activeLessonIdx = module.Lessons
						.Where(l => l.IsActive)
						.Select(l => l.PositionIndex)
						.ToList();

					if (activeLessonIdx.Count != activeLessonIdx.Distinct().Count())
						throw new ValidationException($"Module '{module.ModuleName}' lessons' PositionIndex must be unique.");

					if (activeLessonIdx.Any(i => i <= 0))
						throw new ValidationException($"Module '{module.ModuleName}' lessons' PositionIndex must be > 0 for active lessons.");
				}
			}
		}

		/// <summary>
		/// Internal method to update course modules
		/// </summary>
		private async Task UpdateCourseModulesInternalAsync(CourseEntity course, List<UpdateCourseModuleDto> modules, string actor)
		{
			// If payload is null or empty, mark all existing modules as inactive
			if (modules is null || modules.Count == 0)
			{
				// Mark all existing modules as inactive (soft delete)
				foreach (var module in course.Modules.Where(m => m.IsActive))
				{
					module.IsActive = false;
				}
				return;
			}

			var existingModules = course.Modules.ToDictionary(m => m.ModuleId, m => m);
			var payloadModuleIds = modules.Where(m => m.ModuleId.HasValue).Select(m => m.ModuleId!.Value).ToHashSet();

			// 1. Mark modules not in payload as inactive (soft delete)
			foreach (var existing in existingModules.Values.Where(m => m.IsActive && !payloadModuleIds.Contains(m.ModuleId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing modules or create new ones
			foreach (var moduleDto in modules)
			{
				if (moduleDto.ModuleId.HasValue && existingModules.TryGetValue(moduleDto.ModuleId.Value, out var existingModule))
				{
					// Update existing module
					await UpdateExistingModuleAsync(existingModule, moduleDto, actor);
				}
				else
				{
					// Create new module
					await CreateNewModuleAsync(course, moduleDto, actor);
				}
			}
		}

		/// <summary>
		/// Update existing module with its objectives and lessons
		/// </summary>
		private async Task UpdateExistingModuleAsync(Module existingModule, UpdateCourseModuleDto moduleDto, string actor)
		{
			// Update basic module properties
			existingModule.ModuleName = moduleDto.ModuleName?.Trim() ?? string.Empty;
			existingModule.Description = moduleDto.Description;
			existingModule.PositionIndex = moduleDto.PositionIndex;
			existingModule.IsActive = moduleDto.IsActive;
			existingModule.IsCore = moduleDto.IsCore;
			existingModule.DurationMinutes = moduleDto.DurationMinutes;
			existingModule.Level = moduleDto.Level;

			// Update ModuleObjectives
			await UpdateModuleObjectivesInternalAsync(existingModule, moduleDto.Objectives, actor);

			// Update Lessons
			await UpdateLessonsInternalAsync(existingModule, moduleDto.Lessons, actor);

			// Update Discussions
			await UpdateModuleDiscussionInternalAsync(existingModule, moduleDto.Discussions, actor);

			// Update Materials
			await UpdateModuleMaterialsInternalAsync(existingModule, moduleDto.Materials, actor);
		}

		/// <summary>
		/// Create new module with its objectives and lessons
		/// </summary>
		private Task CreateNewModuleAsync(CourseEntity course, UpdateCourseModuleDto moduleDto, string actor)
		{
			var now = DateTime.UtcNow;

			var newModule = new Module
			{
				CourseId = course.CourseId,
				ModuleName = moduleDto.ModuleName?.Trim() ?? string.Empty,
				Description = moduleDto.Description,
				PositionIndex = moduleDto.PositionIndex,
				IsActive = moduleDto.IsActive,
				IsCore = moduleDto.IsCore,
				DurationMinutes = moduleDto.DurationMinutes,
				Level = moduleDto.Level,
				CreatedAt = now,
				UpdatedAt = now,
				CreatedBy = actor,
				UpdatedBy = actor
			};

			// Add ModuleObjectives
			if (moduleDto.Objectives is { Count: > 0 })
			{
				foreach (var objDto in moduleDto.Objectives.OrderBy(o => o.PositionIndex))
				{
					newModule.ModuleObjectives.Add(new ModuleObjective
					{
						ModuleId = newModule.ModuleId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			// Add Lessons
			if (moduleDto.Lessons is { Count: > 0 })
			{
				foreach (var lessonDto in moduleDto.Lessons.OrderBy(l => l.PositionIndex))
				{
					newModule.Lessons.Add(new Lesson
					{
						ModuleId = newModule.ModuleId,
						Title = lessonDto.Title,
						VideoUrl = lessonDto.VideoUrl,
						VideoDurationSec = lessonDto.VideoDurationSec,
						PositionIndex = lessonDto.PositionIndex,
						IsActive = lessonDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			// Add Discussions
			if (moduleDto.Discussions is { Count: > 0 })
			{
				foreach (var disDto in moduleDto.Discussions)
				{
					newModule.ModuleDiscussions.Add(new ModuleDiscussion
					{
						ModuleId = newModule.ModuleId,
						Title = disDto.Title,
						Description = disDto.Description,
						DiscussionQuestion = disDto.DiscussionQuestion,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			// Add Materials
			if (moduleDto.Materials is { Count: > 0 })
			{
				foreach (var matDto in moduleDto.Materials)
				{
					newModule.ModuleMaterials.Add(new ModuleMaterial
					{
						ModuleId = newModule.ModuleId,
						Title = matDto.Title,
						Description = matDto.Description,
						FileUrl = matDto.FileUrl,
						IsActive = true,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					});
				}
			}

			course.Modules.Add(newModule);

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleObjectives for existing module
		/// </summary>
		private Task UpdateModuleObjectivesInternalAsync(Module module, List<UpdateCourseModuleObjectiveDto>? objectives, string actor)
		{
			if (objectives is null || objectives.Count == 0)
			{
				// Mark all existing objectives as inactive (soft delete)
				foreach (var obj in module.ModuleObjectives.Where(o => o.IsActive))
				{
					obj.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingObjectives = module.ModuleObjectives.ToDictionary(o => o.ObjectiveId, o => o);
			var payloadObjectiveIds = objectives.Where(o => o.ObjectiveId.HasValue).Select(o => o.ObjectiveId!.Value).ToHashSet();

			// 1. Mark objectives not in payload as inactive (soft delete)
			foreach (var existing in existingObjectives.Values.Where(o => o.IsActive && !payloadObjectiveIds.Contains(o.ObjectiveId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing objectives or create new ones
			foreach (var objDto in objectives)
			{
				if (objDto.ObjectiveId.HasValue && existingObjectives.TryGetValue(objDto.ObjectiveId.Value, out var existing))
				{
					// Update existing objective
					existing.Content = objDto.Content;
					existing.PositionIndex = objDto.PositionIndex;
					existing.IsActive = objDto.IsActive;
				}
				else
				{
					// Create new objective - let EF generate the ID
					var newObjective = new ModuleObjective
					{
						ModuleId = module.ModuleId,
						Content = objDto.Content,
						PositionIndex = objDto.PositionIndex,
						IsActive = objDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleObjectives.Add(newObjective);
				}
			}

			return Task.CompletedTask;
		}

		/// <summary>
		/// Update Lessons for existing module
		/// </summary>
		private Task UpdateLessonsInternalAsync(Module module, List<UpdateCourseLessonDto> lessons, string actor)
		{
			if (lessons is null || lessons.Count == 0)
			{
				// Mark all existing lessons as inactive (soft delete)
				foreach (var lesson in module.Lessons.Where(l => l.IsActive))
				{
					lesson.IsActive = false;
				}
				return Task.CompletedTask;
			}

			var now = DateTime.UtcNow;
			var existingLessons = module.Lessons.ToDictionary(l => l.LessonId, l => l);
			var payloadLessonIds = lessons.Where(l => l.LessonId.HasValue).Select(l => l.LessonId!.Value).ToHashSet();

			// 1. Mark lessons not in payload as inactive (soft delete)
			foreach (var existing in existingLessons.Values.Where(l => l.IsActive && !payloadLessonIds.Contains(l.LessonId)))
			{
				existing.IsActive = false;
			}

			// 2. Update existing lessons or create new ones
			foreach (var lessonDto in lessons)
			{
				if (lessonDto.LessonId.HasValue && existingLessons.TryGetValue(lessonDto.LessonId.Value, out var existing))
				{
					// Update existing lesson
					existing.Title = lessonDto.Title;
					existing.VideoUrl = lessonDto.VideoUrl;
					existing.VideoDurationSec = lessonDto.VideoDurationSec;
					existing.PositionIndex = lessonDto.PositionIndex;
					existing.IsActive = lessonDto.IsActive;
				}
				else
				{
					// Create new lesson - let EF generate the ID
					var newLesson = new Lesson
					{
						ModuleId = module.ModuleId,
						Title = lessonDto.Title,
						VideoUrl = lessonDto.VideoUrl,
						VideoDurationSec = lessonDto.VideoDurationSec,
						PositionIndex = lessonDto.PositionIndex,
						IsActive = lessonDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.Lessons.Add(newLesson);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleDiscussions for existing module
		/// </summary>
		/// <param name="module"></param>
		/// <param name="discussions"></param>
		/// <param name="actor"></param>
		/// <returns></returns>
		private Task UpdateModuleDiscussionInternalAsync(Module module, List<UpdateModuleDiscussionDto>? discussions, string actor)
		{
			if (discussions is null || discussions.Count == 0)
			{
				// Mark all existing discussions as inactive (soft delete)
				foreach (var dis in module.ModuleDiscussions.Where(d => d.IsActive))
				{
					dis.IsActive = false;
				}
				return Task.CompletedTask;
			}
			var now = DateTime.UtcNow;
			var existingDiscussions = module.ModuleDiscussions.ToDictionary(d => d.DiscussionId, d => d);
			var payloadDiscussionIds = discussions.Where(d => d.DiscussionId.HasValue).Select(d => d.DiscussionId!.Value).ToHashSet();
			// 1. Mark discussions not in payload as inactive (soft delete)
			foreach (var existing in existingDiscussions.Values.Where(d => d.IsActive && !payloadDiscussionIds.Contains(d.DiscussionId)))
			{
				existing.IsActive = false;
			}
			// 2. Update existing discussions or create new ones
			foreach (var disDto in discussions)
			{
				if (disDto.DiscussionId.HasValue && existingDiscussions.TryGetValue(disDto.DiscussionId.Value, out var existing))
				{
					// Update existing discussion
					existing.Title = disDto.Title;
					existing.Description = disDto.Description;
					existing.DiscussionQuestion = disDto.DiscussionQuestion;
					existing.IsActive = disDto.IsActive;
				}
				else
				{
					// Create new discussion - let EF generate the ID
					var newDiscussion = new ModuleDiscussion
					{
						ModuleId = module.ModuleId,
						Title = disDto.Title,
						Description = disDto.Description,
						DiscussionQuestion = disDto.DiscussionQuestion,
						IsActive = disDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleDiscussions.Add(newDiscussion);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Update ModuleMaterials for existing module
		/// </summary>
		/// <param name="module"></param>
		/// <param name="materials"></param>
		/// <param name="actor"></param>
		/// <returns></returns>
		private Task UpdateModuleMaterialsInternalAsync(Module module, List<UpdateModuleMaterialDto>? materials, string actor)
		{
			if (materials is null || materials.Count == 0)
			{
				// Mark all existing materials as inactive (soft delete)
				foreach (var mat in module.ModuleMaterials.Where(m => m.IsActive))
				{
					mat.IsActive = false;
				}
				return Task.CompletedTask;
			}
			var now = DateTime.UtcNow;
			var existingMaterials = module.ModuleMaterials.ToDictionary(m => m.MaterialId, m => m);
			var payloadMaterialIds = materials.Where(m => m.MaterialId.HasValue).Select(m => m.MaterialId!.Value).ToHashSet();
			// 1. Mark materials not in payload as inactive (soft delete)
			foreach (var existing in existingMaterials.Values.Where(m => m.IsActive && !payloadMaterialIds.Contains(m.MaterialId)))
			{
				existing.IsActive = false;
			}
			// 2. Update existing materials or create new ones
			foreach (var matDto in materials)
			{
				if (matDto.MaterialId.HasValue && existingMaterials.TryGetValue(matDto.MaterialId.Value, out var existing))
				{
					// Update existing material
					existing.Title = matDto.Title;
					existing.Description = matDto.Description;
					existing.FileUrl = matDto.FileUrl;
					existing.IsActive = matDto.IsActive;
				}
				else
				{
					// Create new material - let EF generate the ID
					var newMaterial = new ModuleMaterial
					{
						ModuleId = module.ModuleId,
						Title = matDto.Title,
						Description = matDto.Description,
						FileUrl = matDto.FileUrl,
						IsActive = matDto.IsActive,
						CreatedAt = now,
						UpdatedAt = now,
						CreatedBy = actor,
						UpdatedBy = actor
					};
					module.ModuleMaterials.Add(newMaterial);
				}
			}
			return Task.CompletedTask;
		}

		/// <summary>
		/// Convert CreateQuizDto to QuizCourseInsertEvent for publishing to message bus
		/// </summary>
		/// <param name="dto"></param>
		/// <returns></returns>
		private static QuizCourseInsertEvent ToQuizCourseInsertEvent(
			string userEmail,
			CreateQuizDto q)
		{
			return new QuizCourseInsertEvent
			{
				UserEmail = userEmail,
				DurationMinutes = q.QuizSettings.DurationMinutes,
				PassingScorePercentage = q.QuizSettings.PassingScorePercentage,
				ShuffleQuestions = q.QuizSettings.ShuffleQuestions,
				ShowResultsImmediately = q.QuizSettings.ShowResultsImmediately,
				AllowRetake = q.QuizSettings.AllowRetake,
				Questions = q.Questions.Select(qq => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents.Questions
				{
					QuestionText = qq.QuestionText,
					QuestionType = (short)qq.QuestionType, // 1 & 3 theo bạn yêu cầu
					Explanation = qq.Explanation,
					Answers = qq.Options.Select(a => new BuildingBlocks.Messaging.Events.CourseService.QuizCourseInsertEvents.Answers
					{
						AnswerText = a.Text,
						IsCorrect = a.IsCorrect
					}).ToList()
				}).ToList()
			};
		}

		/// <summary>
		/// Help validate that a list of integers are all distinct
		/// </summary>
		/// <param name="indexes"></param>
		/// <param name="errorMsg"></param>
		/// <exception cref="ValidationException"></exception>
		private static void EnsureDistinct(IEnumerable<int> indexes, string errorMsg)
		{
			var list = indexes.ToList();
			if (list.Count != list.Distinct().Count())
				throw new ValidationException(errorMsg);
		}

		/// <summary>
		/// Check if two lists of integers have any intersection
		/// </summary>
		/// <param name="taken"></param>
		/// <returns></returns>
		private static int NextIndex(ISet<int> taken)
		{
			var next = taken.Count == 0 ? 1 : taken.Max() + 1;
			while (taken.Contains(next)) next++;
			return next;
		}

		private static CourseDetailForStudentDto MapCourseDetailForStudent(
			CourseEntity e,
			IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId,
			IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId,
			IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId,
			IReadOnlyDictionary<Guid, LessonProgressSnap> progressByLessonId,   // lessonId -> { Status, LastPositionSec, CompletedAt }
			IReadOnlyDictionary<Guid, ModuleProgressSnap> moduleProgressById,   // moduleId -> { LessonsTotal, LessonsCompleted, PercentCompleted, Status, StartedAt, CompletedAt }
			CourseProgressSnap? courseProgress,                                 // { LessonsTotal, LessonsCompleted, PercentCompleted, Status, StartedAt, CompletedAt }
			bool preferCoreForCourse
		)
		{
			// MODULES
			var modules = e.Modules
				.Where(m => m.IsActive)
				.OrderBy(m => m.PositionIndex)
				.Select(m =>
				{
					// Quiz cho module
					QuizOutDto? moduleQuiz = null;
					if (moduleQuizIdByModuleId is not null &&
						moduleQuizIdByModuleId.TryGetValue(m.ModuleId, out var qid) &&
						quizByQuizId is not null &&
						quizByQuizId.TryGetValue(qid, out var qdto))
					{
						moduleQuiz = qdto;
					}

					// LESSONS + tick
					var lessons = m.Lessons
						.Where(l => l.IsActive)
						.OrderBy(l => l.PositionIndex)
						.Select(l =>
						{
							QuizOutDto? lessonQuiz = null;
							if (lessonQuizIdByLessonId is not null &&
								lessonQuizIdByLessonId.TryGetValue(l.LessonId, out var lqid) &&
								quizByQuizId is not null &&
								quizByQuizId.TryGetValue(lqid, out var lqdto))
							{
								lessonQuiz = lqdto;
							}

							var has = progressByLessonId.TryGetValue(l.LessonId, out var lp);
							var isCompleted = has && lp!.Status == (short)LessonStatus.Completed;
							var lastPos = has ? lp!.LastPositionSec : 0;

							return new StudentLessonDetailDto(
								l.LessonId,
								l.Title,
								l.VideoUrl,
								l.VideoDurationSec,
								l.PositionIndex,
								l.IsActive,
								isCompleted,
								lastPos,
								lessonQuiz // để FE có thể hiển thị quiz của bài
							);
						})
						.ToList();

					// MODULE PROGRESS (dùng snapshot nếu có; fallback tự tính)
					int lessonsTotal = lessons.Count;
					int lessonsCompleted = lessons.Count(x => x.IsCompleted);
					decimal percent = lessonsTotal == 0 ? 0 : Math.Round((decimal)lessonsCompleted * 100m / lessonsTotal, 2);

					short status = lessonsCompleted == 0 ? (short)LessonStatus.NotStarted : (lessonsCompleted == lessonsTotal ? (short)LessonStatus.Completed : (short)LessonStatus.InProgress);
					DateTime? startedAt = null, completedAt = null;

					if (moduleProgressById.TryGetValue(m.ModuleId, out var mp))
					{
						lessonsTotal = mp.LessonsTotal;
						lessonsCompleted = mp.LessonsCompleted;
						percent = mp.PercentCompleted;
						status = mp.Status;
						startedAt = mp.StartedAt;
						completedAt = mp.CompletedAt;
					}

					return new ModuleDetailForStudentDto(
						m.ModuleId,
						m.ModuleName,
						m.Description,
						m.PositionIndex,
						m.IsActive,
						m.IsCore,
						m.DurationMinutes,
						m.DurationHours,
						m.Level,
						// objectives, discussions, materials giữ nguyên như lecture
						m.ModuleObjectives.Where(o => o.IsActive)
							.OrderBy(o => o.PositionIndex)
							.Select(o => new ModuleObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
							.ToList(),
						m.ModuleDiscussions.Where(d => d.IsActive)
							.Select(d => new ModuleDiscussionDetailDto(d.DiscussionId, d.Title, d.Description, d.DiscussionQuestion, d.CreatedAt, d.UpdatedAt))
							.ToList(),
						m.ModuleMaterials.Where(mat => mat.IsActive)
							.Select(mat => new ModuleMaterialDetailDto(mat.MaterialId, mat.Title, mat.Description, mat.FileUrl, mat.CreatedAt, mat.UpdatedAt))
							.ToList(),
						lessons,
						moduleQuiz,
						// progress
						new ModuleProgressDto(lessonsTotal, lessonsCompleted, percent, status, startedAt, completedAt)
					);
				})
				.ToList();

			// COURSE PROGRESS (chỉ CORE nếu preferCoreForCourse=true)
			int courseTotalLessons, courseCompletedLessons;
			decimal coursePercent;
			short courseStatus;
			DateTime? courseStartedAt = null, courseCompletedAt = null;

			if (courseProgress is not null)
			{
				courseTotalLessons = courseProgress.LessonsTotal;
				courseCompletedLessons = courseProgress.LessonsCompleted;
				coursePercent = courseProgress.PercentCompleted;
				courseStatus = courseProgress.Status;
				courseStartedAt = courseProgress.StartedAt;
				courseCompletedAt = courseProgress.CompletedAt;
			}
			else
			{
				// Fallback: tự tính theo modules is_core = true
				var coreModules = preferCoreForCourse ? modules.Where(m => m.IsCore) : modules;
				courseTotalLessons = coreModules.Sum(m => m.Progress.LessonsTotal);
				courseCompletedLessons = coreModules.Sum(m => m.Progress.LessonsCompleted);
				coursePercent = courseTotalLessons == 0 ? 0 : Math.Round((decimal)courseCompletedLessons * 100m / courseTotalLessons, 2);
				courseStatus = courseCompletedLessons == 0 ? (short)0 : (courseCompletedLessons == courseTotalLessons ? (short)CourseStatus.Completed : (short)CourseStatus.InProgress);
			}

			// Comments, Tags, Ratings giống lecture
			var comments = e.CourseComments
				.OrderBy(c => c.CreatedAt)
				.Select(c => new CourseCommentDto(c.CommentId, c.UserId, c.Content, c.ParentCommentId, c.CreatedAt, c.IsActive))
				.ToList();

			var tags = e.CourseTags.Select(t => new CourseTagDto(t.TagId, t.Tag?.TagName ?? string.Empty)).ToList();

			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(r.RatingId, r.UserId, r.Rating, r.CreatedAt))
				.ToList();

			var ratingsCount = ratings.Count;

			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			var firstLesson = e.Modules.SelectMany(m => m.Lessons).OrderBy(l => l.PositionIndex).FirstOrDefault();

			// Gợi ý “tiếp tục học”
			var continueHint = ComputeContinueLesson(modules);

			return new CourseDetailForStudentDto(
				e.CourseId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				firstLesson?.VideoUrl ?? string.Empty,
				firstLesson?.VideoDurationSec ?? 0,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives.OrderBy(o => o.PositionIndex).Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive)).ToList(),
				e.CourseRequirements.OrderBy(r => r.PositionIndex).Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive)).ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0,
				// Progress course
				new CourseProgressDto(courseTotalLessons, courseCompletedLessons, coursePercent, courseStatus, courseStartedAt, courseCompletedAt),
				// Continue hint – set ở ngoài
				continueHint
			);
		}

		/// <summary>
		/// Return the next lesson to continue learning
		/// </summary>
		/// <param name="modules"></param>
		/// <returns></returns>
		private static ContinueHintDto? ComputeContinueLesson(List<ModuleDetailForStudentDto> modules)
		{
			// Ưu tiên modules core trước, sau đó theo position_index
			foreach (var m in modules.OrderByDescending(x => x.IsCore).ThenBy(x => x.PositionIndex))
			{
				// Tìm bài chưa hoàn thành có position nhỏ nhất
				var next = m.Lessons.OrderBy(l => l.PositionIndex).FirstOrDefault(l => !l.IsCompleted);
				if (next is not null)
				{
					return new ContinueHintDto(
						ModuleId: m.ModuleId,
						ModuleName: m.ModuleName,
						LessonId: next.LessonId,
						LessonTitle: next.Title,
						ResumeSecond: next.LastPositionSec
					);
				}
			}
			return null;
		}


		#endregion
	}
}