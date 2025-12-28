using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService.GetStudentInformation
{
	public record GetStudentNamesEvent(List<Guid> StudentIds);

	public record GetStudentNamesEventResponse : AbstractApiResponse<List<StudentNameExternalServiceDto>>
	{
		public override List<StudentNameExternalServiceDto> Response { get; set; } = new();
	}

	public class StudentNameExternalServiceDto
	{
		public Guid StudentId { get; set; }
		public string? DisplayName { get; set; }
		public string? AvatarUrl { get; set; }
	}
}
