using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using BaseService.Application.Interfaces.IdentityHepers;
using BuildingBlocks.Messaging.Events.AIService.GetLessonInfoEvent;
using MassTransit;
using OpenAI.Chat;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AiService.Infrastructure.Implements
{
    public class ChatBotService(
        IRequestClient<GetLessonInfoEvent> requestClient,
        ChatClient _chat,
        IIdentityService _identityService,
        IAISearchService _aiSearchService
        ) : IChatBotService
    {
        // ===== System prompt dành cho học tập =====
        private const string SystemMessage =
            "You are EduSmart Study Assistant related Information Technology. Be concise, structured, and accurate. " +
            "Default language: Vietnamese (vi). " +
            "When the user asks to summarize, explain concepts, create quiz, or give real-world examples, " +
            "you MUST call the corresponding tool immediately. " +
            "If lesson_text is not provided by the user, still call the tool; the backend will fetch it by LessionId. " +
            "After tool results are returned, write the final answer in compact Markdown: use short headings (### ...), " +
            "bullet lists with '-', no extra blank lines, and include 1–2 short real-world examples for summarize/explain. " +
            "For summarize requests, keep the summary ≤ 500 words while covering all main ideas, THEN list all coverage_points returned (one bullet per item). " +
            "For quiz requests: if the user has NOT specified 'AI-generated' vs 'external links', ask a one-line clarification in current language and WAIT for the reply; do NOT call any tool until the mode is known. " +
            "For quiz requests in 'external_links' mode: after presenting the external practice links, always end with ONE short question in the current language asking whether the learner wants more links for other subtopics. " +
            "Do not call tools again until the learner says yes or asks explicitly. " +
            "The backend paginates lesson topics in batches of 3 using external.page. On the FIRST call you MAY omit external.page (the backend will assume page = 1). " +
            "Whenever the learner clearly asks for more links (e.g. 'thêm', 'thêm đi', 'cho thêm', 'tiếp', 'more', 'next'), you MUST call create_quiz_questions again with mode = 'external_links' and external.page set to previous_page + 1. " +
            "If the backend responds that there are no more topics, explain briefly that all main subtopics have been covered and stop calling the tool." +
            "For quiz requests: if the user has NOT specified 'AI-generated' vs 'external links', ask a one-line clarification in current language and WAIT for the reply; do NOT call any tool until the mode is known. ";

        private const string QuizModeClarificationMessage =
            "Bạn có thể cho mình biết bạn muốn tạo câu hỏi từ các nguồn tự động hay từ các liên kết bên ngoài?";

        private static readonly Regex ReCrLf = new(@"\r\n", RegexOptions.Compiled);
        private static readonly Regex ReSpaceNL = new(@"[ \t]+\r?\n", RegexOptions.Compiled);
        private static readonly Regex ReMultiBlank = new(@"(\r?\n){3,}", RegexOptions.Compiled);
        private static readonly Regex ReBlankBeforeBullet = new(@"(?m)^\s*\r?\n(?=\s*-\s)", RegexOptions.Compiled);
        private static readonly Regex ReBulletSpace = new(@"(?m)^\s*-\s+", RegexOptions.Compiled);

        private static string NormalizeMarkdown(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            s = ReCrLf.Replace(s, "\n");                // thống nhất newline
            s = ReSpaceNL.Replace(s, "\n");             // bỏ space trước newline
            s = ReBlankBeforeBullet.Replace(s, "");     // bỏ dòng trống trước bullet
            s = ReMultiBlank.Replace(s, "\n\n");        // tối đa 1 dòng trống liên tiếp
            s = ReBulletSpace.Replace(s, "- ");         // bullet gọn
            return s.Trim();
        }
        private static bool TryGetPropString(JsonElement elem, string name, out string? value)
        {
            value = null;
            if (elem.ValueKind != JsonValueKind.Object) return false;
            if (!elem.TryGetProperty(name, out var p)) return false;
            value = p.GetString();
            return true;
        }

        private static int MapDifficultyToLevel(string? diffOrNumber)
        {
            if (string.IsNullOrWhiteSpace(diffOrNumber)) return 2;
            var s = diffOrNumber.Trim().ToLowerInvariant();

            // Cho phép người dùng trả "1/2/3"
            if (int.TryParse(s, out var n))
                return n <= 1 ? 1 : (n >= 3 ? 3 : 2);

            return s switch
            {
                "easy" or "beginner" => 1,
                "medium" or "mixed" or "intermediate" => 2,
                "hard" or "advanced" => 3,
                _ => 2
            };
        }

        private static string DeriveTopicFromText(string lessonText)
        {
            if (string.IsNullOrWhiteSpace(lessonText)) return "bài học hiện tại";
            // Lấy dòng đầu/tiêu đề ngắn gọn (tối đa ~10 từ)
            var firstLine = lessonText.Split('\n').FirstOrDefault()?.Trim() ?? lessonText.Trim();
            firstLine = Regex.Replace(firstLine, @"[#>*`~_\-\(\)\[\]\{\}:]+", " "); // làm sạch ký tự đặc biệt
            var words = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(10);
            var topic = string.Join(' ', words);
            return string.IsNullOrWhiteSpace(topic) ? "bài học hiện tại" : topic;
        }
        /// <summary>
        /// Trích 8–16 key concepts từ transcript và gộp thành 1 chuỗi "a; b; c; ..."
        /// Nếu LLM lỗi thì fallback DeriveTopicFromText.
        /// </summary>
        private async Task<string?> BuildTopicFromTranscriptAsync(
         string lessonText,
         string lang,
         int page,
         int batchSize,
         CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(lessonText))
                return page == 1 ? DeriveTopicFromText(lessonText) : null;

            var schema = """
        {
          "type":"object",
          "properties":{
            "topics":{
              "type":"array",
              "items":{"type":"string"},
              "minItems":3,
              "maxItems":16
            }
          },
          "required":["topics"],
          "additionalProperties":false
        }
        """;

            var sys =
                "Return ONLY JSON: {\"topics\":[string,...]}. " +
                $"Language: {lang}. " +
                "Extract the MAIN key concepts actually present in the lesson. " +
                "Rules: concise canonical phrases (1–4 words), no fabrication, deduplicate, order by importance, ensure broad coverage.";

            var user = "From this lesson text, list 8–16 key concepts (short phrases) covering ALL main ideas:\n" + lessonText;

            try
            {
                var json = await RunJsonAsync("topics_schema_v1", schema, sys, user, ct);
                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("topics", out var arr) ||
                    arr.ValueKind != JsonValueKind.Array)
                {
                    return page == 1 ? DeriveTopicFromText(lessonText) : null;
                }

                var list = arr.EnumerateArray()
                              .Select(e => e.GetString())
                              .Where(s => !string.IsNullOrWhiteSpace(s))
                              .Select(s => Regex.Replace(s!, @"\s+", " ").Trim())
                              .Distinct(StringComparer.OrdinalIgnoreCase)
                              .ToList();

                if (list.Count == 0)
                    return page == 1 ? DeriveTopicFromText(lessonText) : null;

                var startIndex = (page - 1) * batchSize;
                var batch = list.Skip(startIndex).Take(batchSize).ToList();

                if (batch.Count == 0)
                    return null; // hết topics

                return string.Join("; ", batch); // ví dụ "if–else; switch; guard clause"
            }
            catch
            {
                return page == 1 ? DeriveTopicFromText(lessonText) : null;
            }
        }

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
                "max_bullets":{"type":"integer","minimum":1,"maximum":100,"default":5},
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
            functionDescription:
                "Create AI-generated quiz questions OR suggest external practice links of the same topic. " +
                "If the learner hasn't chosen a mode, you MAY omit \"mode\"; the backend will ask the learner with a fixed clarification sentence. " +
                "Do NOT write your own clarification question.",
            functionParameters: BinaryData.FromString("""
            {
              "type": "object",
              "properties": {
                "mode": { "type": "string", "enum": ["ai_generate", "external_links"] },
                "lesson_text": { "type": "string", "description": "Full plain text of the lesson (optional; backend may fetch by LessionId)" },
                "topics": { "type": "array", "items": { "type": "string" } },
                "num_questions": { "type": "integer", "minimum": 1, "maximum": 50, "default": 5 },
                "difficulty": { "type": "string", "enum": ["easy", "medium", "hard", "mixed"], "default": "medium" },
                "include_answers": { "type": "boolean", "default": true },
                "language": { "type": "string", "enum": ["vi", "en"], "default": "vi" },
                "external": {
                  "type": "object",
                  "properties": {
                    "num_links": { "type": "integer", "minimum": 1, "maximum": 15, "default": 6 },
                    "preferred_domains": { "type": "array", "items": { "type": "string" } },
                    "query_hint": { "type": "string" },
                    "page": {
                      "type": "integer",
                      "minimum": 1,
                      "description": "Batch index for external links: 1 = first 3 topics, 2 = next 3 topics, etc."
                    }
                  },
                  "additionalProperties": false
                }
              },
              "additionalProperties": false
            }
            """)
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
                        var argsJson = call.FunctionArguments?.ToString();
                        JsonElement root;

                        try
                        {
                            using var doc = JsonDocument.Parse(string.IsNullOrWhiteSpace(argsJson) ? "{}" : argsJson);
                            root = doc.RootElement.Clone();
                        }
                        catch (JsonException)
                        {
                            return new ChatResponseDto
                            {
                                Reply = "Mình gặp lỗi khi đọc yêu cầu cho công cụ. Bạn chọn giúp: **AI tạo câu hỏi** hay **gợi ý link bên ngoài**?",
                                RawFinishReason = "BadToolArgs"
                            };
                        }

                        switch (call.FunctionName)
                        {
                            case "summarize_lesson":
                                {
                                    var lang = root.TryGetProperty("language", out var lEl) ? lEl.GetString() ?? "vi" : "vi";

                                    if (req.Request.LessionId is not Guid lessonId)
                                        throw new ArgumentException("LessionId is required.", nameof(req));
                                    var currentUserId = _identityService.GetCurrentUser()!.UserId;

                                    var @event = new GetLessonInfoEvent(lessonId, currentUserId);

                                    var response = await requestClient.GetResponse<GetLessonInfoResponse>(@event, ct);
                                    if (string.IsNullOrWhiteSpace(response.Message.Response.TranscriptText))
                                    {
                                        return new ChatResponseDto
                                        {
                                            Reply = "Hiện chưa có transcript cho bài học này nên mình chưa thể tóm tắt. \nBạn vui lòng mở lại bài học để đồng bộ transcript hoặc dán nội dung cần tóm tắt nhé.",
                                            RawFinishReason = "MissingTranscript"
                                        };
                                    }
                                    var json = await RunSummarizeAsync(response.Message.Response.TranscriptText, lang, ct);
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
                                    if (string.IsNullOrWhiteSpace(response.Message.Response.TranscriptText))
                                    {
                                        return new ChatResponseDto
                                        {
                                            Reply = "Hiện chưa có transcript cho bài học này nên mình chưa thể tóm tắt. \nBạn vui lòng mở lại bài học để đồng bộ transcript hoặc dán nội dung cần tóm tắt nhé.",
                                            RawFinishReason = "MissingTranscript"
                                        };
                                    }
                                    var json = await RunExplainAsync(response.Message.Response.TranscriptText, max, simplicity, lang, ct);
                                    messages.Add(new ToolChatMessage(call.Id, json));
                                    break;
                                }

                            case "create_quiz_questions":
                                {
                                    // Đọc language (an toàn)
                                    var lang = (TryGetPropString(root, "language", out var langStr) && !string.IsNullOrWhiteSpace(langStr))
                                        ? langStr! : "vi";

                                    // Lấy mode nếu có
                                    string? mode = null;
                                    if (TryGetPropString(root, "mode", out var m)) mode = m;

                                    // Nếu tool trả về chuỗi trần "ai_generate"/"external_links"
                                    if (mode is null && root.ValueKind == JsonValueKind.String)
                                    {
                                        var s = root.GetString()?.Trim().ToLowerInvariant();
                                        if (s == "ai_generate" || s == "ai") mode = "ai_generate";
                                        else if (s == "external_links" || s == "link") mode = "external_links";
                                    }

                                    // Nếu vẫn chưa có mode → hỏi người dùng (không gọi tool)
                                    if (string.IsNullOrWhiteSpace(mode))
                                    {
                                        return new ChatResponseDto
                                        {
                                            Reply = QuizModeClarificationMessage,
                                            RawFinishReason = "ClarificationNeeded"
                                        };
                                    }

                                    // Lấy lesson_text (an toàn)
                                    string text = "";
                                    if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("lesson_text", out var tEl))
                                        text = tEl.GetString() ?? "";

                                    if (string.IsNullOrWhiteSpace(text))
                                    {
                                        if (req.Request.LessionId is not Guid lessonId)
                                            throw new ArgumentException("LessionId is required.", nameof(req));
                                        var currentUserId = _identityService.GetCurrentUser()!.UserId;
                                        var @event = new GetLessonInfoEvent(lessonId, currentUserId);
                                        var resp = await requestClient.GetResponse<GetLessonInfoResponse>(@event, ct);
                                        if (string.IsNullOrWhiteSpace(resp.Message.Response.TranscriptText))
                                        {
                                            return new ChatResponseDto
                                            {
                                                Reply = "Hiện chưa có transcript cho bài học này nên mình chưa thể tóm tắt. \nBạn vui lòng mở lại bài học để đồng bộ transcript hoặc dán nội dung cần tóm tắt nhé.",
                                                RawFinishReason = "MissingTranscript"
                                            };
                                        }
                                        text = resp.Message.Response.TranscriptText ?? "";
                                    }

                                    if (string.Equals(mode, "external_links", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // ✅ NHÁNH MỚI: hỏi độ khó 1–3 nếu chưa có
                                        int? difficultyLevel = null;
                                        if (root.TryGetProperty("difficulty", out var dEl) && dEl.ValueKind == JsonValueKind.String)
                                            difficultyLevel = MapDifficultyToLevel(dEl.GetString());
                                        else if (root.TryGetProperty("difficulty_level", out var dlEl) && dlEl.ValueKind == JsonValueKind.Number)
                                            difficultyLevel = MapDifficultyToLevel(dlEl.GetRawText());

                                        if (difficultyLevel is null)
                                        {
                                            return new ChatResponseDto
                                            {
                                                Reply =
                                                    "Bạn muốn độ khó nào cho nguồn bài tập trắc nghiệm?\n" +
                                                    "- **1**: Beginner\n- **2**: Intermediate\n- **3**: Advanced\n\n" +
                                                    "Hãy **trả lời bằng số 1, 2 hoặc 3** nhé.",
                                                RawFinishReason = "ClarificationNeeded"
                                            };
                                        }

                                        // 🔢 Lấy page từ external.page (mặc định 1)
                                        int page = 1;
                                        if (root.TryGetProperty("external", out var extEl) &&
                                            extEl.ValueKind == JsonValueKind.Object &&
                                            extEl.TryGetProperty("page", out var pageEl) &&
                                            pageEl.ValueKind == JsonValueKind.Number)
                                        {
                                            page = pageEl.GetInt32();
                                            if (page <= 0) page = 1;
                                        }

                                        // ✅ TRÍCH TOPIC THEO PAGE: mỗi page 3 topic
                                        var multiConceptTopic = await BuildTopicFromTranscriptAsync(
                                            text,
                                            lang,
                                            page,
                                            batchSize: 3,
                                            ct);

                                        if (string.IsNullOrWhiteSpace(multiConceptTopic))
                                        {
                                            // Hết keywords để search
                                            return new ChatResponseDto
                                            {
                                                Reply =
                                                    "Có vẻ tớ đã gợi ý gần hết các chủ đề chính trong bài này rồi nên không tìm thêm được nguồn bài tập mới nữa.\n" +
                                                    "Cậu có thể làm lại những câu hiện tại hoặc đổi sang kiểu luyện tập khác (ví dụ: để tớ tạo câu hỏi bằng AI) nhé.",
                                                RawFinishReason = "NoMoreExternalTopics"
                                            };
                                        }

                                        var md = await _aiSearchService.FindMultipleChoiceExcercises(
                                            multiConceptTopic,
                                            difficultyLevel.Value,
                                            fastModeOverride: null
                                        );

                                        messages.Add(new ToolChatMessage(call.Id, md));
                                    }
                                    else
                                    {
                                        var n = root.TryGetProperty("num_questions", out var nEl) ? nEl.GetInt32() : 5;
                                        var diff = root.TryGetProperty("difficulty", out var dEl) ? dEl.GetString() ?? "medium" : "medium";
                                        var includeAns = root.TryGetProperty("include_answers", out var aEl) && aEl.GetBoolean();
                                        var json = await RunQuizAsync(text, n, diff, includeAns, lang, ct);
                                        messages.Add(new ToolChatMessage(call.Id, json));
                                    }
                                    break;
                                }

                            case "give_real_world_examples":
                                {
                                    var text = root.GetProperty("lesson_text").GetString() ?? "";

                                    // Thử fallback theo LessionId nếu rỗng
                                    if (string.IsNullOrWhiteSpace(text) && req.Request.LessionId is Guid lessonId2)
                                    {
                                        var currentUserId = _identityService.GetCurrentUser()!.UserId;
                                        var @event = new GetLessonInfoEvent(lessonId2, currentUserId);
                                        var resp = await requestClient.GetResponse<GetLessonInfoResponse>(@event, ct);
                                        text = resp.Message.Response.TranscriptText ?? "";
                                    }

                                    // ⛔ Guard cuối
                                    if (string.IsNullOrWhiteSpace(text))
                                    {
                                        return new ChatResponseDto
                                        {
                                            Reply = "Chưa có transcript của bài học nên mình chưa thể đưa ví dụ ứng dụng thực tế. " +
                                            "\nBạn vui lòng cung cấp nội dung bài học nhé.",
                                            RawFinishReason = "MissingTranscript"
                                        };
                                    }

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
                    var raw = completion.Value.Content.Count > 0
                        ? completion.Value.Content[0].Text.ToString()
                        : string.Empty;

                    var text = NormalizeMarkdown(raw);

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

        private async Task<string> RunSummarizeAsync(string lesson, string lang, CancellationToken ct)
        {
            var sys =
                "Return ONLY JSON: {\"markdown\": string}. " +
                $"Language: {lang}. " +
                // Style & hygiene
                "Write a COMPACT, well-structured Markdown summary. Prose must be ≤ 500 words TOTAL; do NOT count code blocks in this limit. " +
                "Rules: use '### ' for headings; use '- ' for bullets; single blank line between blocks; no extra blank lines; no trailing spaces; do not invent facts. " +
                // Sectioning (soft)
                "Create 3–6 NATURAL sections named by you from the lesson content (e.g., Tóm tắt, Khái niệm/Quy tắc, Thực hành, Ví dụ code, Ghi nhớ/Kết luận). " +
                "You MAY merge/skip/rename to fit the content; keep it natural and comprehensive. " +
                // Code examples requirement
                "IF the lesson is about programming or contains code/keywords, you MUST include a section '### Ví dụ code' with 1–3 SHORT, CORRECT code snippets using proper fenced code blocks with language tags (e.g., ```js, ```ts, ```python, ```csharp). " +
                "Each snippet ≤ 8 lines, runnable/realistic, and annotated with 1–2 brief inline comments. Pick topics from the lesson (e.g., if–else chain, guard clause, switch, ternary, strict equality ===, nullish coalescing ??, short-circuit). " +
                "If the lesson is NOT programming-related, instead include 1–3 short real-world examples as bullets in a suitable section. " +
                // Tiny hint (non-prescriptive)
                "Minimal style hint (not mandatory):\n" +
                "### {Tiêu đề ngắn}\n- ...\n\n### {Quy tắc/Khái niệm}\n- ...\n\n### Ví dụ code\n```js\n// ...\n```\n\n### {Ghi nhớ}\n- ...";

            var user = $"Summarize this lesson in adaptive Markdown (≤500 words for prose) and prefer code examples if applicable:\n{lesson}";

            var schema = """
            {
              "type": "object",
              "properties": { "markdown": { "type": "string" } },
              "required": ["markdown"],
              "additionalProperties": false
            }
            """;

            return await RunJsonAsync("summarize_markdown_schema", schema, sys, user, ct);
        }
        /// <summary>
        /// Run prompt key concept and explain
        /// </summary>
        /// <param name="lesson"></param>
        /// <param name="maxConcepts"></param>
        /// <param name="simplicity"></param>
        /// <param name="lang"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        private async Task<string> RunExplainAsync(
            string lesson, int maxConcepts, string simplicity, string lang, CancellationToken ct)
        {
            var primaryCap = Math.Clamp(maxConcepts, 1, 60);

            var sys =
                "Return ONLY JSON: {\"concepts\":[...],\"coverage_terms\":[...]}." +
                $" Language: {lang}. Audience level: {simplicity}. " +
                // Mục tiêu bao phủ
                $"Produce up to {primaryCap} PRIMARY concepts (soft cap). If the lesson has more distinct ideas, " +
                "group the extra ones into a 'related' list under the closest primary concept so that NOTHING is lost. " +
                "Every item named in 'coverage_terms' MUST appear either as a 'term' or inside some concept's 'related'." +
                // Chất lượng diễn giải
                " Keep explanations plain and concrete; 28 words max per 'simple_explanation'; 18 words max per 'example'." +
                // Ví dụ code (nếu là bài lập trình)
                " If the lesson is about programming, add a SHORT code snippet in 'code' (<= 8 lines) for some concepts; " +
                "use an appropriate language tag inside the string (e.g., ```js ... ```). Snippets must be correct and reflect the concept." +
                // Bao phủ đầy đủ
                " 'coverage_terms' must be an exhaustive, deduplicated list of distinct concepts explicitly present in the lesson " +
                "(20–80 items typical). Prefer canonical short names (e.g., if–else, else-if chain, guard clause, short-circuit, " +
                "De Morgan, strict equality ===, truthy/falsy, input validation, edge cases, ternary, switch, fallthrough, grouped cases, " +
                "map/strategy, cyclomatic complexity, naming booleans, ordering by likelihood, ambiguous condition, decision table, " +
                "truth table, role-based switch, fail-fast, invariants, pattern matching, switch expression, logging, unit tests, mocks, etc.). " +
                "Do not invent topics not in the lesson.";

            var user =
                "Extract ALL key concepts from this lesson. " +
                "Make 'coverage_terms' exhaustive; then create primary 'concepts' up to the soft cap, " +
                "grouping any extras into 'related'. Include a short real-world 'example' for each concept, " +
                "and add 'code' when appropriate.\n" + lesson;

            var schema = """
            {
              "type": "object",
              "properties": {
                "concepts": {
                  "type": "array",
                  "items": {
                    "type": "object",
                    "properties": {
                      "term": { "type": "string" },
                      "simple_explanation": { "type": "string" },
                      "example": { "type": "string" },
                      "related": { "type": "array", "items": { "type": "string" } },
                      "code": { "type": "string" }
                    },
                    "required": ["term", "simple_explanation", "example"],
                    "additionalProperties": false
                  }
                },
                "coverage_terms": {
                  "type": "array",
                  "items": { "type": "string" },
                  "minItems": 20,
                  "maxItems": 80
                }
              },
              "required": ["concepts", "coverage_terms"],
              "additionalProperties": false
            }
            """;

            return await RunJsonAsync("concepts_schema_v2", schema, sys, user, ct);
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

            if (res.Value.Content.Count > 0)
            {
                var s = res.Value.Content[0].Text.ToString();
                return string.IsNullOrWhiteSpace(s) ? "{}" : s.Trim();
            }
            return "{}";
        }
    }
}
