namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    /// <summary>
    /// Response for synchronous request/response usage of QuizAiFeedBackModuleEvent.
    /// </summary>
    public record QuizAiFeedBackModuleResponse(bool Success, string? Message = null);
}


