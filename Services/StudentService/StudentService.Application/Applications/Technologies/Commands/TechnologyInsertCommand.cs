using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyInsertCommand : ICommand<TechnologyInsertResponse>
{
    public string TechnologyName { get; set; } = null!;

    public string? Description { get; set; }
    
    public ConstantEnum.TechnologyType TechnologyType { get; set; }
}