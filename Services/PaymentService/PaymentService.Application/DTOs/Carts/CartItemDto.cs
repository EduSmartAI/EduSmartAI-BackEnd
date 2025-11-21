namespace PaymentService.Application.DTOs.Carts
{
	public class CartItemDto
	{
		public Guid CartItemId { get; set; }
		public Guid CourseId { get; set; }

		public string CourseTitle { get; set; } = default!;
		public string? CourseImageUrl { get; set; }

		public decimal Price { get; set; }
		public decimal? DealPrice { get; set; }

		public bool IsSelected { get; set; }
	}
}
