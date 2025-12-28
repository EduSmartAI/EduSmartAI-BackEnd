namespace BuildingBlocks.Messaging.Events.StudentService;

public class PdfUploadEvent
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] FileData { get; set; } = Array.Empty<byte>();
    public string StudentEmail { get; set; } = string.Empty;
}

