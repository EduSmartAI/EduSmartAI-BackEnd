using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService;
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

    private readonly IUnitOfWork _unitOfWork;

    public LearningFeedbackEventConsumer(
        ICommandRepository<LearningPath> learningPathRepository,
        ICommandRepository<LearningPathMajor> learningPathMajorRepository,
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseRepository,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IUnitOfWork unitOfWork, IRequestClient<MappingSubjectCodeWithMajorCodeEvent> requestClient, ILearningPathRealtimeNotifier learningPathRealtimeNotifier)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathMajorRepository = learningPathMajorRepository;
        _learningPathSubjectCodeRepository = learningPathSubjectCodeRepository;
        _learningPathCourseRepository = learningPathCourseRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _unitOfWork = unitOfWork;
        _requestClient = requestClient;
        _learningPathRealtimeNotifier = learningPathRealtimeNotifier;
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
        
        learningPath.SummaryFeedback = evt.SummaryFeedback;
        learningPath.HabitAndInterestAnalysis = evt.HabitAndInterestAnalysis;
        learningPath.Personality = evt.Personality;
        learningPath.LearningAbility = evt.LearningAbility;
        learningPath.AbilityFeedback = evt.AbilityAnalyses != null && evt.AbilityAnalyses.Any()
            ? string.Join($"{Environment.NewLine}{new string('*', 15)}{Environment.NewLine}",
                evt.AbilityAnalyses.Select(a => $"{a.Name}: {a.AnalysisMarkdown}"))
            : null;
        _learningPathRepository.Update(learningPath);
        
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
        
        // Insert LearningPathSubjectCodes with CORRECT foreign key based on SubjectCode -> MajorCode mapping
        var learningPathSubjectCodes = new List<LearningPathSubjectCode>();
        foreach (var subCode in evt.LearningPathSubjectCodes)
        {
            // Find which major(s) this subject belongs to
            if (!subjectCodeToMajorCodeMap.TryGetValue(subCode.SubjectCode, out var majorCodes) || !majorCodes.Any())
            {
                continue;
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
                            existingEntity.Status = subCode.Status;
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
                    Status = subCode.Status
                };
                learningPathSubjectCodes.Add(learningPathSubjectCode);
                existingSubjectCodeSet.Add(compositeKey); // Add to set to prevent duplicate in same run
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
                }
                else
                {
                    learningPathMajorCollection.LearningPathSubjectCodes.Add(new LearningPathSubjectCodeCollection
                    {
                        LearningPathSubjectCodeId = subCode.LearningPathSubjectCodeId,
                        SubjectCode = subCode.SubjectCode,
                        AnalysisMarkdown = subCode.AnalysisMarkdown,
                        Status = subCode.Status,
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
}