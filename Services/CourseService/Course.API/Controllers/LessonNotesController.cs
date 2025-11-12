using BaseService.Application.Common;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.LessonNotes.Commands.CreateNote;
using Course.Application.LessonNotes.Commands.DeleteNote;
using Course.Application.LessonNotes.Commands.UpdateNote;
using Course.Application.LessonNotes.Queries.GetLessonNotes;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class LessonNotesController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public Task<CreateNoteResponse> Create(Guid lessonId, [FromBody] CreateNoteDto body)
		=> ApiControllerHelper.HandleRequest<CreateNoteCommand, CreateNoteResponse, bool>(
			new(lessonId, body.TimeSeconds, body.Content), _logger, ModelState, () => sender.Send(new CreateNoteCommand(lessonId, body.TimeSeconds, body.Content)), new());

		[HttpGet]
		public Task<GetLessonNotesResponse> Get(Guid lessonId, [FromQuery] int? page, [FromQuery] int? size)
			=> ApiControllerHelper.HandleRequest<GetLessonNotesQuery, GetLessonNotesResponse, PagedResult<LessonNoteDto>>(
				new(lessonId, page, size), _logger, ModelState, () => sender.Send(new GetLessonNotesQuery(lessonId, page, size)), new());

		[HttpPut("{noteId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public Task<UpdateNoteResponse> Update(Guid lessonId, Guid noteId, [FromBody] UpdateNoteDto body)
			=> ApiControllerHelper.HandleRequest<UpdateNoteCommand, UpdateNoteResponse, bool>(
				new(noteId, body.Content), _logger, ModelState, () => sender.Send(new UpdateNoteCommand(noteId, body.Content)), new());

		[HttpDelete("{noteId:guid}")]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		public Task<DeleteNoteResponse> Delete(Guid lessonId, Guid noteId)
			=> ApiControllerHelper.HandleRequest<DeleteNoteCommand, DeleteNoteResponse, bool>(
				new(noteId), _logger, ModelState, () => sender.Send(new DeleteNoteCommand(noteId)), new());
	}

	public sealed record CreateNoteDto(int TimeSeconds, string Content);
	public sealed record UpdateNoteDto(string Content);
}
