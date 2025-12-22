using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.ApiEntities;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Consumers;

public class PdfUploadEventConsumer(
    ICloudinaryService cloudinaryService,
    ICommandRepository<Emailtemplate> emailTemplateRepository,
    ICommandRepository<Systemconfig> systemConfigRepository) : IConsumer<PdfUploadEvent>
{
    public async Task Consume(ConsumeContext<PdfUploadEvent> context)
    {
        var evt = context.Message;
        var response = new PdfUploadEventResponse
        {
            Success = false,
            Response = new PdfUploadEventResponseEntity()
        };

        await using var stream = new MemoryStream(evt.FileData);
        var uploadUrl = await cloudinaryService.UploadPdfAsync(evt.FileName, stream, evt.ContentType, context.CancellationToken);

        response.Success = true;
        response.Response.FileUrl = uploadUrl;

        // Gửi email thông báo PDF đã sẵn sàng (không chờ kết quả, fire and forget)
        if (!string.IsNullOrWhiteSpace(evt.StudentEmail) && !string.IsNullOrWhiteSpace(uploadUrl))
        {
            try
            {
                await PerformanceLearningPathSendMail.SendMailPdfReady(
                    emailTemplateRepository,
                    systemConfigRepository,
                    "tranduyanh7766@gmail.com",
                    evt.FileName,
                    uploadUrl,
                    expiresMinutes: 30,
                    supportEmail: "support@edusmart.ai",
                    detailErrors: new List<DetailError>());
            }
            catch
            {
                // Log error nhưng không fail upload
                // Email có thể gửi sau hoặc bị lỗi nhưng PDF đã upload thành công
            }
        }

        await context.RespondAsync(response);
    }
}

