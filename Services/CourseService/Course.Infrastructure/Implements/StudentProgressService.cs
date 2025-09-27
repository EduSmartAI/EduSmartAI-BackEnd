using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;
using Course.Application.DTOs.UserLessonProgressDTO;
using Course.Application.Interfaces;
using Course.Application.Interfaces.Helpers;
using Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress;
using Course.Application.UserLessonProgresses.Queries.CheckEnrollment;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressForStudents;
using Course.Domain.Models;
using Course.Domain.ReadModels;
using Course.Infrastructure.Caching;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Infrastructure.Implements
{
	public class StudentProgressService(
		IDatabase _cache,
		IIdentityService _identityService,
		IUnitOfWork unitOfWork,
		ICommandRepository<CourseEntity> _courseRepository,
		ICommandRepository<Lesson> _lessonRepository,
		ICommandRepository<CourseStudentEnrollment> _enrollmentRepository,
		IQueryRepository<CourseStudentEnrollmentCollection> _enrollmentQueryRepository,
		ICommandRepository<ModuleQuiz> _moduleQuizRepository,
		ICommandRepository<LessonQuiz> _lessonQuizRepository,
		ICommandRepository<UserLessonProgress> _userLessonProgress,
		ICommandRepository<UserModuleProgress> _userModuleProgressQuery,
		ICommandRepository<UserCourseProgress> _userCourseProgressQuery,
		ICourseCache _courseCache,
		ICourseMapper _courseMapper,
		IQuizGateway _quizGateway) : IStudentProgressService
	{
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
		/// Get course details by Id for student (learner) users
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetDetailsProgressByCourseIdForStudentResponse> GetCourseByIdForStudentAsync(Guid courseId, CancellationToken ct = default)
		{
			var response = new GetDetailsProgressByCourseIdForStudentResponse() { Success = false };

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
			var quizTasks = allQuizIds.ToDictionary(id => id, id => _quizGateway.FetchQuizAsync(id, ct));
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
			var detail = _courseMapper.MapCourseDetailForStudent(
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
		public async Task<GetDetailsProgressByCourseSlugForStudentResponse> GetCourseBySlugForStudentAsync(string courseSlug, CancellationToken ct = default)
		{
			var response = new GetDetailsProgressByCourseSlugForStudentResponse() { Success = false };

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
			var quizTasks = allQuizIds.ToDictionary(id => id, id => _quizGateway.FetchQuizAsync(id, ct));
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
			var detail = _courseMapper.MapCourseDetailForStudent(
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
			await _courseCache.ClearCourseDetailForStudentCacheAsync();

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00000, "Tạo tiến độ học bài học thành công.");
			return response;
		}

		/// <summary>
		/// Update user lesson progress
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
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
			await _courseCache.ClearCourseDetailForStudentCacheAsync();

			response.Success = true;
			response.Response = true;
			response.SetMessage(MessageId.I00001, "Cập nhật tiến độ học bài học thành công.");
			return response;
		}
	}
}
