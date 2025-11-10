using BaseService.Application.Common;
using Course.Application.Comments.Commands.CreateComment;
using Course.Application.Comments.Commands.ReplyToComment;
using Course.Application.Comments.Queries.GetCourseComments;
using Course.Application.DTOs.CommentsDTO;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CourseCommentsController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpGet]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "List comments")]
		public async Task<GetCourseCommentsResponse> Get(Guid courseId, [FromQuery] int? page, [FromQuery] int? size)
		{
			var q = new GetCourseCommentsQuery(courseId, page, size);
			return await ApiControllerHelper.HandleRequest<GetCourseCommentsQuery, GetCourseCommentsResponse, PagedResult<CommentDto>>(
				q, _logger, ModelState, async () => await sender.Send(q), new());
		}

		[HttpPost]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Create a comment (root)")]
		public async Task<CreateCommentResponse> Create(Guid courseId, [FromBody] CreateCommentBody body)
		{
			var cmd = new CreateCommentCommand(courseId, body.Content);
			return await ApiControllerHelper.HandleRequest<CreateCommentCommand, CreateCommentResponse, CommentDto>(
				cmd, _logger, ModelState, async () => await sender.Send(cmd), new());
		}

		[HttpPost("{parentCommentId:guid}/replies")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Reply to a comment")]
		public async Task<ReplyToCommentResponse> Reply(Guid courseId, Guid parentCommentId, [FromBody] CreateCommentBody body)
		{
			var cmd = new ReplyToCommentCommand(courseId, parentCommentId, body.Content);
			return await ApiControllerHelper.HandleRequest<ReplyToCommentCommand, ReplyToCommentResponse, CommentDto>(
				cmd, _logger, ModelState, async () => await sender.Send(cmd), new());
		}

		public sealed record CreateCommentBody(string Content);
	}
}
