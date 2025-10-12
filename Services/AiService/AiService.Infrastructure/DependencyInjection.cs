using AiService.Application.DTOs;
using AiService.Application.Interfaces;
using AiService.Infrastructure.Implements;
using BaseService.Common.Utils.Const;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AiService.Infrastructure
{
	public static class DependencyInjection
	{
		public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration cfg)
		{
			var groqApiKey = Environment.GetEnvironmentVariable(ConstEnv.GroqApiKey);

			var opt = new GroqOptionsDto
			{
				ApiKey = groqApiKey ?? throw new InvalidOperationException("Missing GROQ_API_KEY in .env")
			};

			services.AddSingleton(opt);

			// HttpClient
			services.AddHttpClient<IAiQuizEvaluatorService, AiQuizEvaluatorService>((sp, http) =>
			{
				http.BaseAddress = new Uri(opt.BaseUrl.TrimEnd('/') + "/");
				http.Timeout = TimeSpan.FromSeconds(30);
			});

			return services;
		}
	}
}
