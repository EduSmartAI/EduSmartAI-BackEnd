using System.Text.Json;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using BaseService.Domain.Snapshort;
using BuildingBlocks.Messaging.Events.AiService.StudentInterestSurveyAnalysisEvents;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using QuizService.Application.Applications.Admin.Queries.StudentTests;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using IdentityEntity = BaseService.Application.Interfaces.IdentityHepers.IdentityEntity;

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
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="deps"></param>
    public StudentTestService(StudentTestServiceDependencies deps)
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
        _identityService = deps.IdentityService;
        _unitOfWork = deps.UnitOfWork;
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
            
            // Calculate score 
            var studentLevel = DetermineStudentLevel(studentTestCollection.StudentAnswers.ToList(), testExist.Quizzes.ToList());
            
            // Send message to StudentService to get student information
            var studentInformationSelectsEvent = new StudentInformationSelectsEvent
            {
                StudentId = currentUser.UserId
            };
            
            // Get technologies from StudentService
            var informationResponse = await _requestStudentInformationSelectsClient.GetResponse<StudentInformationSelectsEventResponse>(studentInformationSelectsEvent, cancellationToken);
            
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
            
            var learningPathId = Guid.NewGuid();
            
            // Find HABIT survey from student quiz collections
            var surveyHabit = studentSurveys.First(x => x.Quiz.SurveyQuizSetting!.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT));

            // Get selected answer IDs
            var selectedAnswerIds = surveyHabit.Quiz.Questions
                .SelectMany(q => q.Answers)
                .Select(a => a.AnswerId)
                .ToList();
       
            // Build StudentQuizAnswerCollection for selected answers
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
            
            var prepareStudentLearningProfileForAiRequest = new StudentLearningProfileContext
            {
                StudentQuizCollections = studentSurveys,
                CurrentUser = currentUser,
                InformationResponse = informationResponse.Message,
                Response = response,
                LearningPathId = learningPathId,
                LimitTime = limitTime,
                StudentLevel = (short)studentLevel
            };
            
            // Publish Message to AIService
            var studentMajorOrientationEvent = await PrepareStudentLearningProfileForAiAsync(prepareStudentLearningProfileForAiRequest, cancellationToken);
            if (!studentMajorOrientationEvent.Success)
            {
                response.MessageId = studentMajorOrientationEvent.MessageId;
                response.Message = studentMajorOrientationEvent.Message;
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
    /// <returns>Student level (1-5): 1=Beginner, 2=Elementary, 3=Intermediate, 4=Advanced, 5=Expert</returns>
    private int DetermineStudentLevel(List<StudentAnswerCollection> studentAnswers, List<QuizCollection> quizzes)
    {
        var difficultyPerformance = new Dictionary<int, (int correct, int total)>();
        
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

            // Get ALL student answers for this question
            var studentAnswersForQuestion = studentAnswers
                .Where(sa => sa.QuestionId == question.QuestionId)
                .Select(sa => sa.AnswerId)
                .ToList();
            
            if (studentAnswersForQuestion.Any())
            {
                // Get all correct answer IDs
                var correctAnswerIds = question.Answers
                    .Where(a => a.IsCorrect)
                    .Select(a => a.AnswerId)
                    .ToHashSet();

                // Check if student selected exactly the correct answers
                bool isCorrect = studentAnswersForQuestion.Count == correctAnswerIds.Count && studentAnswersForQuestion.All(id => correctAnswerIds.Contains(id ?? Guid.Empty));
                
                if (isCorrect)
                {
                    performance.correct++;
                }
                performance.total++;
            }

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
    /// Prepare student learning profile for AI analysis and send message to AiService
    /// </summary>
    /// <param name="context"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<StudentTestInsertResponse> PrepareStudentLearningProfileForAiAsync(StudentLearningProfileContext context, CancellationToken cancellationToken)
    {
        var studentMajorOrientationEvent = new StudentMajorOrientationEvent();

        if (context.InformationResponse.Response.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
        {
            // Find INTEREST survey from student quiz collections
            var interestSurvey = context.StudentQuizCollections.FirstOrDefault(sq => sq.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST))!;

            // Get selected answer IDs from student's answers
            var selectedAnswerIds = interestSurvey.StudentQuizAnswers
                .Select(a => a.AnswerId)
                .ToHashSet();

            // Prepare questions and student answers for AI analysis
            var interestQuestions = interestSurvey.Quiz.Questions.Select(question => new StudentInterestQuestion
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                StudentAnswers = question.Answers
                    .Where(a => selectedAnswerIds.Contains(a.AnswerId))
                    .Select(a => a.AnswerText)
                    .ToList()
            }).Where(q => q.StudentAnswers.Any()).ToList();

            // Set message to AI service for analysis
            var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
            {
                StudentId = context.CurrentUser.UserId,
                Questions = interestQuestions
            };

            // Send request to AiService and get response (you need to inject IRequestClient)
            var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(studentInterestAnalysisEvent, cancellationToken);
            if (!aiAnalysisResponse.Message.Success)
            {
                context.Response.SetMessage(MessageId.E99999);
                return context.Response;
            }
            
            // Set learning goal from AI analysis result
            studentMajorOrientationEvent.LearningGoal = aiAnalysisResponse.Message.Response.LearningGoal;
        }
        else
        {
            studentMajorOrientationEvent.LearningGoal = context.InformationResponse.Response.LearningGoalName;
        }
        
        // Extract frameworks and languages from technologies response
        var frameworks = context.InformationResponse.Response.Technologies
            .Where(x => x.TechnologyType == (short) ConstantEnum.TechnologyType.Framework)
            .Select(x => x.TechnologyName)
            .ToList();

        var languages = context.InformationResponse.Response.Technologies
            .Where(x => x.TechnologyType == (short ) ConstantEnum.TechnologyType.ProgrammingLanguage)
            .Select(x => x.TechnologyName)
            .ToList();

        // Use AI analysis result to determine major orientation
        studentMajorOrientationEvent.Frameworks = frameworks;
        studentMajorOrientationEvent.Languages = languages;
        studentMajorOrientationEvent.IdentityEntity = new BuildingBlocks.Messaging.Events.QuizService.IdentityEntity
        {
            UserId = context.CurrentUser.UserId,
            Email = context.CurrentUser.Email,
        };
        studentMajorOrientationEvent.LimitTime = $"{context.LimitTime} Giờ";
        studentMajorOrientationEvent.LearningPathId = context.LearningPathId;
        studentMajorOrientationEvent.SemesterId = context.InformationResponse.Response.SemesterId;
        studentMajorOrientationEvent.StudentLevel = context.StudentLevel;

        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Type = nameof(StudentMajorOrientationEvent),
            Content = JsonSerializer.Serialize(studentMajorOrientationEvent),
            OccurredOnUtc = DateTime.UtcNow,
        };

        await _outboxRepository.AddAsync(outboxMessage);
        await _unitOfWork.SaveChangesAsync(context.CurrentUser.Email, cancellationToken);

        context.Response.Success = true;
        context.Response.SetMessage(MessageId.I00001, "Chuẩn bị hồ sơ học tập của sinh viên cho AI");
        return context.Response;
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
    public IIdentityService IdentityService { get; }
    public IUnitOfWork UnitOfWork { get; }
}
public class StudentLearningProfileContext
{
    public List<StudentQuizCollection> StudentQuizCollections { get; init; } = null!;
    public IdentityEntity CurrentUser { get; init; } = null!;
    public StudentInformationSelectsEventResponse InformationResponse { get; init; } = null!;
    public StudentTestInsertResponse Response { get; init; } = null!;
    public Guid LearningPathId { get; init; }
    public int LimitTime { get; init; }
    public short StudentLevel { get; init; }
}
