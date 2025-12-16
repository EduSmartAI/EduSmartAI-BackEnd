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
        
        promptBuilder.AppendLine("Bạn là một Chuyên gia Tư vấn Hướng nghiệp cấp cao trong lĩnh vực Công nghệ thông tin (IT).");
        promptBuilder.AppendLine("Nhiệm vụ của bạn là phân tích dữ liệu khảo sát của học sinh để tìm ra chuyên ngành IT phù hợp nhất.");
        promptBuilder.AppendLine("QUAN TRỌNG: Bất kể sở thích của học sinh là gì, hãy tìm mối liên hệ của nó với kỹ năng công nghệ và đề xuất một lộ trình IT tương ứng.");
        
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== DỮ LIỆU ĐẦU VÀO ===");
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

        promptBuilder.AppendLine("=== HƯỚNG DẪN SUY LUẬN (MAPPING GUIDE) ===");
        promptBuilder.AppendLine("Hãy sử dụng logic sau để ánh xạ sở thích sang chuyên ngành IT:");
        promptBuilder.AppendLine("- Thích cái đẹp, vẽ, nghệ thuật, màu sắc -> Gợi ý: Frontend Development, UI/UX Design.");
        promptBuilder.AppendLine("- Thích giải đố, logic, toán học, quy trình -> Gợi ý: Backend Development, Data Science, AI/Machine Learning.");
        promptBuilder.AppendLine("- Thích giao tiếp, lãnh đạo, kinh doanh -> Gợi ý: Business Analyst (BA), Project Management, Product Owner.");
        promptBuilder.AppendLine("- Thích sự tỉ mỉ, soi lỗi, kiểm tra -> Gợi ý: Software Testing (QC/QA).");
        promptBuilder.AppendLine("- Thích phần cứng, lắp ráp, mạng lưới -> Gợi ý: DevOps, Network Engineering, IoT.");
        promptBuilder.AppendLine("- Thích bảo vệ, điều tra, bí ẩn -> Gợi ý: Cyber Security.");

        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== YÊU CẦU ĐẦU RA ===");
        promptBuilder.AppendLine("1. LearningGoal BẮT BUỘC phải là tên một chuyên ngành hoặc vị trí trong ngành IT.");
        promptBuilder.AppendLine("2. Phân tích ngắn gọn lý do tại sao sở thích đó lại phù hợp với chuyên ngành IT này.");
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("=== ĐỊNH DẠNG JSON ===");
        promptBuilder.AppendLine("Chỉ trả về chuỗi JSON thuần (không kèm Markdown ```json), theo cấu trúc:");
        promptBuilder.AppendLine("{");
        promptBuilder.AppendLine("  \"LearningGoal\": \"[Tên chuyên ngành IT - Ví dụ: ReactJS Web Development, Data Analyst, v.v.]\",");
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
