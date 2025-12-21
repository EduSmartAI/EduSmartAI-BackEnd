using StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;

namespace StudentService.Application.Interfaces;

public interface IPdfExportService
{
    Task<byte[]> GenerateSubjectMarkUpdatePdfDocumentAsync(
        List<SubjectMarkUpdateDto> subjectMarkUpdates,
        string? title = null,
        string? studentName = null,
        CancellationToken cancellationToken = default);
}

