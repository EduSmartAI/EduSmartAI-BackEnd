using System.Text.Json;

namespace Course.Application.Interfaces
{
	public interface IMajorEmbeddingBuilderClient
	{
		Task<JsonElement> RebuildAllAsync(int? maxRows = null, CancellationToken ct = default);
	}

}
