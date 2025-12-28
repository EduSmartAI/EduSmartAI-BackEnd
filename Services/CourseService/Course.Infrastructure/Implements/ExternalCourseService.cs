using BuildingBlocks.Messaging.Events.CourseService.LessonQuizScoresSelectEvents;
using BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Infrastructure.Implements
{
	public class ExternalCourseService(
		ICommandRepository<CourseEntity> _courseRepository,
		IDatabase _cache,
		ICacheKeyFactory _cacheKeyFactory,
		ICommandRepository<Module> _moduleRepository,
		ICommandRepository<Lesson> _lessonRepository,
		ICommandRepository<UserLessonProgress> _userLessonProgressRepository,
		ICommandRepository<UserModuleProgress> _userModuleProgressRepository,
		ICommandRepository<ModuleQuiz> _moduleQuizRepository,
		ICommandRepository<LessonQuiz> _lessonQuizRepository,
		IRequestClient<GetLatestModuleQuizScoresEvent> _quizScoresClient,
		IRequestClient<GetLatestLessonQuizScoresEvent> _quizLessonClient
	) : IExternalCourseService
	{
		public async Task<GetCourseLessonDashboardEventResponse> GetCourseLessonDashboardAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken)
		{
			var response = new GetCourseLessonDashboardEventResponse { Success = false };

			// 1) Course exists
			var course = await _courseRepository.FirstOrDefaultAsync(c => c.CourseId == courseId && c.IsActive, cancellationToken);
			if (course is null)
			{
				response.SetMessage(MessageId.E11001, $"CourseId {courseId} không tồn tại");
				return response;
			}

			// 2) Modules
			var modules = await _moduleRepository
				.Find(m => m.CourseId == courseId && m.IsActive, isTracking: false, cancellationToken: cancellationToken)
				.OrderBy(m => m.PositionIndex)
				.ToListAsync(cancellationToken);
			if (modules.Count == 0)
			{
				response.Success = true;
				response.Response = new CourseLessonDashboardContract
				{
					StudentId = studentId,
					CourseId = courseId,
					Modules = Array.Empty<CourseLessonModuleGroup>(),
					Totals = new CourseLessonTotals()
				};
				return response;
			}
			var moduleIds = modules.Select(m => m.ModuleId).ToList();

			// 3) Lessons
			var lessons = await _lessonRepository
				.Find(l => moduleIds.Contains(l.ModuleId) && l.IsActive, isTracking: false, cancellationToken: cancellationToken)
				.OrderBy(l => l.PositionIndex)
				.ToListAsync(cancellationToken);
			var lessonIds = lessons.Select(l => l.LessonId).ToList();

			// 4) UserLessonProgress
			var ulps = lessonIds.Count == 0
				? new List<UserLessonProgress>()
				: await _userLessonProgressRepository
					.Find(ulp => ulp.UserId == studentId && lessonIds.Contains(ulp.LessonId),
						  isTracking: false, cancellationToken: cancellationToken)
					.ToListAsync(cancellationToken);

			// 5) Lesson quizzes
			var lqs = lessonIds.Count == 0
				? new List<LessonQuiz>()
				: await _lessonQuizRepository
					.Find(lq => lessonIds.Contains(lq.LessonId) && lq.IsActive,
						  isTracking: false, cancellationToken: cancellationToken)
					.ToListAsync(cancellationToken);

			var ulpByLesson = ulps.ToDictionary(x => x.LessonId, x => x);
			var quizCountByLesson = lqs
				.GroupBy(q => q.LessonId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.QuizId).Distinct().Count());

			// 6) Gọi QuizService lấy latest score_100 cho mỗi lesson
			Dictionary<Guid, int?> latestScoreByLesson = new();
			if (lessonIds.Count > 0)
			{
				try
				{
					var req = new GetLatestLessonQuizScoresEvent(studentId, courseId, lessonIds);
					var resp = await _quizLessonClient.GetResponse<GetLatestLessonQuizScoresResponseEvent>(req, cancellationToken);

					if (resp.Message.Success && resp.Message.Response?.Lessons is { Count: > 0 } list)
					{
						latestScoreByLesson = list.ToDictionary(x => x.LessonId, x => x.LatestScore100);
					}
				}
				catch
				{
					// không làm fail dashboard; để điểm = null
				}
			}

			// 7) Build groups
			var lessonsByModule = lessons.GroupBy(l => l.ModuleId)
										 .ToDictionary(g => g.Key, g => g.ToList());

			var groups = new List<CourseLessonModuleGroup>(modules.Count);
			foreach (var m in modules)
			{
				var moduleLessons = lessonsByModule.TryGetValue(m.ModuleId, out var list) ? list : new List<Lesson>();
				var items = new List<CourseLessonItem>(moduleLessons.Count);

				foreach (var l in moduleLessons)
				{
					ulpByLesson.TryGetValue(l.LessonId, out var ulp);

					var status = ulp is null ? LessonStatus.NotStarted
						: ulp.Status == 2 ? LessonStatus.Completed
						: ulp.Status == 1 ? LessonStatus.InProgress
						: LessonStatus.NotStarted;

					var videoSec = Math.Max(0, l.VideoDurationSec ?? 0);
					var watchedSec = Math.Max(0, ulp?.DurationWatchedSec ?? 0);
					var percent = videoSec == 0 ? 0m : Math.Round((decimal)watchedSec * 100m / videoSec, 2);

					quizCountByLesson.TryGetValue(l.LessonId, out var lqCount);
					latestScoreByLesson.TryGetValue(l.LessonId, out var latestScore);

					DateTime? updatedAt = null;
					if (ulp?.UpdatedAt is DateTime ulpUpd)
						updatedAt = ulpUpd > l.UpdatedAt ? ulpUpd : l.UpdatedAt;
					else
						updatedAt = l.UpdatedAt;

					items.Add(new CourseLessonItem
					{
						LessonId = l.LessonId,
						Title = l.Title,
						PositionIndex = l.PositionIndex,
						IsActive = l.IsActive,
						VideoUrl = l.VideoUrl,

						Status = status,
						CurrentSecond = ulp?.LastSeenPositionSec,
						VideoDurationSeconds = videoSec,
						ActualStudyMinutes = (int)Math.Ceiling(watchedSec / 60.0),
						PercentWatched = percent,

						LessonQuizCount = lqCount,
						AverageQuizScore = latestScore.HasValue ? latestScore.Value : null,

						CompletedAtUtc = ulp?.CompletedAt,
						UpdatedAtUtc = updatedAt
					});
				}

				groups.Add(new CourseLessonModuleGroup
				{
					ModuleId = m.ModuleId,
					ModuleName = m.ModuleName,
					PositionIndex = m.PositionIndex,
					Lessons = items
				});
			}

			// 8) Totals
			var flat = groups.SelectMany(g => g.Lessons).ToList();

			var totals = new CourseLessonTotals
			{
				ModulesCount = groups.Count,
				LessonsCount = flat.Count,
				TotalVideoDurationMinutes = flat.Sum(x => (int)Math.Ceiling(x.VideoDurationSeconds / 60.0)),
				TotalActualStudyMinutes = flat.Sum(x => x.ActualStudyMinutes),
				TotalLessonQuizCount = flat.Sum(x => x.LessonQuizCount),
				AverageQuizScore = flat.Any(x => x.AverageQuizScore.HasValue)
					? Math.Round(flat.Where(x => x.AverageQuizScore.HasValue)
									 .Average(x => x.AverageQuizScore!.Value), 2)
					: (decimal?)null
			};

			// 9) Response
			response.Success = true;
			response.Response = new CourseLessonDashboardContract
			{
				StudentId = studentId,
				CourseId = courseId,
				Modules = groups,
				Totals = totals
			};

			response.SetMessage(MessageId.I00001, "Lấy Course Lesson Dashboard thành công");

			return response;
		}

		/// <summary>
		/// Get Course Module Dashboard
		/// </summary>
		/// <param name="studentId"></param>
		/// <param name="courseId"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<GetCourseModuleDashboardEventResponse> GetCourseModuleDashboardAsync(Guid studentId, Guid courseId, CancellationToken cancellationToken)
		{
			var response = new GetCourseModuleDashboardEventResponse{ Success = false };

			// 1) Verify course exists & active (optional nhưng tốt cho tính đúng đắn)
			var courseExists = await _courseRepository.FirstOrDefaultAsync(
				c => c.CourseId == courseId && c.IsActive,
				cancellationToken
			);
			if (courseExists is null)
			{
				response.SetMessage(MessageId.E11001, $"CourseId {courseId.ToString()} không tồn tại");
				return response;
			}

			// 2) Lấy danh sách module active của course (order by position_index)
			var modules = await _moduleRepository
				.Find(m => m.CourseId == courseId && m.IsActive, isTracking: false, cancellationToken)
				.OrderBy(m => m.PositionIndex)
				.ToListAsync(cancellationToken);

			if (modules.Count == 0)
			{
				response.SetMessage(MessageId.E11001, $"CourseId {courseId.ToString()} không tồn tại modules nào");
				return response;
			}

			var moduleIds = modules.Select(m => m.ModuleId).ToList();

			// 3) Lấy tất cả lessons active thuộc các module này
			var lessons = await _lessonRepository
				.Find(l => moduleIds.Contains(l.ModuleId) && l.IsActive, isTracking: false, cancellationToken)
				.ToListAsync(cancellationToken);

			var lessonIds = lessons.Select(l => l.LessonId).ToList();

			// 4) Progress theo user cho tất cả lessons liên quan
			var userLessonProgresses = lessonIds.Count == 0
				? new List<UserLessonProgress>()
				: await _userLessonProgressRepository
					.Find(ulp => ulp.UserId == studentId && lessonIds.Contains(ulp.LessonId),
						  isTracking: false, cancellationToken)
					.ToListAsync(cancellationToken);

			// 5) Progress theo user cho modules (nếu bạn duy trì bảng này)
			var userModuleProgresses = await _userModuleProgressRepository
				.Find(ump => ump.UserId == studentId && moduleIds.Contains(ump.ModuleId),
					  isTracking: false, cancellationToken)
				.ToListAsync(cancellationToken);

			// 6) Quiz cấp module
			var moduleQuizzes = await _moduleQuizRepository
				.Find(mq => moduleIds.Contains(mq.ModuleId) && mq.IsActive, isTracking: false, cancellationToken)
				.ToListAsync(cancellationToken);

			// 7) Quiz cấp lesson (chỉ trong phạm vi lessons của các module này)
			var lessonQuizzes = lessonIds.Count == 0
				? new List<LessonQuiz>()
				: await _lessonQuizRepository
					.Find(lq => lessonIds.Contains(lq.LessonId) && lq.IsActive, isTracking: false, cancellationToken)
					.ToListAsync(cancellationToken);

			// 8) Tiền xử lý: gộp theo module để tính nhanh
			var lessonsByModule = lessons.GroupBy(l => l.ModuleId).ToDictionary(g => g.Key, g => g.ToList());
			var ulpByLessonId = userLessonProgresses.ToDictionary(x => x.LessonId, x => x);
			var umpByModuleId = userModuleProgresses.ToDictionary(x => x.ModuleId, x => x);

			var moduleQuizCountByModule = moduleQuizzes
				.GroupBy(q => q.ModuleId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.QuizId).Distinct().Count());

			var lessonQuizCountByModule = lessonQuizzes
				.Join(lessons, lq => lq.LessonId, l => l.LessonId, (lq, l) => new { l.ModuleId, lq.QuizId })
				.GroupBy(x => x.ModuleId)
				.ToDictionary(g => g.Key, g => g.Select(x => x.QuizId).Distinct().Count());

			// 9) Map per-module item
			var items = new List<CourseModuleDashboardItem>(modules.Count);

			foreach (var m in modules)
			{
				lessonsByModule.TryGetValue(m.ModuleId, out var moduleLessons);
				moduleLessons ??= new List<Lesson>();

				var lessonsTotal = moduleLessons.Count; // vì đã filter IsActive ở bước (3)

				// Đếm số lesson completed/in-progress dựa trên userLessonProgress
				var completed = 0;
				var inProgress = 0;
				var watchedSecSum = 0;

				foreach (var lesson in moduleLessons)
				{
					if (ulpByLessonId.TryGetValue(lesson.LessonId, out var ulp))
					{
						// status: 0=chưa học,1=đang học,2=hoàn thành (đồng bộ với enum bạn dùng)
						if (ulp.Status == 2) completed++;
						else if (ulp.Status == 1) inProgress++;

						watchedSecSum += Math.Max(0, ulp.DurationWatchedSec);
					}
				}

				var percentCompleted = lessonsTotal == 0
					? 0m
					: Math.Round(completed * 100m / lessonsTotal, 2);

				// status ưu tiên từ user_module_progress; nếu không có thì derive từ %complete
				ModuleProgressStatus status;
				if (umpByModuleId.TryGetValue(m.ModuleId, out var ump))
				{
					status = (ModuleProgressStatus)ump.Status;
				}
				else
				{
					if (percentCompleted == 100m)
					{
						status = ModuleProgressStatus.Completed;
					}
					else if (percentCompleted > 0m)
					{
						status = ModuleProgressStatus.InProgress;
					}
					else
					{
						status = ModuleProgressStatus.NotStarted;
					}
				}

				// Duration dự kiến: ưu tiên Module.DurationMinutes; fallback tổng video (sec→phút, làm tròn lên)
				var sumVideoSec = moduleLessons.Sum(x => Math.Max(0, x.VideoDurationSec ?? 0));
				var fallbackMinutes = (int)Math.Ceiling(sumVideoSec / 60.0);
				var moduleDurationMinutes = m.DurationMinutes ?? fallbackMinutes;

				// Thời gian học thực tế: sum watchedSec → phút (làm tròn lên)
				var actualStudyMinutes = (int)Math.Ceiling(watchedSecSum / 60.0);

				// Quiz counts
				var moduleQuizCount = moduleQuizCountByModule.TryGetValue(m.ModuleId, out var mqCount) ? mqCount : 0;
				var lessonQuizCount = lessonQuizCountByModule.TryGetValue(m.ModuleId, out var lqCount) ? lqCount : 0;
				var totalQuizCount = moduleQuizCount + lessonQuizCount;
				DateTime? umpUpdatedAt = null;
				if (umpByModuleId.TryGetValue(m.ModuleId, out var ump4))
					umpUpdatedAt = ump4.UpdatedAt;

				var latest = umpUpdatedAt.HasValue && umpUpdatedAt.Value > m.UpdatedAt
					? umpUpdatedAt.Value
					: m.UpdatedAt;

				items.Add(new CourseModuleDashboardItem
				{
					ModuleId = m.ModuleId,
					ModuleName = m.ModuleName,
					PositionIndex = m.PositionIndex,
					Level = m.Level,
					IsCore = m.IsCore,
					Description = m.Description,
					Status = status,
					LessonsVideoTotal = lessonsTotal,
					LessonsCompleted = completed,
					PercentCompleted = percentCompleted,
					LessonsInProgress = inProgress,
					ModuleDurationMinutes = moduleDurationMinutes,
					ActualStudyMinutes = actualStudyMinutes,
					ModuleQuizCount = moduleQuizCount,
					LessonQuizCount = lessonQuizCount,
					TotalQuizCount = totalQuizCount,
					StartedAtUtc = umpByModuleId.TryGetValue(m.ModuleId, out var ump2) ? ump2.StartedAt : null,
					CompletedAtUtc = umpByModuleId.TryGetValue(m.ModuleId, out var ump3) ? ump3.CompletedAt : null,
					UpdatedAtUtc = latest
				});
			}

			// 10) Lấy điểm quiz từ QuizService (nếu có quiz)
			if (items.Count > 0)
			{
				var moduleIdsReq = items.Select(x => x.ModuleId).ToList();

				var quizReq = new GetLatestModuleQuizScoresEvent(
					StudentId: studentId,
					CourseId: courseId,
					ModuleIds: moduleIdsReq
				);
				var quizResp = await _quizScoresClient.GetResponse<GetLatestModuleQuizScoresResponseEvent>(
					quizReq,
					cancellationToken
				);

				if (quizResp.Message.Success && quizResp.Message.Response?.Modules is { Count: > 0 } stats)
				{
					var statByModule = stats.ToDictionary(s => s.ModuleId, s => s);
					foreach (var it in items)
					{
						if (statByModule.TryGetValue(it.ModuleId, out var s))
						{
							// LatestScore100 (int?) từ QuizService → AverageQuizScore (nullable) của item
							// Nếu AverageQuizScore là decimal? thì cast nhẹ:
							it.AverageQuizScore = s.LatestScore100.HasValue
								? s.LatestScore100.Value
								: null;

							// (tùy chọn) nếu bạn muốn theo dõi số lần attempt “thừa”, có thể thêm field:
							// it.QuizAttemptCount = s.AttemptCount
						}
					}
				}
			}

			// 11) Totals (course-level aggregation over modules)
			var lessonsTotalAll = items.Sum(x => x.LessonsVideoTotal);
			var lessonsCompletedAll = items.Sum(x => x.LessonsCompleted);

			var totals = new CourseModuleDashboardTotals
			{
				ModulesCount = items.Count,
				LessonsTotal = lessonsTotalAll,
				LessonsCompleted = lessonsCompletedAll,
				PercentCompleted = lessonsTotalAll == 0
					? 0
					: Math.Round((decimal)lessonsCompletedAll * 100m / (decimal)lessonsTotalAll, 2),
				TotalModuleDurationMinutes = items.Sum(x => x.ModuleDurationMinutes),
				TotalActualStudyMinutes = items.Sum(x => x.ActualStudyMinutes),
				TotalModuleQuizCount = items.Sum(x => x.ModuleQuizCount),
				TotalLessonQuizCount = items.Sum(x => x.LessonQuizCount),
				TotalQuizCount = items.Sum(x => x.TotalQuizCount)
			};

			// 12) Final response
			response.Success = true;
			response.Response = new CourseModuleDashboardContract
			{
				StudentId = studentId,
				CourseId = courseId,
				Modules = items.OrderBy(x => x.PositionIndex).ToList(),
				Totals = totals
			};
			response.SetMessage(MessageId.I00001, "Lấy Course Module Dashboard thành công");
			return response;
		}
	}
}
