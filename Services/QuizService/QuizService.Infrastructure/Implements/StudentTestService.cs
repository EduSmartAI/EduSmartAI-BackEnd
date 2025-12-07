using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BaseService.Domain.Snapshort;
using BuildingBlocks.Messaging.Events.AIService.InsertLearningPathEvent;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using BuildingBlocks.Messaging.Events.StudentService;
using MassTransit;
using QuizService.Application.Applications.Admin.Queries.StudentTests;
using QuizService.Application.Applications.LearningPaths;
using QuizService.Application.Applications.PracticeTest;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using CourseImproveContext = QuizService.Application.Applications.LearningPaths.CourseImproveContext;
using StudentTranscriptContext = QuizService.Application.Applications.LearningPaths.StudentTranscriptContext;
using SubjectMarkContext = QuizService.Application.Applications.LearningPaths.SubjectMarkContext;

namespace QuizService.Infrastructure.Implements;

public class StudentTestService : IStudentTestService
{
    private readonly ICommandRepository<StudentTest> _studentTestRepository;
    private readonly ICommandRepository<StudentQuiz> _studentQuizRepository;
    private readonly IQueryRepository<StudentTestCollection> _studentTestQueryRepository;
    private readonly IQueryRepository<TestCollection> _testQueryRepository;
    private readonly IQueryRepository<QuestionCollection> _questionQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxRepository;
    private readonly IQueryRepository<StudentQuizCollection> _studentQuizCollectionRepository;
    private readonly IRequestClient<StudentInterestSurveyAnalysisEvent> _requestStudentInterestAnalysisClient;
    private readonly IRequestClient<StudentInformationSelectsEvent> _requestStudentInformationSelectsClient;
    private readonly IRequestClient<StudentTranscriptSelectEvent> _requestStudentTranscriptClient;
    private readonly IRequestClient<CourseMajorSemesterSelectEvent> _requestCourseMajorSemesterClient;
    private readonly IRequestClient<SubjectCodeSelectEvent> _subjectCodeSelectEventRequestClient;
    private readonly IRequestClient<InsertLearningPathEvent> _requestInsertLearningPathEventClient;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPracticeTestService _practiceTestService;
    private readonly ICommandRepository<Problem> _problemRepository;
    private readonly ILearningPathService _learningPathService;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="deps"></param>
    /// <param name="practiceTestService"></param>
    /// <param name="problemRepository"></param>
    /// <param name="learningPathService"></param>
    public StudentTestService(StudentTestServiceDependencies deps, IPracticeTestService practiceTestService, ICommandRepository<Problem> problemRepository, ILearningPathService learningPathService, IRequestClient<SubjectCodeSelectEvent> subjectCodeSelectEventRequestClient, IRequestClient<InsertLearningPathEvent> requestInsertLearningPathEventClient)
    {
        _studentQuizCollectionRepository = deps.StudentQuizCollectionRepository;
        _studentTestRepository = deps.StudentTestRepository;
        _studentQuizRepository = deps.StudentQuizRepository;
        _studentTestQueryRepository = deps.StudentTestQueryRepository;
        _testQueryRepository = deps.TestQueryRepository;
        _questionQueryRepository = deps.QuestionQueryRepository;
        _outboxRepository = deps.OutboxRepository;
        _requestStudentInterestAnalysisClient = deps.RequestStudentInterestAnalysisClient;
        _requestStudentInformationSelectsClient = deps.RequestStudentInformationSelectsClient;
        _requestStudentTranscriptClient = deps.RequestStudentTranscriptClient;
        _requestCourseMajorSemesterClient = deps.RequestCourseMajorSemesterClient;
        _identityService = deps.IdentityService;
        _unitOfWork = deps.UnitOfWork;
        _practiceTestService = practiceTestService;
        _problemRepository = problemRepository;
        _learningPathService = learningPathService;
        _subjectCodeSelectEventRequestClient = subjectCodeSelectEventRequestClient;
        _requestInsertLearningPathEventClient = requestInsertLearningPathEventClient;
    }

    /// <summary>
    /// Insert student test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StudentTestInsertResponse> InsertStudentTestAsync(StudentTestInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new StudentTestInsertResponse { Success = false };
        
        // Get current user id
        var currentUser = _identityService.GetCurrentUser()!;

        var currentTime = DateTime.UtcNow;
        
        // Validate startedAt and finishedAt
        if (request.StartedAt > currentTime)
        {
                response.SetMessage(MessageId.E00000, 
                    "Thời gian bắt đầu phải bé hơn hoặc bằng thời gian hiện tại (Giờ hiện tại là " 
                    + currentTime.ToString("yyyy-MM-dd HH:mm:ss") + ")");
            return response;
        }
        
        // Validate testId
        var testExist = await _testQueryRepository.FirstOrDefaultAsync(x => x.TestId == request.TestId);
        if (testExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra");
            return response;
        }
        
        // Validate quizIds
        var quizIds = request.QuizIds;
        var validQuizIds = testExist.Quizzes.Where(q => quizIds.Contains(q.QuizId)).Select(q => q.QuizId).ToList();
        if (validQuizIds.Count != quizIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Có bài quiz không hợp lệ trong danh sách bài quiz");
            return response;
        }
        
        // Validate questionIds in answers
        var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var validQuestions = await _questionQueryRepository.ToListAsync(x => questionIds.Contains(x.QuestionId));
        if (validQuestions.Count != questionIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Có câu hỏi không hợp lệ trong danh sách trả lời");
            return response;
        }
        
        // Validate answerIds in answers
        var answerIds = request.Answers.Select(a => a.AnswerId).ToList();
        var validAnswers = validQuestions
            .SelectMany(q => q.Answers)
            .Where(a => answerIds.Contains(a.AnswerId))
            .ToList();
        if (validAnswers.Count != answerIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Có câu trả lời không hợp lệ trong danh sách trả lời");
            return response;
        }

        if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
        {
            // Validate that we have exactly 3 problems (Easy, Medium, Hard)
            if (request.PracticeTestAnswers.Count != 3)
            {
                response.SetMessage(MessageId.E00000, "PracticeTestAnswers phải có đúng 3 bài: Dễ, Trung bình, Khó");
                return response;
            }
        }

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new student test
            var studentTest = new StudentTest
            {
                StudentId = currentUser.UserId,
                TestId = request.TestId,
                StartedAt = StringUtil.ConvertToUtcTime(request.StartedAt),
                FinishedAt = currentTime,
                StudentAnswers = request.Answers.Select(a => new StudentAnswer
                {
                    QuestionId = a.QuestionId,
                    AnswerId = a.AnswerId,
                }).ToList()
            };
            
            // Insert into StudentQuiz
            var studentQuizzes = request.QuizIds.Select(x => new StudentQuiz
            {
                StudentId = currentUser.UserId,
                QuizId = x,
                QuizType = (short) ConstantEnum.TestType.Quiz,
            }).ToList();
            await _studentQuizRepository.AddRangeAsync(studentQuizzes);
            
            // Save to database
            await _studentTestRepository.AddAsync(studentTest);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            var studentQuizCollections = new List<StudentQuizCollection>();
            
            // Map to StudentTestCollection
            foreach (var studentQuiz in studentQuizzes)
            {
                var quiz = testExist.Quizzes.FirstOrDefault(qu => qu.QuizId == studentQuiz.QuizId);
                var studentQuizCollection = StudentQuizCollection.FromWriteModel(studentQuiz, quiz, new UserInformation{Email = currentUser.Email, FullName = currentUser.FullName});
                _unitOfWork.Store(studentQuizCollection);
                studentQuizCollections.Add(studentQuizCollection);
            }

            var answerDict = new Dictionary<Guid?, AnswerCollection>();
            foreach (var answer in validAnswers) answerDict.Add(answer.AnswerId, answer);

            var studentTestCollection = new StudentTestCollection
            {
                StudentTestId = studentTest.StudentTestId,
                StudentId = studentTest.StudentId,
                TestId = studentTest.TestId,
                StartedAt = studentTest.StartedAt,
                FinishedAt = studentTest.FinishedAt,
                IsActive = studentTest.IsActive,
                CreatedAt = studentTest.CreatedAt,
                CreatedBy = studentTest.CreatedBy,
                UpdatedAt = studentTest.UpdatedAt,
                UpdatedBy = studentTest.UpdatedBy,
                Student = new UserInformation {Email = currentUser.Email, FullName = currentUser.FullName},
                StudentAnswers = studentTest.StudentAnswers.Select(sa => new StudentAnswerCollection
                {
                    QuestionId = sa.QuestionId,
                    AnswerId = sa.AnswerId,
                    Answer = answerDict[sa.AnswerId],
                    Question = validQuestions.FirstOrDefault(q => q.QuestionId == sa.QuestionId),
                    CreatedAt = sa.CreatedAt,
                    CreatedBy = sa.CreatedBy,
                    UpdatedAt = sa.UpdatedAt,
                    UpdatedBy = sa.UpdatedBy,
                    IsActive = sa.IsActive
                }).ToList(),
                StudentQuizzes = studentQuizCollections,
            };
            
            _unitOfWork.Store(studentTestCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Get student transcript first (needed for level calculation and SubjectMarks)
            var studentTranscriptEvent = new StudentTranscriptSelectEvent
            {
                StudentId = currentUser.UserId
            };

            var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent, cancellationToken);
            var studentTranscripts = transcriptResponse.Message.Response;
            
            // Calculate level from Quiz (60% weight if has practice test, or base for transcript calculation)
            var quizLevel = DetermineStudentLevel(studentTestCollection.StudentAnswers.ToList(), testExist.Quizzes.ToList(), out var difficultyPerformance);
            
            int baseLevel = quizLevel; // Level from quiz alone
            
            // Declare practiceTestResults outside to use it later for ability marks calculation
            var practiceTestResults = new Dictionary<string, PracticeTestSubmitInsertResponse>();
            
            // If PracticeTestAnswers is provided, calculate combined level (60% quiz + 40% practice test)
            if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
            {
                // Submit each practice test answer and collect results
                
                foreach (var practiceAnswer in request.PracticeTestAnswers)
                {
                    var submitRequest = new PracticeTestSubmitInsertRequest
                    {
                        ProblemId = practiceAnswer.ProblemId,
                        SourceCode = practiceAnswer.CodeSubmission,
                        LanguageId = practiceAnswer.LanguageId
                    };
                    
                    var submitResponse = await _practiceTestService.InsertPracticeTestSubmitWithoutTransactionAsync(submitRequest, cancellationToken);
                    
                    if (!submitResponse.Success)
                    {
                        response.SetMessage(MessageId.E00000, $"Không thể submit bài practice test: {submitResponse.Message}");
                        return false;
                    }
                    
                    // Get problem difficulty from database to map the result
                    var problem = await _problemRepository.FirstOrDefaultAsync(p => p.ProblemId == practiceAnswer.ProblemId, cancellationToken: cancellationToken);
                    if (problem != null)
                    {
                        practiceTestResults[problem.Difficulty] = submitResponse;
                    }
                }
                
                // Calculate practice test level
                var practiceTestLevel = DeterminePracticeTestLevel(practiceTestResults);
                
                // Combine levels: 60% quiz + 40% practice test
                baseLevel = (int)Math.Round(quizLevel * 0.6 + practiceTestLevel * 0.4);
                
                // Ensure level is between 1 and 3
                baseLevel = Math.Max(1, Math.Min(3, baseLevel));
            }
            
            // Check if transcript has relevant subjects matching quiz subjects
            // If yes, incorporate transcript score (20%) into final level calculation
            int finalStudentLevel = baseLevel;
            var transcriptSubjectsUsed = new List<(string SubjectCode, string SubjectName, double Grade)>();
            
            if (studentTranscripts.Any())
            {
                // Get all subject codes from quizzes
                var quizSubjectNames = testExist.Quizzes
                    .Where(q => q.PlacementTestQuizSetting != null)
                    .Select(q => q.PlacementTestQuizSetting!.SubjectCodeName)
                    .ToList();
                
                // Find matching transcripts: check if transcript SubjectCode is contained in quiz SubjectCodeName
                var matchingTranscripts = studentTranscripts
                    .Where(t => quizSubjectNames.Any(qsn => qsn.Contains(t.SubjectCode, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                if (matchingTranscripts.Any())
                {
                    // Calculate average transcript score (scale 0-10 to level 1-3)
                    // Grade 0-5 -> Level 1, Grade 5-7.5 -> Level 2, Grade 7.5-10 -> Level 3
                    var avgGrade = matchingTranscripts.Average(t => t.Grade ?? 0);
                    
                    // Convert grade to level (1-3)
                    int transcriptLevel = avgGrade switch
                    {
                        < 5 => 1,
                        < 7.5 => 2,
                        _ => 3
                    };
                    
                    // Store for LevelReason
                    transcriptSubjectsUsed = matchingTranscripts
                        .Where(t => t.Grade.HasValue)
                        .Select(t => (t.SubjectCode, t.SubjectName, t.Grade!.Value))
                        .ToList();
                    
                    // Combine: 80% from quiz/practice test + 20% from transcript
                    finalStudentLevel = (int)Math.Round(baseLevel * 0.8 + transcriptLevel * 0.2);
                    
                    // Ensure level is between 1 and 3
                    finalStudentLevel = Math.Max(1, Math.Min(3, finalStudentLevel));
                }
            }
            
            var studentLevel = finalStudentLevel;
            
            // Build detailed level reason
            var levelReason = BuildLevelReason(
                quizLevel, 
                finalStudentLevel, 
                practiceTestResults, 
                difficultyPerformance,
                transcriptSubjectsUsed);
            
            // Prepare SubjectMarks if OtherQuestionAnswerCodes is provided
            List<SubjectMarkContext>? subjectMarks = null;
            
            // Get technologies from StudentService
            // Send message to StudentService to get student information
            var studentInformationSelectsEvent = new StudentInformationSelectsEvent
            {
                StudentId = currentUser.UserId
            };
            var informationResponse = await _requestStudentInformationSelectsClient.GetResponse<StudentInformationSelectsEventResponse>(studentInformationSelectsEvent, cancellationToken);
            
            // Get major information from CourseService
            var majorAndSemesterEvent = new CourseMajorSemesterSelectEvent
            {
                SemesterId = informationResponse.Message.Response.SemesterId,
                MajorId = informationResponse.Message.Response.MajorId
            };
            
            // Request major and semester information
            var majorAndSemesterEventResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(majorAndSemesterEvent, cancellationToken);
            if (!majorAndSemesterEventResponse.Message.Success)
            {
                response.MessageId = majorAndSemesterEventResponse.Message.MessageId;
                response.Message = majorAndSemesterEventResponse.Message.Message;
                return false;
            }
            
            if (!studentTranscripts.Any() && majorAndSemesterEventResponse.Message.Response.SemesterNumber > 4)
            {
                response.SetMessage(MessageId.E00000, "Sinh viên chưa có bảng điểm, không thể tạo lộ trình học tập cho sinh viên từ kỳ 5 trở lên");
                return false;
            }
            
            List<CourseImproveContext> courseImporve = new();
            if (request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any())
            {
                // Map transcript to SubjectMarks and add OtherQuestionAnswerCodes
                subjectMarks = studentTranscripts.Select(st => new SubjectMarkContext
                {
                    SubjectCode = st.SubjectCode,
                    SubjectName = st.SubjectName,
                    Mark = st.Grade
                }).ToList();
                
                var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent(), cancellationToken);
                if (!subjectCodeEventResponse.Message.Success)
                {
                    response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
                    return false;
                }
    
                var subjectCodes = subjectCodeEventResponse.Message.Response;
                
                HashSet<string> subjectCodesForEvaluation = new();
                // Add OtherQuestionAnswerCodes to SubjectMarks
                foreach (var questionCode in request.OtherQuestionAnswerCodes)
                {
                    switch (questionCode)
                    {
                        case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE:
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 5 && t.Grade < 7)
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Beginner,
                                        SubjectPrerequisiteCode = t.Prerequisite
                                    })
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE:
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 7 && t.Grade < 8)
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Intermidiate,
                                        SubjectPrerequisiteCode = t.Prerequisite
                                    })
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE:
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 8 && t.Grade < 9)
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Advanced,
                                        SubjectPrerequisiteCode = t.Prerequisite
                                    })
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_EVALUATION:
                            var subjects5To7 = studentTranscripts
                                .Where(t => t.Grade >= 5 && t.Grade < 7)
                                .Select(t => t.SubjectCode)
                                .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code));
                            foreach (var subjectCode in subjects5To7)
                            {
                                subjectCodesForEvaluation.Add(subjectCode);
                            }
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_EVALUATION:
                            var subjects7To8 = studentTranscripts
                                .Where(t => t.Grade >= 7 && t.Grade < 8)
                                .Select(t => t.SubjectCode)
                                .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code));
                            foreach (var subjectCode in subjects7To8)
                            {
                                subjectCodesForEvaluation.Add(subjectCode);
                            }
                            break;
                    }
                }
            }
            
            // Calculate AbilityMarks based on each Quiz (by SubjectCodeName from PlacementTestQuizSetting)
            var abilityMarks = new List<AbilityMarkContext>();
            
            // Loop through each StudentQuizCollection to calculate ability marks for each quiz
            foreach (var studentQuizCollection in studentTestCollection.StudentQuizzes)
            {
                // Get the quiz from testExist
                var quiz = testExist.Quizzes.FirstOrDefault(q => q.QuizId == studentQuizCollection.QuizId);
                
                if (quiz?.PlacementTestQuizSetting == null)
                    continue;
                
                // Get all question IDs for this quiz
                var quizQuestionIds = quiz.Questions.Select(q => q.QuestionId).ToHashSet();
                
                // Get student answers for this quiz only
                var quizAnswers = studentTestCollection.StudentAnswers
                    .Where(sa => quizQuestionIds.Contains(sa.QuestionId))
                    .ToList();
                
                if (!quizAnswers.Any())
                    continue;
                
                // Calculate score for this quiz
                var correctAnswers = quizAnswers.Count(sa => sa.Answer?.IsCorrect == true);
                var totalQuestions = quizAnswers.Count;
                var abilityScore = totalQuestions > 0 ? (double)correctAnswers / totalQuestions * 100 : 0;
                
                // Add ability mark with SubjectCodeName as the name
                abilityMarks.Add(new AbilityMarkContext
                {
                    Name = quiz.PlacementTestQuizSetting.SubjectCodeName,
                    Mark = abilityScore
                });
            }
            
            // If PracticeTestAnswers is provided, add practice test ability marks
            if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
            {
                // Calculate practice test ability marks based on actual submission results
                // Store problem info with score: (Title, Difficulty, Score)
                var practiceTestScoresWithInfo = new List<(string Title, string Difficulty, double Score)>();
                
                foreach (var practiceAnswer in request.PracticeTestAnswers)
                {
                    var problem = await _problemRepository.FirstOrDefaultAsync(p => p.ProblemId == practiceAnswer.ProblemId, cancellationToken: cancellationToken);
                    if (problem != null)
                    {
                        double score = 0.0;
                        
                        // Find the corresponding submission result
                        if (practiceTestResults.TryGetValue(problem.Difficulty, out var submitResult))
                        {
                            // Calculator score (total passed testcase / total testcase) * 100
                            score = submitResult.Response.TotalTests > 0
                                ? (double)submitResult.Response.PassedTests / submitResult.Response.TotalTests * 100
                                : 0.0;
                        }
                        // No submission result found, score remains 0
                        
                        // Store problem info with score
                        practiceTestScoresWithInfo.Add((problem.Title, problem.Difficulty, score));
                    }
                }
                
                // Add practice test ability marks với tên problem và điểm thực tế
                foreach (var (title, difficulty, score) in practiceTestScoresWithInfo)
                {
                    abilityMarks.Add(new AbilityMarkContext
                    {
                        Name = $"Bài test tự luận cấu trúc dữ liệu và giải thuật ({title}) với độ khó: {difficulty}",
                        Mark = score
                    });
                }
            }
            
            // Get StudentSurvey from cache
            var studentSurveys = await _studentQuizCollectionRepository.GetOrSetListAsync(
                CacheKey.StudentSurvey(currentUser.UserId),
                async () => await _studentQuizCollectionRepository.ToListAsync(sq => sq.StudentId == currentUser.UserId && sq.QuizType == (short) ConstantEnum.TestType.Survey),
                TimeSpan.FromMinutes(10));
            if (!studentSurveys.Any())
            {
                response.SetMessage(MessageId.E00000, "Sinh viên chưa hoàn thành bài khảo sát nào");
                return false;
            }
            
            var surveyHabit = studentSurveys.First(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));

            var selectedAnswerIds = surveyHabit.Quiz.Questions
                .SelectMany(q => q.Answers)
                .Select(a => a.AnswerId)
                .ToList();
       
            var studentQuizAnswers = surveyHabit.Quiz.Questions
                .SelectMany(q => q.Answers)
                .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                .Select(a => new StudentQuizAnswerCollection
                {
                    AnswerId = a.AnswerId,
                    Answer = a,
                })
                .ToList();

            int limitTime = GetStudentStudyTime(studentQuizAnswers);
            
            var learningPathId = Guid.NewGuid();

            var evaluationAndImprove = request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any()
                ? string.Join(",", request.OtherQuestionAnswerCodes.Select(c => ((int)c).ToString()))
                : null;
            
            var learningPathEvent = new InsertLearningPathEvent
            {
                LearningPathId = learningPathId,
                StudentId = currentUser.UserId,
                CurrentUserEmail = currentUser.Email,
                PathName = $"Lộ trình {request.LearningGoal.LearningGoalName}",
                Level = (short) studentLevel,
                LevelReason = levelReason,
                IsSkipTest = false,
                LimitTime = limitTime,
                EvaluationAndImprove = evaluationAndImprove
            };
            var learningPathResponse = await _requestInsertLearningPathEventClient.GetResponse<InsertLearningPathEventResponse>(learningPathEvent, cancellationToken);
            if (!learningPathResponse.Message.Success)
            {
                response.MessageId = learningPathResponse.Message.MessageId;
                response.Message = learningPathResponse.Message.Message;
                return false;
            }            
            
            var context = new LearningPathCreationContext
            {
                StudentQuizCollections = studentSurveys,
                CurrentUser = currentUser,
                InformationResponse = new StudentInformationSelectsEventResponseEntity
                {
                    LearningGoalName = request.LearningGoal.LearningGoalName,
                    LearningGoalType = (short) request.LearningGoal.LearningGoalType,
                    Technologies = informationResponse.Message.Response.Technologies,
                    SemesterId = informationResponse.Message.Response.SemesterId,
                    MajorId = informationResponse.Message.Response.MajorId
                },
                LearningPathId = learningPathId,
                LimitTime = limitTime,
                StudentLevel = (short)studentLevel,
                StudentMajor = new StudentMajor
                {
                    MajorCode = majorAndSemesterEventResponse.Message.Response.MajorCode,
                    MajorName = majorAndSemesterEventResponse.Message.Response.MajorName
                },
                SubjectMarks = subjectMarks,
                AbilityMarks = abilityMarks,
                CourseImprove = courseImporve
            };

            if (studentTranscripts.Any())
            {
                context.StudentTranscripts = studentTranscripts.Select(x => new StudentTranscriptContext
                {
                    SubjectCode = x.SubjectCode,
                    Status = x.Status,
                    Mark = x.Grade
                }).ToList();
            }
            
            var result = await _learningPathService.CreateLearningPathAsync(context, cancellationToken);
            if (!result.Success)
            {
                response.MessageId = result.MessageId;
                response.Message = result.Message;
                return false;
            }
            
            // True
            response.Success = true;
            response.Response = learningPathId;
            response.SetMessage(MessageId.I00001, "Thêm bài kiểm tra của học sinh");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Select student test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<StudentTestSelectResponse> SelectStudentTestAsync(StudentTestSelectQuery request)
    {
        var response = new StudentTestSelectResponse {Success = false};
        
        var studentId = _identityService.GetCurrentUser()!.UserId;
        
        // Validate student test ownership
        var ownershipCheck = await _studentTestQueryRepository.FirstOrDefaultAsync(x => x.StudentTestId == request.StudentTestId && x.StudentId == studentId);
        if (ownershipCheck == null)
        {
            response.SetMessage(MessageId.E00000, "Bài kiểm tra không thuộc về sinh viên hiện tại");
            return response;
        }

        var cacheKey = CacheKey.StudentTest(request.StudentTestId);
        var result = await GetStudentTestDetailAsync(request.StudentTestId, cacheKey);
        
        if (result == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra của học sinh");
            return response;
        }

        response.Success = true;
        response.Response = result;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài kiểm tra của học sinh");
        return response;
    }

    public async Task<AdminStudentTestSelectDetailResponse> SelectAdminStudentTestDetailAsync(AdminStudentTestSelectDetailQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminStudentTestSelectDetailResponse {Success = false};
        
        var cacheKey = CacheKey.StudentTest(request.StudentTestId);
        var result = await GetStudentTestDetailAsync(request.StudentTestId, cacheKey);
        
        if (result == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra của học sinh");
            return response;
        }

        response.Success = true;
        response.Response = result;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài kiểm tra của học sinh");
        return response;
    }

    /// <summary>
    /// Get student test detail - shared logic for both student and admin queries
    /// </summary>
    /// <param name="studentTestId">Student test ID</param>
    /// <param name="cacheKey">Cache key to use</param>
    /// <returns>Student test detail entity or null if not found</returns>
    private async Task<StudentTestSelectResponseEntity?> GetStudentTestDetailAsync(Guid studentTestId, string cacheKey)
    {
        // Get student test from cache or database
        var studentTest = await _studentTestQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                return await _studentTestQueryRepository.FirstOrDefaultAsync(x => x.StudentTestId == studentTestId && x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        
        if (studentTest == null)
        {
            return null;
        }

        // Get test info
        var test = await _testQueryRepository.FirstOrDefaultAsync(x => x.TestId == studentTest.TestId);
        if (test == null)
        {
            return null;
        }

        // Build quiz results
        var quizResults = BuildQuizResults(studentTest, test);

        return new StudentTestSelectResponseEntity
        {
            StudentTestId = studentTest.StudentTestId,
            TestId = studentTest.TestId,
            TestName = test.TestName,
            TestDescription = test.Description,
            StartedAt = studentTest.StartedAt,
            FinishedAt = studentTest.FinishedAt,
            QuizResults = quizResults
        };
    }

    /// <summary>
    /// Build quiz results from student test and test data
    /// </summary>
    /// <param name="studentTest">Student test collection</param>
    /// <param name="test">Test collection</param>
    /// <returns>List of quiz results</returns>
    private List<QuizResultSelectResponseEntity> BuildQuizResults(StudentTestCollection studentTest, TestCollection test)
    {
        var quizResults = new List<QuizResultSelectResponseEntity>();
        
        foreach (var studentQuiz in studentTest.StudentQuizzes.Where(sq => sq.QuizType == (short)ConstantEnum.TestType.Quiz))
        {
            // Find quiz details from test.Quizzes
            var quiz = test.Quizzes.FirstOrDefault(q => q.QuizId == studentQuiz.QuizId);
            
            // Skip if quiz not found
            if (quiz == null) continue;
            
            var questionResults = BuildQuestionResults(quiz, studentTest);
            
            // Calculate total correct answers
            var answeredQuestionIds = studentTest.StudentAnswers
                .Where(sa => quiz.Questions.Any(q => q.QuestionId == sa.QuestionId))
                .Select(sa => sa.QuestionId)
                .Distinct()
                .ToHashSet();
                
            // Count correct answers
            var totalCorrectAnswers = quiz.Questions
                .Where(q => answeredQuestionIds.Contains(q.QuestionId))
                .Count(q => studentTest.StudentAnswers.Any(sa =>
                    sa.QuestionId == q.QuestionId &&
                    q.Answers.Any(a => a.AnswerId == sa.AnswerId && a.IsCorrect)
                ));
            
            quizResults.Add(new QuizResultSelectResponseEntity
            {
                QuizId = quiz.QuizId,
                Title = quiz.PlacementTestQuizSetting!.Title,
                Description = quiz.PlacementTestQuizSetting.Description,
                SubjectCode = quiz.PlacementTestQuizSetting!.SubjectCode,
                SubjectCodeName = quiz.PlacementTestQuizSetting.SubjectCodeName,
                TotalQuestions = quiz.Questions.Count,
                TotalCorrectAnswers = totalCorrectAnswers,
                QuestionResults = questionResults
            });
        }
        
        return quizResults;
    }

    /// <summary>
    /// Build question results for a quiz
    /// </summary>
    /// <param name="quiz">Quiz collection</param>
    /// <param name="studentTest">Student test collection</param>
    /// <returns>List of question results</returns>
    private List<QuestionsResultSelectResponseEntity> BuildQuestionResults(QuizCollection quiz, StudentTestCollection studentTest)
    {
        var questionResults = new List<QuestionsResultSelectResponseEntity>();
        
        // Get question results for this quiz - including answers and whether student selected them
        foreach (var question in quiz.Questions)
        {
            var answerResults = new List<StudentAnswerDetailResponse>();
            foreach (var answer in question.Answers)
            {
                var selectedByStudent = studentTest.StudentAnswers.Any(sa => 
                    sa.QuestionId == question.QuestionId && sa.AnswerId == answer.AnswerId);
                    
                answerResults.Add(new StudentAnswerDetailResponse
                {
                    AnswerId = answer.AnswerId,
                    IsCorrectAnswer = answer.IsCorrect,
                    SelectedByStudent = selectedByStudent,
                    AnswerText = answer.AnswerText
                });
            }
            
            questionResults.Add(new QuestionsResultSelectResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                DifficultyLevel = question.DifficultyLevel,
                Explanation = question.Explanation,
                Answers = answerResults
            });
        }
        
        return questionResults;
    }

    /// <summary>
    /// Determine student level based on test performance across difficulty levels
    /// </summary>
    /// <param name="studentAnswers">Student's answers</param>
    /// <param name="quizzes">List of quizzes in the test</param>
    /// <param name="difficultyPerformance">Output: Performance by difficulty level</param>
    /// <returns>Student level (1-3)</returns>
    private int DetermineStudentLevel(List<StudentAnswerCollection> studentAnswers, List<QuizCollection> quizzes, out Dictionary<int, (int correct, int total)> difficultyPerformance)
    {
        difficultyPerformance = new Dictionary<int, (int correct, int total)>();
        
        for (int i = 1; i <= 3; i++)
        {
            difficultyPerformance[i] = (0, 0);
        }

        var allQuestions = quizzes.SelectMany(q => q.Questions).ToList();

        foreach (var question in allQuestions)
        {
            if (!question.DifficultyLevel.HasValue || question.DifficultyLevel.Value < 1 || question.DifficultyLevel.Value > 3)
                continue;

            int level = question.DifficultyLevel.Value;
            var performance = difficultyPerformance[level];

            // Increment total questions for this difficulty level although student may not have answered
            performance.total++;

            // Get ALL student answers for this question
            var studentAnswersForQuestion = studentAnswers
                .Where(sa => sa.QuestionId == question.QuestionId)
                .Select(sa => sa.AnswerId)
                .ToList();
            
            // Nếu sinh viên có trả lời, kiểm tra đúng/sai
            if (studentAnswersForQuestion.Any())
            {
                // Get all correct answer IDs
                var correctAnswerIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.AnswerId)
                    .ToHashSet();

                // Check if student selected exactly the correct answers
                bool isCorrect = studentAnswersForQuestion.Count == correctAnswerIds.Count 
                    && studentAnswersForQuestion.All(id => correctAnswerIds.Contains(id ?? Guid.Empty));
                
                if (isCorrect)
                {
                    performance.correct++;
                }
            }
            // Nếu sinh viên không trả lời -> tính là sai (không cộng correct)

            difficultyPerformance[level] = performance;
        }

        // Simplified logic: Find highest level with >= 70% accuracy
        int studentLevel = 1;
        
        for (int level = 3; level >= 1; level--)
        {
            var (correct, total) = difficultyPerformance[level];
            
            if (total == 0) continue;
            
            double accuracy = (double)correct / total;
            
            if (accuracy >= 0.7)
            {
                studentLevel = level;
                break;
            }
        }

        return studentLevel;
    }
    
    /// <summary>
    /// Determine student level based on practice test results (Easy, Medium, Hard)
    /// Logic: Find highest difficulty level where student passed (>= 70% test cases)
    /// </summary>
    /// <param name="practiceTestResults">Dictionary with difficulty as key and submission response as value</param>
    /// <returns>Level from 1 (Easy) to 3 (Hard)</returns>
    private int DeterminePracticeTestLevel(Dictionary<string, PracticeTestSubmitInsertResponse> practiceTestResults)
    {
        var difficultyLevelMap = new Dictionary<string, int>
        {
            { nameof(ConstantEnum.ProblemDifficultyLevel.Easy), 1 },
            { nameof(ConstantEnum.ProblemDifficultyLevel.Medium), 2 },
            { nameof(ConstantEnum.ProblemDifficultyLevel.Hard), 3 }
        };
        
        int studentLevel = 1; // Default to Easy
        
        // Check from hardest to easiest
        var difficulties = new[] 
        { 
            nameof(ConstantEnum.ProblemDifficultyLevel.Easy), 
            nameof(ConstantEnum.ProblemDifficultyLevel.Medium), 
            nameof(ConstantEnum.ProblemDifficultyLevel.Hard) 
        };
        
        foreach (var difficulty in difficulties)
        {
            if (practiceTestResults.ContainsKey(difficulty))
            {
                var result = practiceTestResults[difficulty];
                {
                    double passRate = result.Response.TotalTests > 0 
                        ? (double)result.Response.PassedTests / result.Response.TotalTests 
                        : 0;
                    
                    // If student passed >= 70% test cases at this difficulty
                    if (passRate >= 0.7)
                    {
                        studentLevel = difficultyLevelMap[difficulty];
                        break;
                    }
                }
            }
        }
        
        return studentLevel;
    }

    /// <summary>
    /// Get student study time from survey answers
    /// </summary>
    /// <param name="studentQuizAnswers"></param>
    /// <returns></returns>
    private int GetStudentStudyTime(IEnumerable<StudentQuizAnswerCollection> studentQuizAnswers)
    {
        var answerRules = studentQuizAnswers
            .Where(a => a.Answer?.AnswerRule != null)
            .SelectMany(a => a.Answer!.AnswerRule!)
            .ToList();

        int? hourPerDay = null;
        int? hourPerWeek = null;
        int? daysPerWeek = null;
        int? months = null;

        foreach (var rule in answerRules)
        {
            var avg = (rule.NumericMin ?? 0) + (rule.NumericMax ?? rule.NumericMin ?? 0);
            avg /= ((rule.NumericMin.HasValue && rule.NumericMax.HasValue) ? 2 : 1);

            if (Enum.TryParse<ConstantEnum.AnswerRuleUnit>(rule.Unit, out var unit))
            {
                switch (unit)
                {
                    case ConstantEnum.AnswerRuleUnit.HourPerDay:
                        hourPerDay = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.HourPerWeek:
                        hourPerWeek = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.Days:
                        daysPerWeek = avg;
                        break;
                    case ConstantEnum.AnswerRuleUnit.Months:
                        months = avg;
                        break;
                }
            }
        }

        int totalMinutes = 0;

        if (hourPerDay.HasValue && daysPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4 * months.Value;
        }
        else if (hourPerWeek.HasValue && months.HasValue)
        {
            totalMinutes = hourPerWeek.Value * 60 * 4 * months.Value;
        }
        else if (hourPerDay.HasValue && months.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * 7 * 4 * months.Value;
        }
        else if (hourPerDay.HasValue && daysPerWeek.HasValue)
        {
            totalMinutes = hourPerDay.Value * 60 * daysPerWeek.Value * 4;
        }

        int totalHours = totalMinutes / 60;
        return totalHours;
    }

    /// <summary>
    /// Build detailed level reason in Vietnamese based on quiz and practice test performance
    /// </summary>
    /// <param name="quizLevel">Level from quiz (1-3)</param>
    /// <param name="finalLevel">Final combined level (1-3)</param>
    /// <param name="practiceTestResults">Practice test results (optional)</param>
    /// <param name="difficultyPerformance">Performance by difficulty level from quiz</param>
    /// <param name="transcriptSubjectsUsed">Transcript subjects used in calculation (optional)</param>
    /// <returns>Detailed reason string in Vietnamese</returns>
    private string BuildLevelReason(
        int quizLevel, 
        int finalLevel, 
        Dictionary<string, PracticeTestSubmitInsertResponse>? practiceTestResults,
        Dictionary<int, (int correct, int total)> difficultyPerformance,
        List<(string SubjectCode, string SubjectName, double Grade)>? transcriptSubjectsUsed = null)
    {
        var reason = new System.Text.StringBuilder();
        
        // Part 1: Quiz performance explanation
        reason.AppendLine("📊 **Kết quả bài kiểm tra lý thuyết:**");
        reason.AppendLine();
        
        for (int level = 1; level <= 3; level++)
        {
            var (correct, total) = difficultyPerformance[level];
            if (total > 0)
            {
                double accuracy = (double)correct / total * 100;
                string levelName = level switch
                {
                    1 => "Cơ bản",
                    2 => "Trung bình",
                    3 => "Nâng cao",
                    _ => "Không xác định"
                };
                
                reason.AppendLine($"- Câu hỏi mức độ **{levelName}**: {correct}/{total} câu đúng ({accuracy:F1}%)");
            }
        }
        
        reason.AppendLine();
        reason.AppendLine($"→ Kết quả đánh giá từ bài kiểm tra lý thuyết: **Trình độ {quizLevel}**");
        
        string quizLevelDescription = quizLevel switch
        {
            1 => "Bạn đã nắm vững các kiến thức nền tảng cơ bản. Đây là nền tảng tốt để bắt đầu hành trình học tập.",
            2 => "Bạn đã có kiến thức vững vàng ở mức trung bình. Bạn có thể tiếp tục phát triển lên các mức độ cao hơn.",
            3 => "Xuất sắc! Bạn đã thể hiện năng lực vượt trội với kiến thức nâng cao. Bạn có nền tảng rất tốt để học các khóa học chuyên sâu.",
            _ => "Chưa xác định được trình độ."
        };
        reason.AppendLine($"  {quizLevelDescription}");
        
        // Part 2: Practice test performance (if available)
        if (practiceTestResults != null && practiceTestResults.Any())
        {
            reason.AppendLine();
            reason.AppendLine(" **Kết quả bài kiểm tra thực hành (Coding):**");
            reason.AppendLine();
            
            var difficulties = new[] 
            { 
                (nameof(ConstantEnum.ProblemDifficultyLevel.Easy), "Dễ", 1),
                (nameof(ConstantEnum.ProblemDifficultyLevel.Medium), "Trung bình", 2),
                (nameof(ConstantEnum.ProblemDifficultyLevel.Hard), "Khó", 3)
            };
            
            int practiceLevel = 1;
            foreach (var (diffKey, diffName, level) in difficulties)
            {
                if (practiceTestResults.ContainsKey(diffKey))
                {
                    var result = practiceTestResults[diffKey];
                    double passRate = result.Response.TotalTests > 0 
                        ? (double)result.Response.PassedTests / result.Response.TotalTests * 100
                        : 0;
                    
                    string status = passRate >= 70 ? " Đạt" : " Chưa đạt";
                    reason.AppendLine($"- Bài tập mức độ **{diffName}**: {result.Response.PassedTests}/{result.Response.TotalTests} test cases ({passRate:F1}%) {status}");
                    
                    if (passRate >= 70)
                    {
                        practiceLevel = level;
                    }
                }
            }
            
            reason.AppendLine();
            reason.AppendLine($"→ Kết quả đánh giá từ bài kiểm tra thực hành: **Trình độ {practiceLevel}**");
            
            string practiceLevelDescription = practiceLevel switch
            {
                1 => "Bạn đã hoàn thành tốt các bài tập cơ bản. Hãy tiếp tục luyện tập để nâng cao kỹ năng coding.",
                2 => "Tốt! Bạn đã giải quyết được các bài tập ở mức trung bình. Kỹ năng lập trình của bạn đang phát triển tốt.",
                3 => "Tuyệt vời! Bạn đã vượt qua các bài tập khó. Kỹ năng giải quyết vấn đề và tư duy thuật toán của bạn rất ấn tượng.",
                _ => "Chưa xác định được trình độ thực hành."
            };
            reason.AppendLine($"  {practiceLevelDescription}");
            
            // Part 3: Transcript scores (if available)
            if (transcriptSubjectsUsed != null && transcriptSubjectsUsed.Any())
            {
                reason.AppendLine();
                reason.AppendLine(" **Kết quả học tập từ bảng điểm:**");
                reason.AppendLine();
                
                foreach (var (subjectCode, subjectName, grade) in transcriptSubjectsUsed)
                {
                    reason.AppendLine($"- {subjectName} ({subjectCode}): {grade:F1}/10");
                }
                
                var avgGrade = transcriptSubjectsUsed.Average(t => t.Grade);
                int transcriptLevel = avgGrade switch
                {
                    < 5 => 1,
                    < 7.5 => 2,
                    _ => 3
                };
                
                reason.AppendLine();
                reason.AppendLine($"→ Điểm trung bình: {avgGrade:F1}/10");
                reason.AppendLine($"→ Đánh giá từ bảng điểm: **Trình độ {transcriptLevel}**");
                
                string transcriptDescription = transcriptLevel switch
                {
                    1 => "Bạn cần cải thiện kết quả học tập ở các môn này. Lộ trình sẽ tập trung củng cố lại kiến thức nền tảng.",
                    2 => "Bạn có kết quả học tập khá tốt. Lộ trình sẽ giúp bạn phát triển và nâng cao kiến thức.",
                    3 => "Xuất sắc! Kết quả học tập của bạn rất tốt. Bạn đã có nền tảng vững để học các khóa học nâng cao.",
                    _ => ""
                };
                reason.AppendLine($"  {transcriptDescription}");
            }
            
            // Part 4: Combined result
            reason.AppendLine();
            reason.AppendLine(" **Kết quả tổng hợp:**");
            reason.AppendLine();
            
            // Calculate base level from quiz + practice
            int baseLevel = (int)Math.Round(quizLevel * 0.6 + practiceLevel * 0.4);
            
            if (transcriptSubjectsUsed != null && transcriptSubjectsUsed.Any())
            {
                var avgGrade = transcriptSubjectsUsed.Average(t => t.Grade);
                int transcriptLevel = avgGrade switch
                {
                    < 5 => 1,
                    < 7.5 => 2,
                    _ => 3
                };
                
                reason.AppendLine($"- Trọng số: [Lý thuyết (60%) + Thực hành (40%)] × 80% + Bảng điểm (20%)");
                reason.AppendLine($"- Điểm từ bài kiểm tra: {quizLevel} × 0.6 + {practiceLevel} × 0.4 = {(quizLevel * 0.6 + practiceLevel * 0.4):F1}");
                reason.AppendLine($"- Điểm từ bảng điểm: Level {transcriptLevel}");
                reason.AppendLine($"- Điểm cuối cùng: {baseLevel} × 0.8 + {transcriptLevel} × 0.2 = {(baseLevel * 0.8 + transcriptLevel * 0.2):F1}");
            }
            else
            {
                reason.AppendLine($"- Trọng số: Lý thuyết (60%) + Thực hành (40%)");
                reason.AppendLine($"- Điểm tổng hợp: {quizLevel} × 0.6 + {practiceLevel} × 0.4 = {(quizLevel * 0.6 + practiceLevel * 0.4):F1}");
            }
            
            reason.AppendLine($"- Trình độ cuối cùng: **Level {finalLevel}**");
        }
        else
        {
            // No practice test - only quiz result (and possibly transcript)
            
            // Check if transcript is used
            if (transcriptSubjectsUsed != null && transcriptSubjectsUsed.Any())
            {
                reason.AppendLine();
                reason.AppendLine("📚 **Kết quả học tập từ bảng điểm:**");
                reason.AppendLine();
                
                foreach (var (subjectCode, subjectName, grade) in transcriptSubjectsUsed)
                {
                    reason.AppendLine($"- {subjectName} ({subjectCode}): {grade:F1}/10");
                }
                
                var avgGrade = transcriptSubjectsUsed.Average(t => t.Grade);
                int transcriptLevel = avgGrade switch
                {
                    < 5 => 1,
                    < 7.5 => 2,
                    _ => 3
                };
                
                reason.AppendLine();
                reason.AppendLine($"→ Điểm trung bình: {avgGrade:F1}/10");
                reason.AppendLine($"→ Đánh giá từ bảng điểm: **Trình độ {transcriptLevel}**");
                
                string transcriptDescription = transcriptLevel switch
                {
                    1 => "Bạn cần cải thiện kết quả học tập ở các môn này. Lộ trình sẽ tập trung củng cố lại kiến thức nền tảng.",
                    2 => "Bạn có kết quả học tập khá tốt. Lộ trình sẽ giúp bạn phát triển và nâng cao kiến thức.",
                    3 => "Xuất sắc! Kết quả học tập của bạn rất tốt. Bạn đã có nền tảng vững để học các khóa học nâng cao.",
                    _ => ""
                };
                reason.AppendLine($"  {transcriptDescription}");
            }
            
            reason.AppendLine();
            reason.AppendLine("🎯 **Kết quả cuối cùng:**");
            reason.AppendLine();
            
            if (transcriptSubjectsUsed != null && transcriptSubjectsUsed.Any())
            {
                var avgGrade = transcriptSubjectsUsed.Average(t => t.Grade);
                int transcriptLevel = avgGrade switch
                {
                    < 5 => 1,
                    < 7.5 => 2,
                    _ => 3
                };
                
                reason.AppendLine($"- Trọng số: Bài kiểm tra lý thuyết (80%) + Bảng điểm (20%)");
                reason.AppendLine($"- Điểm từ bài kiểm tra: Level {quizLevel}");
                reason.AppendLine($"- Điểm từ bảng điểm: Level {transcriptLevel}");
                reason.AppendLine($"- Điểm cuối cùng: {quizLevel} × 0.8 + {transcriptLevel} × 0.2 = {(quizLevel * 0.8 + transcriptLevel * 0.2):F1}");
                reason.AppendLine($"- Trình độ cuối cùng: **Level {finalLevel}**");
            }
            else
            {
                reason.AppendLine($"Trình độ của bạn được đánh giá là **Level {finalLevel}** dựa trên kết quả bài kiểm tra lý thuyết.");
            }
        }
        
        reason.AppendLine();
        reason.AppendLine("---");
        return reason.ToString();
    }
}

public class StudentTestServiceDependencies
{
    public StudentTestServiceDependencies(
        ICommandRepository<StudentTest> studentTestRepository,
        ICommandRepository<StudentQuiz> studentQuizRepository,
        IQueryRepository<StudentTestCollection> studentTestQueryRepository,
        IQueryRepository<TestCollection> testQueryRepository,
        IQueryRepository<QuestionCollection> questionQueryRepository,
        IQueryRepository<StudentQuizCollection> studentQuizCollectionRepository,
        ICommandRepository<OutboxMessage> outboxRepository,
        IRequestClient<StudentInterestSurveyAnalysisEvent> requestStudentInterestAnalysisClient,
        IRequestClient<StudentInformationSelectsEvent> requestStudentInformationSelectsClient,
        IRequestClient<StudentTranscriptSelectEvent> requestStudentTranscriptClient,
        IRequestClient<CourseMajorSemesterSelectEvent> requestCourseMajorSemesterClient,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        StudentTestRepository = studentTestRepository;
        StudentQuizRepository = studentQuizRepository;
        StudentTestQueryRepository = studentTestQueryRepository;
        TestQueryRepository = testQueryRepository;
        QuestionQueryRepository = questionQueryRepository;
        StudentQuizCollectionRepository = studentQuizCollectionRepository;
        OutboxRepository = outboxRepository;
        RequestStudentInterestAnalysisClient = requestStudentInterestAnalysisClient;
        RequestStudentInformationSelectsClient = requestStudentInformationSelectsClient;
        RequestStudentTranscriptClient = requestStudentTranscriptClient;
        RequestCourseMajorSemesterClient = requestCourseMajorSemesterClient;
        IdentityService = identityService;
        UnitOfWork = unitOfWork;
    }

    public ICommandRepository<StudentTest> StudentTestRepository { get; }
    public ICommandRepository<StudentQuiz> StudentQuizRepository { get; }
    public IQueryRepository<StudentTestCollection> StudentTestQueryRepository { get; }
    public IQueryRepository<TestCollection> TestQueryRepository { get; }
    public IQueryRepository<QuestionCollection> QuestionQueryRepository { get; }
    public IQueryRepository<StudentQuizCollection> StudentQuizCollectionRepository { get; }
    public ICommandRepository<OutboxMessage> OutboxRepository { get; }
    public IRequestClient<StudentInterestSurveyAnalysisEvent> RequestStudentInterestAnalysisClient { get; }
    public IRequestClient<StudentInformationSelectsEvent> RequestStudentInformationSelectsClient { get; }
    public IRequestClient<StudentTranscriptSelectEvent> RequestStudentTranscriptClient { get; }
    public IRequestClient<CourseMajorSemesterSelectEvent> RequestCourseMajorSemesterClient { get; }
    public IIdentityService IdentityService { get; }
    public IUnitOfWork UnitOfWork { get; }
}
