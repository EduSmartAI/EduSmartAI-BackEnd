using MediatR;
using NLog;
using StudentService.Application.Interfaces;

namespace StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;

public class ExportSubjectMarkUpdateWordCommandHandler : IRequestHandler<ExportSubjectMarkUpdateWordCommand, ExportSubjectMarkUpdateWordResponse>
{
    private readonly IPdfExportService _pdfExportService;
    private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

    public ExportSubjectMarkUpdateWordCommandHandler(IPdfExportService pdfExportService)
    {
        _pdfExportService = pdfExportService;
    }

    public async Task<ExportSubjectMarkUpdateWordResponse> Handle(ExportSubjectMarkUpdateWordCommand request, CancellationToken cancellationToken)
    {
        if (request.SubjectMarkUpdates == null || request.SubjectMarkUpdates.Count == 0)
        {
            _logger.Warn("ExportSubjectMarkUpdateWord: SubjectMarkUpdates is null or empty");
            return new ExportSubjectMarkUpdateWordResponse
            {
                Success = false,
                Response = Array.Empty<byte>(),
                FileName = "SubjectMarkUpdateReport.pdf"
            };
        }

        try
        {
            _logger.Info($"ExportSubjectMarkUpdateWord: Generating PDF for {request.SubjectMarkUpdates.Count} subjects");
            
            var documentBytes = await _pdfExportService.GenerateSubjectMarkUpdatePdfDocumentAsync(
                request.SubjectMarkUpdates,
                request.Title,
                request.StudentName,
                cancellationToken);

            _logger.Info($"ExportSubjectMarkUpdateWord: PDF generated successfully, size: {documentBytes.Length} bytes");

            return new ExportSubjectMarkUpdateWordResponse
            {
                Success = true,
                Response = documentBytes,
                FileName = $"SubjectMarkUpdateReport_{DateTime.UtcNow:yyyyMMddHHmmss}.pdf"
            };
        }
        catch (Exception ex)
        {
            _logger.Error(ex, $"ExportSubjectMarkUpdateWord: Error generating PDF - {ex.Message}");
            throw; // Re-throw to be handled by ApiControllerHelper
        }
    }
}

