namespace AiService.Application.Interfaces
{
    public interface IAISearchService
    {
        Task<string> FindCourseResourcesAsync(string topic, string audience, bool? fastModeOverride);
    }
}
