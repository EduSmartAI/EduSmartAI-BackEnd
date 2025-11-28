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

        var curriculumLookup = curriculumListRaw
            .Where(x => !string.IsNullOrWhiteSpace(x.subjectCode))
            .ToDictionary(
                x => x.subjectCode,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var markLookup = subjectMarkList
            .Where(x => !string.IsNullOrWhiteSpace(x.subjectCode))
            .ToDictionary(
                x => x.subjectCode,
                x => x.mark,
                StringComparer.OrdinalIgnoreCase);

        var dependentsLookup = BuildDependentsLookup(curriculumListRaw);

        var subjectList = subjectMarkList.Select(s =>
        {
            curriculumLookup.TryGetValue(s.subjectCode, out var subjectInfo);
            var canonicalName = subjectInfo != null && !string.IsNullOrWhiteSpace(subjectInfo.subjectName)
                ? subjectInfo.subjectName
                : s.subjectName;

            var prereqDetails = (subjectInfo?.subjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code =>
                {
                    curriculumLookup.TryGetValue(code, out var prereqInfo);
                    var name = prereqInfo == null
                        ? code
                        : string.IsNullOrWhiteSpace(prereqInfo.subjectName) ? code : prereqInfo.subjectName;
                    return new
                    {
                        subjectCode = code,
                        subjectName = name,
                        mark = markLookup.TryGetValue(code, out var prereqMark) ? prereqMark : (int?)null,
                        semesterIndex = prereqInfo?.index
                    } as object;
                })
                .ToList();

            dependentsLookup.TryGetValue(s.subjectCode, out var dependents);
            var dependentDetails = dependents?.Select(dep => new
            {
                subjectCode = dep.subjectCode,
                subjectName = dep.subjectName,
                semesterIndex = dep.semesterIndex
            } as object).ToList() ?? new List<object>();

            var dependentWarningTexts = dependents?.Select(dep =>
            {
                var semesterLabel = FormatSemesterLabel(dep.semesterIndex);
                var scope = semesterLabel == null ? "các kỳ sau" : semesterLabel.ToLowerInvariant();
                return $"Điểm thấp ở {canonicalName} → {dep.subjectName} ({dep.subjectCode}) {scope} dễ hụt chuẩn.";
            }).ToList() ?? new List<string>();

            return new
            {
                s.subjectCode,
                subjectName = canonicalName,
                s.mark,
                semesterIndex = subjectInfo?.index,
                prerequisites = prereqDetails,
                dependents = dependentDetails,
                dependentWarnings = dependentWarningTexts
            };
        }).ToList();

        var curriculumScopeCodes = CollectCurriculumScope(subjectMarkList, curriculumLookup, dependentsLookup);

        var curriculumList = curriculumListRaw
            .Where(s => curriculumScopeCodes.Contains(s.subjectCode))
            .Select(s => new
            {
                s.subjectCode,
                s.subjectName,
                index = s.index,
                prerequisites = s.subjectPrerequisiteCode ?? new List<string>()
            }).ToList();

        var dependencyEdges = curriculumListRaw
            .Where(subject => curriculumScopeCodes.Contains(subject.subjectCode))
            .SelectMany(subject => (subject.subjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(prerequisite =>
                {
                    curriculumLookup.TryGetValue(prerequisite, out var prereqInfo);
                    return new
                    {
                        prerequisite,
                        prerequisiteIndex = prereqInfo?.index,
                        dependentCode = subject.subjectCode,
                        dependentName = string.IsNullOrWhiteSpace(subject.subjectName)
                            ? subject.subjectCode
                            : subject.subjectName,
                        dependentIndex = subject.index
                    };
                }))
            .Where(edge => curriculumScopeCodes.Contains(edge.prerequisite) && curriculumScopeCodes.Contains(edge.dependentCode))
            .ToList();

        var payload = new
        {
            subjectMarks = subjectList,
            curriculum = new { subjects = curriculumList },
            dependencyEdges,
            careerGoal
        };

        var json = Serialize(payload);

        //Console.WriteLine(json);

        return $$"""
DỮ LIỆU MÔN HỌC VÀ CHƯƠNG TRÌNH:
```json
{{json}}
```

NHIỆM VỤ:
- Phân loại từng môn theo thang 0-100 và viết rõ ràng: tình hình, kiến thức trọng tâm cần bù, **liên kết tiền đề** (nếu có trong `prerequisites`), cảnh báo ảnh hưởng đến môn kế tiếp (dựa trên `dependents`), kế hoạch hành động **2–4 tuần**.
- `semesterIndex` thể hiện kỳ học (1 = kỳ 1, 2 = kỳ 2...). Trong `### Tình hình`, mở đầu bằng câu nêu rõ môn thuộc kỳ nào (nếu có dữ liệu) rồi mới đến đánh giá điểm.
- `prerequisites` chính là các môn PHẢI hoàn thành tốt trước khi học môn hiện tại. Nếu điểm < 70 hoặc chưa có điểm, phải cảnh báo trực tiếp trong phần của môn hiện tại (ví dụ: “PRO192 phụ thuộc PRF192 đang thấp nên cần ôn lại”) và đề xuất cách củng cố trước khi tiếp tục.
- Mỗi bullet trong `### Liên kết tiền đề` phải viết đúng mẫu:
  `- Ràng buộc {tên} ({mã}){nếu có kỳ → " – Kỳ {index}"}: điểm {x}/100 – nhận xét về việc nên củng cố/duy trì để hỗ trợ môn hiện tại`
  (nếu không có điểm → dùng “chưa có điểm – cần hoàn thành ...”).
- Trường `dependents` trong từng môn liệt kê CHÍNH XÁC các môn bị ảnh hưởng khi điểm hiện tại thấp. Chỉ tạo cảnh báo dựa trên danh sách này, nêu rõ kỳ (`semesterIndex`) của từng môn phụ thuộc. Nếu điểm hiện tại < 70, bắt buộc liệt kê từng phần trong `dependents`.
- `dependentWarnings` là danh sách câu văn đã chuẩn hoá cho từng phụ thuộc. Khi viết `### Tình hình` và đặc biệt là `### Cảnh báo`, **phải** chép nguyên văn từng câu (mỗi câu một bullet). Không được bỏ sót câu nào khi mảng này không rỗng.
- Tuyệt đối không suy đoán thêm mối quan hệ ngoài dữ liệu được cung cấp. Nếu danh sách rỗng thì ghi rõ “—” hoặc “Không có”.
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
- Luôn có `### Tình hình`, `### Kiến thức trọng tâm`, `### Liên kết tiền đề` (khi có `prerequisites`), `### Lộ trình 2–4 tuần`.
- Heading `### Cảnh báo` bắt buộc xuất hiện khi `dependents` hoặc `dependentWarnings` không rỗng; mỗi bullet phải lặp lại đúng câu trong `dependentWarnings` (có thể bổ sung thêm nhấn mạnh nhưng không được bỏ câu).
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

    private static HashSet<string> CollectCurriculumScope(
        IEnumerable<SubjectMark> subjects,
        Dictionary<string, SubjectCur> curriculumLookup,
        Dictionary<string, List<(string subjectCode, string subjectName, int? semesterIndex)>> dependentsLookup)
    {
        var scope = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var subject in subjects ?? Enumerable.Empty<SubjectMark>())
        {
            if (string.IsNullOrWhiteSpace(subject.subjectCode)) continue;

            scope.Add(subject.subjectCode);

            if (curriculumLookup.TryGetValue(subject.subjectCode, out var subjectInfo))
            {
                foreach (var prereq in subjectInfo.subjectPrerequisiteCode ?? Enumerable.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(prereq))
                    {
                        scope.Add(prereq);
                    }
                }
            }

            if (dependentsLookup.TryGetValue(subject.subjectCode, out var dependents))
            {
                foreach (var dependent in dependents)
                {
                    if (!string.IsNullOrWhiteSpace(dependent.subjectCode))
                    {
                        scope.Add(dependent.subjectCode);
                    }
                }
            }
        }

        return scope;
    }

    private static Dictionary<string, List<(string subjectCode, string subjectName, int? semesterIndex)>> BuildDependentsLookup(IEnumerable<SubjectCur> subjects)
    {
        var map = new Dictionary<string, List<(string subjectCode, string subjectName, int? semesterIndex)>>(StringComparer.OrdinalIgnoreCase);

        foreach (var subject in subjects ?? Enumerable.Empty<SubjectCur>())
        {
            foreach (var prerequisite in subject.subjectPrerequisiteCode ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(prerequisite)) continue;

                if (!map.TryGetValue(prerequisite, out var list))
                {
                    list = new List<(string subjectCode, string subjectName, int? semesterIndex)>();
                    map[prerequisite] = list;
                }

                var dependentName = string.IsNullOrWhiteSpace(subject.subjectName)
                    ? subject.subjectCode
                    : subject.subjectName;
                var semesterIndex = subject.index > 0 ? subject.index : (int?)null;

                if (!list.Any(dep => dep.subjectCode.Equals(subject.subjectCode, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add((subject.subjectCode, dependentName, semesterIndex));
                }
            }
        }

        return map;
    }

    private static string? FormatSemesterLabel(int? semesterIndex) =>
        semesterIndex.HasValue && semesterIndex.Value > 0
            ? $"Kỳ {semesterIndex.Value}"
            : null;
}

