using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using PaymentService.Application.Applications.Orders.Commands.InsertOrder;
using PaymentService.Application.Applications.Orders.Queries.SelectOrder;
using PaymentService.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace PaymentService.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class OrderController(ISender sender) : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Create new order and initiate payment
    /// </summary>
    /// <param name="request">Danh sách CourseIds cần mua</param>
    /// <returns>Trả về thông tin đơn hàng và link thanh toán</returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Tạo đơn hàng mới và khởi tạo thanh toán qua PayOS.",
        Description = "PaymentMethod: 1. Momo, 2. PayOS"
    )]
    public async Task<InsertOrderResponse> InsertOrder([FromBody] InsertOrderCommand request)
    {
        return await ApiControllerHelper.HandleRequest<InsertOrderCommand, InsertOrderResponse, PaymentResultEntity>(
            request,
            _logger,
            ModelState,
            async () => await sender.Send(request),
            new InsertOrderResponse()
        );
    }

    /// <summary>
    /// Get orders list or order detail
    /// </summary>
    /// <param name="orderId">ID đơn hàng (optional - nếu không có sẽ lấy danh sách)</param>
    /// <param name="pageIndex">Trang hiện tại (default: 0)</param>
    /// <param name="pageSize">Số lượng records mỗi trang (default: 10)</param>
    /// <returns>Danh sách đơn hàng hoặc chi tiết đơn hàng</returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách đơn hàng hoặc chi tiết một đơn hàng.",
        Description = "Get paginated list of user's orders or single order detail by OrderId."
    )]
    public async Task<SelectOrderResponse> SelectOrder(
        [FromQuery] Guid? orderId = null,
        [FromQuery] int pageIndex = 0,
        [FromQuery] int pageSize = 10)
    {
        var query = new SelectOrderQuery
        {
            OrderId = orderId,
            PageIndex = pageIndex,
            PageSize = pageSize
        };

        return await ApiControllerHelper.HandleRequest<SelectOrderQuery, SelectOrderResponse, List<SelectOrderResponseEntity>>(
            query,
            _logger,
            ModelState,
            async () => await sender.Send(query),
            new SelectOrderResponse()
        );
    }
}

