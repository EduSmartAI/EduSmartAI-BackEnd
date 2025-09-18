using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Technologies.Commands;

public class TechnologyInsertCommand : ICommand<TechnologyInsertResponse>
{
    public string TechnologyName { get; set; } = null!;

    public string? Description { get; set; }
    
    public short TechnologyType { get; set; }
}