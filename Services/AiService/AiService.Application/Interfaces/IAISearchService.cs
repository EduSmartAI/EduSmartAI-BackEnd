namespace AiService.Application.Interfaces
{
    public interface IAISearchService
    {
        Task<string> FindCourseResourcesAsync(string topic, string audience, bool? fastModeOverride);
        Task<string> FindMultipleChoiceExcercises(string topic, int difficultyLevel, bool? fastModeOverride);
    }
}
