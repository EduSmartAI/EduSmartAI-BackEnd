using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;

public record ExportSubjectMarkUpdateWordResponse : AbstractApiResponse<byte[]>
{
    public override byte[] Response { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "SubjectMarkUpdateReport.pdf";
    public string ContentType { get; set; } = "application/pdf";
}

