using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using MediatR;

namespace PaymentService.Application.Applications.Orders.Commands.InsertOrder;

public class InsertOrderCommand : IRequest<InsertOrderResponse>
{
    [Required(ErrorMessage = "CourseIds is required")]
    public List<Guid> CourseIds { get; set; }
    
    [Required(ErrorMessage = "PaymentMethod is required")]
    public ConstantEnum.PaymentGateway PaymentMethod { get; set; }
}

