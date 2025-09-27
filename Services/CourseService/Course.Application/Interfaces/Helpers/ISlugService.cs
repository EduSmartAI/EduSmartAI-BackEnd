namespace Course.Application.Interfaces.Helpers
{
	public interface ISlugService
	{
		string ToSlug(string input);
		Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct);
		Task<string> EnsureUniqueSlugForUpdateAsync(Guid courseId, string candidate, CancellationToken ct, bool allowRandomSuffix = false);
	}
}
