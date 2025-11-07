using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.Students.Commands.Inserts;

public record StudentTranscriptInsertResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; }
}