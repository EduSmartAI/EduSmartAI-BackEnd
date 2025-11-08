using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.LessonDashboard;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using MassTransit;
using StudentService.Application.Applications.Dashboards.Queries;
using StudentService.Application.Interfaces;

namespace StudentService.Infrastructure.Implements
{
	public class DashboardService(
		IIdentityService _identityService,
		IRequestClient<GetModuleDashboardEvent> _courseModuleClient,
		IRequestClient<GetLessonDashboardEvent> _courseLessonClient,
		IAiQuizEvaluateStudentService _aiService
	) : IDashboardService
	{
		/// <summary>
		/// Get Lesson Dashboard
		/// </summary>
		/// <param name="request"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetLessonDashboardEventResponse> GetLessonDashboardAsync(GetLessonDashboardQuery request, CancellationToken ct = default)
		{
			var response = new GetLessonDashboardEventResponse { Success = false };
			var userId = _identityService.GetCurrentUser()!.UserId;

			// 1) Lấy lesson dashboard “thô” từ CourseService
			GetCourseLessonDashboardEventResponse cr;
			try
			{
				var courseReply = await _courseLessonClient
					.GetResponse<GetCourseLessonDashboardEventResponse>(
						new GetLessonDashboardEvent(userId, request.CourseId), ct);
				cr = courseReply.Message;
			}
			catch (Exception ex)
			{
				response.SetMessage(MessageId.E11001, $"Không gọi được CourseService: {ex.Message}");
				return response;
			}

			if (!cr.Success || cr.Response is null)
			{
				response.SetMessage(cr.MessageId ?? MessageId.E11001, cr.Message ?? "CourseService lỗi");
				return response;
			}

			var course = cr.Response;

			// 2) Map sang contract thống nhất của StudentService (chưa enrich AI)
			var unifiedGroups = new List<LessonDashboardModuleGroup>(course.Modules.Count);
			var lessonIds = new List<Guid>();

			foreach (var g in course.Modules.OrderBy(m => m.PositionIndex))
			{
				var items = new List<LessonDashboardItem>(g.Lessons.Count);

				foreach (var l in g.Lessons.OrderBy(x => x.PositionIndex))
				{
					lessonIds.Add(l.LessonId);

					items.Add(new LessonDashboardItem
					{
						LessonId = l.LessonId,
						Title = l.Title,
						PositionIndex = l.PositionIndex,
						IsActive = l.IsActive,
						VideoUrl = l.VideoUrl,

						Status = l.Status,
						CurrentSecond = l.CurrentSecond,
						VideoDurationSeconds = l.VideoDurationSeconds,
						ActualStudyMinutes = l.ActualStudyMinutes,
						PercentWatched = l.PercentWatched,

						LessonQuizCount = l.LessonQuizCount,
						AverageQuizScore = l.AverageQuizScore, // decimal?

						StartedAtUtc = l.StartedAtUtc,
						CompletedAtUtc = l.CompletedAtUtc,
						UpdatedAtUtc = l.UpdatedAtUtc
					});
				}

				unifiedGroups.Add(new LessonDashboardModuleGroup
				{
					ModuleId = g.ModuleId,
					ModuleName = g.ModuleName,
					PositionIndex = g.PositionIndex,
					Lessons = items
				});
			}

			// 3) Lấy AI evaluations (latest per lesson) từ StudentService DB và enrich
			if (lessonIds.Count > 0)
			{
				GetLatestLessonAiEvaluationsResponse aiResp;
				try
				{
					aiResp = await _aiService.GetLatestLessonAiEvaluationsAsync(
						new GetLatestLessonAiEvaluationsQuery(userId, request.CourseId, lessonIds), ct);
				}
				catch (Exception ex)
				{
					// Không làm fail dashboard nếu AI lỗi
					aiResp = new GetLatestLessonAiEvaluationsResponse
					{
						Success = false,
						Response = new GetLatestLessonAiEvaluationsPayload { Lessons = Array.Empty<LessonAiEvaluationDto>() }
					};
					// Optional: log cảnh báo
				}

				if (aiResp.Success && aiResp.Response?.Lessons is { Count: > 0 } aiList)
				{
					var aiByLesson = aiList.ToDictionary(x => x.LessonId, x => x);

					// enrich từng item
					for (int gi = 0; gi < unifiedGroups.Count; gi++)
					{
						var group = unifiedGroups[gi];
						var newLessons = new List<LessonDashboardItem>(group.Lessons.Count);

						foreach (var it in group.Lessons)
						{
							if (!aiByLesson.TryGetValue(it.LessonId, out var ai))
							{
								newLessons.Add(it);
								continue;
							}

							// Vì LessonDashboardItem là class + init, ta tạo instance mới để “gắn” AI
							newLessons.Add(new LessonDashboardItem
							{
								// copy các field cũ
								LessonId = it.LessonId,
								Title = it.Title,
								PositionIndex = it.PositionIndex,
								IsActive = it.IsActive,
								VideoUrl = it.VideoUrl,
								Status = it.Status,
								CurrentSecond = it.CurrentSecond,
								VideoDurationSeconds = it.VideoDurationSeconds,
								ActualStudyMinutes = it.ActualStudyMinutes,
								PercentWatched = it.PercentWatched,
								LessonQuizCount = it.LessonQuizCount,
								AverageQuizScore = it.AverageQuizScore,
								StartedAtUtc = it.StartedAtUtc,
								CompletedAtUtc = it.CompletedAtUtc,
								UpdatedAtUtc = it.UpdatedAtUtc,

								// AI fields
								AiScore = ai.Score100,
								AiScoreRaw = ai.Score100Raw,
								AiFeedbackSummary = ai.Summary,
								AiStrengths = ai.Strengths?.ToList(),
								AiImprovementResources = ai.ImprovementResources?.ToList(),
								AiEvaluatedAtUtc = ai.CreatedAt
							});
						}

						unifiedGroups[gi] = new LessonDashboardModuleGroup
						{
							ModuleId = group.ModuleId,
							ModuleName = group.ModuleName,
							PositionIndex = group.PositionIndex,
							Lessons = newLessons
						};
					}
				}
			}

			// 4) Tính totals
			var flat = unifiedGroups.SelectMany(g => g.Lessons).ToList();

			var totals = new LessonDashboardTotals
			{
				ModulesCount = unifiedGroups.Count,
				LessonsCount = flat.Count,
				TotalVideoDurationMinutes = flat.Sum(x => (int)Math.Ceiling(x.VideoDurationSeconds / 60.0)),
				TotalActualStudyMinutes = flat.Sum(x => x.ActualStudyMinutes),
				TotalLessonQuizCount = flat.Sum(x => x.LessonQuizCount),
				AverageQuizScore = flat.Any(x => x.AverageQuizScore.HasValue)
					? Math.Round(flat.Where(x => x.AverageQuizScore.HasValue)
									 .Average(x => x.AverageQuizScore!.Value), 2)
					: (decimal?)null,
				AverageAiScore = flat.Any(x => x.AiScore.HasValue)
					? (int?)(int)Math.Round(flat.Where(x => x.AiScore.HasValue)
												 .Average(x => (double)x.AiScore!.Value), 0)
					: null
			};

			// 5) Return
			response.Success = true;
			response.Response = new LessonDashboardContract
			{
				StudentId = userId,
				CourseId = request.CourseId,
				Modules = unifiedGroups,
				Totals = totals
			};
			return response;
		}

		/// <summary>
		/// Get Module Dashboard
		/// </summary>
		/// <param name="request"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<GetModuleDashboardEventResponse> GetModuleDashboardAsync(GetModuleDashboardQuery request, CancellationToken ct = default)
		{
			var response = new GetModuleDashboardEventResponse { Success = false };
			var studentId = _identityService.GetCurrentUser()!.UserId;

			var events = new GetModuleDashboardEvent(studentId, request.CourseId);

			var courseReply = await _courseModuleClient.GetResponse<GetCourseModuleDashboardEventResponse>(events, ct);

			if (!courseReply.Message.Success || courseReply.Message.Response is null)
			{
				response.SetMessage(MessageId.E11001, courseReply.Message.Message);
				return response;
			}

			var course = courseReply.Message.Response;

			if (course.Modules is null || course.Modules.Count == 0)
			{
				// Không có module → trả luôn (không cần gọi AI)
				response.Success = false;
				//response.Response = course
				return response;
			}

			var modulesFromCourse = course.Modules?.OrderBy(m => m.PositionIndex).ToList() ?? new();

			var unifiedItems = new List<ModuleDashboardItemContract>(modulesFromCourse.Count);

			foreach (var m in modulesFromCourse)
			{
				unifiedItems.Add(new ModuleDashboardItemContract
				{
					// identity/meta
					ModuleId = m.ModuleId,
					ModuleName = m.ModuleName,
					PositionIndex = m.PositionIndex,
					Level = m.Level,
					IsCore = m.IsCore,
					Description = m.Description,

					// progress
					Status = m.Status,
					LessonsVideoTotal = m.LessonsVideoTotal,         // map tên khác nhau
					LessonsCompleted = m.LessonsCompleted,
					PercentCompleted = m.PercentCompleted,
					LessonsInProgress = m.LessonsInProgress,

					// durations
					ModuleDurationMinutes = m.ModuleDurationMinutes,
					ActualStudyMinutes = m.ActualStudyMinutes,

					// quiz counts & quiz score (điểm latest từ QuizService đã được CourseService điền vào AverageQuizScore)
					ModuleQuizCount = m.ModuleQuizCount,
					LessonQuizCount = m.LessonQuizCount,
					TotalQuizCount = m.TotalQuizCount,
					AverageQuizScore = m.AverageQuizScore,      // 0..100, nullable

					// timestamps
					StartedAtUtc = m.StartedAtUtc,
					CompletedAtUtc = m.CompletedAtUtc,
					UpdatedAtUtc = m.UpdatedAtUtc               // Course định nghĩa non-nullable, contract Student là nullable => gán OK
				});
			}

			if (unifiedItems.Count > 0)
			{
				var moduleIds = unifiedItems.Select(x => x.ModuleId).ToList();

				var query = new GetLatestModuleAiEvaluationsQuery
				(
					studentId,
					request.CourseId,
					moduleIds
				);

				var aiResp = await _aiService.GetLatestModuleAiEvaluationsAsync(query, ct);

				if (aiResp.Success && aiResp.Response?.Modules is { Count: > 0 } aiModules)
				{
					var aiByModule = aiModules.ToDictionary(x => x.ModuleId, x => x);

					for (int i = 0; i < unifiedItems.Count; i++)
					{
						var it = unifiedItems[i];
						if (!aiByModule.TryGetValue(it.ModuleId, out var ai)) continue;

						unifiedItems[i] = it with
						{
							AiScore = ai.Score100,                    // int?
							AiFeedbackSummary = ai.Summary,                     // string?
							AiStrengths = ai.Strengths?.ToList(),         // IReadOnlyList<string>?
							ImprovementResources = ai.ImprovementResources       // IReadOnlyList<string>?
																				 // Nếu có thêm: AiScoreRaw, AiEvaluatedAtUtc ... thì set ở đây
						};
					}
				}
			}

			// 4) Tính Totals thống nhất (có thể tận dụng totals từ Course + bổ sung trung bình điểm nếu muốn)
			var unifiedTotals = new ModuleDashboardTotalsContract
			{
				ModulesCount = unifiedItems.Count,
				LessonsTotal = unifiedItems.Sum(x => x.LessonsVideoTotal),
				LessonsCompleted = unifiedItems.Sum(x => x.LessonsCompleted),
				PercentCompleted = unifiedItems.Sum(x => x.LessonsVideoTotal) == 0
					? 0
					: Math.Round(
						(decimal)unifiedItems.Sum(x => x.LessonsCompleted) * 100m /
						unifiedItems.Sum(x => x.LessonsVideoTotal), 2),

				TotalModuleDurationMinutes = unifiedItems.Sum(x => x.ModuleDurationMinutes),
				TotalActualStudyMinutes = unifiedItems.Sum(x => x.ActualStudyMinutes),
				TotalModuleQuizCount = unifiedItems.Sum(x => x.ModuleQuizCount),
				TotalLessonQuizCount = unifiedItems.Sum(x => x.LessonQuizCount),
				TotalQuizCount = unifiedItems.Sum(x => x.TotalQuizCount),

				// Trung bình điểm quiz toàn khóa (nếu cần hiển thị)
				AverageQuizScore = unifiedItems.Any(x => x.AverageQuizScore.HasValue)
					? Math.Round(unifiedItems.Where(x => x.AverageQuizScore.HasValue)
											 .Average(x => x.AverageQuizScore!.Value), 2)
					: (decimal?)null,

				// Trung bình AI score toàn khóa (nếu cần hiển thị)
				AverageAiScore = unifiedItems.Any(x => x.AiScore.HasValue)
					? (int?)(int)Math.Round(unifiedItems.Where(x => x.AiScore.HasValue)
														  .Average(x => (decimal)x.AiScore!.Value), 0)
					: null
			};

			// 5) Gán lại response
			response.Success = true;
			response.Response = new ModuleDashboardContract
			{
				StudentId = course.StudentId,
				CourseId = course.CourseId,
				Modules = unifiedItems,
				Totals = unifiedTotals
			};

			return response;
		}
	}
}
