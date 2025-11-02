using System.ComponentModel.DataAnnotations;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyUpdateCommand : ICommand<TechnologyUpdateResponse>
{
    [Required(ErrorMessage = "TechnologyId is required")]
    public Guid TechnologyId { get; set; }
    
    [Required(ErrorMessage = "TechnologyName is required")]
    public string TechnologyName { get; set; } = null!;

    public string? Description { get; set; }
    
    [Required(ErrorMessage = "TechnologyType is required")]
    public ConstantEnum.TechnologyType TechnologyType { get; set; }
}