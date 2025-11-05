using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService.Dashboards.ModuleDashboard;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public sealed record GetLatestLessonAiEvaluationsQuery(Guid StudentId, Guid CourseId, IReadOnlyList<Guid> LessonIds) : IQuery<GetLatestLessonAiEvaluationsResponse>;
	public sealed record GetLatestLessonAiEvaluationsResponse : AbstractApiResponse<GetLatestLessonAiEvaluationsPayload>
	{
		public override GetLatestLessonAiEvaluationsPayload Response { get; set; } = default!;
	}
	public sealed class GetLatestLessonAiEvaluationsPayload
	{
		public IReadOnlyList<LessonAiEvaluationDto> Lessons { get; init; } = Array.Empty<LessonAiEvaluationDto>();
	}
	public sealed class LessonAiEvaluationDto
	{
		public Guid? LessonId { get; init; }
		public Guid? QuizId { get; init; }
		public int? Score100Raw { get; init; }
		public int? Score100 { get; init; }
		public string? Summary { get; init; }
		public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
		public IReadOnlyList<AiImprovementDto> ImprovementResources { get; init; } = Array.Empty<AiImprovementDto>();
		public DateTime CreatedAt { get; init; }
	}

}
