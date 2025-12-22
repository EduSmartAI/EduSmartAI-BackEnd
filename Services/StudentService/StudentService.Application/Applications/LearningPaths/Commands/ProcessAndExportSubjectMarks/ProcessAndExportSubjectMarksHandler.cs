using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.AiRecommend;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using MediatR;
using StudentService.Application.Applications.LearningPaths.Commands.ExportSubjectMarkUpdateWord;
using StudentService.Application.Applications.LearningPaths.Queries.GetSubjectMarksByLearningPath;

namespace StudentService.Application.Applications.LearningPaths.Commands.ProcessAndExportSubjectMarks;

public class ProcessAndExportSubjectMarksHandler : IRequestHandler<ProcessAndExportSubjectMarksCommand, ProcessAndExportSubjectMarksResponse>
{
    private readonly IMediator _mediator;
    private readonly IRequestClient<SubjectMarkUpdateEvent> _aiRequestClient;
    private readonly IRequestClient<PdfUploadEvent> _pdfUploadRequestClient;
    private readonly IIdentityService _identityService;

    public ProcessAndExportSubjectMarksHandler(
        IMediator mediator, 
        IRequestClient<SubjectMarkUpdateEvent> aiRequestClient,
        IRequestClient<PdfUploadEvent> pdfUploadRequestClient,
        IIdentityService identityService)
    {
        _mediator = mediator;
        _aiRequestClient = aiRequestClient;
        _pdfUploadRequestClient = pdfUploadRequestClient;
        _identityService = identityService;
    }

    public async Task<ProcessAndExportSubjectMarksResponse> Handle(ProcessAndExportSubjectMarksCommand request, CancellationToken cancellationToken)
    {
        var response = new ProcessAndExportSubjectMarksResponse
        {
            Success = false,
            Response = string.Empty
        };

        try
        {
            // Validate request
            if (request.LearningPathId == Guid.Empty)
            {
                response.SetMessage(MessageId.E00000, "LearningPathId không hợp lệ");
                return response;
            }

            // 1. Lấy danh sách subject marks từ learning path
            var getSubjectMarksQuery = new GetSubjectMarksByLearningPathQuery
            {
                LearningPathId = request.LearningPathId
            };

            var subjectMarksResponse = await _mediator.Send(getSubjectMarksQuery, cancellationToken);

            if (!subjectMarksResponse.Success || subjectMarksResponse.Response == null || !subjectMarksResponse.Response.Any())
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy điểm môn học nào cho learning path này");
                return response;
            }

            // 2. Lọc bỏ các item có cả oldMark và newMark đều = null
            // Và bỏ các item có newMark = 0 và newAnalysis rỗng/null
            var filteredMarks = subjectMarksResponse.Response
                .Where(m => (m.OldMark.HasValue || m.NewMark.HasValue) &&
                           !(m.NewMark.HasValue && m.NewMark.Value == 0 && string.IsNullOrWhiteSpace(m.NewAnalysis)))
                .ToList();

            if (!filteredMarks.Any())
            {
                response.SetMessage(MessageId.E00000, "Không có môn học nào có điểm để export");
                return response;
            }

            // 3. Gọi AI service để phân tích subject marks (đồng bộ, chờ response)
            List<SubjectMarkUpdateAnalysisDto>? aiAnalysisResults = null;
            
            var marksToAnalyze = filteredMarks
                .Where(m => m.NewMark.HasValue) // Chỉ phân tích các item có newMark
                .ToList();
            
            if (marksToAnalyze.Any())
            {
                var subjectMarkUpdateEvent = new SubjectMarkUpdateEvent
                {
                    LearningPathId = request.LearningPathId,
                    SubjectMarks = marksToAnalyze.Select(m => new SubjectMarkUpdateItem
                    {
                        SubjectCode = m.SubjectCode,
                        SubjectName = m.SubjectName,
                        OldMark = m.OldMark,
                        NewMark = m.NewMark!.Value,
                        NewAnalysis = m.NewAnalysis,
                        CareerGoal = m.CareerGoal
                    }).ToList()
                };

                // Gọi AI service và chờ response
                var aiResponse = await _aiRequestClient.GetResponse<SubjectMarkUpdateEventResponse>(
                    subjectMarkUpdateEvent, 
                    cancellationToken);

                if (aiResponse.Message.Success && aiResponse.Message.Response != null)
                {
                    aiAnalysisResults = aiResponse.Message.Response;
                }
            }

            // 4. Chuyển đổi sang format cho ExportSubjectMarkUpdateWordCommand
            // Sử dụng kết quả từ AI analysis nếu có, nếu không thì dùng dữ liệu gốc
            var exportCommand = new ExportSubjectMarkUpdateWordCommand
            {
                SubjectMarkUpdates = filteredMarks.Select(m =>
                {
                    // Tìm kết quả phân tích từ AI service
                    var aiResult = aiAnalysisResults?.FirstOrDefault(r => 
                        r.SubjectCode.Equals(m.SubjectCode, StringComparison.OrdinalIgnoreCase));
                    
                    return new SubjectMarkUpdateDto
                    {
                        SubjectCode = m.SubjectCode,
                        SubjectName = m.SubjectName,
                        OldMark = m.OldMark,
                        NewMark = m.NewMark ?? 0,
                        MarkImprovement = aiResult != null 
                            ? aiResult.MarkImprovement 
                            : ((m.NewMark ?? 0) - (m.OldMark ?? 0)),
                        ImprovementAnalysis = aiResult?.ImprovementAnalysis ?? m.NewAnalysis,
                        ComparisonAnalysis = aiResult?.ComparisonAnalysis
                    };
                }).ToList()
            };

            // 5. Gọi ExportSubjectMarkUpdateWordCommand để tạo PDF
            var exportResponse = await _mediator.Send(exportCommand, cancellationToken);

            if (!exportResponse.Success)
            {
                response.SetMessage(exportResponse.MessageId ?? MessageId.E00000, exportResponse.Message ?? "Lỗi khi export PDF");
                return response;
            }

            // 6. Lấy email của student từ identity service
            var currentUser = _identityService.GetCurrentUser();
            var studentEmail = currentUser?.Email ?? string.Empty;

            // 7. Upload PDF lên Cloudinary qua Utility Service
            var pdfUploadEvent = new PdfUploadEvent
            {
                FileName = exportResponse.FileName ?? "SubjectMarkUpdateReport.pdf",
                ContentType = exportResponse.ContentType ?? "application/pdf",
                FileData = exportResponse.Response,
                StudentEmail = studentEmail
            };

            var uploadResponse = await _pdfUploadRequestClient.GetResponse<PdfUploadEventResponse>(
                pdfUploadEvent,
                cancellationToken);

            if (!uploadResponse.Message.Success || string.IsNullOrWhiteSpace(uploadResponse.Message.Response?.FileUrl))
            {
                response.SetMessage(MessageId.E00000, "Lỗi khi upload PDF lên Cloudinary");
                return response;
            }

            response.Success = true;
            response.Response = uploadResponse.Message.Response.FileUrl;
            response.SetMessage(MessageId.I00001, "Xử lý và upload PDF thành công");
            return response;
        }
        catch
        {
            throw;
        }
    }
}

