using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BuildingBlocks.CQRS;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using StudentService.Domain.ReadModels;
using StudentService.Domain.WriteModels;

namespace StudentService.Application.Applications.LearningPaths.Commands;

public class RegenerateLearningPathCommandHandler : ICommandHandler<RegenerateLearningPathCommand, RegenerateLearningPathCommandResponse>
{
    private readonly ICommandRepository<LearningPath> _learningPathCommandRepository;
    private readonly ICommandRepository<Domain.WriteModels.LearningPathCourse> _learningPathCourseCommandRepository;
    private readonly ICommandRepository<LearningPathSubjectCode> _learningPathSubjectCodeCommandRepository;
    private readonly ICommandRepository<StudentTranscript> _studentTranscriptCommandRepository;
    private readonly IQueryRepository<LearningPathCollection> _learningPathCollectionRepository;
    private readonly IQueryRepository<StudentCollection> _studentCollectionRepository;
    private readonly IRequestClient<SubjectCodeSelectEvent> _subjectCodeSelectEventRequestClient;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IIdentityService _identityService;
    private readonly IRequestClient<RegenerateLearningPathEvent> _regenerateLearningPathEventRequestClient;

    public RegenerateLearningPathCommandHandler(ICommandRepository<LearningPath> learningPathCommandRepository,
        IQueryRepository<LearningPathCollection> learningPathCollectionRepository,
        IUnitOfWork unitOfWork,
        IIdentityService identityService,
        ICommandRepository<Domain.WriteModels.LearningPathCourse> learningPathCourseCommandRepository, 
        ICommandRepository<LearningPathSubjectCode> learningPathSubjectCodeCommandRepository,
        IQueryRepository<StudentCollection> studentCollectionRepository, 
        IRequestClient<RegenerateLearningPathEvent> regenerateLearningPathEventRequestClient,
        ICommandRepository<StudentTranscript> studentTranscriptCommandRepository, 
        IRequestClient<SubjectCodeSelectEvent> subjectCodeSelectEventRequestClient)
    {
        _learningPathCommandRepository = learningPathCommandRepository;
        _learningPathCollectionRepository = learningPathCollectionRepository;
        _unitOfWork = unitOfWork;
        _identityService = identityService;
        _learningPathCourseCommandRepository = learningPathCourseCommandRepository;
        _learningPathSubjectCodeCommandRepository = learningPathSubjectCodeCommandRepository;
        _studentCollectionRepository = studentCollectionRepository;
        _regenerateLearningPathEventRequestClient = regenerateLearningPathEventRequestClient;
        _studentTranscriptCommandRepository = studentTranscriptCommandRepository;
        _subjectCodeSelectEventRequestClient = subjectCodeSelectEventRequestClient;
    }

    public async Task<RegenerateLearningPathCommandResponse> Handle(RegenerateLearningPathCommand request, CancellationToken cancellationToken)
    {
       var response = new RegenerateLearningPathCommandResponse {Success = false};

       var currentUser = _identityService.GetCurrentUser()!;

       #region Validate

        short studentLevel = 0;
        bool isSkipTest = false;
        string? levelReason = null;
        int limitTime = 0;
        string? evaluationAndImproveString = null;
        Guid? studentTestId = null;
        List<Guid>? practiceSubmissionIds = null;
        List<Guid> studentSurveyIds = new();
       await _unitOfWork.BeginTransactionAsync(async () =>
       {
           var learningPathOld = await _learningPathCommandRepository
               .Find(x => x.IsActive && x.StudentId == currentUser.UserId, cancellationToken: cancellationToken)
               .OrderByDescending(x => x.CreatedAt)
               .Include(x => x.LearningPathMajors)
               .ThenInclude(m => m.LearningPathCourses)
               .Include(x => x.LearningPathMajors)
               .ThenInclude(m => m.LearningPathSubjectCodes)
               .FirstOrDefaultAsync(cancellationToken: cancellationToken);
           if (learningPathOld == null)
           {
               response.SetMessage(MessageId.I00000, "Bạn chưa có lộ trình học nào để tạo lại");
               return false;
           }
           #endregion

           #region Xoá lộ trình cũ

           studentLevel = learningPathOld.Level;
           isSkipTest = learningPathOld.IsSkipTest;
           levelReason = learningPathOld.LevelReason;
           limitTime = learningPathOld.LimitTime;
           evaluationAndImproveString = learningPathOld.EvaluationAndImprove;
       
           // Delete old learning path
           foreach (var major in learningPathOld.LearningPathMajors)
           {
               foreach (var subjectCode in major.LearningPathSubjectCodes)
                   _learningPathSubjectCodeCommandRepository.Update(subjectCode);

               foreach (var course in major.LearningPathCourses)
                   _learningPathCourseCommandRepository.Update(course);
           }
           _learningPathCommandRepository.Update(learningPathOld);
           await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken, needLogicalDelete: true);
       
           // Delete old collection
           var learningPathCollectionOld = await _learningPathCollectionRepository
               .FirstOrDefaultAsync(x => x.PathId == learningPathOld.PathId && x.IsActive);
           studentTestId = learningPathCollectionOld!.StudentQuizSubmission.PlacementTestSubmissionId;
           practiceSubmissionIds = learningPathCollectionOld.StudentQuizSubmission.StudentPracticeTestSubmissions?
                .Select(x => x.PracticeTestSubmissionId)
                .ToList();
           studentSurveyIds = learningPathCollectionOld.StudentQuizSubmission!.StudentSurveySubmissions
               .Select(x => x.StudentSurveyId)
               .ToList();
           
           _unitOfWork.Delete(learningPathCollectionOld);
           await _unitOfWork.SessionSaveChangesAsync();
           return true;
           #endregion
       }, cancellationToken);
       
       #region Tính toán bảng điểm của student
       var studentTranscripts = await _studentTranscriptCommandRepository
           .Find(x => x.StudentId == currentUser.UserId && x.IsActive)
           .ToListAsync(cancellationToken);
       
       List<int>? evaluationAndImprovementSubjectInt = null;
       if (!string.IsNullOrWhiteSpace(evaluationAndImproveString) && evaluationAndImproveString != null)
       {
           evaluationAndImprovementSubjectInt = evaluationAndImproveString
               .Split(',', StringSplitOptions.RemoveEmptyEntries)
               .Select(s => s.Trim())
               .Where(s => int.TryParse(s, out _))
               .Select(int.Parse)
               .ToList();
       }
       
       List<CourseImproveContext> courseImprove = new();
       HashSet<string> subjectCodesForEvaluation = new();
       
       if (evaluationAndImprovementSubjectInt != null && evaluationAndImprovementSubjectInt.Any())
       {
            // Get all subject codes
            var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent(), cancellationToken);
            if (!subjectCodeEventResponse.Message.Success)
            {
                response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
                return response;
            }

            var allSubjectCodes = subjectCodeEventResponse.Message.Response;
            
            // Process each other question answer code
            foreach (var questionCode in evaluationAndImprovementSubjectInt)
            {
                switch (questionCode)
                {
                    case (short) ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE:
                        courseImprove.AddRange(
                            studentTranscripts
                                .Where(t => t.Grade >= 5 && t.Grade < 7)
                                .Select(t => new CourseImproveContext
                                {
                                    SubjectCode = t.SubjectCode,
                                    Level = (short)ConstantEnum.CourseLevel.Beginner,
                                    SubjectPrerequisiteCode = t.Prerequisite
                                })
                                .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                        );
                        break;

                    case (short) ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE:
                        courseImprove.AddRange(
                            studentTranscripts
                                .Where(t => t.Grade >= 7 && t.Grade < 8)
                                .Select(t => new CourseImproveContext
                                {
                                    SubjectCode = t.SubjectCode,
                                    Level = (short)ConstantEnum.CourseLevel.Intermidiate,
                                    SubjectPrerequisiteCode = t.Prerequisite
                                })
                                .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                        );
                        break;

                    case (short) ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE:
                        courseImprove.AddRange(
                            studentTranscripts
                                .Where(t => t.Grade >= 8 && t.Grade < 9)
                                .Select(t => new CourseImproveContext
                                {
                                    SubjectCode = t.SubjectCode,
                                    Level = (short)ConstantEnum.CourseLevel.Advanced,
                                    SubjectPrerequisiteCode = t.Prerequisite
                                })
                                .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                        );
                        break;

                    case (short) ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_EVALUATION:
                        var subjects5To7 = studentTranscripts
                            .Where(t => t.Grade >= 5 && t.Grade < 7)
                            .Select(t => t.SubjectCode)
                            .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code));
                        foreach (var subjectCode in subjects5To7)
                        {
                            subjectCodesForEvaluation.Add(subjectCode);
                        }
                        break;

                    case (short) ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_EVALUATION:
                        var subjects7To8 = studentTranscripts
                            .Where(t => t?.Grade >= 7 && t.Grade < 8)
                            .Select(t => t?.SubjectCode)
                            .Where(code => allSubjectCodes.Any(sc => sc.SubjectCode == code));
                        foreach (var subjectCode in subjects7To8)
                        {
                            subjectCodesForEvaluation.Add(subjectCode);
                        }
                        break;
                }
            }
       }

       List<SubjectMarkContext>? subjectMarks = null;
       if (studentTranscripts.Any())
       {
           subjectMarks = studentTranscripts
               .Where(x =>
                   x.Status == ConstantEnum.StudentTranscriptStatus.NotPassed.GetDescription() ||
                   (x.Status == ConstantEnum.StudentTranscriptStatus.Passed.GetDescription() &&
                    subjectCodesForEvaluation.Contains(x.SubjectCode))
               )
               .Select(x => new SubjectMarkContext
               {
                   SubjectCode = x.SubjectCode,
                   SubjectName = x.SubjectName,
                   Mark = x.Grade
               })
               .ToList();
       }

       List<string>? studentPassedSubjects = null;
       if (studentTranscripts.Any())
       {
           studentPassedSubjects =studentTranscripts
               .Where(x => x.Status == ConstantEnum.StudentTranscriptStatus.Passed.GetDescription())
               .Select(x => x.SubjectCode)
               .ToList();
       }
       #endregion
       
     #region Tạo lộ trình học tập mới
     var studentProfile = await _studentCollectionRepository.FirstOrDefaultAsync(x => x.StudentId == currentUser.UserId && x.IsActive);
     var learningGoal = studentProfile!
         .LearningGoals!
         .OrderByDescending(x => x.CreatedAt)
         .FirstOrDefault(x => x.IsActive);

     var learningPathId = Guid.NewGuid();
    
     // Publish even to QuizService to select quiz and test, after that, calculate learning path details
     var technologies = studentProfile!.Technologies!.Select(x => new RegenerateLearningPathEventTechnologies
     {
         TechnologyId = x.TechnologyId,
         TechnologyName = x.Technology.TechnologyName,
         TechnologyType = x.Technology.TechnologyType
     }).ToList();
     var learningGoalEvent = new RegenerateLearningPathEventLearningGoal
     {
         LearningGoalId = learningGoal!.Goal!.GoalId,
         LearningGoalName = learningGoal.Goal.GoalName,
         LearningGoalType = learningGoal.Goal.LearningGoalType
     };
     var reRegenerateLearningPathEvent = new RegenerateLearningPathEvent
     {
         LearningPathId = learningPathId,
         StudentId = currentUser.UserId,
         StudentEmail = currentUser.Email,
         Level = studentLevel,
         LevelReason = levelReason!,
         IsSkipTest = isSkipTest,
         Technologies = technologies,
         LearningGoal = learningGoalEvent,
         LimitTime = limitTime,
         StudentMajorId = studentProfile.MajorId ?? Guid.Empty,
         SemesterId = studentProfile.SemesterId ?? Guid.Empty,
         CourseImprove = courseImprove,
         StudentPassedSubjects = studentPassedSubjects,
         SubjectMarks = subjectMarks,
         StudentTranscripts = studentTranscripts.Select(x => new StudentTranscriptContext
         {
            SubjectCode = x.SubjectCode,
            Mark = x.Grade,
            Status = x.Status
         }).ToList(),
         EvaluationAndImprove = evaluationAndImproveString,
         StudentTestId = studentTestId,
         PracticeSubmissionIds = practiceSubmissionIds,
         StudentSurveyIds = studentSurveyIds
     };

     var eventResponse = await _regenerateLearningPathEventRequestClient.GetResponse<RegenerateLearningPathEventResponse>(reRegenerateLearningPathEvent, cancellationToken);
     if (!eventResponse.Message.Success)
     {
         response.MessageId = eventResponse.Message.MessageId;
         response.Message = eventResponse.Message.Message;
         return response;
     }
     #endregion
       
       // True
       response.Success = true;
       response.Response = learningPathId;
       response.SetMessage(MessageId.I00001, "Tạo lại lộ trình học");
       return response;
    }
}