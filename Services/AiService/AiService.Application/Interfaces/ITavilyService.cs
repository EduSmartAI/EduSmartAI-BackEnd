namespace AiService.Application.Interfaces
{
    public sealed class TavilyItem
    {
        public string? Title { get; set; }
        public string? Content { get; set; }
        public string? Url { get; set; }
        public double Score { get; set; }
        public string? Host { get; set; }
    }
    public interface ITavilyService
    {
        Task<List<TavilyItem>> TavilySearchParallelAsync(List<string> queries, HashSet<string> trustedDomains, HashSet<string> avoidDomains);
        Task<List<TavilyItem>> TavilySearchParallelExhaustiveAsync(
        List<string> queries,
        HashSet<string> trustedDomains,
        HashSet<string> avoidDomains,
        int limitPerDomain = 3,
        int maxTake = 90);
    }
}
