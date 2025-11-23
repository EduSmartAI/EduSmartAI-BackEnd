using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using Microsoft.EntityFrameworkCore;
using PaymentService.Application.Applications.Payments;
using PaymentService.Application.Interfaces;
using PaymentService.Domain.Models;
using SystemConfig = PaymentService.Domain.Models.SystemConfig;
using ConstSystemConfig = BaseService.Common.Utils.Const.SystemConfig;
namespace PaymentService.Infrastructure.Implements;

public class PaymentServiceClient(
    ICommandRepository<SystemConfig> systemConfigRepository, 
    ICommandRepository<PaymentTransaction> paymentTransactionRepository,
    ICommandRepository<Order> orderRepository,
    IUnitOfWork unitOfWork,
    IIdentityService identityService) : IPaymentServiceClient
{
    /// <summary>
    /// Process payment via PayOS
    /// </summary>
    /// <param name="orderId">Order ID to process payment</param>
    /// <param name="amount">Amount to pay</param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<PaymentResponse> ProcessPaymentAsync(Guid orderId, decimal amount, CancellationToken ct = default)
    {
        var response = new PaymentResponse {Success = false};
        
        // Get Order
        var order = await orderRepository
            .Find(x => x.OrderId == orderId, isTracking: true)
            .FirstOrDefaultAsync(ct);
            
        if (order == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đơn hàng");
            return response;
        }
        
        // Check if order is already paid or processing
        if (order.Status != (short) ConstantEnum.OrderStatus.Pending)
        {
            response.SetMessage(MessageId.E00000, "ĐƠn hàng không ở trạng thái chờ thanh toán");
            return response;
        }
        
        // Get PayOS config
        var payOsCheckSumKey = systemConfigRepository.Find(x => x.Id == ConstSystemConfig.PayOsCheckSumKey).FirstOrDefault()!.Value;
        var payOsApiKey = systemConfigRepository.Find(x => x.Id == ConstSystemConfig.PayOsApiKey).FirstOrDefault()!.Value;
        var payOsClientId = systemConfigRepository.Find(x => x.Id == ConstSystemConfig.PayOsClientId).FirstOrDefault()!.Value;
        var returnUrl = systemConfigRepository.Find(x => x.Id == ConstSystemConfig.PaymentReturnUrl).FirstOrDefault()!.Value;
        var cancelUrl = systemConfigRepository.Find(x => x.Id == ConstSystemConfig.PaymentCancelUrl).FirstOrDefault()!.Value;

        // Create order code
        var orderCode = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); 
        
        // Limit description to 25 characters
        var description = $"Thanh toan khoa hoc";
        if (description.Length > 25) description = description.Substring(0, 25);

        returnUrl = $"{returnUrl}?orderId={orderId}";
        cancelUrl = $"{cancelUrl}?orderId={orderId}";
        
        // Create signature for PayOS
        var data = $"amount={amount}&cancelUrl={cancelUrl}" +
                   $"&description={description}" +
                   $"&orderCode={orderCode}" +
                   $"&returnUrl={returnUrl}";
        string signature = ComputeHmacSha256(data, payOsCheckSumKey);

        var payRequest = new
        {
            orderCode = orderCode,
            amount = amount,
            description = description,
            returnUrl = returnUrl,
            cancelUrl = cancelUrl,
            signature = signature,
        };
        
        // Call PayOS API
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("x-client-id", payOsClientId);
        client.DefaultRequestHeaders.Add("x-api-key", payOsApiKey);

        var jsonContent = new StringContent(JsonSerializer.Serialize(payRequest), Encoding.UTF8, "application/json");

        var payosResponse = await client.PostAsync("https://api-merchant.payos.vn/v2/payment-requests", jsonContent, ct);

        if (!payosResponse.IsSuccessStatusCode)
        {
            response.SetMessage(MessageId.E00000, "Failed to create payment request");
            return response;
        }

        var responseContent = await payosResponse.Content.ReadAsStringAsync(ct);
        var jsonDoc = JsonDocument.Parse(responseContent);
        var root = jsonDoc.RootElement;

        var dataElement = root.GetProperty("data");

        var checkoutUrl = dataElement.GetProperty("checkoutUrl").GetString();
        var qrCode = dataElement.GetProperty("qrCode").GetString();
        
        if (string.IsNullOrEmpty(checkoutUrl) || string.IsNullOrEmpty(qrCode))
        {
            response.SetMessage(MessageId.E00000, "Failed to retrieve payment data");
            return response;
        }

        // Create PaymentTransaction
        var paymentTransaction = new PaymentTransaction
        {
            PaymentId = Guid.NewGuid(),
            OrderId = orderId,
            Gateway = nameof(ConstantEnum.PaymentGateway.PayOs),
            GatewayTransactionId = orderCode.ToString(),
            Amount = amount,
            Currency = order.Currency,
            Status = (short) ConstantEnum.PaymentStatus.Pending,
            RawResponse = responseContent,
            PaymentUrl = checkoutUrl
        };

        // Update Order status to WaitingForPayment
        order.Status = (short) ConstantEnum.OrderStatus.WaitingForPayment;

        // Save to database
        await paymentTransactionRepository.AddAsync(paymentTransaction);
        orderRepository.Update(order);
        await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, ct);

        var entityResponse = new PaymentResultEntity
        {
            CheckoutUrl = checkoutUrl,
            QrCode = qrCode,
            TransactionId = orderCode.ToString()
        };
        
        // True
        response.Success = true;
        response.Response = entityResponse;
        response.SetMessage(MessageId.I00001);
        return response;
    }

    public Task<bool> RefundPaymentAsync(string transactionId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }
    
    public async Task<PaymentCallbackResponse> PaymentCallbackAsync(PaymentCallBackRequest request, IdentityEntity identityEntity)
    {
        var response = new PaymentCallbackResponse { Success = false };
        
        // Get Order with PaymentTransactions
        var order = await orderRepository
            .Find(predicate:x => x.OrderId == request.OrderId, 
                isTracking: true, 
                includes: o => o.PaymentTransactions)
            .FirstOrDefaultAsync();
        if (order == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy đơn hàng");
            return response;
        }

        // Verify user authorization
        if (order.UserId != identityEntity.UserId)
        {
            response.SetMessage(MessageId.E00000, "Bạn không có quyền thực hiện hành động này");
            return response;
        }
        
        // Get the payment transaction
        var paymentTransaction = order.PaymentTransactions
            .FirstOrDefault(pt => pt.GatewayTransactionId == request.OrderCode.ToString() 
                               && pt.Status == (short) ConstantEnum.PaymentStatus.Pending);
                               
        if (paymentTransaction == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy giao dịch thanh toán phù hợp");
            return response;
        }
        
        // Begin transaction
        await unitOfWork.BeginTransactionAsync(async () =>
        {
            // User cancelled payment
            if (request.Cancel)
            {
                order.Status = (short) ConstantEnum.OrderStatus.Cancelled;
                paymentTransaction.Status = (short) ConstantEnum.PaymentStatus.Failed;
                paymentTransaction.ReturnCode = nameof(ConstantEnum.PaymentReturnCode.CANCELLED);
                paymentTransaction.ReturnMessage = "User cancelled payment";
                paymentTransaction.PaymentUrl = null;
                
                orderRepository.Update(order);
                paymentTransactionRepository.Update(paymentTransaction);
                await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken: CancellationToken.None);
                
                response.SetMessage(MessageId.I00001, "Thanh toán đã bị hủy bởi người dùng");
                return true;
            }
            
            // Payment failed
            if (request.Code != "00")
            {
                order.Status = (short) ConstantEnum.OrderStatus.Failed;
                paymentTransaction.Status = (short) ConstantEnum.PaymentStatus.Failed;
                paymentTransaction.ReturnCode = request.Code;
                paymentTransaction.ReturnMessage = string.IsNullOrEmpty(request.Status) ? "Payment failed" : request.Status;
                
                orderRepository.Update(order);
                
                // Create new payment link for retry
                var retryPaymentResponse = await ProcessPaymentAsync(order.OrderId, order.FinalAmount);
                if (retryPaymentResponse.Success)
                {
                    response.Response = new PaymentCallbackEntity
                    {
                        CheckoutUrl = retryPaymentResponse.Response.CheckoutUrl,
                        QrCode = retryPaymentResponse.Response.QrCode,
                    };
                    response.SetMessage(MessageId.I00000, "Thanh toán thất bại, vui lòng thử lại với liên kết thanh toán mới");
                    
                    paymentTransaction.PaymentUrl = retryPaymentResponse.Response.CheckoutUrl;
                }
                else
                {
                    response.SetMessage(MessageId.E00000, "Thanh toán thất bại và không thể tạo liên kết thanh toán mới");
                }
                
                // Update payment transaction
                paymentTransactionRepository.Update(paymentTransaction);
                await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken: CancellationToken.None);
                return false;
            }
            
            // Payment success
            order.Status = (short) ConstantEnum.OrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            
            paymentTransaction.Status = (short)ConstantEnum.PaymentStatus.Paid;
            paymentTransaction.ReturnCode = request.Code;
            paymentTransaction.ReturnMessage = "Payment successful";
            
            if (!string.IsNullOrEmpty(request.Id))
            {
                paymentTransaction.GatewayTransactionId = request.Id;
            }
            
            orderRepository.Update(order);
            paymentTransactionRepository.Update(paymentTransaction);
            await unitOfWork.SaveChangesAsync(identityService.GetCurrentUser()!.Email, cancellationToken: CancellationToken.None);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thanh toán");
            return true;
        });
        
        return response;
    }

    
    private static string ComputeHmacSha256(string message, string secretKey)
    {
        if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));
        if (string.IsNullOrWhiteSpace(secretKey)) throw new ArgumentNullException(nameof(secretKey));

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
        byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));

        // Convert to lowercase hex string without hyphens
        var hashString = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
        return hashString;
    }
}