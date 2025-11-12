using BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents;
using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;
using Course.Application.DTOs.UserLessonProgressDTO;
using Course.Application.UserLessonProgresses.Commands.EnrollCourse;
using Course.Application.UserLessonProgresses.Commands.UpsertUserLessonProgress;
using Course.Application.UserLessonProgresses.Queries.CheckEnrollment;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseIdForStudents;
using Course.Application.UserLessonProgresses.Queries.GetDetailsProgressByCourseSlugForStudents;
using Course.Application.UserLessonProgresses.Queries.GetMyLearningCourses;
using Course.Domain.ReadModels;
using Course.Infrastructure.Caching;
using System.Text.Json;
using static BaseService.Common.Utils.Const.ConstantEnum;
using static Course.Infrastructure.Helpers.StudentLessonProgress.LessonProgressPolicy;

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

			// Clear relevant caches
			await _courseCache.ClearCourseDetailForStudentCacheAsync();
			await _courseCache.ClearEnrollmentStatusCacheAsync(userId: currentUser.UserId, courseId: courseId);

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
				.Find(x => x.CourseId == courseId, isTracking: false, ct)
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
			var quizTasks = allQuizIds.ToDictionary(id => id, id => _quizGateway.FetchQuizForStudentAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?
			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			var attemptTasks = allQuizIds.ToDictionary(
				id => id,
				id => _quizGateway.CheckCheckAttemptAsync(id, userId, ct)
			);

			await Task.WhenAll(attemptTasks.Values);

			var attemptsByQuizId = attemptTasks.ToDictionary(
				kvp => kvp.Key,
				kvp =>
				{
					var r = kvp.Value.Result;
					if (r.Success && r.Response is not null)
						return r.Response;

					return new QuizCourseCheckAttemptEntity
					{
						CanAttempt = false,
						StudentQuizId = null
					};
				}
			);

			// 4) Tải progress bài học của user (để tick bài đã hoàn thành + resume)
			var lessonProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new LessonProgressSnap(
					x.LessonId, x.Status, x.LastSeenPositionSec, x.CompletedAt))
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
				preferCoreForCourse: true, // % course chỉ tính modules IsCore = true
				attemptsByQuizId
			);

			// 8) Cache ngắn
			await _cache.SetAsync(cacheKey, detail, TimeSpan.FromSeconds(5));

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
				.Find(x => x.Slug == courseSlug, isTracking: false, ct)
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
				.Find(x => x.CourseId == courseId, isTracking: false, ct)
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
			var quizTasks = allQuizIds.ToDictionary(id => id, id => _quizGateway.FetchQuizForStudentAsync(id, ct));
			await Task.WhenAll(quizTasks.Values);

			var quizDict = quizTasks.ToDictionary(k => k.Key, v => v.Value.Result); // Guid -> QuizOutDto?
			var moduleQuizIdByModuleId = moduleMaps.ToDictionary(x => x.ModuleId, x => x.QuizId);
			var lessonQuizIdByLessonId = lessonMaps.ToDictionary(x => x.LessonId, x => x.QuizId);

			var attemptTasks = allQuizIds.ToDictionary(
				id => id,
				id => _quizGateway.CheckCheckAttemptAsync(id, userId, ct)
			);

			await Task.WhenAll(attemptTasks.Values);

			// Guid -> QuizCourseCheckAttemptEntity (fallback safe nếu lỗi)
			var attemptsByQuizId = attemptTasks.ToDictionary(
				kvp => kvp.Key,
				kvp =>
				{
					var r = kvp.Value.Result;
					if (r.Success && r.Response is not null)
						return r.Response;

					return new QuizCourseCheckAttemptEntity
					{
						CanAttempt = false,
						StudentQuizId = null
					};
				}
			);

			// 4) Tải progress bài học của user (để tick bài đã hoàn thành + resume)
			var lessonProgress = await _userLessonProgress
				.Find(x => x.UserId == userId && lessonIds.Contains(x.LessonId), isTracking: false, ct)
				.Select(x => new LessonProgressSnap(
					x.LessonId, x.Status, x.LastSeenPositionSec, x.CompletedAt))
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
				preferCoreForCourse: true, // % course chỉ tính modules IsCore = true
				attemptsByQuizId
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
		/// Create or update user lesson progress
		/// Upsert: nếu chưa có thì tạo mới, nếu đã có thì cập nhật (idempotent, đảm bảo đơn điệu)
		/// </summary>
		/// <param name="dto"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<UpsertUserLessonProgressResponse> UpsertUserLessonProgressAsync(Guid lessonId, UpsertUserLessonProgressDto dto, CancellationToken ct = default)
		{
			var response = new UpsertUserLessonProgressResponse() { Success = false };
			var currentUser = _identityService.GetCurrentUser()!;
			var userId = currentUser.UserId;
			//var userId = new Guid("74984b58-f266-4d92-b6b1-3a4d65aa01d2")
			var now = DateTime.UtcNow;
			const int MaxDeltaPerTick = 300;

			// 1) Validate lesson
			var lesson = await _lessonRepository
				.Find(x => x.LessonId == lessonId && x.IsActive, isTracking: false, ct)
				.FirstOrDefaultAsync(ct);
			if (lesson is null)
			{
				response.SetMessage(MessageId.E00000, $"Không tìm thấy bài học {lessonId}");
				return response;
			}
			var videoMax = lesson.VideoDurationSec ?? int.MaxValue;

			// 2) Tìm progress hiện có
			var progress = await _userLessonProgress
				.Find(x => x.UserId == userId && x.LessonId == lessonId, isTracking: true, ct)
				.FirstOrDefaultAsync(ct);

			// Chuẩn hóa input
			int? incomingSeen = dto.LastSeenPositionSec.HasValue ? ClampNonNeg(dto.LastSeenPositionSec.Value, videoMax) : null;

			int watchedDelta = (dto.WatchedDeltaSec is int d && d > 0) ? Math.Min(d, MaxDeltaPerTick) : 0;

			// 3) Nếu chưa có → tạo mới
			if (progress is null)
			{
				var lastSeen = incomingSeen ?? 0;
				var lastMax = lastSeen;

				progress = new UserLessonProgress
				{
					UserId = userId,
					LessonId = lessonId,
					Status = (short)LessonStatus.InProgress,
					LastSeenPositionSec = lastSeen,
					LastPositionSec = lastMax,
					DurationWatchedSec = watchedDelta,
					CreatedAt = now,
					UpdatedAt = now,
					CompletedAt = null
				};

				if (ShouldCompleteSimple(videoMax, progress.LastPositionSec))
				{
					progress.Status = (short)LessonStatus.Completed;
					progress.CompletedAt = now;
				}

				await unitOfWork.BeginTransactionAsync(async () =>
				{
					await _userLessonProgress.AddAsync(progress);
					await unitOfWork.SaveChangesAsync(ct);

					unitOfWork.Store(UserLessonProgressCollection.FromWriteModel(progress));
					await unitOfWork.SessionSaveChangesAsync();
					return true;
				}, ct);

				await _courseCache.ClearCourseDetailForStudentCacheAsync();

				var result = new UserLessonProgressEntity(
					progress.LessonId,
					progress.Status,
					progress.LastPositionSec ?? 0,
					progress.DurationWatchedSec,
					progress.CompletedAt);

				response.Success = true;
				response.Response = result;
				response.SetMessage(MessageId.I00000, "Ghi tiến độ lần đầu thành công.");
				return response;
			}

			// 4) ĐÃ CÓ RECORD
			if (incomingSeen.HasValue) progress.LastSeenPositionSec = incomingSeen.Value;

			progress.LastPositionSec = Math.Max(progress.LastPositionSec ?? 0, progress.LastSeenPositionSec);

			if (watchedDelta > 0) progress.DurationWatchedSec += watchedDelta;

			// 4.a) Nếu đã Completed: KHÔNG cho revert; chỉ cập nhật resume/time
			if (progress.Status == (short)LessonStatus.Completed)
			{
				progress.UpdatedAt = now;

				await unitOfWork.BeginTransactionAsync(async () =>
				{
					_userLessonProgress.Update(progress);
					await unitOfWork.SaveChangesAsync(ct);

					unitOfWork.Store(UserLessonProgressCollection.FromWriteModel(progress));
					await unitOfWork.SessionSaveChangesAsync();
					return true;
				}, ct);

				await _courseCache.ClearCourseDetailForStudentCacheAsync();

				var result = new UserLessonProgressEntity(
					progress.LessonId,
					progress.Status,
					progress.LastPositionSec ?? 0,
					progress.DurationWatchedSec,
					progress.CompletedAt);

				response.Success = true;
				response.Response = result;
				response.SetMessage(MessageId.I00001, "Bài đã hoàn thành — cập nhật thành công.");
				return response;
			}

			// 4.b) Chưa Completed: cập nhật đơn điệu + tự xét Completed

			// Auto-complete sau khi đã cập nhật vị trí/thời gian
			if (ShouldCompleteSimple(videoMax, progress.LastPositionSec))
			{
				progress.Status = (short)LessonStatus.Completed;
				progress.CompletedAt ??= now;
			}
			else
			{
				// nếu chưa đủ ngưỡng, luôn để InProgress
				progress.Status = (short)LessonStatus.InProgress;
			}

			progress.UpdatedAt = now;

			await unitOfWork.BeginTransactionAsync(async () =>
			{
				_userLessonProgress.Update(progress);
				await unitOfWork.SaveChangesAsync(ct);

				unitOfWork.Store(UserLessonProgressCollection.FromWriteModel(progress));
				await unitOfWork.SessionSaveChangesAsync();
				return true;
			}, ct);

			await _courseCache.ClearCourseDetailForStudentCacheAsync();

			var updated = new UserLessonProgressEntity(
				progress.LessonId,
				progress.Status,
				progress.LastPositionSec ?? 0,
				progress.DurationWatchedSec,
				progress.CompletedAt);

			response.Success = true;
			response.Response = updated;
			response.SetMessage(MessageId.I00001, "Cập nhật tiến độ thành công.");
			return response;
		}

		public async Task<GetMyLearningCoursesResponse> GetMyLearningAsync(GetMyLearningCoursesQuery request, CancellationToken ct = default)
		{
			var response = new GetMyLearningCoursesResponse() { Success = false };
			var currentUser = _identityService.GetCurrentUser()!;
			if (currentUser is null)
			{
				response.SetMessage(MessageId.E00000, "Người dùng chưa đăng nhập");
				return response;
			}

			var userId = currentUser.UserId;

			var pageNumber = request.Page.GetValueOrDefault(1);
			var pageSize = request.Size.GetValueOrDefault(20);
			if (pageNumber <= 0) pageNumber = 1;
			if (pageSize <= 0) pageSize = 20;
			if (pageSize > 100) pageSize = 100;

			var searchNorm = (request.Search ?? string.Empty).Trim();
			var pageIndexZeroBased = pageNumber - 1;

			var cacheKey = $"mylearning:{userId}:p{pageNumber}:s{pageSize}:q:{searchNorm.ToLower()}";
			if (!request.NoCache)
			{
				var cached = await _cache.StringGetAsync(cacheKey);
				if (cached.HasValue)
				{
					var cachedRes = SafeDeserialize<GetMyLearningCoursesResponse>(cached!);
					if (cachedRes is not null) return cachedRes;
				}
			}

			var enrollQ = _enrollmentRepository.Find(x => x.UserId == userId && x.IsActive, isTracking: false, ct);
			var courseQ = _courseRepository.Find(x => x.IsActive, isTracking: false, ct);
			var progressQ = _userCourseProgressQuery.Find(x => x.UserId == userId, isTracking: false, ct);

			var baseQ =
				from e in enrollQ
				join c in courseQ on e.CourseId equals c.CourseId
				join ucp in progressQ on c.CourseId equals ucp.CourseId
				where string.IsNullOrEmpty(searchNorm) || EF.Functions.ILike(c.Title, $"%{searchNorm}%")
				select new
				{
					c.CourseId,
					c.Title,
					c.Slug,
					ImageUrl = c.CourseImageUrl,
					PercentCompleted = ucp.PercentCompleted ?? 0m,
					LessonsTotal = ucp.LessonsTotal,
					LessonsCompleted = ucp.LessonsCompleted,
					StartedAt = ucp.StartedAt,      // DateTime?
					CompletedAt = ucp.CompletedAt,    // DateTime?

					// updatedAt: lấy mốc mới nhất giữa progress và course (không dùng ?? với DateTime)
					UpdatedAt = (ucp.UpdatedAt > c.UpdatedAt) ? ucp.UpdatedAt : c.UpdatedAt,

					// Lấy trực tiếp từ enum short
					Status = (ucp.Status == (short)CourseProgressStatus.Completed || ucp.CompletedAt != null)
								? CourseProgressStatus.Completed
							: (ucp.Status == (short)CourseProgressStatus.InProgress)
								? CourseProgressStatus.InProgress
								: CourseProgressStatus.NotStarted
				};

			var totalCount = await baseQ.CountAsync(ct);

			var pageItems = await baseQ
				.OrderByDescending(x => x.UpdatedAt)
				.Skip(pageIndexZeroBased * pageSize)
				.Take(pageSize)
				.ToListAsync(ct);

			var dtoItems = pageItems.Select(x => new MyLearningCourseItemDto
			{
				CourseId = x.CourseId,
				Title = x.Title,
				Slug = x.Slug,
				ImageUrl = x.ImageUrl,
				PercentCompleted = x.PercentCompleted,
				LessonsTotal = x.LessonsTotal,
				LessonsCompleted = x.LessonsCompleted,
				StartedAt = x.StartedAt,
				CompletedAt = x.CompletedAt,
				LastUpdatedAt = x.UpdatedAt,
				Status = x.Status
			}).ToList();

			var paged = new PaginatedResult<MyLearningCourseItemDto>(
				pageIndex: pageIndexZeroBased,
				pageSize: pageSize,
				totalCount: totalCount,
				data: dtoItems
			);



			response.Response = paged;
			response.Success = true;
			response.SetMessage(MessageId.I00001, "Lấy danh sách khóa đang học thành công.");

			if (totalCount > 0 && !request.NoCache)
			{
				await _cache.StringSetAsync(cacheKey, SafeSerialize(response), TimeSpan.FromMinutes(2));
			}

			return response;

		}

		#region Helpers
		private static int ClampNonNeg(int value, int max) => Math.Clamp(value, 0, max);

		private static T? SafeDeserialize<T>(string json)
		{
			try { return JsonSerializer.Deserialize<T>(json); }
			catch { return default; }
		}

		private static string SafeSerialize<T>(T obj)
		{
			return JsonSerializer.Serialize(obj, new JsonSerializerOptions
			{
				PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
				DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
			});
		}
		#endregion

	}
}
