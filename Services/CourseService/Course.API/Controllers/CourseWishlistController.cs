using BaseService.Application.Common;
using Course.Application.DTOs.CoursesDTO.WishlistDTO;
using Course.Application.Wishlists.Commands.AddToWishlist;
using Course.Application.Wishlists.Commands.RemoveFromWishlist;
using Course.Application.Wishlists.Queries.GetWishlistByUserId;

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
			return await ApiControllerHelper.HandleRequest<AddToWishlistCommand, AddToWishlistResponse, bool>(
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

		[HttpGet]
		[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
		[SwaggerOperation(Summary = "Get my wishlist")]
		public async Task<GetMyWishlistResponse> Get(int? Page = 1, int? Size = 10, string? Search = null)
		{
			var query = new GetMyWishlistQuery(Page, Size, Search);
			return await ApiControllerHelper.HandleRequest<GetMyWishlistQuery, GetMyWishlistResponse, PagedResult<WishlistItemDto>>(
				query,
				_logger,
				ModelState,
				async () => await sender.Send(query),
				new());
		}
	}
}
