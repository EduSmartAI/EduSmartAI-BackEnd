using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;
using MassTransit;
using OpenAI.Chat;
using System.Text.Json;

namespace AiService.Infrastructure.Implements
{
    public class ChatBotService(
        IRequestClient<GetLessonInfoEvent> requestClient,
        ChatClient _chat,
        IIdentityService _identityService
        ) : IChatBotService
    {
        // ===== System prompt dành cho học tập =====
        private const string SystemMessage =
        "You are EduSmart Study Assistant. Be concise, structured, and accurate. " +
        "Default language: Vietnamese (vi). " +
        "When the user asks to summarize, explain concepts, create quiz, or give real-world examples, "
        + "you MUST call the corresponding tool immediately. "
        + "If lesson_text is not provided by the user, still call the tool; the backend will fetch it by LessionId.";

        // ====== 4 Tools ======

        // 1) Tóm tắt nhanh bài học
        private static readonly ChatTool SummarizeLessonTool = ChatTool.CreateFunctionTool(
            functionName: "summarize_lesson",
            functionDescription:
                "Summarize the current lesson. Always call this when the user asks to summarize. "
              + "If lesson_text is not provided, call with no args; the backend will fetch by LessionId.",
            functionParameters: BinaryData.FromString("""
            {
              "type":"object",
              "properties":{
                "max_bullets":{"type":"integer","minimum":1,"maximum":10,"default":5},
                "language":{"type":"string","enum":["vi","en"],"default":"vi"}
              },
              "additionalProperties":false
            }
            """)
        );

        // 2) Giải thích đơn giản các khái niệm chính
        private static readonly ChatTool ExplainConceptsTool = ChatTool.CreateFunctionTool(
            functionName: "explain_key_concepts",
            functionDescription: "Extract key concepts from the lesson and explain them simply.",
            functionParameters: BinaryData.FromBytes("""
            {
              "type":"object",
              "properties":{
                "max_concepts":{"type":"integer","minimum":1,"maximum":10,"default":5},
                "simplicity":{"type":"string","enum":["primary","secondary","college"],"default":"secondary"},
                "language":{"type":"string","enum":["vi","en"],"default":"vi"}
              },
              "additionalProperties":false
            }
            """u8.ToArray())
        );

        // 3) Tạo câu hỏi trắc nghiệm + đáp án
        private static readonly ChatTool CreateQuizTool = ChatTool.CreateFunctionTool(
            functionName: "create_quiz_questions",
            functionDescription: "Create multiple-choice review questions (with answer key) from the lesson.",
            functionParameters: BinaryData.FromBytes("""
            {
              "type":"object",
              "properties":{
                "lesson_text":{"type":"string","description":"Full plain text of the lesson"},
                "num_questions":{"type":"integer","minimum":1,"maximum":20,"default":5},
                "difficulty":{"type":"string","enum":["easy","medium","hard"],"default":"medium"},
                "include_answers":{"type":"boolean","default":true},
                "language":{"type":"string","enum":["vi","en"],"default":"vi"}
              },
              "required":["lesson_text"],
              "additionalProperties":false
            }
            """u8.ToArray())
        );

        // 4) Ví dụ ứng dụng thực tế
        private static readonly ChatTool RealExamplesTool = ChatTool.CreateFunctionTool(
            functionName: "give_real_world_examples",
            functionDescription: "Provide real-world applications/examples for the lesson content.",
            functionParameters: BinaryData.FromBytes("""
            {
              "type":"object",
              "properties":{
                "lesson_text":{"type":"string","description":"Full plain text of the lesson"},
                "num_examples":{"type":"integer","minimum":1,"maximum":10,"default":3},
                "domain":{"type":"string","description":"Optional domain/industry to tailor examples"},
                "language":{"type":"string","enum":["vi","en"],"default":"vi"}
              },
              "required":["lesson_text"],
              "additionalProperties":false
            }
            """u8.ToArray())
        );

        public async Task<ChatResponseDto> ChatAsync(AIChatBotRequest req, CancellationToken ct = default)
        {
            var messages = new List<ChatMessage> { new SystemChatMessage(SystemMessage) };

            if (req.Request.History is not null)
            {
                foreach (var m in req.Request.History)
                {
                    messages.Add(m.Role?.ToLowerInvariant() switch
                    {
                        "assistant" => new AssistantChatMessage(m.Content ?? string.Empty),
                        "system" => new SystemChatMessage(m.Content ?? string.Empty),
                        _ => new UserChatMessage(m.Content ?? string.Empty)
                    });
                }
            }

            messages.Add(new UserChatMessage(req.Request.Message ?? string.Empty));

            var options = new ChatCompletionOptions
            {
                Tools = { SummarizeLessonTool, ExplainConceptsTool, CreateQuizTool, RealExamplesTool }
            };

            while (true)
            {
                var completion = await _chat.CompleteChatAsync(messages, options, ct);

                if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    messages.Add(new AssistantChatMessage(completion)); // giữ tool_calls

                    foreach (var call in completion.Value.ToolCalls)
                    {
                        var argsJson = call.FunctionArguments?.ToString() ?? "{}";
                        using var args = JsonDocument.Parse(argsJson);
                        var root = args.RootElement;

                        switch (call.FunctionName)
                        {
                            case "summarize_lesson":
                                {
                                    var max = root.TryGetProperty("max_bullets", out var mEl) ? mEl.GetInt32() : 5;
                                    var lang = root.TryGetProperty("language", out var lEl) ? lEl.GetString() ?? "vi" : "vi";

                                    if (req.Request.LessionId is not Guid lessonId)
                                        throw new ArgumentException("LessionId is required.", nameof(req));
                                    var currentUserId = _identityService.GetCurrentUser()!.UserId;

                                    var @event = new GetLessonInfoEvent(lessonId, currentUserId);

                                    var response = await requestClient.GetResponse<GetLessonInfoResponse>(@event, ct);
                                    var json = await RunSummarizeAsync(response.Message.Response.TranscriptText, max, lang, ct);
                                    messages.Add(new ToolChatMessage(call.Id, json));
                                    break;
                                }

                            case "explain_key_concepts":
                                {
                                    var max = root.TryGetProperty("max_concepts", out var mEl) ? mEl.GetInt32() : 5;
                                    var simplicity = root.TryGetProperty("simplicity", out var sEl) ? sEl.GetString() ?? "secondary" : "secondary";
                                    var lang = root.TryGetProperty("language", out var lEl) ? lEl.GetString() ?? "vi" : "vi";

                                    if (req.Request.LessionId is not Guid lessonId)
                                        throw new ArgumentException("LessionId is required.", nameof(req));
                                    var currentUserId = _identityService.GetCurrentUser()!.UserId;

                                    var @event = new GetLessonInfoEvent(lessonId, currentUserId);

                                    var response = await requestClient.GetResponse<GetLessonInfoResponse>(@event, ct);

                                    var json = await RunExplainAsync(response.Message.Response.TranscriptText, max, simplicity, lang, ct);
                                    messages.Add(new ToolChatMessage(call.Id, json));
                                    break;
                                }

                            case "create_quiz_questions":
                                {
                                    var text = root.GetProperty("lesson_text").GetString() ?? "";
                                    var n = root.TryGetProperty("num_questions", out var nEl) ? nEl.GetInt32() : 5;
                                    var diff = root.TryGetProperty("difficulty", out var dEl) ? dEl.GetString() ?? "medium" : "medium";
                                    var includeAns = root.TryGetProperty("include_answers", out var aEl) && aEl.GetBoolean();
                                    var lang = root.TryGetProperty("language", out var lEl) ? lEl.GetString() ?? "vi" : "vi";

                                    var json = await RunQuizAsync(text, n, diff, includeAns, lang, ct);
                                    messages.Add(new ToolChatMessage(call.Id, json));
                                    break;
                                }

                            case "give_real_world_examples":
                                {
                                    var text = root.GetProperty("lesson_text").GetString() ?? "";
                                    var n = root.TryGetProperty("num_examples", out var nEl) ? nEl.GetInt32() : 3;
                                    var domain = root.TryGetProperty("domain", out var dEl) ? dEl.GetString() : null;
                                    var lang = root.TryGetProperty("language", out var lEl) ? lEl.GetString() ?? "vi" : "vi";

                                    var json = await RunExamplesAsync(text, n, domain, lang, ct);
                                    messages.Add(new ToolChatMessage(call.Id, json));
                                    break;
                                }

                            default:
                                throw new NotImplementedException($"Unknown tool: {call.FunctionName}");
                        }
                    }

                    continue; // gọi lại model để tạo câu trả lời cuối
                }

                if (completion.Value.FinishReason == ChatFinishReason.Stop)
                {
                    var text = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : string.Empty;
                    return new ChatResponseDto
                    {
                        Reply = text,
                        RawFinishReason = completion.Value.FinishReason.ToString()
                    };
                }

                throw new InvalidOperationException($"FinishReason: {completion.Value.FinishReason}");
            }
        }

        // ===== Mini LLM calls (trả JSON) =====

        private async Task<string> RunSummarizeAsync(string lesson, int maxBullets, string lang, CancellationToken ct)
        {
            var sys = $"Return ONLY JSON: {{\"bullets\": string[]}}. Language: {lang}. Max {maxBullets} bullets, each ≤ 20 words.";
            var user = $"Summarize this lesson:\n{lesson}";
            var schema = """
            {
              "type":"object",
              "properties":{"bullets":{"type":"array","items":{"type":"string"}}},
              "required":["bullets"],
              "additionalProperties":false
            }
            """;
            return await RunJsonAsync("summarize_schema", schema, sys, user, ct);
        }

        private async Task<string> RunExplainAsync(string lesson, int maxConcepts, string simplicity, string lang, CancellationToken ct)
        {
            var sys = $"Return ONLY JSON: {{\"concepts\":[{{\"term\":string,\"simple_explanation\":string}}]}}. " +
                      $"Language: {lang}. Audience level: {simplicity}. Max {maxConcepts} concepts.";
            var user = $"Extract and explain main concepts from the lesson:\n{lesson}";
            var schema = """
            {
              "type":"object",
              "properties":{
                "concepts":{
                  "type":"array",
                  "items":{
                    "type":"object",
                    "properties":{
                      "term":{"type":"string"},
                      "simple_explanation":{"type":"string"}
                    },
                    "required":["term","simple_explanation"],
                    "additionalProperties":false
                  }
                }
              },
              "required":["concepts"],
              "additionalProperties":false
            }
            """;
            return await RunJsonAsync("concepts_schema", schema, sys, user, ct);
        }

        private async Task<string> RunQuizAsync(string lesson, int n, string difficulty, bool includeAnswers, string lang, CancellationToken ct)
        {
            var sys = $"Return ONLY JSON: {{\"questions\":[{{\"question\":string,\"options\":string[],\"answer\":string,\"explanation\":string}}]}}. " +
                      $"Language: {lang}. Count: {n}. Difficulty: {difficulty}. Include answers: {includeAnswers}. Ensure exactly 4 options.";
            var user = $"Create multiple-choice review questions from this lesson:\n{lesson}";
            var schema = """
            {
              "type":"object",
              "properties":{
                "questions":{
                  "type":"array",
                  "items":{
                    "type":"object",
                    "properties":{
                      "question":{"type":"string"},
                      "options":{"type":"array","items":{"type":"string"},"minItems":4,"maxItems":4},
                      "answer":{"type":"string"},
                      "explanation":{"type":"string"}
                    },
                    "required":["question","options","answer"],
                    "additionalProperties":false
                  }
                }
              },
              "required":["questions"],
              "additionalProperties":false
            }
            """;
            return await RunJsonAsync("quiz_schema", schema, sys, user, ct);
        }

        private async Task<string> RunExamplesAsync(string lesson, int n, string? domain, string lang, CancellationToken ct)
        {
            var sys = $"Return ONLY JSON: {{\"examples\":[{{\"title\":string,\"description\":string}}]}}. " +
                      $"Language: {lang}. Count: {n}. {(string.IsNullOrWhiteSpace(domain) ? "" : $"Domain: {domain}.")}";
            var user = $"Provide real-world applications/examples for the lesson:\n{lesson}";
            var schema = """
            {
              "type":"object",
              "properties":{
                "examples":{
                  "type":"array",
                  "items":{
                    "type":"object",
                    "properties":{
                      "title":{"type":"string"},
                      "description":{"type":"string"}
                    },
                    "required":["title","description"],
                    "additionalProperties":false
                  }
                }
              },
              "required":["examples"],
              "additionalProperties":false
            }
            """;
            return await RunJsonAsync("examples_schema", schema, sys, user, ct);
        }

        // Helper: gọi LLM trả JSON theo JSON Schema, KHÔNG kèm Tools để tránh lặp ToolCalls
        private async Task<string> RunJsonAsync(string schemaName, string schemaJson, string sys, string user, CancellationToken ct)
        {
            var opts = new ChatCompletionOptions
            {
                // Nếu SDK của bạn hỗ trợ structured outputs:
                ResponseFormat = ChatResponseFormat.CreateJsonSchemaFormat(
                    jsonSchemaFormatName: schemaName,
                    jsonSchema: BinaryData.FromString(schemaJson)
                )
            };
            var res = await _chat.CompleteChatAsync(
                messages: new List<ChatMessage>
                {
                    new SystemChatMessage(sys),
                    new UserChatMessage(user)
                },
                options: opts,
                cancellationToken: ct
            );


            // Trả đúng phần text JSON
            return res.Value.Content.Count > 0 ? (res.Value.Content[0].Text ?? "{}") : "{}";
        }
    }
}
