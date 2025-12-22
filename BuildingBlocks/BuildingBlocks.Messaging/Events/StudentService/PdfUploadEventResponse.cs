using BaseService.Common.ApiEntities;

namespace BuildingBlocks.Messaging.Events.StudentService;

public record PdfUploadEventResponse : AbstractApiResponse<PdfUploadEventResponseEntity>
{
    public override PdfUploadEventResponseEntity Response { get; set; } = null!;
}

public class PdfUploadEventResponseEntity
{
    public string FileUrl { get; set; } = string.Empty;
}

