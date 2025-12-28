using BaseService.Application.Common;
using Course.Application.Comments.ModuleDiscussionComment.Commands.PostModuleDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Commands.ReplyDiscussionComments;
using Course.Application.Comments.ModuleDiscussionComment.Queries.GetDiscussionThread;
using Course.Application.DTOs.CommentsDTO;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class ModuleDiscussionCommentsController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost("{moduleId}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public Task<PostDiscussionCommentResponse> Post([FromRoute] Guid moduleId, [FromBody] PostCommentRequest request)
			=> ApiControllerHelper.HandleRequest<PostDiscussionCommentCommand, PostDiscussionCommentResponse, bool>(
				new(moduleId, request.Content),
				_logger, ModelState,
				() => sender.Send(new PostDiscussionCommentCommand(moduleId, request.Content)),
				new());

		[HttpPost("{moduleId:guid}/{parentId:guid}/reply")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public Task<ReplyDiscussionCommentResponse> Reply([FromRoute] Guid moduleId, [FromRoute] Guid parentId, [FromBody] ReplyCommentRequest request)
			=> ApiControllerHelper.HandleRequest<ReplyDiscussionCommentCommand, ReplyDiscussionCommentResponse, bool>(
				new(moduleId, parentId, request.Content),
				_logger, ModelState,
				() => sender.Send(new ReplyDiscussionCommentCommand(moduleId, parentId, request.Content)),
				new());

		[HttpGet("~/api/modules/{moduleId:guid}/discussion/thread")]
		[AllowAnonymous]
		[SwaggerOperation(Summary = "Not implemented yet")]
		public Task<GetDiscussionThreadResponse> GetThread([FromRoute] Guid moduleId, [FromQuery] int? page, [FromQuery] int? size)
			=> ApiControllerHelper.HandleRequest<GetDiscussionThreadQuery, GetDiscussionThreadResponse, PagedResult<DiscussionCommentDto>>(
				new(moduleId, page, size), 
				_logger, ModelState, 
				() => sender.Send(new GetDiscussionThreadQuery(moduleId, page, size)),
				new());

		public record PostCommentRequest(string Content);

		public record ReplyCommentRequest(string Content);
	}
}
