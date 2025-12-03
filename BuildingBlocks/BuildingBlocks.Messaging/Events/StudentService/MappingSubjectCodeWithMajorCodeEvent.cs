using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public class MappingSubjectCodeWithMajorCodeEvent
{
    public List<string> SubjectCodes { get; set; } = null!;
}

public record MappingSubjectCodeWithMajorCodeEventResponse : AbstractApiResponse<List<MappingSubjectCodeWithMajorCodeEventResponseEntity>>
{
    public override List<MappingSubjectCodeWithMajorCodeEventResponseEntity> Response { get; set; }
}

public class MappingSubjectCodeWithMajorCodeEventResponseEntity
{
    public string MajorCode { get; set; } = null!;
    
    public string SubjectCode { get; set; } = null!;
}