using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Dashboards.Queries
{
	public record GetLatestModuleAiEvaluationsQuery(Guid StudentId, Guid CourseId, IReadOnlyList<Guid> ModuleIds) : IQuery<GetLatestModuleAiEvaluationsResponse>;

	public sealed record GetLatestModuleAiEvaluationsResponse : AbstractApiResponse<GetLatestModuleAiEvaluationsPayload>
	{
		public override GetLatestModuleAiEvaluationsPayload Response { get; set; } = default!;
	}

	public sealed class GetLatestModuleAiEvaluationsPayload
	{
		public IReadOnlyList<ModuleAiEvaluationDto> Modules { get; init; } = Array.Empty<ModuleAiEvaluationDto>();
	}

	public sealed class ModuleAiEvaluationDto
	{
		public Guid ModuleId { get; init; }          // scope_id (Module)
		public Guid QuizId { get; init; }
		public int? Score100Raw { get; init; }       // score_100_raw (nullable)
		public int Score100 { get; init; }           // điểm do AI (đã chỉnh 70/30)
		public string? Summary { get; init; }
		public IReadOnlyList<string> Strengths { get; init; } = Array.Empty<string>();
		public IReadOnlyList<string> Improvements { get; init; } = Array.Empty<string>();
		public DateTime CreatedAt { get; init; }     // latest theo module
	}
}
