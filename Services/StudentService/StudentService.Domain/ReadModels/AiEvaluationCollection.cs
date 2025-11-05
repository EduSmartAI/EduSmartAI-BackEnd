using StudentService.Domain.WriteModels;

namespace StudentService.Domain.ReadModels
{
	public sealed class AiEvaluationCollection
	{
		public Guid EvaluationId { get; set; }

		public Guid AttemptId { get; set; }

		public Guid UserId { get; set; }

		public Guid CourseId { get; set; }

		public short Scope { get; set; }

		public Guid? ScopeId { get; set; }

		public Guid? QuizId { get; set; }

		public short? Score100 { get; set; }

		public string Summary { get; set; }

		public string Strengths { get; set; }

		public string Improvements { get; set; }

		public string Actions { get; set; }

		public string SkillGaps { get; set; }

		public string Model { get; set; }

		public string RubricVersion { get; set; }

		public decimal Confidence { get; set; }

		public DateTime CreatedAt { get; set; }

		public short? Score100Raw { get; set; }

		public static AiEvaluationCollection FromWriteModel(AiEvaluation model)
		{
			return new AiEvaluationCollection
			{
				EvaluationId = model.EvaluationId,
				AttemptId = model.AttemptId,
				UserId = model.UserId,
				Scope = model.Scope,
				ScopeId = model.ScopeId,
				QuizId = model.QuizId,
				Score100 = model.Score100,
				Strengths = model.Strengths,
				Improvements = model.Improvements,
				Actions = model.Actions,
				SkillGaps = model.SkillGaps,
				Model = model.Model,
				RubricVersion = model.RubricVersion,
				Confidence = model.Confidence,
				CreatedAt = model.CreatedAt,
				Score100Raw = model.Score100Raw
			};
		}
	}
}
