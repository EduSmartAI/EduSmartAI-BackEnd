using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using UtilityService.Application.Interfaces;

namespace UtilityService.Application.Consumers;

public class AvatarUploadEventConsumer(ICloudinaryService cloudinaryService) : IConsumer<AvatarUploadEvent>
{
    public async Task Consume(ConsumeContext<AvatarUploadEvent> context)
    {
        var evt = context.Message;
        var response = new AvatarUploadEventResponse
        {
            Success = false,
            Response = new AvatarUploadEventResponseEntity()
        };

        await using var stream = new MemoryStream(evt.FileData);
        var uploadUrl = await cloudinaryService.UploadImageAsync(evt.FileName, stream, evt.ContentType);

        response.Success = true;
        response.Response.AvatarUrl = uploadUrl;
        await context.RespondAsync(response);
    }
}