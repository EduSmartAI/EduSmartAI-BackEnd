using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.UserLessonProgressDTO;

namespace Course.Application.UserLessonProgresses.Commands.UpdateUserLessonProgress
{
	public record UpdateUserLessonProgressCommand(UpdateUserLessonProgressDto UpdateUserLessonProgress) : ICommand<UpdateUserLessonProgressResponse>;

	public record UpdateUserLessonProgressResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = default!;
	}
}
