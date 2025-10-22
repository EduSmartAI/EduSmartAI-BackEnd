using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
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
		ICommandRepository<AiEvaluation> _aiEvaluateCommandRepository) : IAiQuizEvaluateStudentService
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

			// Base query: theo student + course + scope = Module + scope_id ∈ ModuleIds
			var q = _aiEvaluateCommandRepository.Find(
				ev => ev.UserId == request.StudentId
				   && ev.CourseId == request.CourseId
				   && ev.Scope == (short)QuizScope.Module
				   && request.ModuleIds.Contains(ev.ScopeId),
				isTracking: false,
				cancellationToken: cancellationToken
			); // IQueryable<AiEvaluation>

			// Lấy newest per module (GroupBy → OrderByDescending → FirstOrDefault)
			// EF Core 6/7/8 dịch tốt pattern này về SQL (SELECT DISTINCT ON / CROSS APPLY tùy provider)
			var latestPerModule = await q
				.GroupBy(ev => ev.ScopeId)
				.Select(g => g.OrderByDescending(ev => ev.CreatedAt).FirstOrDefault()!)
				.ToListAsync(cancellationToken);

			var modules = latestPerModule
				.Where(ev => ev != null)
				.Select(ev => new ModuleAiEvaluationDto
				{
					ModuleId = ev.ScopeId,
					QuizId = ev.QuizId,
					Score100Raw = ev.Score100Raw.HasValue ? (int?)ev.Score100Raw.Value : null,
					Score100 = ev.Score100,
					Summary = ev.Summary,
					Strengths = ToList(ev.Strengths),
					Improvements = ToList(ev.Improvements),
					CreatedAt = ev.CreatedAt
				})
				// Optional: sắp theo thời gian mới → cũ khi trả về
				.OrderByDescending(m => m.CreatedAt)
				.ToList();

			response.Response = new GetLatestModuleAiEvaluationsPayload
			{
				Modules = modules
			};
			response.Success = true;
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
