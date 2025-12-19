namespace Course.Application.Wishlists.Commands.AddToWishlist
{
	public sealed class AddToWishlistHandler(IWishlistService _wishlistService) : ICommandHandler<AddToWishlistCommand, AddToWishlistResponse>
	{
		public async Task<AddToWishlistResponse> Handle(AddToWishlistCommand request, CancellationToken cancellationToken)
			=> await _wishlistService.AddAsync(request.CourseId, cancellationToken);
	}
}
