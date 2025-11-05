using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.StudentService.GetAllDetailCourse;
using BuildingBlocks.Messaging.Events.StudentService.GetInfoEvaluation;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Interfaces;
using StudentService.Domain.WriteModels;

namespace StudentService.Infrastructure.Implements
{
	public class AiEvaluationService(ICommandRepository<AiEvaluation> _aiEvaluateCommandRepository, IRequestClient<GetAllDetailCourseEvent> requestClient) : IAiEvaluationService
	{
		/// <summary>
		/// Get and map info evaluation
		/// </summary>
		/// <param name="studentId"></param>
		/// <param name="courseId"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<GetInfoEvaluationEventResponse> GetAllEvaluationByCourseId(Guid studentId, Guid courseId, CancellationToken cancellationToken)
		{
			try
			{
				// 1) Lấy thông tin course hierarchy (Module + Lesson)
				var evt = new GetAllDetailCourseEvent(courseId, studentId);
				var courseInfoResp = await requestClient.GetResponse<GetAllDetailCourseResponse>(evt, cancellationToken);

				var courseInfo = courseInfoResp.Message.Response;

				// Phòng null
				var modules = courseInfo?.Modules ?? new List<CourseModuleDto>();

				// 2) Build dictionary để tra nhanh tên theo Id
				var moduleNameById = modules.ToDictionary(m => m.ModuleId, m => m.ModuleName);
				var lessonNameById = modules
					.SelectMany(m => m.Lessons ?? new List<LessonInfor>())
					.ToDictionary(l => l.LessonId, l => l.LessonTitle);

				var raw = await _aiEvaluateCommandRepository
				.Find(x => x.UserId == studentId && x.CourseId == courseId, isTracking: false)
				.OrderByDescending(x => x.CreatedAt)
				.ThenByDescending(x => x.EvaluationId)
				.Select(x => new
				{
					x.EvaluationId,
					x.AttemptId,
					x.QuizId,
					x.Score100,
					x.Score100Raw,
					x.Summary,
					x.Strengths,
					x.Improvements,
					x.Actions,
					x.SkillGaps,
					x.Model,
					x.RubricVersion,
					x.Confidence,
					x.Scope,
					x.ScopeId,
					x.CreatedAt
				})
				.ToListAsync(cancellationToken);

				// B2: group và lấy bản ghi mới nhất cho mỗi (Scope, ScopeId, QuizId) ở memory
				var flatItems = raw
					.GroupBy(x => new { x.Scope, x.ScopeId, x.QuizId })
					.Select(g => g.First()) // vì đã order desc ở B1
					.Select(x => new GetInfoEvaluationItemDto
					{
						EvaluationId = x.EvaluationId,
						AttemptId = x.AttemptId,
						QuizId = x.QuizId,
						Name = string.Empty, // sẽ set ở bước 4
						Score100 = x.Score100,
						Score100Raw = x.Score100Raw,
						Summary = x.Summary ?? string.Empty,
						Strengths = x.Strengths ?? string.Empty,
						Improvements = x.Improvements ?? string.Empty,
						Actions = x.Actions ?? string.Empty,
						SkillGaps = x.SkillGaps ?? string.Empty,
						Model = x.Model ?? string.Empty,
						RubricVersion = x.RubricVersion ?? string.Empty,
						Confidence = x.Confidence,
						Scope = x.Scope,
						ScopeId = x.ScopeId,
						CreatedAt = x.CreatedAt
					})
					.ToList();

				// 4) Map Name theo (Scope, ScopeId) — thực hiện ở memory để tránh EF translate
				foreach (var item in flatItems)
				{
					if (item.Scope == (short)ConstantEnum.QuizScope.Lesson && item.ScopeId.HasValue)
					{
						if (!lessonNameById.TryGetValue(item.ScopeId.Value, out var lessonName))
							lessonName = string.Empty;
						item.Name = lessonName;
					}
					else if (item.Scope == (short)ConstantEnum.QuizScope.Module && item.ScopeId.HasValue)
					{
						if (!moduleNameById.TryGetValue(item.ScopeId.Value, out var moduleName))
							moduleName = string.Empty;
						item.Name = moduleName;
					}
					else
					{
						item.Name = string.Empty;
					}
				}

				// 5) Group theo ScopeId cho từng scope để trả về đúng schema
				var lessons = flatItems
						.Where(i => i.Scope == (short)ConstantEnum.QuizScope.Lesson && i.ScopeId.HasValue)
						.GroupBy(i => i.ScopeId) // key: Guid?
						.Select(g => new EvaluationGroupDto
						{
						ScopeId = g.Key, // LessonId
							Evaluations = g.OrderByDescending(e => e.CreatedAt).ToList()
						})
						.ToList();

				var modulesGrouped = flatItems
						.Where(i => i.Scope == (short)ConstantEnum.QuizScope.Module && i.ScopeId.HasValue)
						.GroupBy(i => i.ScopeId) // key: Guid?
						.Select(g => new EvaluationGroupDto
						{
						ScopeId = g.Key, // ModuleId
							Evaluations = g.OrderByDescending(e => e.CreatedAt).ToList()
						})
						.ToList();

				return new GetInfoEvaluationEventResponse
				{
					Response = new GetInfoEvaluationGroupedDto
					{
						Lessons = lessons,
						Modules = modulesGrouped
					}
				};
			}
			catch (Exception)
			{
				return new GetInfoEvaluationEventResponse
				{
					Response = new GetInfoEvaluationGroupedDto
					{
						Lessons = [],
						Modules = []
					}
				};
			}
		}
	}
}
