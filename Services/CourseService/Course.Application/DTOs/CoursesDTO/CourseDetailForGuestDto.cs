using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.ModulesDTO;

namespace Course.Application.DTOs.CoursesDTO
{
	public record CourseDetailForGuestDto
	{
		public Guid CourseId { get; set; }
		public Guid TeacherId { get; set; }
		public Guid SubjectId { get; set; }
		public string SubjectCode { get; set; }
		public string Title { get; set; }
		public string? ShortDescription { get; set; }
		public string? Description { get; set; }
		public string? Slug { get; set; }
		public string? CourseImageUrl { get; set; }
		public int LearnerCount { get; set; }
		public int? DurationMinutes { get; set; }
		public decimal? DurationHours { get; set; }
		public short? Level { get; set; }
		public decimal Price { get; set; }
		public decimal? DealPrice { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public List<CourseObjectiveDto> Objectives { get; set; } = new();
		public List<CourseRequirementDto> Requirements { get; set; } = new();
		public List<ModuleDetailDto<GuestLessonDetailDto>> Modules { get; set; } = new();
		public List<CourseCommentDto> Comments { get; set; } = new();
		public List<CourseTagDto> Tags { get; set; } = new();
		public List<CourseRatingDto> Ratings { get; set; } = new();
		public int RatingsCount { get; set; }
		public double RatingsAverage { get; set; }
		public bool IsWishlist { get; set; }
		public bool IsEnrolled { get; set; }
	}

}
