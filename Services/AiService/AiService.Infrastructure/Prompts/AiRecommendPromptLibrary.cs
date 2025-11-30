using AiService.Application.Features.AiRecommend;
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

    public static string BuildAbilityPrompt(IEnumerable<AbilityMark>? abilityMarks, string careerGoal)
    {
        var abilityEntries = (abilityMarks ?? [])
            .Select(m => new { name = m.Name, mark = m.Mark })
            .ToList();

        var payload = new
        {
            abilityMarks = abilityEntries,
            expectedAbilities = AbilityLabels,
            markScale = "0-10",
            careerGoal = careerGoal
        };

        var json = Serialize(payload);

        return $$"""
DỮ LIỆU NĂNG LỰC:
```json
{{json}}
```

NHIỆM VỤ:
- Nếu `abilityMarks` rỗng hoặc null: Điều này có nghĩa là sinh viên đã ở kỳ 5 trở lên và đã có bảng điểm môn học đầy đủ, không cần đánh giá năng lực cơ bản nữa. 
  Trong trường hợp này, trả về JSON với thông báo rằng sinh viên đã vượt qua giai đoạn đánh giá năng lực cơ bản, và hệ thống sẽ phân tích dựa trên **kết quả học tập thực tế từ bảng điểm môn học**.
- Nếu có `abilityMarks`: Đánh giá từng khả năng lập trình theo thang 0-10 và liên hệ trực tiếp với mục tiêu nghề nghiệp `careerGoal` (nếu có).
  1. Tình trạng hiện tại (điểm/10).
  2. Kiến thức/nền tảng cần củng cố (nêu ví dụ cụ thể).
  3. Lộ trình hành động **2–4 tuần** với số buổi/bài cụ thể.

OUTPUT JSON (không thêm văn bản khác):
{
  "abilityAnalyses": [
    {
      "name": "<trùng tên khả năng HOẶC 'Phân tích dựa trên bảng điểm' nếu null>",
      "analysisMarkdown": "## <Tên khả năng>\n### Hiện trạng\n- ...\n### Kiến thức trọng tâm\n- ...\n### Lộ trình 2–4 tuần\n- ... (nêu rõ khối lượng luyện tập và liên hệ careerGoal)"
    }
  ]
}

LƯU Ý ĐẶC BIỆT:
- Nếu `abilityMarks` null/rỗng, trả về 1 phần tử duy nhất với `name: "Phân tích dựa trên bảng điểm"` và nội dung giải thích rằng sinh viên đã có đủ bảng điểm môn học, không cần đánh giá năng lực cơ bản riêng.

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
        var scoredSubjects = subjectMarkList.Where(s => s.Mark.HasValue).ToList();
        var curriculumListRaw = (curriculumSubjects ?? Enumerable.Empty<SubjectCur>()).ToList();

        var curriculumLookup = curriculumListRaw
            .Where(x => !string.IsNullOrWhiteSpace(x.SubjectCode))
            .ToDictionary(
                x => x.SubjectCode,
                x => x,
                StringComparer.OrdinalIgnoreCase);

        var markLookup = subjectMarkList
            .Where(x => !string.IsNullOrWhiteSpace(x.SubjectCode))
            .ToDictionary(
                x => x.SubjectCode,
                x => x.Mark,
                StringComparer.OrdinalIgnoreCase);

        var dependentsLookup = BuildDependentsLookup(curriculumListRaw);

        var subjectList = scoredSubjects.Select(s =>
        {
            curriculumLookup.TryGetValue(s.SubjectCode, out var subjectInfo);
            var canonicalName = subjectInfo != null && !string.IsNullOrWhiteSpace(subjectInfo.SubjectName)
                ? subjectInfo.SubjectName
                : s.SubjectName;

            var prereqDetails = (subjectInfo?.SubjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code =>
                {
                    curriculumLookup.TryGetValue(code, out var prereqInfo);
                    var name = prereqInfo == null
                        ? code
                        : string.IsNullOrWhiteSpace(prereqInfo.SubjectName) ? code : prereqInfo.SubjectName;
                    return new
                    {
                        subjectCode = code,
                        subjectName = name,
                        mark = markLookup.TryGetValue(code, out var prereqMark) ? prereqMark : (int?)null,
                        semesterIndex = prereqInfo?.Index
                    } as object;
                })
                .ToList();

            dependentsLookup.TryGetValue(s.SubjectCode, out var dependents);
            var dependentDetails = dependents?.Select(dep => new
            {
                subjectCode = dep.subjectCode,
                subjectName = dep.subjectName,
                semesterIndex = dep.semesterIndex
            } as object).ToList() ?? new List<object>();

            var dependentWarningTexts = dependents?.Select(dep =>
            {
                var semesterLabel = FormatSemesterLabel(dep.semesterIndex);
                var scopeSuffix = semesterLabel == null ? "trong các kỳ sau" : $"ở {semesterLabel}";
                return $"Điểm thấp ở {canonicalName} → {dep.subjectName} ({dep.subjectCode}) {scopeSuffix} có thể ảnh hưởng đến kết quả sau cùng; cần củng cố sớm.";
            }).ToList() ?? new List<string>();

            return new
            {
                subjectCode = s.SubjectCode,
                subjectName = canonicalName,
                mark = s.Mark!.Value,
                semesterIndex = subjectInfo?.Index,
                prerequisites = prereqDetails,
                dependents = dependentDetails,
                dependentWarnings = dependentWarningTexts
            };
        }).ToList();

        var curriculumScopeCodes = CollectCurriculumScope(scoredSubjects, curriculumLookup, dependentsLookup);

        var curriculumList = curriculumListRaw
            .Where(s => curriculumScopeCodes.Contains(s.SubjectCode))
            .Select(s => new
            {
                subjectCode = s.SubjectCode,
                subjectName = s.SubjectName,
                index = s.Index,
                prerequisites = s.SubjectPrerequisiteCode ?? new List<string>()
            }).ToList();

        var dependencyEdges = curriculumListRaw
            .Where(subject => curriculumScopeCodes.Contains(subject.SubjectCode))
            .SelectMany(subject => (subject.SubjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(prerequisite =>
                {
                    curriculumLookup.TryGetValue(prerequisite, out var prereqInfo);
                    return new
                    {
                        prerequisite,
                        prerequisiteIndex = prereqInfo?.Index,
                        dependentCode = subject.SubjectCode,
                        dependentName = string.IsNullOrWhiteSpace(subject.SubjectName)
                            ? subject.SubjectCode
                            : subject.SubjectName,
                        dependentIndex = subject.Index
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
- Phân loại từng môn theo thang 0-10 và viết rõ ràng: tình hình, kiến thức trọng tâm cần bù, **liên kết tiền đề** (nếu có trong `prerequisites`), cảnh báo ảnh hưởng đến môn kế tiếp (dựa trên `dependents`), kế hoạch hành động **2–4 tuần**.
- `semesterIndex` thể hiện kỳ học (1 = kỳ 1, 2 = kỳ 2...). Trong `### Tình hình`, mở đầu bằng câu nêu rõ môn học nào (nếu có dữ liệu) rồi mới đến đánh giá điểm.
- `prerequisites` chính là các môn PHẢI hoàn thành tốt trước khi học môn hiện tại. Nếu điểm < 7.0 hoặc chưa có điểm, phải cảnh báo trực tiếp trong phần của môn hiện tại (ví dụ: "PRO192 phụ thuộc PRF192 đang thấp nên cần ôn lại") và đề xuất cách củng cố trước khi tiếp tục.
- Mỗi bullet trong `### Môn nền tảng quan trọng` phải viết theo mẫu:
  `- {Tên môn tiền đề} ({mã}){nếu có kỳ → " – Kỳ {index}"}: {điểm/trạng thái hiện tại} – nhận xét ngắn gọn về cách hỗ trợ môn đang phân tích`
  (nếu chưa có điểm → ghi “Chưa có điểm – cần hoàn thành trước khi học sâu môn hiện tại”).
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
- Luôn có `### Tình hình`, `### Kiến thức trọng tâm`, `### Môn nền tảng quan trọng` (khi có `prerequisites`), `### Lộ trình 2–4 tuần`.
- Heading `### Cảnh báo` bắt buộc xuất hiện khi `dependents` hoặc `dependentWarnings` không rỗng; mỗi bullet phải lặp lại đúng câu trong `dependentWarnings` (có thể bổ sung thêm nhấn mạnh nhưng không được bỏ câu).
- Bullet cần viện dẫn thẳng tên môn hoặc kỹ năng để người học dễ áp dụng.
""";
    }

    public static string BuildMissingSubjectPrompt(
        IEnumerable<SubjectMark> missingSubjects,
        IEnumerable<SubjectCur> curriculumSubjects,
        string careerGoal)
    {
        var missingList = (missingSubjects ?? Enumerable.Empty<SubjectMark>())
            .Where(s => !string.IsNullOrWhiteSpace(s.SubjectCode))
            .ToList();

        var curriculumListRaw = (curriculumSubjects ?? Enumerable.Empty<SubjectCur>()).ToList();
        var curriculumLookup = curriculumListRaw
            .Where(x => !string.IsNullOrWhiteSpace(x.SubjectCode))
            .ToDictionary(
                x => x.SubjectCode,
                x => x,
                StringComparer.OrdinalIgnoreCase);
        var dependentsLookup = BuildDependentsLookup(curriculumListRaw);

        var subjects = missingList.Select(s =>
        {
            curriculumLookup.TryGetValue(s.SubjectCode, out var subjectInfo);
            var canonicalName = subjectInfo != null && !string.IsNullOrWhiteSpace(subjectInfo.SubjectName)
                ? subjectInfo.SubjectName
                : (string.IsNullOrWhiteSpace(s.SubjectName) ? s.SubjectCode : s.SubjectName);

            var semesterIndex = subjectInfo?.Index;

            var prereqDetails = (subjectInfo?.SubjectPrerequisiteCode ?? new List<string>())
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code =>
                {
                    curriculumLookup.TryGetValue(code, out var prereqInfo);
                    return new
                    {
                        subjectCode = code,
                        subjectName = string.IsNullOrWhiteSpace(prereqInfo?.SubjectName) ? code : prereqInfo.SubjectName,
                        semesterIndex = prereqInfo?.Index
                    };
                })
                .ToList();

            dependentsLookup.TryGetValue(s.SubjectCode, out var dependents);
            var dependentDetails = dependents?.Select(dep => (object)new
            {
                subjectCode = dep.subjectCode,
                subjectName = dep.subjectName,
                semesterIndex = dep.semesterIndex
            }).ToList() ?? new List<object>();

            return new
            {
                subjectCode = s.SubjectCode,
                subjectName = canonicalName,
                semesterIndex,
                prerequisites = prereqDetails,
                dependents = dependentDetails
            };
        }).ToList();

        var payload = new
        {
            subjects,
            careerGoal
        };

        var json = Serialize(payload);

        return $$"""
DỮ LIỆU MÔN THIẾU ĐIỂM:
```json
{{json}}
```

NHIỆM VỤ:
- Với mỗi môn (chưa học hoặc chưa có điểm), viết 1 phân tích Markdown gồm 3 heading:
  1. `### Vì sao nên chuẩn bị sớm`: nêu lý do phải chuẩn bị trước khi vào môn (kỳ học, tiến độ, careerGoal).
  2. `### Môn phụ thuộc dễ bị ảnh hưởng`: liệt kê các môn phụ thuộc trong `dependents`, giải thích hậu quả nếu nền tảng yếu.
  3. `### Hành động cần làm`: 3–5 bullet nêu rõ phải ôn/làm bài gì để xây nền, ghi khối lượng cụ thể.
- Liên hệ `careerGoal` khi có thông tin.

OUTPUT JSON:
{
  "withoutMarkAnalysis": [
    {
      "subjectCode": "...",
      "subjectName": "...",
      "analysisMarkdown": "## <Tên môn> (<Mã>)\n### Vì sao nên chuẩn bị sớm\n- ...\n### Môn phụ thuộc dễ bị ảnh hưởng\n- ...\n### Hành động cần làm\n- ..."
    }
  ]
}

LƯU Ý:
- Nếu không có môn phụ thuộc, ghi rõ “Không có môn phụ thuộc trực tiếp”.
- Bullet phải bắt đầu bằng động từ và nêu khối lượng (ví dụ: “Ôn 2 buổi/tuần ...”).
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
        var scoredSubjects = subjectMarkList.Where(s => s.Mark.HasValue).ToList();
        var abilityMarkList = (abilityMarks ?? Enumerable.Empty<AbilityMark>()).ToList();

        var subjectList = subjectMarkList.Select(s => new
        {
            subjectCode = s.SubjectCode,
            subjectName = s.SubjectName,
            mark = s.Mark
        }).ToList();

        var abilityList = abilityMarkList.Select(a => new { name = a.Name, mark = a.Mark }).ToList();

        var stats = new
        {
            avgSubject = scoredSubjects.Count > 0 ? scoredSubjects.Average(s => s.Mark!.Value) : 0,
            avgAbility = abilityMarkList.Count > 0 ? abilityMarkList.Average(a => a.Mark) : 0,
            strongSubjects = scoredSubjects.Where(s => s.Mark >= 8.0).Select(s => s.SubjectName).ToList(),
            weakSubjects = scoredSubjects.Where(s => s.Mark < 6.5).Select(s => s.SubjectName).ToList(),
            strongAbilities = abilityMarkList.Where(a => a.Mark >= 8.0).Select(a => a.Name).ToList(),
            weakAbilities = abilityMarkList.Where(a => a.Mark < 6.5).Select(a => a.Name).ToList()
        };

        var surveyPayload = new
        {
            quizHabits = quizSurvey.QuizHabits, quizInterests = quizSurvey.QuizInterests
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
- Nếu `abilityMarks` null/rỗng: Đây là sinh viên kỳ 5+ đã có bảng điểm đầy đủ. Phân tích dựa trên **kết quả môn học thực tế** thay vì năng lực cơ bản. 
  Trong `learningAbility`, nhấn mạnh rằng sinh viên đã vượt qua giai đoạn đánh giá cơ bản, nên tập trung vào chuyên môn sâu và dự án thực tế.
- Nếu có `abilityMarks`: Dùng dữ liệu môn học + năng lực + khảo sát để soi chiếu tính cách học tập, thói quen, năng lực tiếp thu.
- Nhấn mạnh các môn/khả năng nổi bật và liệt kê tối đa 2 ưu tiên cải thiện rõ ràng, đo được để tiến gần `careerGoal`.

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
- **learningAbility**: Nếu `abilityMarks` null/rỗng (sinh viên kỳ 5+), phân tích dựa trên điểm trung bình các môn chuyên ngành, khuyến nghị tập trung dự án thực tế thay vì kiến thức cơ bản.
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
            if (string.IsNullOrWhiteSpace(subject.SubjectCode)) continue;

            scope.Add(subject.SubjectCode);

            if (curriculumLookup.TryGetValue(subject.SubjectCode, out var subjectInfo))
            {
                foreach (var prereq in subjectInfo.SubjectPrerequisiteCode ?? Enumerable.Empty<string>())
                {
                    if (!string.IsNullOrWhiteSpace(prereq))
                    {
                        scope.Add(prereq);
                    }
                }
            }

            if (dependentsLookup.TryGetValue(subject.SubjectCode, out var dependents))
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
            foreach (var prerequisite in subject.SubjectPrerequisiteCode ?? new List<string>())
            {
                if (string.IsNullOrWhiteSpace(prerequisite)) continue;

                if (!map.TryGetValue(prerequisite, out var list))
                {
                    list = new List<(string subjectCode, string subjectName, int? semesterIndex)>();
                    map[prerequisite] = list;
                }

                var dependentName = string.IsNullOrWhiteSpace(subject.SubjectName)
                    ? subject.SubjectCode
                    : subject.SubjectName;
                var semesterIndex = subject.Index > 0 ? subject.Index : (int?)null;

                if (!list.Any(dep => dep.subjectCode.Equals(subject.SubjectCode, StringComparison.OrdinalIgnoreCase)))
                {
                    list.Add((subject.SubjectCode, dependentName, semesterIndex));
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

