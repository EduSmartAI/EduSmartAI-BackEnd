using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using PaymentService.Application.Applications.Carts.Commands.AddToCart;
using PaymentService.Application.Applications.Carts.Commands.RemoveCart;
using PaymentService.Application.Applications.Carts.Queries.CheckCourseInMyCart;
using PaymentService.Application.Applications.Carts.Queries.GetMyCart;
using PaymentService.Application.DTOs.Carts;
using Swashbuckle.AspNetCore.Annotations;

namespace PaymentService.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class CartController(ISender sender) : ControllerBase
{
	private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Lấy giỏ hàng của current user.
	/// </summary>
	[HttpGet]
	[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
	[SwaggerOperation(
		Summary = "Get current user's cart",
		Description = "Return active cart of the authenticated user"
	)]
	public async Task<GetMyCartResponse> GetMyCart()
	{
		var request = new GetMyCartQuery();

		return await ApiControllerHelper.HandleRequest<GetMyCartQuery, GetMyCartResponse, CartDto>(
			request,
			_logger,
			ModelState,
			async () => await sender.Send(request),
			new GetMyCartResponse()
		);
	}

	/// <summary>
	/// Thêm một khóa học vào giỏ hàng.
	/// </summary>
	[HttpPost("items")]
	[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
	[SwaggerOperation(
		Summary = "Add course to cart",
		Description = "Add a course into current user's cart"
	)]
	public async Task<AddToCartResponse> AddToCart([FromBody] AddToCartRequest requestBody)
	{
		var request = new AddToCartCommand(requestBody.CourseId);

		return await ApiControllerHelper.HandleRequest<AddToCartCommand, AddToCartResponse, string>(
			request,
			_logger,
			ModelState,
			async () => await sender.Send(request),
			new AddToCartResponse()
		);
	}
	
	/// <summary>
	/// Xóa item khỏi giỏ.
	/// </summary>
	[HttpDelete("items/{cartItemId:guid}")]
	[Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
	[SwaggerOperation(
		Summary = "Remove cart item",
		Description = "Logical delete a cart item from current user's cart"
	)]
	public async Task<RemoveCartItemResponse> RemoveCartItem([FromRoute] Guid cartItemId)
	{
		var request = new RemoveCartItemCommand(cartItemId);

		return await ApiControllerHelper.HandleRequest<RemoveCartItemCommand, RemoveCartItemResponse, string>(
			request,
			_logger,
			ModelState,
			async () => await sender.Send(request),
			new RemoveCartItemResponse()
		);
	}

	[HttpGet("items/check")]
	public async Task<CheckCourseInCartResponse> CheckCourseInMyCart([FromQuery] Guid courseId)
	{
		var request = new CheckCourseInMyCartQuery(courseId);

		return await ApiControllerHelper
			.HandleRequest<CheckCourseInMyCartQuery, CheckCourseInCartResponse, CheckCourseInCartDto>(
				request,
				_logger,
				ModelState,
				async () => await sender.Send(request),
				new CheckCourseInCartResponse()
			);
	}
}

/// <summary>
/// Request body để thêm khóa học vào cart.
/// </summary>
public class AddToCartRequest
{
	public Guid CourseId { get; set; }
}

/// <summary>
/// Request body để update cart item.
/// </summary>
public class UpdateCartItemRequest
{
	public bool? IsSelected { get; set; }
}
