namespace AiService.Application.DTOs
{
    public class ChatRequestDto
    {
        public string? Message { get; set; }
        public List<ChatHistoryItem>? History { get; set; }
    }

    public class ChatHistoryItem
    {
        // "user" | "assistant" | "system"
        public string? Role { get; set; }
        public string? Content { get; set; }
    }

    public class ChatResponseDto
    {
        public string? Reply { get; set; }
        public string? RawFinishReason { get; set; }
    }
}
