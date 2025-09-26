using BaseService.Common.ApiEntities;
using BuildingBlocks.CQRS;
using Course.Application.DTOs.UserLessonProgressDTO;

namespace Course.Application.UserLessonProgresses.Commands.CreateUserLessonProgress
{
	public record CreateUserLessonProgressCommand(CreateUserLessonProgressDto UserLessonProgress) : ICommand<CreateUserLessonProgressResponse>;

	public record CreateUserLessonProgressResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = default!;
	}
}
