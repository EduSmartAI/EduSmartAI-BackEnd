using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using QuizService.Application.Applications.LearningPaths;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using AbilityImprove = QuizService.Application.Applications.LearningPaths.AbilityImprove;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

namespace QuizService.Application.Applications.Consumers;

public class RegenerateLearningPathEventConsumer : IConsumer<RegenerateLearningPathEvent>
{
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizRepository;
    private readonly IQueryRepository<StudentTestCollection> _studentTestRepository;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IRequestClient<InsertLearningPathEvent> _requestInsertLearningPathEventClient;
    private readonly ILearningPathService _learningPathService;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    
    public RegenerateLearningPathEventConsumer(IQueryRepository<StudentQuizCollection> studentQuizRepository, 
        IQueryRepository<StudentTestCollection> studentTestRepository,
        ILearningPathService learningPathService, 
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient, 
        IRequestClient<InsertLearningPathEvent> requestInsertLearningPathEventClient)
    {
        _studentQuizRepository = studentQuizRepository;
        _studentTestRepository = studentTestRepository;
        _learningPathService = learningPathService;
        _requestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        _requestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient;
        _requestInsertLearningPathEventClient = requestInsertLearningPathEventClient;
    }

    public async Task Consume(ConsumeContext<RegenerateLearningPathEvent> context)
    {
        var evt = context.Message;
        var response = new RegenerateLearningPathEventResponse { Success = false };
        
        // Publish event to CourseService to Select Major
        var majorAndSemesterEventResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(
            new CourseMajorSemesterSelectEvent
            {
                MajorId = evt.StudentMajorId,
                SemesterId = evt.SemesterId
            });

        if (!majorAndSemesterEventResponse.Message.Success)
        {
            response.MessageId = majorAndSemesterEventResponse.Message.MessageId;
            response.Message = majorAndSemesterEventResponse.Message.Message;
            await context.RespondAsync(response);
            return;
        }

        #region Map data to LearningPathCreationContext
        var currentUser = new IdentityEntity
        {
            Email = evt.StudentEmail,
            UserId = evt.StudentId
        };

        var studentMajor = new StudentMajor
        {
            MajorCode = majorAndSemesterEventResponse.Message.Response.MajorCode,
            MajorName = majorAndSemesterEventResponse.Message.Response.MajorName,
        };
        
        var informationResponse = new StudentInformationSelectsEventResponseEntity
        {
            Technologies = evt.Technologies.Select(x => new StudentTechnologySelectsEventResponseEntity
            {
                TechnologyName = x.TechnologyName,
                TechnologyType = x.TechnologyType
            }).ToList(),
            MajorId = evt.StudentMajorId,
            LearningGoalName = evt.LearningGoal.LearningGoalName,
            LearningGoalType = evt.LearningGoal.LearningGoalType,
            SemesterId = evt.SemesterId,
        };
        var studentQuizCollections = await _studentQuizRepository.ToListAsync(x => x.StudentId == evt.StudentId && x.IsActive);
        string learningGoalName = evt.LearningGoal.LearningGoalName;
            
        if (evt.LearningGoal.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
        {
            var interestSurvey = studentQuizCollections.FirstOrDefault(sq => sq.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST));
            if (interestSurvey == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy bài khảo sát sở thích học tập");
                await context.RespondAsync(response);
                return;
            }

            var selectedAnswerIds = interestSurvey.StudentQuizAnswers
                .Select(a => a.AnswerId)
                .ToHashSet();

            var interestQuestions = interestSurvey.Quiz.Questions.Select(question => new StudentInterestQuestion
            {
                QuestionText = question.QuestionText,
                StudentAnswers = question.Answers
                    .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                    .Select(a => a.AnswerText)
                    .ToList()
            }).Where(q => q.StudentAnswers.Any()).ToList();

            var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
            {
                StudentId = currentUser.UserId,
                Questions = interestQuestions
            };

            var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(studentInterestAnalysisEvent);
            
            if (!aiAnalysisResponse.Message.Success)
            {
                response.SetMessage(MessageId.E99999);
                await context.RespondAsync(response);
            }

            learningGoalName = aiAnalysisResponse.Message.Response.LearningGoal;
        }
        
        var learningPathEvent = new InsertLearningPathEvent
        {
            LearningPathId = evt.LearningPathId,
            StudentId = currentUser.UserId,
            CurrentUserEmail = currentUser.Email,
            PathName = $"Lộ trình {learningGoalName}",
            Level = evt.Level,
            LevelReason = evt.LevelReason,
            IsSkipTest = false,
            LimitTime = evt.LimitTime,
            EvaluationAndImprove = evt.EvaluationAndImprove,
            StudentTestId = evt.StudentTestId,
            StudentSurveyIds = evt.StudentSurveyIds,
            PracticeSubmissionIds = evt.PracticeSubmissionIds,
        };
        var learningPathResponse = await _requestInsertLearningPathEventClient.GetResponse<InsertLearningPathEventResponse>(learningPathEvent);
        if (!learningPathResponse.Message.Success)
        {
            response.MessageId = learningPathResponse.Message.MessageId;
            response.Message = learningPathResponse.Message.Message;
            await context.RespondAsync(response);
            return;
        }            
        
        List<AbilityMarkContext>? abilityMarks = null;
        if (!evt.IsSkipTest)
        {
            var studentTests = await _studentTestRepository
                .ToListAsync(x => x.StudentId == evt.StudentId && x.IsActive);
            
            if (studentTests.Any())
            {
                var latestStudentTest = studentTests
                    .OrderByDescending(x => x.CreatedAt)
                    .First();
                
                // Initialize abilityMarks list
                abilityMarks = new List<AbilityMarkContext>();
                
                // Loop through each StudentQuizCollection to calculate ability marks for each quiz
                foreach (var studentQuizCollection in latestStudentTest.StudentQuizzes)
                {
                    var quiz = studentQuizCollection.Quiz;
                    
                    if (quiz?.PlacementTestQuizSetting == null)
                        continue;
                    
                    // Get all question IDs for this quiz
                    var quizQuestionIds = quiz.Questions.Select(q => q.QuestionId).ToHashSet();
                    
                    // Get student answers for this quiz only from latestStudentTest.StudentAnswers
                    var quizAnswers = latestStudentTest.StudentAnswers
                        .Where(sa => quizQuestionIds.Contains(sa.QuestionId))
                        .ToList();
                    
                    if (!quizAnswers.Any())
                        continue;
                    
                    // Calculate score for this quiz with proper logic for MultipleChoice
                    var correctCount = 0;
                    var totalQuestions = quiz.Questions.Count;
                    
                    foreach (var question in quiz.Questions)
                    {
                        // Get student's selected answers for this question
                        var studentSelectedAnswerIds = quizAnswers
                            .Where(sa => sa.QuestionId == question.QuestionId && sa.AnswerId.HasValue)
                            .Select(sa => sa.AnswerId!.Value)
                            .ToHashSet();
                        
                        // Skip if student didn't answer this question
                        if (!studentSelectedAnswerIds.Any()) continue;
                        
                        // Get all correct answer IDs for this question
                        var correctAnswerIds = question.Answers
                            .Where(a => a.IsCorrect)
                            .Select(a => a.AnswerId)
                            .ToHashSet();
                        
                        bool isCorrect;
                        
                        // For MultipleChoice: student must select ALL correct answers and NO incorrect answers
                        if (question.QuestionType == (short)ConstantEnum.QuestionType.MultipleChoice)
                        {
                            isCorrect = studentSelectedAnswerIds.Count == correctAnswerIds.Count 
                                        && studentSelectedAnswerIds.All(id => correctAnswerIds.Contains(id));
                        }
                        else
                        {
                            // For SingleChoice/TrueFalse: just check if selected answer is correct
                            isCorrect = studentSelectedAnswerIds.Any(id => correctAnswerIds.Contains(id));
                        }
                        
                        if (isCorrect)
                        {
                            correctCount++;
                        }
                    }
                    
                    var abilityScore = totalQuestions > 0 ? (double) correctCount / totalQuestions * 100 : 0;
                    
                    // Add ability mark with SubjectCodeName as the name
                    abilityMarks.Add(new AbilityMarkContext
                    {
                        Name = quiz.PlacementTestQuizSetting.SubjectCodeName,
                        Mark = abilityScore
                    });
                }
            }
        }

        var courseImproves = evt.CourseImprove?.Select(x => new LearningPaths.CourseImproveContext
        {
            Level = x.Level,
            SubjectCode = x.SubjectCode,
            SubjectPrerequisiteCode = x.SubjectPrerequisiteCode
        }).ToList();
        
        var subjectMarks = evt.SubjectMarks?.Select(x => new LearningPaths.SubjectMarkContext
        {
            SubjectCode = x.SubjectCode,
            SubjectName = x.SubjectName,
            Mark = x.Mark
        }).ToList();
        
        var studentTranscripts = evt.StudentTranscripts?.Select(x => new LearningPaths.StudentTranscriptContext
        {
            SubjectCode = x.SubjectCode,
            Mark = x.Mark,
            Status = x.Status
        }).ToList();
        #endregion
        
        var learningPathCreationContext = new LearningPathCreationContext
        {
            CurrentUser = currentUser,
            LearningPathId = evt.LearningPathId,
            LimitTime = evt.LimitTime,
            StudentLevel = evt.Level,
            StudentMajor = studentMajor,
            InformationResponse = informationResponse,
            StudentQuizCollections = studentQuizCollections,
            AbilityMarks = abilityMarks,
            CourseImprove = courseImproves,
            SubjectMarks = subjectMarks,
            StudentPassedSubjects = evt.StudentPassedSubjects,
            StudentTranscripts = studentTranscripts,
            AbilityImprove = evt.AbilityImprove?.Select(x => new AbilityImprove
            {
                Name = x.Name,
                Mark = x.Mark
            }).ToList()
        };
        // Generate learning path details
        await _learningPathService.CreateLearningPathAsync(learningPathCreationContext, cancellationToken: CancellationToken.None);
        
        // True
        response.Success = true;
        response.SetMessage(MessageId.I00001);
        await context.RespondAsync(response);
    }
}