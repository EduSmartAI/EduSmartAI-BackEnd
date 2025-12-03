# 📚 Hướng Dẫn Triển Khai Learning Feedback System

## 📋 Mục Lục
1. [Tổng Quan Hệ Thống](#1-tổng-quan-hệ-thống)
2. [Cấu Trúc Database Hiện Tại](#2-cấu-trúc-database-hiện-tại)
3. [Flow Xử Lý Hiện Tại](#3-flow-xử-lý-hiện-tại)
4. [Vấn Đề Cần Giải Quyết](#4-vấn-đề-cần-giải-quyết)
5. [Giải Pháp Chi Tiết](#5-giải-pháp-chi-tiết)
6. [Các Bước Thực Hiện](#6-các-bước-thực-hiện)
7. [Code Implementation](#7-code-implementation)
8. [Testing & Validation](#8-testing--validation)

---

## 1. Tổng Quan Hệ Thống

### 🎯 Mục Tiêu
Sau khi AI generate feedback, hệ thống cần:
- **Tạo feedback tổng quát** cho toàn bộ Learning Path
- **Tạo feedback cho từng Major** (chuyên ngành)
- **Tạo feedback cho từng Subject** (môn học) trong Major đó
- **Liên kết Course với Subject feedback** tương ứng

### 🏗️ Kiến Trúc Mong Muốn

```
LearningPath (1)
├── SummaryFeedback (tổng quát)
├── HabitAndInterestAnalysis
├── Personality
├── LearningAbility
└── LearningPathMajors (nhiều Major)
    ├── Major 1 (SE - Software Engineering)
    │   ├── Reason (feedback cho major này)
    │   ├── LearningPathSubjectCodes (nhiều Subject)
    │   │   ├── Subject 1 (OOP)
    │   │   │   ├── AnalysisMarkdown (feedback cho môn này)
    │   │   │   └── LearningPathCourses (các khóa học liên quan)
    │   │   │       ├── Course 1
    │   │   │       └── Course 2
    │   │   └── Subject 2 (DSA)
    │   │       ├── AnalysisMarkdown
    │   │       └── LearningPathCourses
    │   │           └── Course 3
    │   └── LearningPathCourses (các khóa học không thuộc subject cụ thể)
    │       └── Course 4 (Soft Skills...)
    └── Major 2 (AI - Artificial Intelligence)
        └── ... (tương tự)
```

---

## 2. Cấu Trúc Database Hiện Tại

### 📊 Entity Relationships

#### **LearningPath** (1) → (N) **LearningPathMajor**
```csharp
public partial class LearningPath
{
    public Guid PathId { get; set; }
    public string PathName { get; set; }
    
    // Feedback tổng quát cho cả Learning Path
    public string? SummaryFeedback { get; set; }
    public string? HabitAndInterestAnalysis { get; set; }
    public string? Personality { get; set; }
    public string? LearningAbility { get; set; }
    
    public virtual ICollection<LearningPathMajor> LearningPathMajors { get; set; }
}
```

#### **LearningPathMajor** (1) → (N) **LearningPathSubjectCode**
```csharp
public partial class LearningPathMajor
{
    public Guid LearningPathMajorId { get; set; }
    public Guid PathId { get; set; }
    public string MajorCode { get; set; }
    
    // ❗ QUAN TRỌNG: Đây sẽ là feedback cho Major này
    public string? Reason { get; set; }
    
    public short Type { get; set; } // 1: Internal, 2: External
    public int? PositionIndex { get; set; }
    
    public virtual ICollection<LearningPathCourse> LearningPathCourses { get; set; }
    public virtual ICollection<LearningPathSubjectCode> LearningPathSubjectCodes { get; set; }
}
```

#### **LearningPathSubjectCode** (1) → (N) **LearningPathCourse**
```csharp
public partial class LearningPathSubjectCode
{
    public Guid LearningPathSubjectCodeId { get; set; }
    public string SubjectCode { get; set; }
    
    // ❗ QUAN TRỌNG: Đây là feedback cho Subject này
    public string? AnalysisMarkdown { get; set; }
    
    public Guid LearningPathMajorId { get; set; }
    
    public virtual ICollection<LearningPathCourse> LearningPathCourses { get; set; }
    public virtual LearningPathMajor LearningPathMajor { get; set; }
}
```

#### **LearningPathCourse**
```csharp
public partial class LearningPathCourse
{
    public Guid LearningPathCourseId { get; set; }
    public Guid LearningPathMajorId { get; set; }
    
    // ❗ QUAN TRỌNG: Course có thể thuộc về Subject hoặc không
    public Guid? LearningPathSubjectCodeId { get; set; }
    public string SubjectCode { get; set; }
    
    public Guid? InternalCourseId { get; set; }
    public string? ExternalCourseLink { get; set; }
    // ... external course details
    
    public virtual LearningPathMajor LearningPathMajor { get; set; }
    public virtual LearningPathSubjectCode? LearningPathSubjectCode { get; set; }
}
```

---

## 3. Flow Xử Lý Hiện Tại

### ⚡ Kiến Trúc Event-Driven (Async)

**QUAN TRỌNG:** Toàn bộ hệ thống chạy **bất đồng bộ** (asynchronous) thông qua **MassTransit Event Bus**. API response ngay lập tức, các tác vụ AI chạy background.

---

### 📤 Flow 1: Student Submit Survey & Create Learning Path (Sync - Return ngay)

```
┌─────────────────────────────────────────────────────────────────┐
│ CLIENT REQUEST: POST /api/student-survey                        │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ InsertStudentSurveyAsync (QuizService)                          │
│                                                                  │
│ 1. Validate survey (INTEREST + HABIT)                          │
│ 2. Save student quiz answers                                    │
│ 3. ✅ Create Learning Path (LearningPathId)                    │
│ 4. Publish StudentMajorOrientationEvent (Outbox)              │
│ 5. ⚡ RETURN Response { LearningPathId } NGAY LẬP TỨC         │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ ✅ CLIENT NHẬN RESPONSE:                                        │
│ {                                                                │
│   "success": true,                                              │
│   "response": "guid-learning-path-id",                          │
│   "message": "Ghi nhận câu trả lời của sinh viên"             │
│ }                                                                │
└─────────────────────────────────────────────────────────────────┘

📌 Lúc này Learning Path đã được tạo NHƯNG chưa có:
   - Major recommendations
   - Courses
   - AI feedbacks
   → Tất cả sẽ xử lý ASYNC ở background
```

---

### 🔄 Flow 2: Major Recommendation (Async - Background)

```
┌─────────────────────────────────────────────────────────────────┐
│ EVENT BUS: StudentMajorOrientationEvent published              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ StudentMajorRecommendConsumer (AiService)                      │
│                                                                  │
│ Nhận event:                                                     │
│ {                                                                │
│   LearningGoal: "Backend Developer",                           │
│   Frameworks: ["React", "Node.js"],                            │
│   Languages: ["JavaScript", "Python"],                         │
│   LearningPathId: "guid",                                      │
│   StudentLevel: 2,                                             │
│   StudentPassedSubjects: ["OOP", "DSA"],                       │
│   CourseImproves: [...]                                        │
│ }                                                                │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ AiRecommendHandler (AiService)                                 │
│                                                                  │
│ 1. Gọi AI: _advisorService.EvaluateAsync()                     │
│    → AI recommend majors dựa vào:                              │
│      - Career goal                                              │
│      - Known frameworks/languages                               │
│      - Student level                                            │
│                                                                  │
│ 2. AI trả về:                                                   │
│    {                                                             │
│      Matched: [                                                 │
│        { MajorCode: "SE", Reasons: "Phù hợp vì..." },         │
│        { MajorCode: "AI", Reasons: "Nên học thêm..." }        │
│      ],                                                          │
│      ExternalSuggestions: [                                     │
│        { MajorCode: "CLOUD", WhyForYou: "...", Courses: [...] }│
│      ]                                                           │
│    }                                                             │
│                                                                  │
│ 3. Parallel Tasks:                                             │
│    ├─ Task 1: Publish InternalMajorEvent (for SE, AI...)      │
│    └─ Task 2: Send AiBatchExternalRecommendRequest            │
│                                                                  │
│ 4. await Task.WhenAll(internalTask, externalTask)             │
└─────────────────────────────────────────────────────────────────┘
                     ↓                        ↓
        ┌────────────────────┐    ┌──────────────────────┐
        │ Internal Majors    │    │ External Majors      │
        │ (SE, AI...)        │    │ (CLOUD, DevOps...)   │
        └────────────────────┘    └──────────────────────┘
```

---

### 🏗️ Flow 3: Insert Internal Majors & Courses (Async)

```
┌─────────────────────────────────────────────────────────────────┐
│ EVENT: InternalMajorEvent                                       │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ InternalMajorEventConsumer (StudentService)                    │
│                                                                  │
│ 1. Nhận event với majors: [SE, AI]                            │
│                                                                  │
│ 2. Foreach major:                                              │
│    ├─ Insert LearningPathMajor                                │
│    │  {                                                         │
│    │    PathId: learningPathId,                               │
│    │    MajorCode: "SE",                                      │
│    │    Type: 1 (Internal),                                   │
│    │    Reason: null  // ❌ Chưa có feedback                 │
│    │  }                                                         │
│    │                                                            │
│    └─ Publish CoursesSelectEvent → CourseService              │
│       → Get courses for this major                             │
│       → Insert LearningPathCourse(s)                           │
│                                                                  │
│ 3. ✅ Lúc này đã có:                                          │
│    - LearningPathMajor(s) created                             │
│    - LearningPathCourse(s) created                            │
│    - NHƯNG chưa có AI feedback                                │
└─────────────────────────────────────────────────────────────────┘
```

---

### 🧠 Flow 4: Generate AI Feedback (Async)

**⚠️ Flow này chạy SONG SONG với Flow 3, KHÔNG đợi nhau**

```
┌─────────────────────────────────────────────────────────────────┐
│ InternalMajorEventConsumer (tiếp)                              │
│                                                                  │
│ 4. Sau khi insert xong majors & courses                       │
│    → Publish AiRecommendImprovementEvent                      │
│    {                                                             │
│      CareerGoal: "Backend Developer",                          │
│      MajorCode: "SE",  // ❌ HIỆN TẠI: Chỉ gửi 1 major       │
│      SubjectMarks: [các môn cần feedback],                    │
│      AbilityMarks: [nếu có test results],                     │
│      QuizSurvey: { Habits: [...], Interests: [...] },        │
│      LearningPathId: "guid",                                  │
│      Email: "student@email.com"                               │
│    }                                                             │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ AiRecommendImprovementEventConsumer (AiService)               │
│                                                                  │
│ 1. Nhận event                                                  │
│                                                                  │
│ 2. Gọi AI: GenerateLearningFeedbackMarkdownAsync()           │
│    → AI phân tích và generate feedback                        │
│                                                                  │
│ 3. ❌ HIỆN TẠI: AI trả về flat structure                     │
│    {                                                             │
│      SummaryFeedback: "...",                                   │
│      HabitAndInterestAnalysis: "...",                          │
│      Personality: "...",                                       │
│      LearningAbility: "...",                                   │
│                                                                  │
│      SubjectAnalyses: [  // ❌ Không biết thuộc major nào    │
│        { SubjectCode: "OOP", AnalysisMarkdown: "..." },       │
│        { SubjectCode: "DSA", AnalysisMarkdown: "..." }        │
│      ]                                                          │
│    }                                                             │
│                                                                  │
│ 4. Publish LearningFeedbackEvent                              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ LearningFeedbackEventConsumer (StudentService)                │
│                                                                  │
│ 1. Update LearningPath:                                        │
│    - SummaryFeedback ✅                                        │
│    - HabitAndInterestAnalysis ✅                               │
│    - Personality ✅                                            │
│    - LearningAbility ✅                                        │
│                                                                  │
│ 2. ❌ VẤNĐỀ: Insert LearningPathSubjectCode                  │
│    - LearningPathMajorId = learningPath.PathId  // ❌ SAI FK │
│    - Không biết subject thuộc major nào                       │
│    - Không link với courses                                    │
└─────────────────────────────────────────────────────────────────┘
```

---

### 📊 Flow Diagram - Tổng Quan

```
TIME ────────────────────────────────────────────────────────────►

CLIENT          QUIZSERVICE         AISERVICE          STUDENTSERVICE
  │                   │                  │                    │
  │ POST Survey       │                  │                    │
  ├──────────────────►│                  │                    │
  │                   │                  │                    │
  │                   │ 1. Save answers  │                    │
  │                   │ 2. Create Path   │                    │
  │                   │                  │                    │
  │◄──────────────────┤ ⚡ RETURN PathId                      │
  │ { PathId }        │    (NGAY LẬP TỨC)                     │
  │                   │                  │                    │
  │                   │                  │                    │
  │        ┌──────────┴─Publish──────────►                    │
  │        │ StudentMajorOrientationEvent │                    │
  │        │          (Async)              │                    │
  │        │                               │                    │
  │        │                    ┌──────────┤ AI Recommend      │
  │        │                    │          │ Majors            │
  │        │                    │          │                    │
  │        │                    └──────────┤                    │
  │        │                    Publish    ├──────────────────► │
  │        │                 InternalMajor │                    │
  │        │                    Event      │                    │
  │        │                               │                    │
  │        │                               │        ┌───────────┤
  │        │                               │        │ Insert    │
  │        │                               │        │ Majors &  │
  │        │                               │        │ Courses   │
  │        │                               │        │           │
  │        │                               │        └───────────┤
  │        │                               │        Publish     │
  │        │                               │◄───────────────────┤
  │        │                               │ AiRecommend        │
  │        │                               │ ImprovementEvent   │
  │        │                               │                    │
  │        │                    ┌──────────┤                    │
  │        │                    │ AI Gen   │                    │
  │        │                    │ Feedback │                    │
  │        │                    │          │                    │
  │        │                    └──────────┤                    │
  │        │                    Publish    ├──────────────────► │
  │        │                 LearningFeed  │                    │
  │        │                    backEvent  │                    │
  │        │                               │                    │
  │        │                               │        ┌───────────┤
  │        │                               │        │ Update    │
  │        │                               │        │ Feedback  │
  │        │                               │        │           │
  │        │                               │        └───────────┘

📌 Client đã có response từ lâu, tất cả process sau đó chạy background
```

---

### 🔴 Vấn Đề Trong Flow Hiện Tại

#### **Problem 1: Missing Major in AiRecommendImprovementEvent**
```
❌ Hiện tại:
AiRecommendImprovementEvent chỉ có 1 MajorCode

✅ Cần:
Phải gửi TẤT CẢ majors đã được recommend (SE, AI, ...)
→ AI generate feedback cho từng major
```

#### **Problem 2: AI Response Flat Structure**
```
❌ Hiện tại:
SubjectAnalyses: [OOP, DSA, ML, ...]  // Không biết thuộc major nào

✅ Cần:
MajorFeedbacks: [
  { MajorCode: "SE", Reason: "...", SubjectAnalyses: [OOP, DSA] },
  { MajorCode: "AI", Reason: "...", SubjectAnalyses: [ML, DL] }
]
```

#### **Problem 3: Wrong Foreign Key**
```
❌ Hiện tại:
LearningPathSubjectCode.LearningPathMajorId = learningPath.PathId  // SAI

✅ Cần:
LearningPathSubjectCode.LearningPathMajorId = learningPathMajor.LearningPathMajorId
```

#### **Problem 4: Courses Not Linked**
```
❌ Hiện tại:
LearningPathCourse.LearningPathSubjectCodeId = null  // Không link

✅ Cần:
Link course với subject tương ứng sau khi có feedback
```

---

## 4. Vấn Đề Cần Giải Quyết

### 🔴 Problem 1: Missing Major Feedback
**Hiện tại:**
```csharp
// Chỉ update Learning Path level feedback
learningPath.SummaryFeedback = evt.SummaryFeedback;
learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
// ...
```

**Vấn đề:** Không có feedback cho từng Major (LearningPathMajor.Reason)

**Cần:** AI phải generate feedback riêng cho từng Major

---

### 🔴 Problem 2: Wrong Foreign Key
**Hiện tại:**
```csharp
var learningPathSubjectCode = new LearningPathSubjectCode
{
    LearningPathMajorId = learningPath.PathId,  // ❌ SAI: PathId không phải MajorId
    SubjectCode = subCode.SubjectCode,
    AnalysisMarkdown = subCode.AnalysisMarkdown,
};
```

**Vấn đề:** 
- `learningPath.PathId` là Guid của LearningPath
- Nhưng `LearningPathMajorId` cần phải là Guid của LearningPathMajor
- Hiện tại không biết Subject thuộc Major nào

---

### 🔴 Problem 3: Course Not Linked to Subject
**Hiện tại:**
```csharp
// LearningPathCourse được tạo ở InsertStudentSurveyAsync
// Nhưng LearningPathSubjectCodeId = null
// → Không biết Course nào thuộc Subject nào
```

**Cần:**
- Khi tạo Course, nếu Course.SubjectCode trùng với một SubjectCode đã có feedback
- → Link Course đó với LearningPathSubjectCode tương ứng

---

## 5. Giải Pháp Chi Tiết

### ✅ Solution Overview

#### **Điểm Mấu Chốt Cần Fix**

1. **InternalMajorEventConsumer** phải publish `AiRecommendImprovementEvent` với **TẤT CẢ majors**
2. **AI** phải generate feedback theo hierarchy: `MajorFeedback → SubjectAnalysis`
3. **LearningFeedbackEventConsumer** phải map đúng: `Major → SubjectCode → Course`

---

#### **Bước 1: Update InternalMajorEventConsumer**

**Hiện tại:** Publish event với 1 MajorCode
**Cần fix:** Publish với list of majors

```csharp
// ❌ BEFORE (trong InternalMajorEventConsumer)
var aiRecommendEvent = new AiRecommendImprovementEvent
{
    MajorCode = "SE",  // Chỉ 1 major
    // ...
};

// ✅ AFTER
var aiRecommendEvent = new AiRecommendImprovementEvent
{
    MajorCodes = evt.Majors.Select(m => new MajorInfo 
    { 
        MajorCode = m.MajorCode,
        MajorName = majorDetails[m.MajorCode].Name  // Get from CourseService
    }).ToList(),
    // ...
};
```

---

#### **Bước 2: AI Generate Feedback cho từng Major**

AI cần trả về feedback có cấu trúc hierarchy rõ ràng.

**Cấu trúc mới cho AI Response:**
```json
{
  "SummaryFeedback": "Tổng quan về learning path...",
  "HabitAndInterestAnalysis": "...",
  "Personality": "...",
  "LearningAbility": "...",
  
  "MajorFeedbacks": [
    {
      "MajorCode": "SE",
      "MajorName": "Software Engineering", 
      "Reason": "## Tại sao chọn Software Engineering?\n\nDựa vào khảo sát và điểm số của bạn, SE phù hợp vì...",
      "SubjectAnalyses": [
        {
          "SubjectCode": "OOP",
          "SubjectName": "Object-Oriented Programming",
          "AnalysisMarkdown": "## Phân tích môn OOP\n\n### Điểm hiện tại: 6.5\n\n..."
        },
        {
          "SubjectCode": "DSA",
          "SubjectName": "Data Structures & Algorithms",
          "AnalysisMarkdown": "..."
        }
      ]
    },
    {
      "MajorCode": "AI",
      "MajorName": "Artificial Intelligence",
      "Reason": "## Gợi ý học thêm AI\n\nBạn có thể mở rộng sang AI vì...",
      "SubjectAnalyses": [
        {
          "SubjectCode": "ML",
          "AnalysisMarkdown": "..."
        }
      ]
    }
  ]
}
```

#### **Bước 2: Update Event Structure**
```csharp
public class LearningFeedbackEvent
{
    // Global feedback
    public string SummaryFeedback { get; set; }
    public string HabitAndInterestAnalysis { get; set; }
    public string Personality { get; set; }
    public string LearningAbility { get; set; }
    
    // ✅ THÊM MỚI: Feedback cho từng Major
    public List<MajorFeedback> MajorFeedbacks { get; set; }
    
    public Guid LearningPathId { get; set; }
    public string Email { get; set; }
}

public class MajorFeedback
{
    public string MajorCode { get; set; }
    public string Reason { get; set; }  // Feedback cho major
    public List<LearningPathSubjectCodeEvent> SubjectCodes { get; set; }
}

public class LearningPathSubjectCodeEvent
{
    public string SubjectCode { get; set; }
    public string AnalysisMarkdown { get; set; }
}
```

#### **Bước 3: Update Consumer Logic**
```csharp
public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
{
    var evt = context.Message;
    
    // 1. Update Learning Path global feedback
    var learningPath = await _learningPathRepository
        .FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive);
    
    learningPath.SummaryFeedback = evt.SummaryFeedback;
    learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
    learningPath.Personality = evt.Personality;
    learningPath.LearningAbility = evt.LearningAbility;
    
    _learningPathRepository.Update(learningPath);
    
    // 2. ✅ XỬ LÝ TỪNG MAJOR
    foreach (var majorFeedback in evt.MajorFeedbacks)
    {
        // 2.1. Tìm LearningPathMajor tương ứng
        var learningPathMajor = await _learningPathMajorRepository
            .FirstOrDefaultAsync(x => 
                x.PathId == evt.LearningPathId && 
                x.MajorCode == majorFeedback.MajorCode &&
                x.IsActive);
        
        if (learningPathMajor == null) continue; // Skip nếu không tìm thấy
        
        // 2.2. ✅ UPDATE MAJOR REASON (Feedback cho major)
        learningPathMajor.Reason = majorFeedback.Reason;
        _learningPathMajorRepository.Update(learningPathMajor);
        
        // 2.3. ✅ INSERT SUBJECT CODES với đúng MajorId
        foreach (var subjectCode in majorFeedback.SubjectCodes)
        {
            var learningPathSubjectCode = new LearningPathSubjectCode
            {
                LearningPathMajorId = learningPathMajor.LearningPathMajorId, // ✅ Đúng MajorId
                SubjectCode = subjectCode.SubjectCode,
                AnalysisMarkdown = subjectCode.AnalysisMarkdown,
            };
            
            await _learningPathSubjectCodeRepository.AddAsync(learningPathSubjectCode);
        }
    }
    
    await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
    
    // 3. ✅ LINK COURSES với SUBJECTS
    await LinkCoursesWithSubjects(evt.LearningPathId);
}

private async Task LinkCoursesWithSubjects(Guid learningPathId)
{
    // 3.1. Load tất cả LearningPathMajors của Learning Path
    var learningPathMajors = await _learningPathMajorRepository
        .Find(x => x.PathId == learningPathId && x.IsActive)
        .ToListAsync();
    
    foreach (var major in learningPathMajors)
    {
        // 3.2. Load tất cả SubjectCodes của Major này
        var subjectCodes = await _learningPathSubjectCodeRepository
            .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
            .ToListAsync();
        
        // 3.3. Load tất cả Courses của Major này
        var courses = await _learningPathCourseRepository
            .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
            .ToListAsync();
        
        // 3.4. Link Course với Subject tương ứng
        foreach (var course in courses)
        {
            // Tìm SubjectCode tương ứng
            var matchingSubject = subjectCodes
                .FirstOrDefault(sc => sc.SubjectCode == course.SubjectCode);
            
            if (matchingSubject != null)
            {
                course.LearningPathSubjectCodeId = matchingSubject.LearningPathSubjectCodeId;
                _learningPathCourseRepository.Update(course);
            }
        }
    }
    
    await _unitOfWork.SaveChangesAsync("system", CancellationToken.None);
}
```

---

## 6. Các Bước Thực Hiện

### 📝 Checklist Implementation

#### **Phase 0: Update Event Structure (QUAN TRỌNG - LÀM TRƯỚC)**

- [ ] **Step 0.1:** Update `AiRecommendImprovementEvent` để hỗ trợ multiple majors
  ```csharp
  // File: BuildingBlocks.Messaging/Events/QuizService/AiRecommendImprovementEvent.cs
  
  public class AiRecommendImprovementEvent
  {
      public string CareerGoal { get; set; } = null!;
      
      // ✅ THAY ĐỔI: Từ single MajorCode → List of MajorInfos
      public List<MajorInfo> Majors { get; set; } = new();
      
      public List<SubjectMarkEvent> SubjectMarks { get; set; }
      public List<AbilityMarkEvent>? AbilityMarks { get; set; }
      public QuizSurveyEvent QuizSurveyEvent { get; set; }
      public required Guid LearningPathId { get; set; }
      public required string Email { get; set; }
  }
  
  // ✅ NEW
  public class MajorInfo
  {
      public string MajorCode { get; set; } = null!;
      public string MajorName { get; set; } = null!;
  }
  
  // ... existing SubjectMarkEvent, AbilityMarkEvent, QuizSurveyEvent
  ```

- [ ] **Step 0.2:** Update `InternalMajorEventConsumer` (StudentService)
  ```csharp
  // Sau khi insert xong majors & courses
  // Cần get major names từ CourseService
  
  var majorInfos = new List<MajorInfo>();
  foreach (var major in evt.Majors)
  {
      // Call CourseService to get major details
      var majorDetail = await _requestMajorDetailClient.GetResponse<MajorDetailResponse>(
          new MajorDetailRequest { MajorCode = major.MajorCode });
      
      majorInfos.Add(new MajorInfo
      {
          MajorCode = major.MajorCode,
          MajorName = majorDetail.Message.MajorName
      });
  }
  
  // ✅ Publish với TẤT CẢ majors
  var aiEvent = new AiRecommendImprovementEvent
  {
      Majors = majorInfos,  // ✅ List of majors
      // ... other fields
  };
  ```

---

#### **Phase 1: Update AI Service (AiService)**

- [ ] **Step 1.1:** Update `AiAnalysisSubjectAndAbilityDto`
  ```csharp
  public class AiAnalysisSubjectAndAbilityDto
  {
      // Existing fields
      public string SummaryFeedback { get; set; }
      public string HabitAndInterestAnalysis { get; set; }
      public string Personality { get; set; }
      public string LearningAbility { get; set; }
      
      // ✅ NEW: Replace flat SubjectAnalyses with hierarchy MajorFeedbacks
      public List<MajorFeedbackDto> MajorFeedbacks { get; set; }
      
      // Keep for abilities (nếu có test results)
      public List<AbilityAnalysis> AbilityAnalyses { get; set; }
  }
  
  public class MajorFeedbackDto
  {
      public string MajorCode { get; set; }
      public string MajorName { get; set; }
      public string Reason { get; set; }  // Feedback cho major này
      public List<SubjectAnalysis> SubjectAnalyses { get; set; }
  }
  ```

- [ ] **Step 1.2:** Update AI Prompt trong `AiRecommendPromptLibrary`
  ```
  Yêu cầu AI tạo feedback theo format:
  
  1. Summary Feedback (tổng quan)
  2. Habit & Interest Analysis
  3. Personality
  4. Learning Ability
  5. Major Feedbacks (theo từng major):
     - Major Code
     - Major Name  
     - Reason: Tại sao major này phù hợp/không phù hợp
     - Subject Analyses: [feedback cho từng môn trong major]
  ```

- [ ] **Step 1.3:** Update `GenerateLearningFeedbackMarkdownAsync()` parsing logic
  - Parse AI response theo cấu trúc mới
  - Map vào `MajorFeedbackDto`

---

#### **Phase 2: Update Event Structure (BuildingBlocks.Messaging)**

- [ ] **Step 2.1:** Update `LearningFeedbackEvent`
  ```csharp
  public class LearningFeedbackEvent
  {
      public string SummaryFeedback { get; set; }
      public string HabitAndInterestAnalysis { get; set; }
      public string Personality { get; set; }
      public string LearningAbility { get; set; }
      
      // ✅ NEW
      public List<MajorFeedbackEvent> MajorFeedbacks { get; set; }
      
      public Guid LearningPathId { get; set; }
      public string Email { get; set; }
  }
  
  public class MajorFeedbackEvent
  {
      public string MajorCode { get; set; }
      public string Reason { get; set; }
      public List<LearningPathSubjectCodeEvent> SubjectCodes { get; set; }
  }
  
  // LearningPathSubjectCodeEvent giữ nguyên
  ```

---

#### **Phase 3: Update AI Consumer (AiService)**

- [ ] **Step 3.1:** Update `AiRecommendImprovementEventConsumer`
  ```csharp
  // Map từ AI response sang Event
  var learningFeedbackEvent = new LearningFeedbackEvent
  {
      SummaryFeedback = result.Response.SummaryFeedback,
      // ... other global fields
      
      // ✅ NEW: Map Major Feedbacks
      MajorFeedbacks = result.Response.MajorFeedbacks.Select(mf => 
          new MajorFeedbackEvent
          {
              MajorCode = mf.MajorCode,
              Reason = mf.Reason,
              SubjectCodes = mf.SubjectAnalyses.Select(sa => 
                  new LearningPathSubjectCodeEvent
                  {
                      SubjectCode = sa.SubjectCode,
                      AnalysisMarkdown = sa.AnalysisMarkdown
                  }).ToList()
          }).ToList(),
      
      LearningPathId = evt.LearningPathId,
      Email = evt.Email
  };
  ```

---

#### **Phase 4: Update Student Service Consumer**

- [ ] **Step 4.1:** Add new repository dependency
  ```csharp
  public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
  {
      private readonly ICommandRepository<LearningPath> _learningPathRepository;
      private readonly ICommandRepository<LearningPathMajor> _learningPathMajorRepository; // ✅ ADD
      private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeRepository;
      private readonly ICommandRepository<LearningPathCourse> _learningPathCourseRepository; // ✅ ADD
      private readonly IUnitOfWork _unitOfWork;
      
      // Constructor...
  }
  ```

- [ ] **Step 4.2:** Implement new `Consume` logic (xem code ở Solution)

- [ ] **Step 4.3:** Implement `LinkCoursesWithSubjects` method (xem code ở Solution)

---

#### **Phase 5: Update Read Model (Optional nhưng nên làm)**

- [ ] **Step 5.1:** Update `LearningPathCollection` read model
  ```csharp
  public class LearningPathCollection
  {
      // ... existing fields
      
      public List<LearningPathMajorCollection> LearningPathMajors { get; set; }
  }
  
  public class LearningPathMajorCollection
  {
      public Guid LearningPathMajorId { get; set; }
      public string MajorCode { get; set; }
      public string Reason { get; set; } // ✅ ADD feedback
      
      public List<LearningPathSubjectCodeCollection> SubjectCodes { get; set; }
      public List<LearningPathCourseCollection> Courses { get; set; }
  }
  
  public class LearningPathSubjectCodeCollection
  {
      public Guid LearningPathSubjectCodeId { get; set; }
      public string SubjectCode { get; set; }
      public string AnalysisMarkdown { get; set; }
      
      public List<LearningPathCourseCollection> Courses { get; set; }
  }
  ```

- [ ] **Step 5.2:** Update read model trong Consumer
  ```csharp
  // Sau khi save write model, update read model
  var learningPathCollection = await _learningPathQueryRepository
      .FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive);
  
  // Update với full hierarchy
  _unitOfWork.Store(learningPathCollection);
  await _unitOfWork.SessionSaveChangesAsync();
  ```

---

## 7. Code Implementation

### 📄 File 1: `AiAnalysisSubjectAndAbilityDto.cs` (AiService)

```csharp
namespace AiService.Application.Features.AiRecommend
{
    public class AiAnalysisSubjectAndAbilityDto
    {
        public string SummaryFeedback { get; set; } = string.Empty;
        public string HabitAndInterestAnalysis { get; set; } = string.Empty;
        public string Personality { get; set; } = string.Empty;
        public string LearningAbility { get; set; } = string.Empty;
        
        // ✅ NEW: Group subjects by major
        public required List<MajorFeedbackDto> MajorFeedbacks { get; set; }
        
        public required List<AbilityAnalysis> AbilityAnalyses { get; set; }
    }
    
    // ✅ NEW DTO
    public class MajorFeedbackDto
    {
        public string MajorCode { get; set; } = string.Empty;
        public string MajorName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty; // Feedback cho major
        public List<SubjectAnalysis> SubjectAnalyses { get; set; } = new();
    }
    
    public class SubjectAnalysis
    {
        public string SubjectCode { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string AnalysisMarkdown { get; set; } = string.Empty;
    }
    
    public class AbilityAnalysis
    {
        public string Name { get; set; } = string.Empty;
        public string AnalysisMarkdown { get; set; } = string.Empty;
    }
}
```

---

### 📄 File 2: `LearningFeedbackEvent.cs` (BuildingBlocks.Messaging)

```csharp
namespace BuildingBlocks.Messaging.Events.AIService;

public class LearningFeedbackEvent
{
    public string SummaryFeedback { get; set; }
    public string HabitAndInterestAnalysis { get; set; }
    public string Personality { get; set; }
    public string LearningAbility { get; set; }
    
    // ✅ NEW: Feedback grouped by major
    public List<MajorFeedbackEvent> MajorFeedbacks { get; set; } = new();
    
    public required string Email { get; set; }
    public required Guid LearningPathId { get; set; }
}

// ✅ NEW Event
public class MajorFeedbackEvent
{
    public string MajorCode { get; set; } = null!;
    public string Reason { get; set; } = null!; // Feedback cho major
    public List<LearningPathSubjectCodeEvent> SubjectCodes { get; set; } = new();
}

public class LearningPathSubjectCodeEvent
{
    public string SubjectCode { get; set; } = null!;
    public string AnalysisMarkdown { get; set; } = null!;
}
```

---

### 📄 File 3: `AiRecommendImprovementEventConsumer.cs` (AiService)

```csharp
public class AiRecommendImprovementEventConsumer(IAiSummaryService aiSummaryService) 
    : IConsumer<AiRecommendImprovementEvent>
{
    public async Task Consume(ConsumeContext<AiRecommendImprovementEvent> context)
    {
        var evt = context.Message;
        
        // ... (existing request mapping)
        
        var generateLearningFeedbackResult = await aiSummaryService
            .GenerateLearningFeedbackMarkdownAsync(request, context.CancellationToken);
        
        // ✅ NEW: Map với cấu trúc Major-based
        var learningFeedbackEvent = new LearningFeedbackEvent
        {
            SummaryFeedback = generateLearningFeedbackResult.Response.SummaryFeedback,
            HabitAndInterestAnalysis = generateLearningFeedbackResult.Response.HabitAndInterestAnalysis,
            LearningAbility = generateLearningFeedbackResult.Response.LearningAbility,
            Personality = generateLearningFeedbackResult.Response.Personality,
            
            // ✅ NEW: Map Major Feedbacks
            MajorFeedbacks = generateLearningFeedbackResult.Response.MajorFeedbacks
                .Select(mf => new MajorFeedbackEvent
                {
                    MajorCode = mf.MajorCode,
                    Reason = mf.Reason,
                    SubjectCodes = mf.SubjectAnalyses
                        .Select(sa => new LearningPathSubjectCodeEvent
                        {
                            SubjectCode = sa.SubjectCode,
                            AnalysisMarkdown = sa.AnalysisMarkdown
                        })
                        .ToList()
                })
                .ToList(),
            
            LearningPathId = evt.LearningPathId,
            Email = evt.Email
        };
        
        await context.Publish(learningFeedbackEvent, context.CancellationToken);
    }
}
```

---

### 📄 File 4: `LearningFeedbackEventConsumer.cs` (StudentService) - FULL CODE

```csharp
using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Consumers;

public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
{
    private readonly ICommandRepository<LearningPath> _learningPathRepository;
    private readonly ICommandRepository<LearningPathMajor> _learningPathMajorRepository;
    private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeRepository;
    private readonly ICommandRepository<LearningPathCourse> _learningPathCourseRepository;
    private readonly IQueryRepository<LearningPathCollection> _learningPathQueryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public LearningFeedbackEventConsumer(
        ICommandRepository<LearningPath> learningPathRepository,
        ICommandRepository<LearningPathMajor> learningPathMajorRepository,
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseRepository,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IUnitOfWork unitOfWork)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathMajorRepository = learningPathMajorRepository;
        _learningPathSubjectCodeRepository = learningPathSubjectCodeRepository;
        _learningPathCourseRepository = learningPathCourseRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        var evt = context.Message;
        
        // 1. Update Learning Path global feedback
        var learningPath = await _learningPathRepository
            .FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive);
        
        if (learningPath == null)
        {
            // Log error: Learning path not found
            return;
        }
        
        learningPath.SummaryFeedback = evt.SummaryFeedback;
        learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
        learningPath.Personality = evt.Personality;
        learningPath.LearningAbility = evt.LearningAbility;
        
        _learningPathRepository.Update(learningPath);
        
        // 2. Process each Major Feedback
        foreach (var majorFeedback in evt.MajorFeedbacks)
        {
            // 2.1. Find corresponding LearningPathMajor
            var learningPathMajor = await _learningPathMajorRepository
                .FirstOrDefaultAsync(x => 
                    x.PathId == evt.LearningPathId && 
                    x.MajorCode == majorFeedback.MajorCode &&
                    x.IsActive);
            
            if (learningPathMajor == null)
            {
                // Log warning: Major not found, skip
                continue;
            }
            
            // 2.2. ✅ UPDATE MAJOR REASON (Feedback for this major)
            learningPathMajor.Reason = majorFeedback.Reason;
            _learningPathMajorRepository.Update(learningPathMajor);
            
            // 2.3. ✅ INSERT SUBJECT CODES với đúng MajorId
            foreach (var subjectCode in majorFeedback.SubjectCodes)
            {
                var learningPathSubjectCode = new LearningPathSubjectCode
                {
                    LearningPathMajorId = learningPathMajor.LearningPathMajorId, // ✅ Correct FK
                    SubjectCode = subjectCode.SubjectCode,
                    AnalysisMarkdown = subjectCode.AnalysisMarkdown,
                };
                
                await _learningPathSubjectCodeRepository.AddAsync(learningPathSubjectCode);
            }
        }
        
        await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
        
        // 3. ✅ LINK COURSES với SUBJECTS
        await LinkCoursesWithSubjectsAsync(evt.LearningPathId, evt.Email, context.CancellationToken);
        
        // 4. TODO: Update Read Model
        // await UpdateReadModelAsync(evt.LearningPathId);
    }
    
    /// <summary>
    /// Link existing courses with their corresponding subject codes
    /// </summary>
    private async Task LinkCoursesWithSubjectsAsync(Guid learningPathId, string email, CancellationToken ct)
    {
        // 3.1. Load all LearningPathMajors of this Learning Path
        var learningPathMajors = await _learningPathMajorRepository
            .Find(x => x.PathId == learningPathId && x.IsActive)
            .ToListAsync(cancellationToken: ct);
        
        foreach (var major in learningPathMajors)
        {
            // 3.2. Load all SubjectCodes of this Major
            var subjectCodes = await _learningPathSubjectCodeRepository
                .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
                .ToListAsync(cancellationToken: ct);
            
            // Create lookup dictionary for performance
            var subjectCodeLookup = subjectCodes.ToDictionary(sc => sc.SubjectCode);
            
            // 3.3. Load all Courses of this Major
            var courses = await _learningPathCourseRepository
                .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
                .ToListAsync(cancellationToken: ct);
            
            // 3.4. Link Course with matching Subject
            foreach (var course in courses)
            {
                // Find matching SubjectCode
                if (subjectCodeLookup.TryGetValue(course.SubjectCode, out var matchingSubject))
                {
                    course.LearningPathSubjectCodeId = matchingSubject.LearningPathSubjectCodeId;
                    _learningPathCourseRepository.Update(course);
                }
                // Else: Course không thuộc subject cụ thể (e.g., soft skills)
            }
        }
        
        await _unitOfWork.SaveChangesAsync(email, ct);
    }
    
    // TODO: Implement Read Model update
    // private async Task UpdateReadModelAsync(Guid learningPathId)
    // {
    //     var learningPathCollection = await _learningPathQueryRepository
    //         .FirstOrDefaultAsync(x => x.PathId == learningPathId && x.IsActive);
    //     
    //     if (learningPathCollection != null)
    //     {
    //         // Reload full hierarchy từ write model
    //         // Map to read model
    //         // _unitOfWork.Store(learningPathCollection);
    //         // await _unitOfWork.SessionSaveChangesAsync();
    //     }
    // }
}
```

---

### 📄 File 5: Update AI Prompt (AiRecommendPromptLibrary.cs)

```csharp
public static string GenerateLearningFeedbackPrompt(/* parameters */)
{
    return $@"
# NHIỆM VỤ: Phân tích và tạo Learning Feedback cho sinh viên

## 1. THÔNG TIN SINH VIÊN

### Career Goal: {careerGoal}

### Major: {majorCode} - {majorName}

### Subject Marks (Điểm các môn đã học):
{subjectMarksJson}

### Quiz Survey (Khảo sát thói quen & sở thích):
{quizSurveyJson}

---

## 2. YÊU CẦU OUTPUT (JSON Format)

Trả về JSON với cấu trúc sau:

```json
{{
  ""SummaryFeedback"": ""## Tổng quan về lộ trình học tập\n\n[Markdown content]"",
  ""HabitAndInterestAnalysis"": ""## Phân tích thói quen và sở thích\n\n[Markdown]"",
  ""Personality"": ""## Phân tích tính cách\n\n[Markdown]"",
  ""LearningAbility"": ""## Đánh giá khả năng học tập\n\n[Markdown]"",
  
  ""MajorFeedbacks"": [
    {{
      ""MajorCode"": ""SE"",
      ""MajorName"": ""Software Engineering"",
      ""Reason"": ""## Lý do chọn chuyên ngành Software Engineering\n\n[Markdown explaining why this major fits the student]"",
      ""SubjectAnalyses"": [
        {{
          ""SubjectCode"": ""OOP"",
          ""SubjectName"": ""Object-Oriented Programming"",
          ""AnalysisMarkdown"": ""## Phân tích môn OOP\n\n### Điểm hiện tại: X\n\n### Đánh giá:...\n\n### Gợi ý cải thiện:...""
        }},
        {{
          ""SubjectCode"": ""DSA"",
          ""SubjectName"": ""Data Structures & Algorithms"",
          ""AnalysisMarkdown"": ""## Phân tích môn DSA\n\n...""
        }}
      ]
    }},
    {{
      ""MajorCode"": ""AI"",
      ""MajorName"": ""Artificial Intelligence"",
      ""Reason"": ""## Gợi ý học thêm AI\n\n[Markdown explaining why student should consider AI]"",
      ""SubjectAnalyses"": [...]
    }}
  ]
}}
```

## 3. CHI TIẾT YÊU CẦU

### 3.1. SummaryFeedback
- Tổng quan về toàn bộ lộ trình
- Đánh giá chung về năng lực hiện tại
- Định hướng phát triển

### 3.2. MajorFeedbacks
- **Phải bao gồm tất cả major codes có trong request**
- Mỗi major cần:
  - **Reason**: Giải thích tại sao major này phù hợp/không phù hợp với sinh viên
  - **SubjectAnalyses**: Phân tích chi tiết từng môn trong major đó

### 3.3. SubjectAnalyses Format
Mỗi subject cần:
- Đánh giá điểm hiện tại (nếu có)
- Phân tích điểm mạnh/yếu
- Gợi ý cải thiện cụ thể
- Liên hệ với career goal

---

## 4. LƯU Ý QUAN TRỌNG
- Output PHẢI là valid JSON
- Tất cả markdown content PHẢI escape đúng trong JSON string
- Không bỏ sót bất kỳ major code nào trong request
- Subject analyses phải liên quan đến major tương ứng
";
}
```

---

## 8. Testing & Validation

### ✅ Test Cases

#### **Test 1: Single Major with Multiple Subjects**
```json
Input:
{
  "MajorCode": "SE",
  "SubjectMarks": [
    { "SubjectCode": "OOP", "Mark": 6.5 },
    { "SubjectCode": "DSA", "Mark": 7.0 }
  ]
}

Expected Output:
- 1 LearningPath
- 1 LearningPathMajor (SE)
  - Reason = AI feedback cho SE
  - 2 LearningPathSubjectCodes (OOP, DSA)
    - Each with AnalysisMarkdown
- All courses linked correctly to subjects
```

#### **Test 2: Multiple Majors**
```json
Input:
{
  "MajorCodes": ["SE", "AI"],
  "SubjectMarks": [
    { "SubjectCode": "OOP", "Mark": 6.5 },
    { "SubjectCode": "ML", "Mark": 5.0 }
  ]
}

Expected Output:
- 1 LearningPath
- 2 LearningPathMajors (SE, AI)
  - SE.Reason = "SE phù hợp vì..."
  - AI.Reason = "Nên học thêm AI vì..."
- Subjects distributed correctly to majors
```

#### **Test 3: Courses without Subject Feedback**
```json
Scenario:
- Course "Soft Skills" with SubjectCode = "SOFT"
- No SubjectCode entry for "SOFT" in feedback

Expected:
- Course.LearningPathSubjectCodeId = null
- Course still belongs to LearningPathMajor
```

---

### 🔍 Validation Queries

```sql
-- Check Major Reason is populated
SELECT 
    lpm.MajorCode,
    lpm.Reason,
    COUNT(lpsc.LearningPathSubjectCodeId) as SubjectCount
FROM LearningPathMajor lpm
LEFT JOIN LearningPathSubjectCode lpsc ON lpm.LearningPathMajorId = lpsc.LearningPathMajorId
WHERE lpm.PathId = @LearningPathId
GROUP BY lpm.MajorCode, lpm.Reason;

-- Check Subject Codes linked correctly
SELECT 
    lpsc.SubjectCode,
    lpsc.AnalysisMarkdown,
    COUNT(lpc.LearningPathCourseId) as CourseCount
FROM LearningPathSubjectCode lpsc
LEFT JOIN LearningPathCourse lpc ON lpsc.LearningPathSubjectCodeId = lpc.LearningPathSubjectCodeId
WHERE lpsc.LearningPathMajorId IN (
    SELECT LearningPathMajorId 
    FROM LearningPathMajor 
    WHERE PathId = @LearningPathId
)
GROUP BY lpsc.SubjectCode, lpsc.AnalysisMarkdown;

-- Check courses linked to subjects
SELECT 
    lpc.SubjectCode,
    lpc.LearningPathSubjectCodeId,
    CASE 
        WHEN lpc.LearningPathSubjectCodeId IS NULL THEN 'Not Linked'
        ELSE 'Linked'
    END as LinkStatus
FROM LearningPathCourse lpc
WHERE lpc.LearningPathMajorId IN (
    SELECT LearningPathMajorId 
    FROM LearningPathMajor 
    WHERE PathId = @LearningPathId
);
```

---

## 💡 Tại Sao Phải Dùng Async Architecture?

### ⏱️ Performance & User Experience

#### **Nếu Chạy Sync (Blocking):**
```
Client Request
    ↓ (waiting...)
Save Survey (0.5s)
    ↓ (waiting...)
AI Recommend Majors (5-10s) 🐌
    ↓ (waiting...)
Insert Majors & Courses (2-3s)
    ↓ (waiting...)
AI Generate Feedback (15-20s) 🐌🐌
    ↓ (waiting...)
Save Feedback (1s)
    ↓
Response to Client ✅ (TOTAL: 23-35 seconds!!!)
```

**Vấn đề:**
- ❌ Client phải đợi **20-35 giây** mới có response
- ❌ Timeout risk (nhiều API gateway có timeout 30s)
- ❌ User experience tệ (loading quá lâu)
- ❌ Server resources bị giữ (thread blocking)

---

#### **Với Async (Event-Driven):**
```
Client Request
    ↓
Save Survey (0.5s)
Create Learning Path (0.3s)
Publish Event to Message Bus (0.1s)
    ↓
✅ Response to Client NGAY LẬP TỨC (< 1 second)

──────────────────────────────────────
Background (không block client):
    ↓
AI Recommend Majors (5-10s)
    ↓ (parallel)
Insert Majors & Courses (2-3s)
    ↓
AI Generate Feedback (15-20s)
    ↓
Save Feedback (1s)
    ↓
✅ Done (client đã có response từ lâu)
```

**Lợi ích:**
- ✅ Client response **< 1 giây** (chỉ tạo learning path)
- ✅ No timeout risk
- ✅ User experience tốt (loading nhanh)
- ✅ Scalable (AI tasks chạy trên worker nodes)
- ✅ Fault tolerance (retry mechanism cho failed events)

---

### 🔄 Message Bus Benefits

#### **MassTransit + RabbitMQ:**
```
┌─────────────┐         ┌──────────────┐
│ QuizService │────────►│ Message Bus  │
└─────────────┘         │  (RabbitMQ)  │
                        └──────┬───────┘
                               │
                ┌──────────────┼──────────────┐
                ↓              ↓              ↓
         ┌────────────┐ ┌────────────┐ ┌────────────┐
         │ AiService  │ │ Student    │ │ Course     │
         │ (Worker 1) │ │ Service    │ │ Service    │
         └────────────┘ └────────────┘ └────────────┘
```

**Features:**
- ✅ **Guaranteed Delivery**: Event không bị mất
- ✅ **Retry Mechanism**: Tự động retry nếu failed
- ✅ **Dead Letter Queue**: Failed events vào DLQ để debug
- ✅ **Scalability**: Thêm workers khi cần
- ✅ **Decoupling**: Services không phụ thuộc trực tiếp

---

### 📊 Real-World Example

```
Student làm khảo sát lúc 10:00:00
    ↓
10:00:01 - ✅ Nhận response { LearningPathId }
           → UI hiển thị: "Đang xử lý lộ trình học tập của bạn..."
           → Student có thể đóng browser, làm việc khác

10:00:10 - (Background) Majors được recommend
10:00:15 - (Background) Courses được insert
10:00:35 - (Background) AI feedback được generate ✅
           → Push notification: "Lộ trình học tập đã sẵn sàng!"
           → Student mở lại app → Xem full feedback
```

**User Journey:**
1. Submit survey → Nhận confirmation ngay (< 1s)
2. UI polling hoặc WebSocket để check status
3. Khi ready → Notification → View results

---

## 📌 Summary

### Điểm chính cần nhớ:

1. **AI phải generate feedback theo Major** (không chỉ Subject)
2. **LearningPathMajor.Reason** = Feedback cho major đó
3. **LearningPathSubjectCode.AnalysisMarkdown** = Feedback cho subject
4. **LearningPathSubjectCode.LearningPathMajorId** = Phải link đúng với Major
5. **LearningPathCourse.LearningPathSubjectCodeId** = Link course với subject (nếu có)

### Flow hoàn chỉnh (Event-Driven Architecture):

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         COMPLETE ASYNC FLOW                                  │
└─────────────────────────────────────────────────────────────────────────────┘

CLIENT                    QUIZSERVICE              AISERVICE           STUDENTSERVICE
  │                            │                        │                     │
  │ 1. POST Survey             │                        │                     │
  ├───────────────────────────►│                        │                     │
  │                            │                        │                     │
  │                            │ 2. Save Answers        │                     │
  │                            │ 3. Create LearningPath │                     │
  │                            │    (PathId)            │                     │
  │                            │                        │                     │
  │◄───────────────────────────┤ ⚡ RETURN PathId                             │
  │ Response { PathId }        │    (< 1 second)                              │
  │ (Client có thể đóng app)   │                        │                     │
  │                            │                        │                     │
  │                            │                        │                     │
  │                     [ASYNC PROCESSING STARTS]       │                     │
  │                            │                        │                     │
  │                            │ 4. Publish Event       │                     │
  │                            ├───────────────────────►│                     │
  │                            │ StudentMajorOrientation│                     │
  │                            │        Event           │                     │
  │                            │                        │                     │
  │                            │                        │ 5. AI Recommend     │
  │                            │                        │    Majors           │
  │                            │                        │    (5-10s)          │
  │                            │                        │                     │
  │                            │                        │ 6. Publish          │
  │                            │                        ├────────────────────►│
  │                            │                        │ InternalMajorEvent  │
  │                            │                        │ { Majors: [SE,AI] } │
  │                            │                        │                     │
  │                            │                        │         7. Insert   │
  │                            │                        │            - Majors │
  │                            │                        │            - Courses│
  │                            │                        │            (2-3s)   │
  │                            │                        │                     │
  │                            │                        │         8. Publish  │
  │                            │                        │◄────────────────────┤
  │                            │                        │ AiRecommend         │
  │                            │                        │ ImprovementEvent    │
  │                            │                        │ { Majors: [SE,AI],  │
  │                            │                        │   Subjects: [...] } │
  │                            │                        │                     │
  │                            │                        │ 9. AI Generate      │
  │                            │                        │    Feedback         │
  │                            │                        │    - Per Major      │
  │                            │                        │    - Per Subject    │
  │                            │                        │    (15-20s)         │
  │                            │                        │                     │
  │                            │                        │ 10. Publish         │
  │                            │                        ├────────────────────►│
  │                            │                        │ LearningFeedback    │
  │                            │                        │ Event               │
  │                            │                        │ { MajorFeedbacks:   │
  │                            │                        │   [SE, AI] }        │
  │                            │                        │                     │
  │                            │                        │         11. Update  │
  │                            │                        │             - Path  │
  │                            │                        │             - Majors│
  │                            │                        │             - Subs  │
  │                            │                        │             - Link  │
  │                            │                        │             (1s)    │
  │                            │                        │                     │
  │ 12. Notification           │                        │                     │
  │◄───────────────────────────┴────────────────────────┴─────────────────────┤
  │ "Lộ trình đã sẵn sàng!"    │                        │                     │
  │                            │                        │                     │
  │ 13. GET LearningPath       │                        │                     │
  ├────────────────────────────┴────────────────────────┴────────────────────►│
  │                            │                        │                     │
  │◄───────────────────────────┴────────────────────────┴─────────────────────┤
  │ Full Learning Path with Feedbacks ✅                                      │
  │                            │                        │                     │

TOTAL TIME FOR CLIENT: < 1 second (step 1-3)
TOTAL BACKGROUND TIME: 22-34 seconds (step 4-11)
```

### Key Points trong Flow:

1. **Step 1-3 (Sync):** Client submit → Server tạo LearningPath → Response ngay
2. **Step 4 (Event Bus):** Publish event vào message queue
3. **Step 5-11 (Async Background):** Tất cả AI processing chạy background
4. **Step 12-13 (Optional):** Notification + Client fetch results sau

### Architecture Benefits:

- ✅ **Fast Response:** Client nhận response < 1s
- ✅ **Fault Tolerant:** Events có retry mechanism
- ✅ **Scalable:** Thêm workers khi cần
- ✅ **Decoupled:** Services độc lập
- ✅ **Observable:** Track được từng bước qua event logs

---

## 🔍 Client-Side: Track Async Progress

### Option 1: Polling (Đơn giản)

```typescript
// Client code example
async function submitSurvey(surveyData) {
  // 1. Submit survey
  const response = await api.post('/api/student-survey', surveyData);
  const { learningPathId } = response.data.response;
  
  // 2. Show loading UI
  showLoadingMessage('Đang xử lý lộ trình học tập của bạn...');
  
  // 3. Poll for status
  const checkStatus = async () => {
    const status = await api.get(`/api/learning-paths/${learningPathId}/status`);
    
    switch (status.data.status) {
      case 'PROCESSING':
        // Still processing, check again after 3s
        setTimeout(checkStatus, 3000);
        break;
        
      case 'READY':
        // Done! Fetch full data
        const learningPath = await api.get(`/api/learning-paths/${learningPathId}`);
        showLearningPath(learningPath.data);
        break;
        
      case 'FAILED':
        showError('Có lỗi xảy ra, vui lòng thử lại');
        break;
    }
  };
  
  // Start polling after 5 seconds (give time for first AI processing)
  setTimeout(checkStatus, 5000);
}
```

**Backend API cần implement:**
```csharp
[HttpGet("{learningPathId}/status")]
public async Task<LearningPathStatusResponse> GetLearningPathStatus(Guid learningPathId)
{
    var learningPath = await _learningPathService.GetByIdAsync(learningPathId);
    
    // Check if feedback is ready
    bool hasFeedback = !string.IsNullOrEmpty(learningPath.SummaryFeedback);
    bool hasMajors = learningPath.LearningPathMajors.Any();
    bool hasCourses = learningPath.LearningPathMajors.Any(m => m.LearningPathCourses.Any());
    
    var status = hasFeedback && hasMajors && hasCourses 
        ? LearningPathStatus.READY 
        : LearningPathStatus.PROCESSING;
    
    return new LearningPathStatusResponse 
    { 
        Status = status,
        Progress = CalculateProgress(hasMajors, hasCourses, hasFeedback)
    };
}
```

---

### Option 2: WebSocket / SignalR (Real-time, phức tạp hơn)

```csharp
// Backend: Push notification khi ready
public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
{
    private readonly IHubContext<LearningPathHub> _hubContext;
    
    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        // ... existing code to save feedback
        
        // Push real-time update to client
        await _hubContext.Clients
            .User(evt.Email)
            .SendAsync("LearningPathReady", new 
            {
                LearningPathId = evt.LearningPathId,
                Message = "Lộ trình học tập của bạn đã sẵn sàng!"
            });
    }
}
```

```typescript
// Client: Listen to WebSocket
const connection = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/learning-path')
  .build();

connection.on('LearningPathReady', (data) => {
  showNotification(data.Message);
  fetchAndDisplayLearningPath(data.LearningPathId);
});

await connection.start();
```

---

### Option 3: Push Notification (Mobile/PWA)

```csharp
// Backend: Send push notification
public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
{
    private readonly IPushNotificationService _pushService;
    
    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        // ... existing code
        
        // Send push notification
        await _pushService.SendAsync(new PushNotification
        {
            UserId = evt.StudentId,
            Title = "Lộ trình học tập đã sẵn sàng!",
            Body = "Hãy xem lộ trình học tập được AI tạo riêng cho bạn",
            Data = new { LearningPathId = evt.LearningPathId }
        });
    }
}
```

---

### 🎯 Recommended Approach

**Kết hợp cả 3:**
1. **Polling** cho basic tracking (fallback)
2. **WebSocket** cho real-time updates (nếu connection available)
3. **Push Notification** cho mobile apps

```typescript
class LearningPathTracker {
  private useWebSocket = true;
  private pollingInterval: NodeJS.Timer | null = null;
  
  async track(learningPathId: string) {
    // Try WebSocket first
    if (this.useWebSocket) {
      try {
        await this.connectWebSocket();
      } catch (error) {
        // Fallback to polling if WebSocket fails
        this.useWebSocket = false;
        this.startPolling(learningPathId);
      }
    } else {
      this.startPolling(learningPathId);
    }
  }
  
  private startPolling(learningPathId: string) {
    this.pollingInterval = setInterval(async () => {
      const status = await checkStatus(learningPathId);
      if (status === 'READY') {
        this.stopTracking();
        this.onReady(learningPathId);
      }
    }, 3000);
  }
  
  private stopTracking() {
    if (this.pollingInterval) {
      clearInterval(this.pollingInterval);
    }
  }
}
```

---

## 🚀 Next Steps

1. **Implement theo thứ tự Phase 0 → 5** (bắt đầu từ Phase 0 - Event structure)
2. **Test từng phase trước khi chuyển sang phase tiếp theo**
3. **Validate database sau mỗi lần test**
4. **Update Read Model (Phase 5) sau khi tất cả logic đã ổn**

---

---

## 🛠️ Error Handling & Monitoring

### ⚠️ Common Issues & Solutions

#### **Issue 1: Event Processing Failed**

**Scenario:** AI service bị crash khi đang generate feedback

**Solution:** MassTransit Retry Policy
```csharp
// Startup.cs / Program.cs
services.AddMassTransit(x =>
{
    x.AddConsumer<LearningFeedbackEventConsumer>();
    
    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.UseMessageRetry(r => 
        {
            r.Interval(3, TimeSpan.FromSeconds(5)); // Retry 3 lần, mỗi lần cách 5s
            r.Handle<HttpRequestException>(); // Retry cho network errors
            r.Handle<TimeoutException>();
        });
        
        // Dead Letter Queue cho failed messages
        cfg.ReceiveEndpoint("learning-feedback-queue", e =>
        {
            e.ConfigureConsumer<LearningFeedbackEventConsumer>(context);
            
            e.UseMessageRetry(r => r.Interval(3, TimeSpan.FromMinutes(1)));
            
            // Sau 3 lần retry → DLQ
            e.BindDeadLetterQueue("learning-feedback-dlq");
        });
    });
});
```

---

#### **Issue 2: Partial Processing**

**Scenario:** 
- Majors được insert ✅
- Courses được insert ✅
- AI feedback FAILED ❌

**Solution:** Idempotency + Status Tracking
```csharp
public class LearningPath
{
    public Guid PathId { get; set; }
    
    // ✅ ADD: Track processing status
    public short Status { get; set; } // 0: Created, 1: Processing, 2: Ready, 3: Failed
    
    public DateTime? MajorsGeneratedAt { get; set; }
    public DateTime? FeedbackGeneratedAt { get; set; }
    
    // Existing fields...
}

// Consumer với idempotency
public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
{
    var evt = context.Message;
    
    // Check if already processed (idempotency)
    var learningPath = await _repo.FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId);
    
    if (learningPath.FeedbackGeneratedAt != null)
    {
        // Already processed, skip
        return;
    }
    
    try
    {
        // Process...
        learningPath.Status = (short)LearningPathStatus.Ready;
        learningPath.FeedbackGeneratedAt = DateTime.UtcNow;
        
        await _repo.UpdateAsync(learningPath);
    }
    catch (Exception ex)
    {
        learningPath.Status = (short)LearningPathStatus.Failed;
        await _repo.UpdateAsync(learningPath);
        
        // Log error
        _logger.LogError(ex, "Failed to process learning feedback for path {PathId}", evt.LearningPathId);
        
        throw; // Re-throw để MassTransit retry
    }
}
```

---

### 📊 Monitoring & Observability

#### **1. Structured Logging**

```csharp
public class LearningFeedbackEventConsumer : IConsumer<LearningFeedbackEvent>
{
    private readonly ILogger<LearningFeedbackEventConsumer> _logger;
    
    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        var evt = context.Message;
        
        using (_logger.BeginScope(new Dictionary<string, object>
        {
            ["LearningPathId"] = evt.LearningPathId,
            ["Email"] = evt.Email,
            ["MajorCount"] = evt.MajorFeedbacks.Count
        }))
        {
            _logger.LogInformation("Starting to process learning feedback");
            
            var sw = Stopwatch.StartNew();
            
            try
            {
                // Process...
                
                _logger.LogInformation("Completed processing in {ElapsedMs}ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process learning feedback after {ElapsedMs}ms", 
                    sw.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
```

---

#### **2. Application Insights / ELK Stack**

**Metrics to track:**
```
- event.processing.time (histogram)
- event.processing.success (counter)
- event.processing.failed (counter)
- event.retry.count (counter)
- learningpath.created (counter)
- learningpath.ready (counter)
- ai.generation.time (histogram)
```

**Query Examples (Application Insights):**
```kusto
// Average processing time per event type
customMetrics
| where name == "event.processing.time"
| summarize avg(value), percentile(value, 95) by tostring(customDimensions.EventType)

// Failed events in last hour
traces
| where severityLevel >= 3 // Error or Critical
| where message contains "Failed to process"
| where timestamp > ago(1h)
| project timestamp, message, customDimensions.LearningPathId

// Events stuck in processing > 5 minutes
let threshold = 5m;
LearningPath
| where Status == 1 // Processing
| where UpdatedAt < ago(threshold)
| project PathId, StudentId, UpdatedAt, timeSinceUpdate = now() - UpdatedAt
```

---

#### **3. Health Checks**

```csharp
// Startup.cs
services.AddHealthChecks()
    .AddCheck<MessageBusHealthCheck>("message-bus")
    .AddCheck<AiServiceHealthCheck>("ai-service")
    .AddDbContextCheck<StudentServiceDbContext>("database");

// MessageBusHealthCheck.cs
public class MessageBusHealthCheck : IHealthCheck
{
    private readonly IBus _bus;
    
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, 
        CancellationToken ct)
    {
        try
        {
            // Check if message bus is responsive
            var busHealth = await _bus.GetProbeResult();
            
            return busHealth.Status == HealthStatus.Healthy
                ? HealthCheckResult.Healthy("Message bus is operational")
                : HealthCheckResult.Unhealthy("Message bus is not responding");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Message bus check failed", ex);
        }
    }
}
```

---

### 🐛 Debugging Strategies

#### **Debug Async Flow:**

1. **Enable MassTransit Diagnostics:**
```csharp
cfg.UseMessageRetry(r => r.Immediate(3));
cfg.ConfigureEndpoints(context);

// ✅ Enable diagnostics
cfg.UseInMemoryOutbox();
cfg.ConfigureSend(s => s.UseExecute(ctx => 
{
    Console.WriteLine($"Sending: {ctx.Message.GetType().Name}");
}));
```

2. **Trace Correlation ID:**
```csharp
// Add correlation ID to events
public class StudentMajorOrientationEvent
{
    public Guid CorrelationId { get; set; } = Guid.NewGuid();
    // ... other fields
}

// Log correlation ID in all consumers
_logger.LogInformation("Processing event {EventType} with CorrelationId {CorrelationId}", 
    nameof(StudentMajorOrientationEvent), evt.CorrelationId);
```

3. **Use RabbitMQ Management UI:**
```
http://localhost:15672
- View queues
- Check message counts
- Inspect DLQ
- Monitor consumer status
```

---

### 🔧 Troubleshooting Checklist

**If LearningPath stuck in "Processing":**

- [ ] Check RabbitMQ: Are events in queue?
- [ ] Check AiService logs: Any errors?
- [ ] Check DLQ: Are messages in dead letter queue?
- [ ] Check database: Is `Status` field correct?
- [ ] Check network: Can services communicate?
- [ ] Check AI API: Is API responding?

**Query to find stuck learning paths:**
```sql
SELECT 
    lp.PathId,
    lp.Status,
    lp.CreatedAt,
    lp.UpdatedAt,
    DATEDIFF(MINUTE, lp.UpdatedAt, GETDATE()) as MinutesSinceUpdate,
    COUNT(lpm.LearningPathMajorId) as MajorCount,
    (SELECT COUNT(*) FROM LearningPathCourse WHERE LearningPathMajorId IN 
        (SELECT LearningPathMajorId FROM LearningPathMajor WHERE PathId = lp.PathId)) as CourseCount
FROM LearningPath lp
LEFT JOIN LearningPathMajor lpm ON lp.PathId = lpm.PathId
WHERE lp.Status = 1 -- Processing
  AND lp.UpdatedAt < DATEADD(MINUTE, -10, GETDATE()) -- Stuck > 10 minutes
GROUP BY lp.PathId, lp.Status, lp.CreatedAt, lp.UpdatedAt;
```

---

**Tài liệu này sẽ được sử dụng làm context cho việc implementation. Hãy đọc kỹ và follow từng bước!** 🎯

---

## 📚 Reference Links

### Documentation:
- **MassTransit**: https://masstransit-project.com/
- **RabbitMQ**: https://www.rabbitmq.com/documentation.html
- **SignalR**: https://learn.microsoft.com/en-us/aspnet/core/signalr/

### Related Patterns:
- **Event-Driven Architecture**: https://martinfowler.com/articles/201701-event-driven.html
- **Saga Pattern**: https://microservices.io/patterns/data/saga.html
- **Outbox Pattern**: https://microservices.io/patterns/data/transactional-outbox.html

### Best Practices:
- **Idempotency**: https://aws.amazon.com/builders-library/making-retries-safe-with-idempotent-APIs/
- **Error Handling**: https://www.enterpriseintegrationpatterns.com/patterns/messaging/DeadLetterChannel.html

