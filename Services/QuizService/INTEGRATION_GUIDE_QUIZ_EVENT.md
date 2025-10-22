# Integration Guide: Publish QuizModuleCompletedEvent from QuizService

## Purpose
When a student completes a module quiz, QuizService needs to publish `QuizModuleCompletedEvent` so that StudentService can automatically:
1. Save the quiz result
2. Check if the student has failed 40% of module quizzes
3. Create course suggestion if threshold is met

## Important Discovery

QuizService already has `StudentQuiz` entity with the following structure:
```csharp
public partial class StudentQuiz
{
    public Guid StudentQuizId { get; set; }
    public Guid StudentId { get; set; }
    public Guid QuizId { get; set; }
    public short QuizType { get; set; }
    public Guid? CourseId { get; set; }           // ✅ Available
    public short? Scope { get; set; }              // ✅ 1=Lesson, 2=Module
    public Guid? ScopeId { get; set; }             // ✅ ModuleId or LessonId
    public short? TotalQuestions { get; set; }     // ✅ Available
    public short? TotalCorrect { get; set; }       // ✅ Available
    public short? Score100 { get; set; }           // ✅ Score 0-100
}
```

## Implementation Steps

### Step 1: Find StudentQuiz Insert/Update Handler

Find the handler that processes quiz submission in QuizService. This is likely in:
- `QuizService.Application/Applications/StudentQuizzes/Commands/` or
- `QuizService.Infrastructure/Implements/StudentQuizService.cs`

Look for methods like:
- `InsertStudentQuizAsync`
- `SubmitQuizAsync`
- `SaveStudentQuizResultAsync`

### Step 2: Add Event Publishing

Add this code after successfully saving the `StudentQuiz` result:

```csharp
using BuildingBlocks.Messaging.Events.QuizService;
using BaseService.Common.Utils.Const;
using MassTransit;

public class StudentQuizHandler // or whatever your handler/service is called
{
    private readonly IPublishEndpoint _publishEndpoint; // Add this
    
    public StudentQuizHandler(
        // ...existing dependencies...
        IPublishEndpoint publishEndpoint) // Add this
    {
        _publishEndpoint = publishEndpoint;
    }
    
    public async Task<Response> Handle(Command request, CancellationToken cancellationToken)
    {
        // ...existing code to save StudentQuiz result...
        
        var studentQuiz = new StudentQuiz
        {
            StudentQuizId = Guid.NewGuid(),
            StudentId = request.StudentId,
            QuizId = request.QuizId,
            CourseId = request.CourseId,      // Make sure this is populated
            Scope = request.Scope,             // 1=Lesson, 2=Module
            ScopeId = request.ScopeId,         // ModuleId or LessonId
            TotalQuestions = totalQuestions,
            TotalCorrect = correctAnswers,
            Score100 = score100,               // Score from 0-100
            // ...other fields...
        };
        
        await _repository.AddAsync(studentQuiz);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        // ✅ ADD THIS: Publish event ONLY for MODULE quizzes
        if (studentQuiz.Scope == (short)ConstantEnum.QuizScope.Module 
            && studentQuiz.CourseId.HasValue 
            && studentQuiz.ScopeId.HasValue)
        {
            // Convert score from 0-100 to 0-10 scale
            var score10 = studentQuiz.Score100.HasValue 
                ? (decimal)studentQuiz.Score100.Value / 10m 
                : 0m;
            
            await _publishEndpoint.Publish(new QuizModuleCompletedEvent
            {
                StudentId = studentQuiz.StudentId,
                CourseId = studentQuiz.CourseId.Value,
                ModuleId = studentQuiz.ScopeId.Value,  // ScopeId is ModuleId when Scope=2
                QuizId = studentQuiz.QuizId,
                Score = score10,  // Convert to 0-10 scale (threshold is 4.0)
                TotalQuestions = studentQuiz.TotalQuestions ?? 0,
                CorrectAnswers = studentQuiz.TotalCorrect ?? 0,
                CompletedAt = DateTime.UtcNow
            }, cancellationToken);
            
            _logger.LogInformation(
                "Published QuizModuleCompletedEvent for Student {StudentId}, Quiz {QuizId}, Score {Score}/10",
                studentQuiz.StudentId, studentQuiz.QuizId, score10);
        }
        
        // ...rest of the code...
        return response;
    }
}
```

### Step 3: Key Points

1. **Only publish for MODULE quizzes**
   ```csharp
   if (studentQuiz.Scope == (short)ConstantEnum.QuizScope.Module)
   ```

2. **Score conversion is critical**
   - `StudentQuiz.Score100` is 0-100 scale
   - Event needs 0-10 scale: `score10 = Score100 / 10`
   - Threshold check uses `score < 4.0`

3. **Ensure required fields are populated**
   - `CourseId` must have value
   - `ScopeId` must have value (this is the ModuleId)
   - `Scope` must be 2 (Module)

4. **Event is fire-and-forget**
   - No need to wait for response
   - StudentService will handle it asynchronously

### Step 4: Example Integration Point

```csharp
public async Task<InsertStudentQuizResponse> InsertStudentQuizAsync(
    InsertStudentQuizCommand command, 
    CancellationToken cancellationToken)
{
    var response = new InsertStudentQuizResponse { Success = false };
    
    // Calculate score (0-100)
    var score100 = CalculateScore(command.Answers);
    
    // Save to database
    var studentQuiz = new StudentQuiz
    {
        StudentQuizId = Guid.NewGuid(),
        StudentId = command.StudentId,
        QuizId = command.QuizId,
        CourseId = command.CourseId,
        Scope = command.Scope,  // From request or quiz settings
        ScopeId = command.ModuleId, // ModuleId when Scope=2
        TotalQuestions = command.Answers.Count,
        TotalCorrect = correctAnswersCount,
        Score100 = (short)score100,
        QuizType = (short)ConstantEnum.TestType.Quiz,
        IsActive = true
    };
    
    await _studentQuizRepository.AddAsync(studentQuiz, command.StudentId.ToString());
    await _unitOfWork.SaveChangesAsync(cancellationToken);
    
    // ✅ Publish event if it's a module quiz
    if (studentQuiz.Scope == (short)ConstantEnum.QuizScope.Module 
        && studentQuiz.CourseId.HasValue 
        && studentQuiz.ScopeId.HasValue)
    {
        var score10 = (decimal)studentQuiz.Score100 / 10m;
        
        await _publishEndpoint.Publish(new QuizModuleCompletedEvent
        {
            StudentId = studentQuiz.StudentId,
            CourseId = studentQuiz.CourseId.Value,
            ModuleId = studentQuiz.ScopeId.Value,
            QuizId = studentQuiz.QuizId,
            Score = score10,
            TotalQuestions = studentQuiz.TotalQuestions ?? 0,
            CorrectAnswers = studentQuiz.TotalCorrect ?? 0,
            CompletedAt = DateTime.UtcNow
        }, cancellationToken);
    }
    
    response.Success = true;
    response.Response = new StudentQuizDto { /* ... */ };
    return response;
}
```

## Testing

1. **Submit a module quiz with score < 40 (on 0-100 scale) = < 4.0 (on 0-10 scale)**
2. **Check logs for "Published QuizModuleCompletedEvent"**
3. **Check StudentService logs for "Received QuizModuleCompletedEvent"**
4. **Verify student_quizzes table in StudentService has the record**
5. **Submit enough quizzes to hit 40% threshold (e.g., 5 out of 12 modules with score < 4.0)**
6. **Verify course_suggestions table has a suggestion**

## Score Conversion Table

| Score100 (0-100) | Score10 (0-10) | Pass/Fail |
|------------------|----------------|-----------|
| 0-39             | 0.0-3.9        | ❌ Failed |
| 40               | 4.0            | ✅ Passed |
| 50               | 5.0            | ✅ Passed |
| 100              | 10.0           | ✅ Passed |

Threshold: **< 4.0 on 0-10 scale = < 40 on 0-100 scale**

## Notes

- The event consumer in StudentService will automatically handle retry if there's an error
- Make sure `CourseId` and `ScopeId` (ModuleId) are properly populated in StudentQuiz entity
- The `Scope` field distinguishes between Lesson (1) and Module (2) quizzes
- Score conversion is critical: divide by 10 to convert from 0-100 to 0-10 scale
- Make sure MassTransit is properly configured in QuizService to publish events
