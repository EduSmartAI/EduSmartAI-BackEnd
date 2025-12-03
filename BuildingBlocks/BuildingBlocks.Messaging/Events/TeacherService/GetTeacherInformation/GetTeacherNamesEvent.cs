using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation
{
	public record GetTeacherNamesEvent(List<Guid> TeacherIds);

	public record GetTeacherNamesEventResponse : AbstractApiResponse<List<TeacherNameExternalServiceDto>>
	{
		public override List<TeacherNameExternalServiceDto> Response { get; set; } = new();
	}

	public class TeacherNameExternalServiceDto
	{
		public Guid TeacherId { get; set; }
		public string? DisplayName { get; set; }
	}
}
