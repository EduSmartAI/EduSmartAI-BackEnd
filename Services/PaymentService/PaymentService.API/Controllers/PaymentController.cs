using BaseService.API.BaseControllers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using PaymentService.Application.Applications.Payments.Commands.PaymentCallback;
using PaymentService.Application.Applications.Payments.Commands.ProcessPayment;
using Swashbuckle.AspNetCore.Annotations;
using PaymentCallbackCommand = PaymentService.Application.Applications.Payments.Commands.PaymentCallback.PaymentCallbackCommand;
using PaymentCallbackResponse = PaymentService.Application.Applications.Payments.Commands.PaymentCallback.PaymentCallbackResponse;

namespace PaymentService.API.Controllers;

[Route("api/v1/[controller]")]
[ApiController]
public class PaymentController(ISender sender) : ControllerBase
{
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Tạo yêu cầu thanh toán cho đơn hàng qua PayOS.
    /// </summary>
    /// <param name="request">Request chứa OrderId cần thanh toán</param>
    /// <returns>Trả về checkout URL, QR code và transaction ID</returns>
    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Process payment for an order",
        Description = "Create payment request via PayOS gateway. Returns checkout URL and QR code for payment."
    )]
    public async Task<ProcessPaymentResponse> InsertPayment([FromBody] ProcessPaymentCommand request)
    {
        return await ApiControllerHelper.HandleRequest<ProcessPaymentCommand, ProcessPaymentResponse, ProcessPaymentDto>(
            request,
            _logger,
            ModelState,
            async () => await sender.Send(request),
            new ProcessPaymentResponse()
        );
    }

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
}

