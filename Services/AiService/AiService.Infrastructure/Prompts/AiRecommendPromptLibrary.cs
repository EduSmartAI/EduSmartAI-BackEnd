using AiService.Application.Features.AiRecommend;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AiService.Infrastructure.Prompts;

internal static class AiRecommendPromptLibrary
{
    public static readonly string[] AbilityLabels =
    [
        "Lập trình hướng đối tượng",
        "Lập trình web HTML/CSS cơ bản",
        "Cấu trúc dữ liệu và giải thuật",
        "Cơ sở dữ liệu"
    ];

    private static readonly JsonSerializerOptions PromptJsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public const string SystemPrompt = """
Bạn là cố vấn học tập bậc đại học. Luôn trả về duy nhất **một** JSON hợp lệ theo schema mà user prompt yêu cầu. Không bao giờ bao quanh JSON bằng ``` hoặc thêm chú thích bên ngoài.
- Tất cả nội dung phải viết bằng tiếng Việt có dấu, ngắn gọn nhưng giàu dẫn chứng.
- Các chuỗi có thể dùng Markdown (###, bullet -) để trình bày rõ ràng.
- Nếu dữ liệu thiếu, hãy ghi chú "chưa có đủ dữ liệu" thay vì suy đoán.
""";

    public static string BuildAbilityPrompt(IEnumerable<AbilityMark> abilityMarks, string careerGoal)
    {
        var abilityEntries = (abilityMarks ?? Enumerable.Empty<AbilityMark>())
            .Select(m => new { m.name, m.mark })
            .ToList();

        var payload = new
        {
            abilityMarks = abilityEntries,
            expectedAbilities = AbilityLabels,
            markScale = "0-100",
            careerGoal = careerGoal
        };

        var json = Serialize(payload);

        return $$"""
DỮ LIỆU NĂNG LỰC:
```json
{{json}}
```

NHIỆM VỤ:
- Đánh giá từng khả năng lập trình theo thang 0-100 và liên hệ trực tiếp với mục tiêu nghề nghiệp `careerGoal` (nếu có).
- Với mỗi khả năng, mô tả rõ:
  1. Tình trạng hiện tại.
  2. Kiến thức/nền tảng cần củng cố (nêu ví dụ cụ thể).
  3. Lộ trình hành động **2–4 tuần** với số buổi/bài cụ thể.
- Nếu thiếu điểm cho một khả năng, hãy ghi chú "chưa có đủ dữ liệu" và đề xuất cách xây dựng nền tảng.

OUTPUT JSON (không thêm văn bản khác):
{
  "abilityAnalyses": [
    {
      "name": "<trùng tên khả năng>",
      "analysisMarkdown": "## <Tên khả năng>\n### Hiện trạng\n- ...\n### Kiến thức trọng tâm\n- ...\n### Lộ trình 2–4 tuần\n- ... (nêu rõ khối lượng luyện tập và liên hệ careerGoal)"
    }
  ]
}

YÊU CẦU ĐỊNH DẠNG:
- Luôn mở đầu `analysisMarkdown` bằng `## <Tên khả năng>`.
- Bắt buộc đủ 3 heading `### Hiện trạng`, `### Kiến thức trọng tâm`, `### Lộ trình 2–4 tuần`.
- Mỗi heading có 2-3 bullet, bắt đầu bằng động từ, ghi rõ khối lượng (vd: “Ôn 3 buổi/tuần”, “Làm 5 bài/tuần”).
""";
    }

    public static string BuildSubjectPrompt(
        IEnumerable<SubjectMark> subjectMarks,
        IEnumerable<SubjectCur> curriculumSubjects,
        string careerGoal)
    {
        var subjectMarkList = (subjectMarks ?? Enumerable.Empty<SubjectMark>()).ToList();
        var curriculumListRaw = (curriculumSubjects ?? Enumerable.Empty<SubjectCur>()).ToList();

        var subjectList = subjectMarkList.Select(s => new
        {
            s.subjectCode,
            s.subjectName,
            s.mark
        }).ToList();

        var curriculumList = curriculumListRaw.Select(s => new
        {
            s.subjectCode,
            s.subjectName,
            prerequisites = s.sụbjectPrerequisiteCode ?? new List<string>()
        }).ToList();

        var dependencyEdges = curriculumListRaw
            .SelectMany(subject => (subject.sụbjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(prerequisite => new
                {
                    prerequisite,
                    dependentCode = subject.subjectCode,
                    dependentName = string.IsNullOrWhiteSpace(subject.subjectName)
                        ? subject.subjectCode
                        : subject.subjectName
                }))
            .ToList();

        var payload = new
        {
            subjectMarks = subjectList,
            curriculum = new { subjects = curriculumList },
            dependencyEdges,
            careerGoal
        };

        var json = Serialize(payload);

        return $$"""
DỮ LIỆU MÔN HỌC VÀ CHƯƠNG TRÌNH:
```json
{{json}}
```

NHIỆM VỤ:
- Phân loại từng môn theo thang 0-100 và viết rõ ràng: tình hình, kiến thức trọng tâm cần bù, cảnh báo phụ thuộc (nếu có), kế hoạch hành động **2–4 tuần**.
- Khi `dependencyEdges` cho thấy môn đang là tiên quyết và điểm < 70, phải tạo cảnh báo “⚠️ ...”.
- Luôn liên hệ với `careerGoal` (nếu có) để giải thích vì sao môn này quan trọng hoặc nên ưu tiên.

OUTPUT JSON:
{
  "subjectAnalyses": [
    {
      "subjectCode": "...",
      "subjectName": "...",
      "analysisMarkdown": "## <Tên môn> (<Mã>)\n### Tình hình\n- ...\n### Kiến thức trọng tâm\n- ...\n### Cảnh báo (nếu có)\n- ...\n### Lộ trình 2–4 tuần\n- ... (mỗi bullet nêu số buổi/bài, kiến thức phải nắm, liên hệ careerGoal)"
    }
  ]
}

QUY TẮC ĐỊNH DẠNG:
- `analysisMarkdown` phải mở đầu bằng `## <Tên môn> (<Mã>)`.
- Luôn có `### Tình hình`, `### Kiến thức trọng tâm`, `### Lộ trình 2–4 tuần`.
- Heading `### Cảnh báo` chỉ xuất hiện khi có phụ thuộc hoặc điểm < 70.
- Bullet cần viện dẫn thẳng tên môn hoặc kỹ năng để người học dễ áp dụng.
""";
    }

    public static string BuildPersonaPrompt(
        IEnumerable<SubjectMark> subjectMarks,
        IEnumerable<AbilityMark> abilityMarks,
        QuizSurvey quizSurvey,
        string careerGoal)
    {
        quizSurvey ??= new QuizSurvey();

        var subjectMarkList = (subjectMarks ?? Enumerable.Empty<SubjectMark>()).ToList();
        var abilityMarkList = (abilityMarks ?? Enumerable.Empty<AbilityMark>()).ToList();

        var subjectList = subjectMarkList.Select(s => new
        {
            s.subjectCode,
            s.subjectName,
            s.mark
        }).ToList();

        var abilityList = abilityMarkList.Select(a => new { a.name, a.mark }).ToList();

        var stats = new
        {
            avgSubject = subjectMarkList.Count > 0 ? subjectMarkList.Average(s => s.mark) : 0,
            avgAbility = abilityMarkList.Count > 0 ? abilityMarkList.Average(a => a.mark) : 0,
            strongSubjects = subjectMarkList.Where(s => s.mark >= 80).Select(s => s.subjectName).ToList(),
            weakSubjects = subjectMarkList.Where(s => s.mark < 65).Select(s => s.subjectName).ToList(),
            strongAbilities = abilityMarkList.Where(a => a.mark >= 80).Select(a => a.name).ToList(),
            weakAbilities = abilityMarkList.Where(a => a.mark < 65).Select(a => a.name).ToList()
        };

        var surveyPayload = new
        {
            quizSurvey.quizHabits,
            quizSurvey.quizInterests
        };

        var payload = new
        {
            subjectMarks = subjectList,
            abilityMarks = abilityList,
            survey = surveyPayload,
            stats,
            careerGoal
        };

        var json = Serialize(payload);

        return $$"""
DỮ LIỆU TỔNG HỢP:
```json
{{json}}
```

NHIỆM VỤ:
- Viết 4 đoạn mô tả bằng tiếng Việt, mỗi đoạn bắt đầu bằng tiêu đề `##` và kết thúc bằng gợi ý hành động cụ thể (ưu tiên tầm 2–4 tuần).
- Dùng dữ liệu môn học + năng lực + khảo sát để soi chiếu tính cách học tập, thói quen, năng lực tiếp thu và chỉ rõ người học nên làm gì để tiến gần `careerGoal`.
- Nhấn mạnh các môn/khả năng nổi bật và liệt kê tối đa 2 ưu tiên cải thiện rõ ràng, đo được.

OUTPUT JSON:
{
  "summaryFeedback": "<Markdown 3-4 câu tổng kết kết quả học tập>",
  "habitAndInterestAnalysis": "<Markdown 2-3 câu kết nối quizHabits + quizInterests>",
  "personality": "<Đoạn văn mô tả phong cách/tính cách học tập>",
  "learningAbility": "<Đánh giá năng lực học và tốc độ bắt kịp kiến thức kèm khuyến nghị>"
}

LƯU Ý:
- Mỗi chuỗi phải mở đầu bằng tiêu đề `##`.
- Dẫn chứng bằng tên môn hoặc khả năng cụ thể thay vì nói chung chung.
- Nếu khảo sát thiếu câu trả lời, ghi chú rõ và đề xuất 1 hành động để bổ sung dữ liệu.
- Mỗi đoạn phải đề cập tới kế hoạch hành động (ít nhất 2 tuần) để tiến gần mục tiêu.
""";
    }

    private static string Serialize(object payload) => JsonSerializer.Serialize(payload, PromptJsonOptions);
}

