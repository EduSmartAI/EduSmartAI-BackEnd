using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.CourseService;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using MassTransit;
using StudentService.Application.Applications.Dashboards.Queries;
using StudentService.Application.Interfaces;

namespace StudentService.Infrastructure.Implements
{
	public class DashboardService(
		IIdentityService _identityService,
		IRequestClient<GetModuleDashboardEvent> _courseClient,
		IAiQuizEvaluateStudentService _aiService
	) : IDashboardService
	{
		public async Task<GetModuleDashboardEventResponse> GetModuleDashboardAsync(GetModuleDashboardQuery request, CancellationToken ct = default)
		{
			var response = new GetModuleDashboardEventResponse { Success = false };
			var studentId = _identityService.GetCurrentUser()!.UserId;

			var events = new GetModuleDashboardEvent(studentId, request.CourseId);

			var courseReply = await _courseClient.GetResponse<GetCourseModuleDashboardEventResponse>(events, ct);

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
							AiImprovements = ai.Improvements?.ToList()       // IReadOnlyList<string>?
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
