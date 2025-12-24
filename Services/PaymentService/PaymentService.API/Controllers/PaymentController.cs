using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using PaymentService.Application.Applications.Payments.Commands.PaymentCallback;
using PaymentService.Application.Applications.Payments.Commands.ProcessPayment;
using PaymentService.Application.Applications.Payments.Commands.RePayment;
using PaymentService.Application.Applications.Payments.Queries;
using PaymentService.Application.Applications.Payments.Queries.PaymentHistory;
using Swashbuckle.AspNetCore.Annotations;
using PaymentCallbackCommand = PaymentService.Application.Applications.Payments.Commands.PaymentCallback.PaymentCallbackCommand;
using PaymentCallbackResponse = PaymentService.Application.Applications.Payments.Commands.PaymentCallback.PaymentCallbackResponse;

namespace PaymentService.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PaymentController(ISender sender) : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    // /// <summary>
    // /// Tạo yêu cầu thanh toán cho đơn hàng qua PayOS.
    // /// </summary>
    // /// <param name="request">Request chứa OrderId cần thanh toán</param>
    // /// <returns>Trả về checkout URL, QR code và transaction ID</returns>
    // [HttpPost("[action]")]
    // [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    // [SwaggerOperation(
    //     Summary = "Process payment for an order",
    //     Description = "Create payment request via PayOS gateway. Returns checkout URL and QR code for payment."
    // )]
    // public async Task<ProcessPaymentResponse> InsertPayment([FromBody] ProcessPaymentCommand request)
    // {
    //     return await ApiControllerHelper.HandleRequest<ProcessPaymentCommand, ProcessPaymentResponse, ProcessPaymentDto>(
    //         request,
    //         _logger,
    //         ModelState,
    //         async () => await sender.Send(request),
    //         new ProcessPaymentResponse()
    //     );
    // }

    /// <summary>
    /// Xử lý callback từ PayOS sau khi user thanh toán.
    /// </summary>
    /// <param name="request">Thông tin callback từ PayOS</param>
    /// <returns>Kết quả xử lý thanh toán</returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Handle payment callback from PayOS",
        Description = "Process payment result from PayOS gateway. Update order and payment transaction status based on payment result."
    )]
    public async Task<PaymentCallbackResponse> PaymentCallback([FromBody] PaymentCallbackCommand request)
    {
        return await ApiControllerHelper.HandleRequest<PaymentCallbackCommand, PaymentCallbackResponse, PaymentCallbackDto>(
            request,
            _logger,
            ModelState,
            async () => await sender.Send(request),
            new PaymentCallbackResponse()
        );
    }

    /// <summary>
    /// Get payment Url to retry payment for an order.
    /// </summary>
    /// <param name="orderId">ID của đơn hàng cần lấy lại link thanh toán</param>
    /// <returns>Trả về PaymentUrl và QR code để thanh toán</returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy lại link thanh toán khi người dùng đóng tab hoặc cần thanh toán lại.",
        Description = "Retrieve existing payment URL and QR code when user closed the payment tab or needs to retry payment."
    )]
    public async Task<RePaymentResponse> RePayment([FromQuery] Guid orderId)
    {
        var command = new RePaymentCommand { OrderId = orderId };
        return await ApiControllerHelper.HandleRequest<RePaymentCommand, RePaymentResponse, RePaymentResponseEntity>(
            command,
            _logger,
            ModelState,
            async () => await sender.Send(command),
            new RePaymentResponse()
        );
    }
    
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<PaymentAmountsSelectQueryResponse> SelectAmounts()
    {
        var query = new PaymentAmountsSelectQuery();
        return await ApiControllerHelper.HandleRequest<PaymentAmountsSelectQuery, PaymentAmountsSelectQueryResponse, decimal>(
            query,
            _logger,
            ModelState,
            async () => await sender.Send(query),
            new PaymentAmountsSelectQueryResponse()
        );
    }
    
    /// <summary>
    /// Lấy lịch sử thanh toán của người dùng hiện tại
    /// </summary>
    /// <param name="pageNumber">Số trang (mặc định = 1)</param>
    /// <param name="pageSize">Số bản ghi mỗi trang (mặc định = 10)</param>
    /// <param name="status">Trạng thái thanh toán (1=Pending, 2=Paid, 3=Failed, 4=SystemError)</param>
    /// <param name="fromDate">Ngày bắt đầu lọc</param>
    /// <param name="toDate">Ngày kết thúc lọc</param>
    /// <returns>Danh sách lịch sử thanh toán có phân trang</returns>
    [HttpGet("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy lịch sử thanh toán của người dùng",
        Description = "Trả về danh sách các giao dịch thanh toán của người dùng hiện tại với phân trang và bộ lọc"
    )]
    public async Task<PaymentHistorySelectQueryResponse> SelectPaymentHistory(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] short? status = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        var query = new PaymentHistorySelectQuery
        {
            PageNumber = pageNumber,
            PageSize = pageSize,
            Status = status,
            FromDate = fromDate,
            ToDate = toDate
        };
        return await ApiControllerHelper.HandleRequest<PaymentHistorySelectQuery, PaymentHistorySelectQueryResponse, PaymentHistoryResponseData>(
            query,
            _logger,
            ModelState,
            async () => await sender.Send(query),
            new PaymentHistorySelectQueryResponse()
        );
    }
}

