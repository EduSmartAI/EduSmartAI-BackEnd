using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.TeacherService.GetTeacherInformation
{
	public record GetTeacherDetailsEvent(Guid TeacherId);

	public record GetTeacherDetailsEventResponse : AbstractApiResponse<TeacherDetailExternalServiceDto>
	{
		public override TeacherDetailExternalServiceDto Response { get; set; }
	}

	public class TeacherDetailExternalServiceDto
	{
		public Guid TeacherId { get; set; }
		public string? DisplayName { get; set; }
		public string? FirstName { get; set; }
		public string? LastName { get; set; }
		public string? Bio { get; set; }
		public string? ProfilePictureUrl { get; set; }
	}
}
