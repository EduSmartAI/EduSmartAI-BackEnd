using System.Text.Json;
using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using OpenAI;
using OpenAI.Chat;

namespace AiService.Infrastructure.Implements;

public class SurveyAnalysisService : ISurveyAnalysis
{
    private readonly OpenAIClient _openAiClient;

    public SurveyAnalysisService(OpenAIClient openAiClient)
    {
        _openAiClient = openAiClient;
    }

    public async Task<StudentInterestSurveyAnalysisEventResponse> AnalyzeStudentInterestSurveyAsync(
        StudentInterestSurveyAnalysisEvent request, 
        CancellationToken cancellationToken = default)
    {
        var response = new StudentInterestSurveyAnalysisEventResponse { Success = false };

        try
        {
            var prompt = BuildAnalysisPrompt(request);

            var chatClient = _openAiClient.GetChatClient("gpt-4o-mini");
            
            var chatCompletion = await chatClient.CompleteChatAsync(new ChatMessage[]
                {
                    new SystemChatMessage("Bạn là một chuyên gia tư vấn định hướng nghề nghiệp và học tập. Nhiệm vụ của bạn là phân tích câu trả lời khảo sát sở thích của học sinh và đưa ra định hướng học tập phù hợp."),
                    new UserChatMessage(prompt)
                },
                new ChatCompletionOptions
                {
                    Temperature = 0.3f,
                    ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat()
                },
                cancellationToken);

            var result = ParseAiResponse(chatCompletion.Value.Content[0].Text);
            
            response.Success = true;
            response.Response = result;
            response.SetMessage(MessageId.I00001, "Phân tích khảo sát");
        }
        catch (Exception ex)
        {
            response.SetMessage("Error", $"Lỗi khi phân tích khảo sát: {ex.Message}");
        }

        return response;
    }

    private string BuildAnalysisPrompt(StudentInterestSurveyAnalysisEvent request)
    {
        var promptBuilder = new System.Text.StringBuilder();
        
        promptBuilder.AppendLine("Hãy phân tích các câu trả lời khảo sát sở thích của học sinh dưới đây và đưa ra định hướng học tập phù hợp:");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== THÔNG TIN KHẢO SÁT ===");
        promptBuilder.AppendLine($"ID Học sinh: {request.StudentId}");
        promptBuilder.AppendLine();

        foreach (var question in request.Questions)
        {
            promptBuilder.AppendLine($"Câu hỏi: {question.QuestionText}");
            promptBuilder.AppendLine("Câu trả lời của học sinh:");
            foreach (var answer in question.StudentAnswers)
            {
                promptBuilder.AppendLine($"- {answer}");
            }
            promptBuilder.AppendLine();
        }

        promptBuilder.AppendLine("=== YÊU CẦU PHÂN TÍCH ===");
        promptBuilder.AppendLine("Dựa trên các câu trả lời trên, hãy:");
        promptBuilder.AppendLine("1. Phân tích sở thích, năng lực và xu hướng của học sinh");
        promptBuilder.AppendLine("2. Đưa ra 1 định hướng học tập chính phù hợp nhất (LearningGoal)");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== ĐỊNH DẠNG TRUYỀN VỀ ===");
        promptBuilder.AppendLine("Vui lòng trả về kết quả dưới định dạng JSON với cấu trúc sau:");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"LearningGoal\": \"[Tên định hướng học tập được chọn]\",");
        promptBuilder.AppendLine("}");

        return promptBuilder.ToString();
    }

    private StudentInterestAnalysisResult ParseAiResponse(string aiResponse)
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        return JsonSerializer.Deserialize<StudentInterestAnalysisResult>(aiResponse, options);
    }
}
