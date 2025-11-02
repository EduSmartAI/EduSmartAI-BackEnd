using Microsoft.AspNetCore.Http;

namespace BuildingBlocks.Messaging.Events.StudentService;

public class AvatarUploadEvent
{
    public string FileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public byte[] FileData { get; set; } = Array.Empty<byte>();
}