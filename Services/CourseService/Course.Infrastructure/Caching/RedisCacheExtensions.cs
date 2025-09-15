using StackExchange.Redis;
using System.Text.Json;

namespace Course.Infrastructure.Caching
{
	public static class RedisCacheExtensions
	{
		private static readonly JsonSerializerOptions Options = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = false
		};

		public static async Task<T?> GetAsync<T>(this IDatabase db, string key)
		{
			var val = await db.StringGetAsync(key);
			return val.HasValue ? JsonSerializer.Deserialize<T>(val!, Options) : default;
		}

		public static Task SetAsync<T>(this IDatabase db, string key, T value, TimeSpan ttl) =>
			db.StringSetAsync(key, JsonSerializer.Serialize(value, Options), ttl);
	}
}
