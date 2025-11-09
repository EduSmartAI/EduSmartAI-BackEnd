using Course.Application.DTOs.CoursesDTO.WishlistDTO;
using Course.Application.Wishlists.Commands.AddToWishlist;
using Course.Application.Wishlists.Commands.RemoveFromWishlist;

namespace Course.API.Controllers
{
	[Route("api/[controller]")]
	[ApiController]
	public class CourseWishlistController(ISender sender) : ControllerBase
	{
		private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

		[HttpPost]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Add course to my wishlist")]
		public async Task<AddToWishlistResponse> Add(Guid courseId)
		{
			var command = new AddToWishlistCommand(courseId);
			return await ApiControllerHelper.HandleRequest<AddToWishlistCommand, AddToWishlistResponse, WishlistItemDto>(
				command,
				_logger,
				ModelState,
				async () => await sender.Send(command),
				new());
		}

		[HttpDelete]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Remove course from my wishlist")]
		public async Task<RemoveFromWishlistResponse> Remove(Guid courseId)
		{
			var command = new RemoveFromWishlistCommand(courseId);
			return await ApiControllerHelper.HandleRequest<RemoveFromWishlistCommand, RemoveFromWishlistResponse, string>(
				command,
				_logger,
				ModelState,
				async () => await sender.Send(command),
				new());
		}
	}
}
