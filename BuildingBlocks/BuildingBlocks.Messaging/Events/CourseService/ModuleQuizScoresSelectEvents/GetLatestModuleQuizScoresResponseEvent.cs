using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.CourseService.ModuleQuizScoresSelectEvents
{
	public sealed record GetLatestModuleQuizScoresResponseEvent : AbstractApiResponse<GetLatestModuleQuizScoresPayload>
	{
		public override GetLatestModuleQuizScoresPayload Response { get; set; } = default!;
	}

	public sealed class GetLatestModuleQuizScoresPayload
	{
		public IReadOnlyList<ModuleLatestQuizScore> Modules { get; init; } = Array.Empty<ModuleLatestQuizScore>();
	}

	public sealed class ModuleLatestQuizScore
	{
		public Guid ModuleId { get; init; }
		public int? LatestScore100 { get; init; } // null nếu chưa có attempt
		public int AttemptCount { get; init; }    // số attempt hiện có (để debug/giám sát)
	}
}
