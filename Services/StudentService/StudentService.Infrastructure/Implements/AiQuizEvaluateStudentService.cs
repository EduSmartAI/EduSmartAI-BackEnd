using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;
using StudentService.Application.Applications.Dashboards.Queries;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using System.Text.Json;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace StudentService.Infrastructure.Implements
{
	public class AiQuizEvaluateStudentService(
		IUnitOfWork unitOfWork,
		ICommandRepository<AiEvaluation> _aiEvaluateCommandRepository,
		ICommandRepository<AiEvaluationImprovement> _aiEvaluationImprovementCommandRepository) : IAiQuizEvaluateStudentService
	{
		/// <summary>
		/// Create AI Quiz Evaluate
		/// </summary>
		/// <param name="aiEvaluationUpsertEvent"></param>
		/// <returns></returns>
		/// <exception cref="NotImplementedException"></exception>
		public async Task<CreateAiQuizEvaluateResponse> CreateAiQuizEvaluate(AiEvaluationUpsertEvent aiEvaluationUpsertEvent, CancellationToken ct = default)
		{
			var response = new CreateAiQuizEvaluateResponse { Success = false };

			var strengthsJson = JsonSerializer.Serialize(aiEvaluationUpsertEvent.Strengths);
			var improvementsJson = JsonSerializer.Serialize(aiEvaluationUpsertEvent.Improvements);
			var actionsJson = JsonSerializer.Serialize(aiEvaluationUpsertEvent.Actions);
			var gapsJson = JsonSerializer.Serialize(aiEvaluationUpsertEvent.SkillGaps);


			var result = new AiEvaluation
			{
				AttemptId = aiEvaluationUpsertEvent.AttemptId,
				UserId = aiEvaluationUpsertEvent.UserId,
				CourseId = aiEvaluationUpsertEvent.CourseId,
				Scope = (short)aiEvaluationUpsertEvent.Scope,
				ScopeId = aiEvaluationUpsertEvent.ScopeId,
				QuizId = aiEvaluationUpsertEvent.QuizId,
				Score100 = aiEvaluationUpsertEvent.Score100,
				Score100Raw = aiEvaluationUpsertEvent.Score100Raw,
				Summary = aiEvaluationUpsertEvent.Summary,
				Strengths = strengthsJson,
				Improvements = improvementsJson,
				Actions = actionsJson,
				SkillGaps = gapsJson,
				Model = aiEvaluationUpsertEvent.Model,
				RubricVersion = aiEvaluationUpsertEvent.RubricVersion,
				Confidence = aiEvaluationUpsertEvent.Confidence,
				CreatedAt = DateTime.UtcNow,
			};

			// Save to DB
			await unitOfWork.BeginTransactionAsync(async () =>
						{
							await _aiEvaluateCommandRepository.AddAsync(result);
							await unitOfWork.SaveChangesAsync(ct);

							unitOfWork.Store(AiEvaluationCollection.FromWriteModel(result));
							await unitOfWork.SessionSaveChangesAsync();

							return true; // yêu cầu của BeginTransactionAsync: trả true để commit
						}, ct);

			if (aiEvaluationUpsertEvent.Improvements is { Count: > 0 })
			{
				var improvements = aiEvaluationUpsertEvent.Improvements
					.Select((text, idx) => new AiEvaluationImprovement
					{
						// ImprovementId do DB tự sinh (DEFAULT gen_random_uuid())
						EvaluationId = result.EvaluationId,
						PositionIndex = idx,
						ImprovementsText = text,
						ContentMarkdown = null,
					})
					.ToList();

				await unitOfWork.BeginTransactionAsync(async () =>
				{
					await _aiEvaluationImprovementCommandRepository.AddRangeAsync(improvements);
					await unitOfWork.SaveChangesAsync(ct);
					return true; // yêu cầu của BeginTransactionAsync: trả true để commit
				}, ct);
			}

			// response
			response.Success = true;
			response.Response = result.EvaluationId.ToString();
			response.SetMessage(MessageId.I00001, "Lưu kết quả AI đánh giá thành công");


			return response;
		}

		/// <summary>
		/// Get latest AI evaluations for multiple modules
		/// </summary>
		/// <param name="request"></param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public async Task<GetLatestModuleAiEvaluationsResponse> GetLatestModuleAiEvaluationsAsync(GetLatestModuleAiEvaluationsQuery request, CancellationToken cancellationToken)
		{
			var response = new GetLatestModuleAiEvaluationsResponse { Success = false };

			if (request.ModuleIds is null || request.ModuleIds.Count == 0)
			{
				response.SetMessage(MessageId.E11001, "ModuleIds trống");
				return response;
			}

			// base query
			var baseQuery = _aiEvaluateCommandRepository.Find(
				ev => ev.UserId == request.StudentId
				   && ev.CourseId == request.CourseId
				   && ev.Scope == (short)QuizScope.Module
				   && request.ModuleIds.Contains(ev.ScopeId),
				isTracking: false,
				cancellationToken: cancellationToken);

			// Lấy record mới nhất cho MỖI module bằng correlated subquery (tránh Join)
			var latestPerModule = await
			(
				from ev in baseQuery
				where ev.CreatedAt ==
					  baseQuery
						 .Where(x => x.ScopeId == ev.ScopeId)
						 .Max(x => x.CreatedAt)
				select new
				{
					ev.EvaluationId,
					ModuleId = ev.ScopeId,
					ev.QuizId,
					ev.Score100Raw,
					ev.Score100,
					ev.Summary,
					ev.Strengths,
					ev.CreatedAt
				}
			).ToListAsync(cancellationToken);

			if (latestPerModule.Count == 0)
			{
				response.Success = true;
				response.Response = new GetLatestModuleAiEvaluationsPayload
				{
					Modules = Array.Empty<ModuleAiEvaluationDto>()
				};
				return response;
			}

			// Lấy improvements (markdown) theo evaluation_id của bản ghi latest
			var evalIds = latestPerModule.Select(x => x.EvaluationId).Distinct().ToList();

			var improvements = await _aiEvaluationImprovementCommandRepository
				.Find(im => evalIds.Contains(im.EvaluationId), isTracking: false, cancellationToken)
				.Select(im => new
				{
					im.ImprovementId,
					im.EvaluationId,
					im.PositionIndex,
					im.ImprovementsText,
					im.ContentMarkdown,
					im.Slug,
					im.CreatedAt,
					im.UpdatedAt
				})
				.ToListAsync(cancellationToken);

			var improvementsByEval = improvements
				.GroupBy(im => im.EvaluationId)
				.ToDictionary(
					g => g.Key,
					g => g.OrderBy(x => x.PositionIndex)
						  .Select(x => new AiImprovementDto
						  {
							  ImprovementId = x.ImprovementId,
							  PositionIndex = x.PositionIndex,
							  ImprovementText = x.ImprovementsText,
							  ContentMarkdown = x.ContentMarkdown,
							  Slug = x.Slug,
							  CreatedAt = x.CreatedAt,
							  UpdatedAt = x.UpdatedAt
						  })
						  .ToList()
						  .AsReadOnly()
				);

			var modules = latestPerModule
				.Select(x => new ModuleAiEvaluationDto
				{
					ModuleId = x.ModuleId,
					QuizId = x.QuizId,
					Score100Raw = x.Score100Raw,
					Score100 = x.Score100,
					Summary = x.Summary,
					Strengths = ToList(x.Strengths),
					CreatedAt = x.CreatedAt,
					ImprovementResources = improvementsByEval.TryGetValue(x.EvaluationId, out var list)
											? list
											: Array.Empty<AiImprovementDto>()
				})
				.OrderByDescending(m => m.CreatedAt)
				.ToList();

			response.Response = new GetLatestModuleAiEvaluationsPayload { Modules = modules };
			response.Success = true;
			return response;
		}


		public async Task<GetLatestLessonAiEvaluationsResponse> GetLatestLessonAiEvaluationsAsync(GetLatestLessonAiEvaluationsQuery request, CancellationToken cancellationToken)
		{
			var response = new GetLatestLessonAiEvaluationsResponse { Success = false };

			if (request.LessonIds is null || request.LessonIds.Count == 0)
			{
				response.SetMessage(MessageId.E11001, "LessonIds trống");
				return response;
			}

			// Base query: student + course + scope=Lesson + scope_id ∈ LessonIds
			var baseQuery = _aiEvaluateCommandRepository.Find(
				ev => ev.UserId == request.StudentId
				   && ev.CourseId == request.CourseId
				   && ev.Scope == (short)QuizScope.Lesson
				   && request.LessonIds.Contains(ev.ScopeId),
				isTracking: false,
				cancellationToken: cancellationToken);

			// Latest per lesson bằng correlated subquery (tránh Join/type inference)
			var latestPerLesson = await
			(
				from ev in baseQuery
				where ev.CreatedAt ==
					  baseQuery.Where(x => x.ScopeId == ev.ScopeId)
							   .Max(x => x.CreatedAt)
				select new
				{
					ev.EvaluationId,
					LessonId = ev.ScopeId,
					ev.QuizId,
					ev.Score100Raw,
					ev.Score100,
					ev.Summary,
					ev.Strengths,   // sẽ parse ra List<string>
					ev.CreatedAt
				}
			).ToListAsync(cancellationToken);

			if (latestPerLesson.Count == 0)
			{
				response.Success = true;
				response.Response = new GetLatestLessonAiEvaluationsPayload
				{
					Lessons = Array.Empty<LessonAiEvaluationDto>()
				};
				return response;
			}

			// Lấy improvements (markdown) theo evaluation_id của bản ghi latest
			var evalIds = latestPerLesson.Select(x => x.EvaluationId).Distinct().ToList();

			var improvements = await _aiEvaluationImprovementCommandRepository
				.Find(im => evalIds.Contains(im.EvaluationId), isTracking: false, cancellationToken)
				.Select(im => new
				{
					im.ImprovementId,
					im.EvaluationId,
					im.PositionIndex,
					im.ImprovementsText,
					im.ContentMarkdown,
					im.Slug,
					im.CreatedAt,
					im.UpdatedAt
				})
				.ToListAsync(cancellationToken);

			var improvementsByEval = improvements
				.GroupBy(im => im.EvaluationId)
				.ToDictionary(
					g => g.Key,
					g => g.OrderBy(x => x.PositionIndex)
						  .Select(x => new AiImprovementDto
						  {
							  ImprovementId = x.ImprovementId,
							  PositionIndex = x.PositionIndex,
							  ImprovementText = x.ImprovementsText,
							  ContentMarkdown = x.ContentMarkdown,
							  Slug = x.Slug,
							  CreatedAt = x.CreatedAt,
							  UpdatedAt = x.UpdatedAt
						  })
						  .ToList()
						  .AsReadOnly()
				);

			// Map payload: KHÔNG có field Improvements; dùng ImprovementResources thay thế
			var lessons = latestPerLesson
				.Select(x => new LessonAiEvaluationDto
				{
					LessonId = x.LessonId,
					QuizId = x.QuizId,
					Score100Raw = x.Score100Raw,
					Score100 = x.Score100,
					Summary = x.Summary,
					Strengths = ToList(x.Strengths),
					CreatedAt = x.CreatedAt,
					ImprovementResources = improvementsByEval.TryGetValue(x.EvaluationId, out var list)
											? list
											: Array.Empty<AiImprovementDto>()
				})
				.OrderByDescending(m => m.CreatedAt)
				.ToList();

			response.Success = true;
			response.Response = new GetLatestLessonAiEvaluationsPayload { Lessons = lessons };
			return response;
		}

		// Helper: cố gắng parse JSON array, nếu không thì fallback tách theo xuống dòng/ký hiệu bullet
		private static IReadOnlyList<string> ToList(string? src)
		{
			if (string.IsNullOrWhiteSpace(src)) return Array.Empty<string>();

			try
			{
				var json = JsonSerializer.Deserialize<string[]>(src);
				if (json != null)
					return json.Where(s => !string.IsNullOrWhiteSpace(s))
							   .Select(s => s.Trim())
							   .ToArray();
			}
			catch { /* not json */ }

			return src.Split(new[] { '\r', '\n', ';', '•', '-' }, StringSplitOptions.RemoveEmptyEntries)
					  .Select(s => s.Trim().TrimStart('*', '-', '•'))
					  .Where(s => s.Length > 0)
					  .ToArray();
		}
	}
}
