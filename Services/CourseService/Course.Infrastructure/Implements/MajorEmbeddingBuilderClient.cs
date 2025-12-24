using System.Net.Http.Json;
using System.Text.Json;

namespace Course.Infrastructure.Implements
{
	public class MajorEmbeddingBuilderClient(HttpClient _http) : IMajorEmbeddingBuilderClient
	{
		public async Task<JsonElement> RebuildAllAsync(int? maxRows = null, CancellationToken ct = default)
		{
			// Gradio API endpoint: /api/{api_name}
			// Request body: JSON array of inputs [maxRows] (for Gradio Blocks with api_name)
			// Response: JSON array of outputs [{result}]
			// Note: BaseAddress is configured in DependencyInjection
			
			// Convert maxRows to the appropriate format (null -> None in Python, int -> int)
			var requestBody = new object?[] { maxRows };
			
			var postResp = await _http.PostAsJsonAsync(
				"/api/build_major_embeddings",
				requestBody,
				ct
			);

			if (!postResp.IsSuccessStatusCode)
			{
				var content = await postResp.Content.ReadAsStringAsync(ct);
				var requestUrl = _http.BaseAddress?.ToString().TrimEnd('/') + "/api/build_major_embeddings";
				throw new HttpRequestException(
					$"POST to API failed: {(int)postResp.StatusCode} {postResp.StatusCode} | " +
					$"URL: {requestUrl} | " +
					$"Response: {content}"
				);
			}

			// Response từ FastAPI là JSON object trực tiếp (không phải array)
			var responseJson = await postResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
			if (responseJson.ValueKind != JsonValueKind.Object)
				throw new HttpRequestException($"Invalid response from API. Expected JSON object. Got: {responseJson.ValueKind}");

			// Trả về JSON object trực tiếp
			return responseJson;
		}
	}
}
