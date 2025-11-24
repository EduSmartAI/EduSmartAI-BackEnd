using PaymentService.Application.DTOs.Carts;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Infrastructure.Common.Helpers
{
	public static class CartHelper
	{
		public static CartItemDto MapToDto(CartItem item)
		=> new()
		{
			CartItemId = item.CartItemId,
			CourseId = item.CourseId,
			CourseTitle = item.CourseTitleSnapshot,
			CourseImageUrl = item.CourseImageUrlSnapshot,
			Price = item.PriceSnapshot,
			DealPrice = item.DealPriceSnapshot,
			IsSelected = item.IsSelected
		};
	}
}
