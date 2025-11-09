using Course.Application.DTOs.CoursesDTO.WishlistDTO;

namespace Course.Application.Wishlists.Commands.AddToWishlist
{
	public record AddToWishlistCommand(Guid CourseId) : ICommand<AddToWishlistResponse>;

	public sealed record AddToWishlistResponse : AbstractApiResponse<WishlistItemDto>
	{
		public override WishlistItemDto Response { get; set; } = default!;
	}
}
