using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.WriteModels;

namespace PaymentService.Application.Applications.Payments.Queries;

public class PaymentAmountsSelectQueryHandler(ICommandRepository<PaymentTransaction> paymentTransactionRepository) : IQueryHandler<PaymentAmountsSelectQuery, PaymentAmountsSelectQueryResponse>
{
    public async Task<PaymentAmountsSelectQueryResponse> Handle(PaymentAmountsSelectQuery request, CancellationToken cancellationToken)
    {
        var response = new PaymentAmountsSelectQueryResponse { Success = false };

        var amountResponse = paymentTransactionRepository
            .Find(x => x.IsActive && x.PaymentUrl == null)
            .Select(x => x.Amount)
            .Sum(x => x);

        // True
        response.Success = true;
        response.Response = amountResponse;
        response.SetMessage(MessageId.I00001, "Lấy tổng số tiền thanh toán");
        return response;
    }
}