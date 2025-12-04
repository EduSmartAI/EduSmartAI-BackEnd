using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService
{
	public record GetCourseBasicInfoEvent(List<Guid> CourseIds);
	public record GetCourseBasicInfoResponse : AbstractApiResponse<List<CourseBasicInfoDto>>
	{
		public override List<CourseBasicInfoDto> Response { get; set; } = new();
	};

	public sealed class CourseBasicInfoDto
	{
		public Guid CourseId { get; set; }
		public string Title { get; set; } = string.Empty;
		public string ShortDescription { get; set; } = string.Empty;
		public string CourseImageUrl { get; set; } = string.Empty;
		public int Level { get; set; }
		public decimal Price { get; set; }
		public decimal? DealPrice { get; set; }
		public Guid TeacherId { get; set; }
		public string? TeacherName { get; set; }
		public string SubjectCode { get; set; } = string.Empty;
	}

}
