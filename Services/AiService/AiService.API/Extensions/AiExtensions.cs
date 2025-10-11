using AiService.Application.Interfaces;
using AiService.Infrastructure.Implements;
using BaseService.Common.Settings;
using BaseService.Common.Utils.Const;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using OpenAI.Embeddings;

namespace AiService.API.Extensions
{
    public sealed class AiOptions
    {
        public string? ApiKey { get; set; }
        public string ChatModel { get; set; } = "gpt-4o-mini";
        public string EmbedModel { get; set; } = "text-embedding-3-small";
        public string AudioModel { get; set; } = "whisper-1";
    }
    public static class AiExtensions
    {
        public static IServiceCollection AddAiServices(this IServiceCollection services, IConfiguration config)
        {
            // Bind + ENV fallback
            EnvLoader.Load();
            services.Configure<AiOptions>(opt =>
            {
                config.GetSection("AI").Bind(opt);
                opt.ApiKey = Environment.GetEnvironmentVariable(ConstEnv.OpenAIKey);

                var envChat = Environment.GetEnvironmentVariable(ConstEnv.ChatModel);
                var envEmb = Environment.GetEnvironmentVariable(ConstEnv.EmbedModel);
                if (!string.IsNullOrWhiteSpace(envChat)) opt.ChatModel = envChat!;
                if (!string.IsNullOrWhiteSpace(envEmb)) opt.EmbedModel = envEmb!;
            });

            // Validate
            services.PostConfigure<AiOptions>(opt =>
            {
                if (string.IsNullOrWhiteSpace(opt.ApiKey))
                    throw new InvalidOperationException("OpenAI API key is required. Set AI:ApiKey or OPENAI_API_KEY.");
            });

            services.AddSingleton(sp =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                return new OpenAIClient(o.ApiKey);
            });

            // Chat / Embedding / Audio
            services.AddSingleton(sp =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                return new ChatClient(o.ChatModel, o.ApiKey);
            });

            services.AddSingleton(sp =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                return new EmbeddingClient(o.EmbedModel, o.ApiKey);
            });

            services.AddSingleton(sp =>
            {
                var o = sp.GetRequiredService<IOptions<AiOptions>>().Value;
                var root = sp.GetRequiredService<OpenAIClient>();
                return root.GetAudioClient(o.AudioModel);
            });

            services.AddScoped<ISurveyAnalysis, SurveyAnalysisService>();

            return services;
        }
    }
}
