using System.Text.Json.Serialization;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Application.DTOs.CoursesDTO
{
	public sealed class MyLearningCourseItemDto
	{
		public Guid CourseId { get; set; }
		public string Title { get; set; } = default!;
		public string? Slug { get; set; }
		public string? ImageUrl { get; set; }
		public decimal PercentCompleted { get; set; }
		public int LessonsTotal { get; set; }
		public int LessonsCompleted { get; set; }
		public DateTime? StartedAt { get; set; }
		public DateTime? CompletedAt { get; set; }
		public DateTime LastUpdatedAt { get; set; }
		[JsonConverter(typeof(JsonStringEnumConverter))]
		public CourseProgressStatus Status { get; set; }
	}

}
