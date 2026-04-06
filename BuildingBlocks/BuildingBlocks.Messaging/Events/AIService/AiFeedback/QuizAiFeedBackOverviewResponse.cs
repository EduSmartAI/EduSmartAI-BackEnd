namespace BuildingBlocks.Messaging.Events.AIService.AiFeedback
{
    /// <summary>
    /// Response for synchronous request/response usage of QuizAiFeedBackOverviewEvent.
    /// </summary>
    public record QuizAiFeedBackOverviewResponse(bool Success, string? Message = null);
}


