using BaseService.Application.Common;
using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Technologies.Queries;

public record AdminTechnologiesSelectResponse : AbstractApiResponse<PagedResult<AdminTechnologyItem>>
{
    public override PagedResult<AdminTechnologyItem> Response { get; set; } = new();
}

public class AdminTechnologyItem
{
    public Guid TechnologyId { get; set; }
    
    public string TechnologyName { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public short TechnologyType { get; set; }
    
    public string TechnologyTypeName { get; set; } = null!;
    
    public DateTime CreatedAt { get; set; }
}

