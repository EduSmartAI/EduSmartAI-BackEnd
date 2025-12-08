using BaseService.Common.ApiEntities;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace BuildingBlocks.Messaging.Events.StudentService
{
	public class GetSuggestedCoursesEvent
	{
		public Guid UserId { get; set; }
		public string SubjectCode { get; set; } = string.Empty;

		/// <summary>
		/// Level hiện tại từ bảng learning_paths.level
		/// </summary>
		public int CurrentLevel { get; set; }

		/// <summary>
		/// 1 = Easier, 2 = Harder
		/// </summary>
		public SuggestedCourseType Type { get; set; }

		/// <summary>
		/// Danh sách course đã tồn tại trong learning path (để loại bỏ)
		/// </summary>
		public List<Guid> ExistingCourseIds { get; set; } = new();
	}

	public record GetSuggestedCoursesEventResponse : AbstractApiResponse<List<CourseBasicInfoDto>>
	{
		public override List<CourseBasicInfoDto> Response { get; set; } = new();
	}

}
