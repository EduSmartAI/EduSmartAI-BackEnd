namespace PaymentService.Application.DTOs.Carts
{
	public class CartDto
	{
		public Guid CartId { get; set; }
		public Guid UserId { get; set; }

		public List<CartItemDto> Items { get; set; } = new();
	}
}
