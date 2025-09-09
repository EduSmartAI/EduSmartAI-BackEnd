using BaseService.Common.ApiEntities;

namespace StudentService.Application.Majors.Queries;

public record MajorsSelectResponse : AbstractApiResponse<List<MajorsSelectResponseEntity>>
{
    public override List<MajorsSelectResponseEntity>? Response { get; set; }
}

public class MajorsSelectResponseEntity
{
    public Guid MajorId { get; set; }

    public string MajorName { get; set; } = null!;
    
    public string MajorCode { get; set; } = null!;

    public string? Description { get; set; }
}