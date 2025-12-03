using BaseService.Application.Interfaces.Repositories;
using BuildingBlocks.Messaging.Events.AIService;
using BuildingBlocks.Messaging.Events.StudentService;
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
    private readonly IRequestClient<MajorSubjectCodeEvent> _majorSubjectCodeRequestClient;
    private readonly IUnitOfWork _unitOfWork;

    public LearningFeedbackEventConsumer(
        ICommandRepository<LearningPath> learningPathRepository,
        ICommandRepository<LearningPathMajor> learningPathMajorRepository,
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeRepository,
        ICommandRepository<LearningPathCourse> learningPathCourseRepository,
        IQueryRepository<LearningPathCollection> learningPathQueryRepository,
        IUnitOfWork unitOfWork,
        IRequestClient<MajorSubjectCodeEvent> majorSubjectCodeRequestClient)
    {
        _learningPathRepository = learningPathRepository;
        _learningPathMajorRepository = learningPathMajorRepository;
        _learningPathSubjectCodeRepository = learningPathSubjectCodeRepository;
        _learningPathCourseRepository = learningPathCourseRepository;
        _learningPathQueryRepository = learningPathQueryRepository;
        _unitOfWork = unitOfWork;
        _majorSubjectCodeRequestClient = majorSubjectCodeRequestClient;
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
        _learningPathRepository.Update(learningPath);
        
        // TODO Phase 2: Update to handle MajorFeedbacks hierarchy when event structure is updated
        
        // 2. Get first major as fallback (since we don't have major info in current event)
        var learningPathMajors = await _learningPathMajorRepository
            .Find(x => evt.Majors.Select(id => id.LearningPathMajorId).Contains(x.LearningPathMajorId) && x.IsActive)
            .Include(x => x.LearningPathSubjectCodes)
            .ToListAsync(context.CancellationToken);
        
        if (learningPathMajors == null || !learningPathMajors.Any())
        {
            await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
            return;
        }
        
        // Insert LearningPathSubjectCodes with CORRECT foreign key
        var learningPathSubjectCodes = new List<LearningPathSubjectCode>();

        // Publish event to CourseService to get SubjectCode mapping to MajorCode

        foreach (var subCode in evt.LearningPathSubjectCodes)
        {
            var learningPathSubjectCode = new LearningPathSubjectCode
            {
                LearningPathSubjectCodeId = Guid.NewGuid(),
                LearningPathMajorId = learningPathMajor.LearningPathMajorId,
                SubjectCode = subCode.SubjectCode,
                AnalysisMarkdown = subCode.AnalysisMarkdown,
                Status = subCode.Status
            };
            learningPathSubjectCodes.Add(learningPathSubjectCode);
            await _learningPathSubjectCodeRepository.AddAsync(learningPathSubjectCode);
        }
        
        await _unitOfWork.SaveChangesAsync(evt.Email, context.CancellationToken);
        
        await LinkCoursesWithSubjectsAsync(evt.LearningPathMajorId, evt.Email, context.CancellationToken);
        
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
                    .FirstOrDefault(x => x.LearningPathMajorId == learningPathMajor.LearningPathMajorId);
                
                var existingSubjectCode = learningPathMajorCollection?.LearningPathSubjectCodes
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
                        LearningPathMajorId = learningPathMajorCollection.LearningPathMajorId,
                        IsActive = true,
                    });
                }

            }
        }
        _unitOfWork.Store(learningPathCollection);
        await _unitOfWork.SessionSaveChangesAsync();
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