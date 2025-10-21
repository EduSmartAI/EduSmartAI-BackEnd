using AiService.Application.DTOs;
using AiService.Application.Features.AIChatBot;
using AiService.Application.Interfaces;
using OpenAI.Chat;
using System.Text.Json;

namespace AiService.Infrastructure.Implements
{
    public class ChatBotService : IChatBotService
    {
        private readonly ChatClient _chat;

        // system prompt giống Python
        private const string SystemMessage =
            "You are a helpful assistant for an Airline called FlightAI. " +
            "Give short, courteous answers, no more than 1 sentence. " +
            "Always be accurate. If you don't know the answer, say so.";

        // Định nghĩa tool schema cho function calling
        private static readonly ChatTool TicketPriceTool = ChatTool.CreateFunctionTool(
            functionName: "get_ticket_price",
            functionDescription: "Get the price of a return ticket to the destination city. Call this whenever you need to know the ticket price, for example when a customer asks 'How much is a ticket to this city'",
            functionParameters: BinaryData.FromBytes("""
        {
          "type": "object",
          "properties": {
            "destination_city": {
              "type": "string",
              "description": "The city that the customer wants to travel to"
            }
          },
          "required": ["destination_city"],
          "additionalProperties": false
        }
        """u8.ToArray())
        );

        public ChatBotService(ChatClient chat)
        {
            _chat = chat;
        }

        public async Task<ChatResponseDto> ChatAsync(AIChatBotRequest req, CancellationToken ct = default)
        {
            // Chuẩn bị messages: system + history + user
            var messages = new List<ChatMessage>
        {
            new SystemChatMessage(SystemMessage)
        };

            if (req.Request.History is not null)
            {
                foreach (var m in req.Request.History)
                {
                    messages.Add(m.Role?.ToLowerInvariant() switch
                    {
                        "assistant" => new AssistantChatMessage(m.Content ?? string.Empty),
                        "system" => new SystemChatMessage(m.Content ?? string.Empty),
                        _ => new UserChatMessage(m.Content ?? string.Empty) // mặc định là user
                    });
                }
            }

            messages.Add(new UserChatMessage(req.Request.Message ?? string.Empty));

            var options = new ChatCompletionOptions
            {
                Tools = { TicketPriceTool }
            };

            // Vòng lặp xử lý tool-calls giống Python: call -> nếu cần tool thì gọi -> append tool result -> call lại
            while (true)
            {
                var completion = await _chat.CompleteChatAsync(messages, options, ct);

                if (completion.Value.FinishReason == ChatFinishReason.ToolCalls)
                {
                    // Lưu assistant msg có tool_calls
                    messages.Add(new AssistantChatMessage(completion));

                    foreach (var call in completion.Value.ToolCalls)
                    {
                        if (call.FunctionName == "get_ticket_price")
                        {
                            using var argsJson = JsonDocument.Parse(call.FunctionArguments);
                            if (!argsJson.RootElement.TryGetProperty("destination_city", out var cityEl))
                                throw new ArgumentException("destination_city is required by get_ticket_price");

                            var city = cityEl.GetString() ?? string.Empty;
                            var price = GetTicketPrice(city);

                            // Nội dung tool message trả về JSON y như Python
                            var toolResult = JsonSerializer.Serialize(new
                            {
                                destination_city = city,
                                price
                            });

                            messages.Add(new ToolChatMessage(call.Id, toolResult));
                        }
                        else
                        {
                            // Nếu model gọi function lạ (không có), fail-fast
                            throw new NotImplementedException($"Unknown tool: {call.FunctionName}");
                        }
                    }

                    // quay lại vòng lặp để model tạo câu trả lời cuối
                    continue;
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

                // Một số tình huống khác (hiếm)
                throw new InvalidOperationException($"FinishReason: {completion.Value.FinishReason}");
            }
        }

        /// <summary>
        /// Price Temp
        /// </summary>
        private static readonly Dictionary<string, string> _prices = new(StringComparer.OrdinalIgnoreCase)
        {
            ["london"] = "$799",
            ["paris"] = "$899",
            ["tokyo"] = "$1400",
            ["berlin"] = "$499"
        };

        public static string GetTicketPrice(string destinationCity)
            => _prices.TryGetValue(destinationCity?.Trim() ?? "", out var price) ? price : "Unknown";
    }
}
