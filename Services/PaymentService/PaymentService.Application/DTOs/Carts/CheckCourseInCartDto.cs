namespace PaymentService.Application.DTOs.Carts
{
	public class CheckCourseInCartDto
	{
		public bool IsInCart { get; set; }
		public Guid? CartId { get; set; }
		public Guid? CartItemId { get; set; }
	}
}
