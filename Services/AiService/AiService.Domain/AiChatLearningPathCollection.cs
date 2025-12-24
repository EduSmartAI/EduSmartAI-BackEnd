namespace AiService.Domain
{
    public class AiChatLearningPathCollection
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string? Name { get; set; }
        public virtual ICollection<ChatHistoryLearningPathItem> Messages { get; set; } = new List<ChatHistoryLearningPathItem>();
        
        /// <summary>
        /// Simple state machine for chat confirmations (e.g., regenerate learning path).
        /// </summary>
        public string? PendingAction { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }
    public class ChatHistoryLearningPathItem
    {
        // "user" | "assistant" | "system"
        public string? Role { get; set; }
        public string? Content { get; set; }
        public string? RawFinishReason { get; set; }
    }

}
