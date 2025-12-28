namespace Course.Application.Wishlists.Commands.AddToWishlist
{
	public record AddToWishlistCommand(Guid CourseId) : ICommand<AddToWishlistResponse>;

	public sealed record AddToWishlistResponse : AbstractApiResponse<bool>
	{
		public override bool Response { get; set; } = default!;
	}
}
