using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiEvaluationUpsertEvents;
using StudentService.Application.Applications.AiQuizEvaluates.Commands.CreateAiQuizEvaluate;
using StudentService.Application.Interfaces;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;
using System.Text.Json;

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
	}
}
