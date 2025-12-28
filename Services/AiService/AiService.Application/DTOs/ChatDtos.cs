namespace AiService.Application.DTOs
{
    public class ChatRequestDto
    {
        public string? Message { get; set; }
        public Guid? LessionId { get; set; }
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

    public class ChatBotLearningPathRequestDto
    {
        public string? Message { get; set; }
        public Guid? SessionId { get; set; }
    }

    public class ChatSummaryDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int MessageCount { get; set; }
    }

    public class ChatDetailDto
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public List<ChatHistoryLearningPathItemDto> Messages { get; set; } = new List<ChatHistoryLearningPathItemDto>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
    }

    public class ChatHistoryLearningPathItemDto
    {
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? RawFinishReason { get; set; }
    }
}
