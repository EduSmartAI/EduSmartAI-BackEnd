using BaseService.Application.Common;
using BuildingBlocks.CQRS;

namespace StudentService.Application.Applications.Technologies.Queries;

public class AdminTechnologiesSelectQuery : IQuery<AdminTechnologiesSelectResponse>
{
    public int PageNumber { get; set; } = 1;
    
    public int PageSize { get; set; } = 10;
    
    public string? SearchTerm { get; set; }
    
    public short? TechnologyType { get; set; }
}