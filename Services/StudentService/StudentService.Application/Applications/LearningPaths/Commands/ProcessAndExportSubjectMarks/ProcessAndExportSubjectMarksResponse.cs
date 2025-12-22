using BaseService.Common.ApiEntities;

namespace StudentService.Application.Applications.LearningPaths.Commands.ProcessAndExportSubjectMarks;

public record ProcessAndExportSubjectMarksResponse : AbstractApiResponse<string>
{
    public override string Response { get; set; } = string.Empty; // URL của file PDF đã upload
}

