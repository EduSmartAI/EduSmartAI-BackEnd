using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Application.Interfaces;
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
    private readonly IRequestClient<MappingSubjectCodeWithMajorCodeEvent> _requestClient;
    private readonly ILearningPathRealtimeNotifier _learningPathRealtimeNotifier;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<MajorAndSemesterSelectEvent> _requestClientMajorAndSemesterSelect;
    private readonly IQueryRepository<StudentCollection>  _studentCollectionRepository;


    private readonly IUnitOfWork _unitOfWork;

    public LearningFeedbackEventConsumer(
        ICommandRepository<LearningPath> learningPathRepository,
        ICommandRepository<LearningPathMajor> learningPathMajorRepository,
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseRepository,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IUnitOfWork unitOfWork, IRequestClient<MappingSubjectCodeWithMajorCodeEvent> requestClient, ILearningPathRealtimeNotifier learningPathRealtimeNotifier, IRequestClient<StudentTranscriptSelectEvent> requestStudentTranscriptClient, IRequestClient<MajorAndSemesterSelectEvent> requestClientMajorAndSemesterSelect, IQueryRepository<StudentCollection> studentCollectionRepository)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathMajorRepository = learningPathMajorRepository;
        _learningPathSubjectCodeRepository = learningPathSubjectCodeRepository;
        _learningPathCourseRepository = learningPathCourseRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
        _learningPathRealtimeNotifier = learningPathRealtimeNotifier;
        _requestStudentTranscriptClient = requestStudentTranscriptClient;
        _requestClientMajorAndSemesterSelect = requestClientMajorAndSemesterSelect;
        _studentCollectionRepository = studentCollectionRepository;
    }

    public async Task Consume(ConsumeContext<LearningFeedbackEvent> context)
    {
        var evt = context.Message;
        
        // 1. Update Learning Path global feedback
        var learningPath = await _learningPathRepository.FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive)!;
        if (learningPath == null)
        {
            return;
        }
        // Get student transcript (always needed for SubjectMarks in AI event)
        var studentTranscriptEvent = new StudentTranscriptSelectEvent
        {
            StudentId = learningPath.StudentId ?? throw new NullReferenceException(),
        };
        var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent);
        var studentTranscripts = transcriptResponse.Message.Response;

        var student =
            await _studentCollectionRepository.FirstOrDefaultAsync(x =>
                x.StudentId == learningPath.StudentId && x.IsActive);
        
        var responseMajorAndSemester = await _requestClientMajorAndSemesterSelect
            .GetResponse<MajorAndSemesterSelectEventResponse>(new MajorAndSemesterSelectEvent
            {
                MajorId = student!.MajorId,
                SemesterId = student.SemesterId
            });
        if (!responseMajorAndSemester.Message.Success)
        {
            return;
        }
        
        learningPath.SummaryFeedback = evt.SummaryFeedback;
        learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
        learningPath.Personality = evt.Personality;
        learningPath.LearningAbility = evt.LearningAbility;
        learningPath.AbilityFeedback = evt.AbilityAnalyses != null && evt.AbilityAnalyses.Any()
            ? string.Join($"{Environment.NewLine}{new string('*', 15)}{Environment.NewLine}",
                evt.AbilityAnalyses.Select(a => $"{a.Name}: {a.AnalysisMarkdown}"))
            : null;
        _learningPathRepository.Update(learningPath);
        
        // 1.5. Parse EvaluationAndImprove to get OtherQuestionCodes
        var evaluationCodes = new HashSet<ConstantEnum.OtherQuestionCode>();
        if (!string.IsNullOrEmpty(learningPath.EvaluationAndImprove))
        {
            var codeStrings = learningPath.EvaluationAndImprove.Split(',', StringSplitOptions.RemoveEmptyEntries);
            foreach (var codeStr in codeStrings)
            {
                if (int.TryParse(codeStr.Trim(), out var codeInt) && 
                    Enum.IsDefined(typeof(ConstantEnum.OtherQuestionCode), codeInt))
                {
                    evaluationCodes.Add((ConstantEnum.OtherQuestionCode)codeInt);
                }
            }
        }
        
        // 1.6. Build set of subject codes that should be included based on EvaluationAndImprove
        var subjectCodesForEvaluation = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var subjectCodesForCourseImprove = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        if (evaluationCodes.Any() && studentTranscripts.Any())
        {
            foreach (var questionCode in evaluationCodes)
            {
                switch (questionCode)
                {
                    case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE:
                        foreach (var t in studentTranscripts.Where(t => t.Grade >= 5 && t.Grade < 7))
                            subjectCodesForCourseImprove.Add(t.SubjectCode);
                        break;

                    case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE:
                        foreach (var t in studentTranscripts.Where(t => t.Grade >= 7 && t.Grade < 8))
                            subjectCodesForCourseImprove.Add(t.SubjectCode);
                        break;

                    case ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE:
                        foreach (var t in studentTranscripts.Where(t => t.Grade >= 8 && t.Grade < 9))
                            subjectCodesForCourseImprove.Add(t.SubjectCode);
                        break;

                    case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_EVALUATION:
                        foreach (var t in studentTranscripts.Where(t => t.Grade >= 5 && t.Grade < 7))
                            subjectCodesForEvaluation.Add(t.SubjectCode);
                        break;

                    case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_EVALUATION:
                        foreach (var t in studentTranscripts.Where(t => t.Grade >= 7 && t.Grade < 8))
                            subjectCodesForEvaluation.Add(t.SubjectCode);
                        break;
                }
            }
        }
        
        // TODO Phase 2: Update to handle MajorFeedbacks hierarchy when event structure is updated
        
        // 2. Get first major as fallback (since we don't have major info in current event)
        var learningPathMajors = await _learningPathMajorRepository
            .Find(x => evt.Majors.Select(mi => mi.LearningPathMajorId).Contains(x.LearningPathMajorId) && x.IsActive)
            .Include(x => x.LearningPathSubjectCodes)
            .ToListAsync(context.CancellationToken);
        
        if (!learningPathMajors.Any())
        {
            await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
            return;
        }
        
        // Publish event to CourseService to map SubjectCode with MajorCode
        var mappingSubjectCodeWithMajorCodeEvent = new MappingSubjectCodeWithMajorCodeEvent
        {
            SubjectCodes = evt.LearningPathSubjectCodes.Select(x => x.SubjectCode).ToList(),
        };
        
        var mappingResponse = await _requestClient.GetResponse<MappingSubjectCodeWithMajorCodeEventResponse>(mappingSubjectCodeWithMajorCodeEvent);
        
        var subjectCodeToMajorCodeMap = mappingResponse.Message.Response
            .GroupBy(x => x.SubjectCode, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Select(x => x.MajorCode).Distinct(StringComparer.OrdinalIgnoreCase).ToList(), StringComparer.OrdinalIgnoreCase);
        
        // Create dictionary to map MajorCode to LearningPathMajorId
        var majorCodeToLearningPathMajorIdMap = learningPathMajors
            .ToDictionary(m => m.MajorCode, m => m.LearningPathMajorId, StringComparer.OrdinalIgnoreCase);
        
        // Get existing subject codes to avoid duplicates
        var existingSubjectCodes = await _learningPathSubjectCodeRepository
            .Find(x => learningPathMajors.Select(m => m.LearningPathMajorId).Contains(x.LearningPathMajorId) && x.IsActive)
            .Select(x => new { x.SubjectCode, x.LearningPathMajorId, x.LearningPathSubjectCodeId })
            .ToListAsync(context.CancellationToken);
        
        var existingSubjectCodeSet = existingSubjectCodes
            .Select(x => $"{x.SubjectCode}_{x.LearningPathMajorId}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        
        // Create dictionary for quick lookup of transcript grades
        var transcriptGradeMap = studentTranscripts?
            .Where(t => t.Grade.HasValue)
            .ToDictionary(t => t.SubjectCode, t => t.Grade!.Value, StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        
        // Insert LearningPathSubjectCodes with CORRECT foreign key based on SubjectCode -> MajorCode mapping
        var learningPathSubjectCodes = new List<LearningPathSubjectCode>();
        foreach (var subCode in evt.LearningPathSubjectCodes)
        {
            // Find which major(s) this subject belongs to
            if (!subjectCodeToMajorCodeMap.TryGetValue(subCode.SubjectCode, out var majorCodes) || !majorCodes.Any())
            {
                continue;
            }
            
            // Check if this subject should be included based on EvaluationAndImprove
            // If user has selected evaluation codes, only include:
            // - NotPassed subjects (always include)
            // - NotStarted subjects (always include - future subjects)
            // - Studying subjects (always include)
            // - Passed subjects ONLY if they are in subjectCodesForEvaluation or subjectCodesForCourseImprove
            if (transcriptGradeMap.TryGetValue(subCode.SubjectCode, out var subjectGrade))
            {
                // Subject has a grade (Passed)
                // Only include if user selected this grade range for evaluation/improvement
                var isInEvaluationListCheck = subjectCodesForEvaluation.Contains(subCode.SubjectCode);
                var isInCourseImproveListCheck = subjectCodesForCourseImprove.Contains(subCode.SubjectCode);
                
                // If subject is Passed with grade >= 8, skip (PassedWithGoodGrade - no improvement needed)
                if (subjectGrade >= 8.0 && !isInEvaluationListCheck && !isInCourseImproveListCheck)
                {
                    continue;
                }
                
                // If subject is Passed (grade < 8) but user didn't select any evaluation codes for this grade range, skip
                if (subjectGrade >= 5.0 && subjectGrade < 8.0 && evaluationCodes.Any() && !isInEvaluationListCheck && !isInCourseImproveListCheck)
                {
                    continue;
                }
            }
            
            // Determine status based on AnalysisMarkdown, transcript grade, and user selection
            string determinedStatus;
            var isInEvalList = subjectCodesForEvaluation.Contains(subCode.SubjectCode);
            var isInCourseImproveList = subjectCodesForCourseImprove.Contains(subCode.SubjectCode);
            
            if (transcriptGradeMap.TryGetValue(subCode.SubjectCode, out var grade))
            {
                // Subject has a grade (Passed)
                if (grade > 8.0)
                {
                    // Grade > 8.0: PassedWithGoodGrade (no improvement needed)
                    determinedStatus = ConstantEnum.SubjectImprovementStatus.PassedWithGoodGrade.GetDescription();
                }
                else if (isInEvalList && !isInCourseImproveList)
                {
                    // User only selected evaluation (not course improvement): PassedAndEvaluation
                    determinedStatus = ConstantEnum.SubjectImprovementStatus.PassedAndEvaluation.GetDescription();
                }
                else if (isInCourseImproveList)
                {
                    // User selected course improvement: PassedAndImproving
                    determinedStatus = ConstantEnum.SubjectImprovementStatus.PassedAndImproving.GetDescription();
                }
                else
                {
                    // Default for passed subjects
                    determinedStatus = MapToSubjectImprovementStatus(subCode.Status);
                }
            }
            else
            {
                // No grade: use standard mapping logic
                determinedStatus = MapToSubjectImprovementStatus(subCode.Status);
            }
            
            // For each major that this subject belongs to, create a subject code entry
            foreach (var majorCode in majorCodes)
            {
                if (!majorCodeToLearningPathMajorIdMap.TryGetValue(majorCode, out var learningPathMajorId))
                {
                    continue;
                }
                
                // Check if this combination already exists
                var compositeKey = $"{subCode.SubjectCode}_{learningPathMajorId}";
                if (existingSubjectCodeSet.Contains(compositeKey))
                {
                    // Update existing record instead of inserting
                    var existing = existingSubjectCodes.FirstOrDefault(x => 
                        string.Equals(x.SubjectCode, subCode.SubjectCode, StringComparison.OrdinalIgnoreCase) 
                        && x.LearningPathMajorId == learningPathMajorId);
                    
                    if (existing != null)
                    {
                        var existingEntity = await _learningPathSubjectCodeRepository
                            .FirstOrDefaultAsync(x => x.LearningPathSubjectCodeId == existing.LearningPathSubjectCodeId);
                        
                        if (existingEntity != null)
                        {
                            existingEntity.AnalysisMarkdown = subCode.AnalysisMarkdown;
                            existingEntity.Status = determinedStatus;
                            _learningPathSubjectCodeRepository.Update(existingEntity);
                            learningPathSubjectCodes.Add(existingEntity);
                        }
                    }
                    continue;
                }
                
                var learningPathSubjectCode = new LearningPathSubjectCode
                {
                    LearningPathSubjectCodeId = Guid.NewGuid(),
                    LearningPathMajorId = learningPathMajorId,
                    SubjectCode = subCode.SubjectCode,
                    AnalysisMarkdown = subCode.AnalysisMarkdown,
                    Status = determinedStatus
                };
                learningPathSubjectCodes.Add(learningPathSubjectCode);
                existingSubjectCodeSet.Add(compositeKey);
                await _learningPathSubjectCodeRepository.AddAsync(learningPathSubjectCode);
            }
        }
        await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
        await LinkCoursesWithSubjectsAsync(evt.LearningPathId, evt.Email, context.CancellationToken);
        
        var learningPathCollection = await _learningPathQueryRepository.FirstOrDefaultAsync(x => x.PathId == evt.LearningPathId && x.IsActive);
        if (learningPathCollection != null)
        {
            learningPathCollection.SummaryFeedback = evt.SummaryFeedback;
            learningPathCollection.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
            learningPathCollection.Personality = evt.Personality;
            learningPathCollection.LearningAbility = evt.LearningAbility;
            learningPathCollection.AbilityFeedback = learningPath.AbilityFeedback;

            foreach (var subCode in learningPathSubjectCodes)
            {
                var learningPathMajorCollection = learningPathCollection.LearningPathMajors
                    .FirstOrDefault(x => x.LearningPathMajorId == subCode.LearningPathMajorId);
                
                if (learningPathMajorCollection == null) continue;
                
                var existingSubjectCode = learningPathMajorCollection.LearningPathSubjectCodes
                    .FirstOrDefault(x => x.SubjectCode == subCode.SubjectCode);
                if (existingSubjectCode != null)
                {
                    existingSubjectCode.AnalysisMarkdown = subCode.AnalysisMarkdown;
                    existingSubjectCode.Status = subCode.Status; // Use status already calculated
                }
                else
                {
                    learningPathMajorCollection.LearningPathSubjectCodes.Add(new LearningPathSubjectCodeCollection
                    {
                        LearningPathSubjectCodeId = subCode.LearningPathSubjectCodeId,
                        SubjectCode = subCode.SubjectCode,
                        AnalysisMarkdown = subCode.AnalysisMarkdown,
                        Status = subCode.Status, // Use status already calculated
                        CreatedAt = DateTime.Now,
                        UpdatedAt = DateTime.Now,
                        CreatedBy = evt.Email,
                        UpdatedBy = evt.Email,
                        LearningPathMajorId = subCode.LearningPathMajorId,
                        IsActive = true,
                    });
                }

            }

            _unitOfWork.Store(learningPathCollection);
            await _unitOfWork.SessionSaveChangesAsync();
        }

        await _learningPathRealtimeNotifier.PublishLearningPathSnapshotAsync(learningPath.PathId, learningPath.StudentId, context.CancellationToken);
    }
    
    /// <summary>
    /// Link existing courses with their corresponding subject codes
    /// </summary>
    private async Task LinkCoursesWithSubjectsAsync(Guid learningPathId, string email, CancellationToken ct)
    {
        // Load all majors for this learning path
        var majors = await _learningPathMajorRepository
            .Find(x => x.PathId == learningPathId && x.IsActive)
            .ToListAsync(ct);
        
        foreach (var major in majors)
        {
            // Load subject codes for this major
            var subjectCodes = await _learningPathSubjectCodeRepository
                .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
                .ToListAsync(ct);
            
            if (subjectCodes.Count == 0) continue;
            
            // Create lookup dictionary for performance
            var subjectCodeLookup = subjectCodes.ToDictionary(
                sc => sc.SubjectCode, 
                StringComparer.OrdinalIgnoreCase);
            
            // Load courses for this major
            var courses = await _learningPathCourseRepository
                .Find(x => x.LearningPathMajorId == major.LearningPathMajorId && x.IsActive)
                .ToListAsync(ct);
            
            // Link courses with matching subject codes
            foreach (var course in courses)
            {
                if (string.IsNullOrEmpty(course.SubjectCode)) continue;
                
                if (subjectCodeLookup.TryGetValue(course.SubjectCode, out var matchingSubject))
                {
                    course.LearningPathSubjectCodeId = matchingSubject.LearningPathSubjectCodeId;
                    _learningPathCourseRepository.Update(course);
                }
            }
        }
        
        await _unitOfWork.SaveChangesAsync(email, ct);
    }
    
    private static string MapToSubjectImprovementStatus(string statusDescription)
    {
        // Try to match by StudentTranscriptStatus Description first
        foreach (ConstantEnum.StudentTranscriptStatus status in Enum.GetValues(typeof(ConstantEnum.StudentTranscriptStatus)))
        {
            if (string.Equals(status.GetDescription(), statusDescription, StringComparison.OrdinalIgnoreCase))
            {
                // Map to SubjectImprovementStatus
                return status switch
                {
                    ConstantEnum.StudentTranscriptStatus.Passed => ConstantEnum.SubjectImprovementStatus.PassedAndImproving.GetDescription(),
                    ConstantEnum.StudentTranscriptStatus.NotPassed => ConstantEnum.SubjectImprovementStatus.NotPassedAndImproving.GetDescription(),
                    ConstantEnum.StudentTranscriptStatus.NotStarted => ConstantEnum.SubjectImprovementStatus.NotStartedAndImproving.GetDescription(),
                    _ => ConstantEnum.SubjectImprovementStatus.NotStartedAndImproving.GetDescription()
                };
            }
        }

        // Fallback: try direct enum parse
        if (Enum.TryParse<ConstantEnum.StudentTranscriptStatus>(statusDescription, true, out var result))
        {
            return result switch
            {
                ConstantEnum.StudentTranscriptStatus.Passed => ConstantEnum.SubjectImprovementStatus.PassedAndImproving.GetDescription(),
                ConstantEnum.StudentTranscriptStatus.NotPassed => ConstantEnum.SubjectImprovementStatus.NotPassedAndImproving.GetDescription(),
                ConstantEnum.StudentTranscriptStatus.NotStarted => ConstantEnum.SubjectImprovementStatus.NotStartedAndImproving.GetDescription(),
                _ => ConstantEnum.SubjectImprovementStatus.NotStartedAndImproving.GetDescription()
            };
        }

        // Default to NotStartedAndImproving if cannot parse
        return ConstantEnum.SubjectImprovementStatus.NotStartedAndImproving.GetDescription();
    }
}