using BaseService.Common.ApiEntities;

namespace PaymentService.Application.Applications.Payments.Queries;

public record PaymentAmountsSelectQueryResponse : AbstractApiResponse<decimal>
{
    public override decimal Response { get; set; }
}