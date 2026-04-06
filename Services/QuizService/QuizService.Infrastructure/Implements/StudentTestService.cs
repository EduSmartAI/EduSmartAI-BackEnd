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
using AbilityImprove = QuizService.Application.Applications.LearningPaths.AbilityImprove;
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
        // Khởi tạo response với trạng thái mặc định là thất bại - cần thiết để trả về kết quả cho client
        var response = new StudentTestInsertResponse { Success = false };
        
        // Lấy thông tin người dùng hiện tại từ JWT token - cần để xác định sinh viên nào đang submit bài test
        var currentUser = _identityService.GetCurrentUser()!;

        // Lấy thời gian hiện tại theo UTC để đảm bảo tính nhất quán về timezone khi lưu database
        var currentTime = DateTime.UtcNow;
        
        // Kiểm tra thời gian bắt đầu không được lớn hơn thời gian hiện tại
        // Vì không hợp lý khi bắt đầu test ở tương lai nhưng đã submit xong ở hiện tại
        if (request.StartedAt > currentTime)
        {
                response.SetMessage(MessageId.E00000, 
                    "Thời gian bắt đầu phải bé hơn hoặc bằng thời gian hiện tại (Giờ hiện tại là " 
                    + currentTime.ToString("yyyy-MM-dd HH:mm:ss") + ")");
            return response;
        }
        
        // Kiểm tra TestId có tồn tại trong hệ thống hay không
        // Cần thiết để đảm bảo sinh viên submit đúng bài test hợp lệ
        var testExist = await _testQueryRepository.FirstOrDefaultAsync(x => x.TestId == request.TestId);
        if (testExist == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra");
            return response;
        }
        
        // Validate danh sách QuizIds mà sinh viên đã làm có thuộc Test này không
        // Đảm bảo tính toàn vẹn dữ liệu - không cho phép submit quiz không thuộc test
        var quizIds = request.QuizIds;
        var validQuizIds = testExist.Quizzes.Where(q => quizIds.Contains(q.QuizId)).Select(q => q.QuizId).ToList();
        if (validQuizIds.Count != quizIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Có bài quiz không hợp lệ trong danh sách bài quiz");
            return response;
        }
        
        // Validate tất cả QuestionIds trong các câu trả lời có tồn tại trong database không
        // Ngăn chặn việc submit câu hỏi giả mạo hoặc không tồn tại
        var questionIds = request.Answers.Select(a => a.QuestionId).Distinct().ToList();
        var validQuestions = await _questionQueryRepository.ToListAsync(x => questionIds.Contains(x.QuestionId));
        if (validQuestions.Count != questionIds.Count)
        {
            response.SetMessage(MessageId.E00000, "Có câu hỏi không hợp lệ trong danh sách trả lời");
            return response;
        }
        
        // Validate tất cả AnswerIds trong các câu trả lời có thuộc các câu hỏi hợp lệ không
        // Đảm bảo sinh viên không chọn các đáp án không thuộc câu hỏi đó
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

        // if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
        // {
        //     // Validate that we have exactly 3 problems (Easy, Medium, Hard)
        //     if (request.PracticeTestAnswers.Count != 3)
        //     {
        //         response.SetMessage(MessageId.E00000, "PracticeTestAnswers phải có đúng 3 bài: Dễ, Trung bình, Khó");
        //         return response;
        //     }
        // }

        // Bắt đầu transaction để đảm bảo tính ACID (Atomicity, Consistency, Isolation, Durability)
        // Nếu có lỗi xảy ra ở bất kỳ đâu, toàn bộ quá trình sẽ rollback
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Tạo đối tượng StudentTest mới để lưu thông tin bài test của sinh viên
            var studentTest = new StudentTest
            {
                StudentId = currentUser.UserId, // ID sinh viên đang làm bài
                TestId = request.TestId, // ID bài test
                StartedAt = StringUtil.ConvertToUtcTime(request.StartedAt), // Thời gian bắt đầu (convert sang UTC)
                FinishedAt = currentTime, // Thời gian kết thúc (thời điểm submit)
                // Map danh sách câu trả lời của sinh viên
                StudentAnswers = request.Answers.Select(a => new StudentAnswer
                {
                    QuestionId = a.QuestionId, // ID câu hỏi
                    AnswerId = a.AnswerId, // ID đáp án sinh viên chọn
                }).ToList()
            };
            
            // Tạo danh sách StudentQuiz cho từng quiz mà sinh viên đã làm
            // Cần thiết để tracking sinh viên đã làm những quiz nào
            var studentQuizzes = request.QuizIds.Select(x => new StudentQuiz
            {
                StudentId = currentUser.UserId,
                QuizId = x,
                QuizType = (short) ConstantEnum.TestType.Quiz, // Loại quiz (không phải survey)
            }).ToList();
            await _studentQuizRepository.AddRangeAsync(studentQuizzes);
            
            // Lưu StudentTest vào database (write model - SQL Server)
            await _studentTestRepository.AddAsync(studentTest);
            await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);
            
            // Khởi tạo list để chứa StudentQuizCollection (read model - MongoDB)
            var studentQuizCollections = new List<StudentQuizCollection>();
            
            // Map từ write model (SQL) sang read model (MongoDB) cho từng StudentQuiz
            // Read model dùng để query nhanh, tối ưu cho việc đọc dữ liệu
            foreach (var studentQuiz in studentQuizzes)
            {
                var quiz = testExist.Quizzes.FirstOrDefault(qu => qu.QuizId == studentQuiz.QuizId);
                var studentQuizCollection = StudentQuizCollection.FromWriteModel(studentQuiz, quiz, new UserInformation{Email = currentUser.Email, FullName = currentUser.FullName});
                _unitOfWork.Store(studentQuizCollection); // Lưu vào MongoDB
                studentQuizCollections.Add(studentQuizCollection);
            }

            // Tạo dictionary để map nhanh từ AnswerId sang AnswerCollection
            // Tối ưu hiệu suất khi cần tra cứu answer thay vì dùng loop
            var answerDict = new Dictionary<Guid?, AnswerCollection>();
            foreach (var answer in validAnswers) answerDict.Add(answer.AnswerId, answer);

            // Tạo StudentTestCollection (read model) từ StudentTest (write model)
            // Chứa đầy đủ thông tin test, câu trả lời, và thông tin sinh viên
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
                // Map danh sách câu trả lời với đầy đủ thông tin question và answer
                StudentAnswers = studentTest.StudentAnswers.Select(sa => new StudentAnswerCollection
                {
                    QuestionId = sa.QuestionId,
                    AnswerId = sa.AnswerId,
                    Answer = answerDict[sa.AnswerId], // Lấy answer từ dictionary để tối ưu
                    Question = validQuestions.FirstOrDefault(q => q.QuestionId == sa.QuestionId),
                    CreatedAt = sa.CreatedAt,
                    CreatedBy = sa.CreatedBy,
                    UpdatedAt = sa.UpdatedAt,
                    UpdatedBy = sa.UpdatedBy,
                    IsActive = sa.IsActive
                }).ToList(),
                StudentQuizzes = studentQuizCollections,
            };
            
            // Lưu StudentTestCollection vào MongoDB
            _unitOfWork.Store(studentTestCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Lấy bảng điểm của sinh viên từ StudentService thông qua message bus (RabbitMQ)
            // Cần thiết để tính toán level và đánh giá năng lực dựa trên điểm đã học
            var studentTranscriptEvent = new StudentTranscriptSelectEvent
            {
                StudentId = currentUser.UserId
            };

            var transcriptResponse = await _requestStudentTranscriptClient.GetResponse<StudentTranscriptSelectEventResponse>(studentTranscriptEvent, cancellationToken);
            var studentTranscripts = transcriptResponse.Message.Response;
            
            // Xác định level sinh viên dựa trên kết quả bài quiz (1: Beginner, 2: Intermediate, 3: Advanced)
            // difficultyPerformance chứa thông tin chi tiết về tỷ lệ đúng/sai theo từng độ khó
            var quizLevel = DetermineStudentLevel(studentTestCollection.StudentAnswers.ToList(), testExist.Quizzes.ToList(), out var difficultyPerformance);
            
            // baseLevel là level tính từ quiz - sẽ dùng để kết hợp với practice test và transcript
            int baseLevel = quizLevel;
            
            // Dictionary lưu kết quả submit các bài practice test (Easy, Medium, Hard)
            // Cần khai báo ở đây để dùng sau này tính level và ability marks
            var practiceTestResults = new Dictionary<string, PracticeTestSubmitInsertResponse>();
            
            // Nếu sinh viên có làm bài practice test (bài tự luận về thuật toán)
            if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
            {
                // Submit từng bài practice test lên hệ thống chấm code
                
                foreach (var practiceAnswer in request.PracticeTestAnswers)
                {
                    // Tạo request submit bài practice test
                    var submitRequest = new PracticeTestSubmitInsertRequest
                    {
                        ProblemId = practiceAnswer.ProblemId, // ID bài toán
                        SourceCode = practiceAnswer.CodeSubmission, // Code sinh viên viết
                        LanguageId = practiceAnswer.LanguageId // Ngôn ngữ lập trình (C++, Java, Python...)
                    };
                    
                    // Gọi service chấm bài (không dùng transaction riêng vì đang trong transaction lớn)
                    var submitResponse = await _practiceTestService.InsertPracticeTestSubmitWithoutTransactionAsync(submitRequest, cancellationToken);
                    
                    // Nếu submit thất bại (lỗi hệ thống chấm), rollback toàn bộ
                    if (!submitResponse.Success)
                    {
                        response.SetMessage(MessageId.E00000, $"Không thể submit bài practice test: {submitResponse.Message}");
                        return false;
                    }
                    
                    // Lấy độ khó của bài toán để map kết quả (Easy/Medium/Hard)
                    var problem = await _problemRepository.FirstOrDefaultAsync(p => p.ProblemId == practiceAnswer.ProblemId, cancellationToken: cancellationToken);
                    if (problem != null)
                    {
                        // Lưu kết quả theo độ khó - để sau này tính level và ability marks
                        practiceTestResults[problem.Difficulty] = submitResponse;
                    }
                }
                
                // Tính level từ kết quả practice test (dựa vào số test case pass)
                var practiceTestLevel = DeterminePracticeTestLevel(practiceTestResults);
                
                // Kết hợp level: 60% từ quiz + 40% từ practice test
                // Quiz chiếm tỷ trọng cao hơn vì đánh giá kiến thức lý thuyết rộng
                baseLevel = (int)Math.Round(quizLevel * 0.6 + practiceTestLevel * 0.4);
                
                // Đảm bảo level nằm trong khoảng 1-3
                baseLevel = Math.Max(1, Math.Min(3, baseLevel));
            }
            
            // Kiểm tra xem bảng điểm có môn nào liên quan đến các quiz đã làm không
            // Nếu có thì kết hợp điểm bảng điểm (20%) vào level cuối cùng
            int finalStudentLevel = baseLevel;
            var transcriptSubjectsUsed = new List<(string SubjectCode, string SubjectName, double Grade)>();
            
            // Nếu sinh viên có bảng điểm
            if (studentTranscripts.Any())
            {
                // Lấy tên các môn từ quiz settings (ví dụ: "Toán rời rạc (DSAP201)")
                var quizSubjectNames = testExist.Quizzes
                    .Where(q => q.PlacementTestQuizSetting != null)
                    .Select(q => q.PlacementTestQuizSetting!.SubjectCodeName)
                    .ToList();
                
                // Tìm các môn trong bảng điểm khớp với môn trong quiz
                // Kiểm tra xem SubjectCode có nằm trong SubjectCodeName không (case-insensitive)
                var matchingTranscripts = studentTranscripts
                    .Where(t => quizSubjectNames.Any(qsn => qsn.Contains(t.SubjectCode, StringComparison.OrdinalIgnoreCase)))
                    .ToList();
                
                // Nếu có môn khớp, tính level kết hợp
                if (matchingTranscripts.Any())
                {
                    // Tính điểm trung bình các môn khớp (thang điểm 0-10)
                    var avgGrade = matchingTranscripts.Average(t => t.Grade ?? 0);
                    
                    // Chuyển đổi điểm sang level (1-3)
                    // 0-5: Beginner, 5-7.5: Intermediate, 7.5-10: Advanced
                    int transcriptLevel = avgGrade switch
                    {
                        < 5 => 1,
                        < 7.5 => 2,
                        _ => 3
                    };
                    
                    // Lưu danh sách môn đã dùng để tạo LevelReason (giải thích tại sao được level này)
                    transcriptSubjectsUsed = matchingTranscripts
                        .Where(t => t.Grade.HasValue)
                        .Select(t => (t.SubjectCode, t.SubjectName, t.Grade!.Value))
                        .ToList();
                    
                    // Kết hợp: 80% từ quiz/practice test + 20% từ bảng điểm
                    // Test chiếm tỷ trọng cao hơn vì đánh giá thời điểm hiện tại
                    finalStudentLevel = (int)Math.Round(baseLevel * 0.8 + transcriptLevel * 0.2);
                    
                    // Đảm bảo level nằm trong khoảng 1-3
                    finalStudentLevel = Math.Max(1, Math.Min(3, finalStudentLevel));
                }
            }
            
            // Gán level cuối cùng
            var studentLevel = finalStudentLevel;
            
            // Tạo chuỗi giải thích chi tiết tại sao sinh viên được level này
            // Bao gồm: điểm quiz, điểm practice test, điểm bảng điểm, và cách tính
            var levelReason = BuildLevelReason(
                quizLevel, 
                finalStudentLevel, 
                practiceTestResults, 
                difficultyPerformance,
                transcriptSubjectsUsed);
            
            // Khởi tạo biến subjectMarks là null - chỉ được gán khi sinh viên chọn OtherQuestionAnswerCodes
            // SubjectMarks chứa điểm các môn học từ bảng điểm, dùng để AI đánh giá và đề xuất khóa học
            List<SubjectMarkContext>? subjectMarks = null;
            
            // Lấy thông tin sinh viên từ StudentService thông qua message bus
            // Cần để biết: học kỳ hiện tại, chuyên ngành, công nghệ quan tâm
            var studentInformationSelectsEvent = new StudentInformationSelectsEvent
            {
                StudentId = currentUser.UserId
            };
            var informationResponse = await _requestStudentInformationSelectsClient.GetResponse<StudentInformationSelectsEventResponse>(studentInformationSelectsEvent, cancellationToken);
            
            // Tạo event để lấy thông tin chuyên ngành và học kỳ từ CourseService
            var majorAndSemesterEvent = new CourseMajorSemesterSelectEvent
            {
                SemesterId = informationResponse.Message.Response.SemesterId, // Học kỳ hiện tại
                MajorId = informationResponse.Message.Response.MajorId // Chuyên ngành của sinh viên
            };
            
            // Gọi CourseService để lấy thông tin chi tiết về chuyên ngành và học kỳ
            var majorAndSemesterEventResponse = await _requestCourseMajorSemesterClient.GetResponse<CourseMajorSemesterSelectEventResponse>(majorAndSemesterEvent, cancellationToken);
            if (!majorAndSemesterEventResponse.Message.Success)
            {
                response.MessageId = majorAndSemesterEventResponse.Message.MessageId;
                response.Message = majorAndSemesterEventResponse.Message.Message;
                return false;
            }
            
            // Kiểm tra logic nghiệp vụ: Sinh viên từ kỳ 5 trở lên PHẢI có bảng điểm
            // Vì từ kỳ 5, lộ trình học tập cần dựa vào điểm các môn nền tảng đã học
            if (!studentTranscripts.Any() && majorAndSemesterEventResponse.Message.Response.SemesterNumber > 4)
            {
                response.SetMessage(MessageId.E00000, "Sinh viên chưa có bảng điểm, không thể tạo lộ trình học tập cho sinh viên từ kỳ 5 trở lên");
                return false;
            }
            
            // Khởi tạo list chứa các môn học cần cải thiện (improvement courses)
            List<CourseImproveContext> courseImporve = new();
            
            // Nếu sinh viên có trả lời câu hỏi bổ sung (OtherQuestionAnswerCodes)
            // Ví dụ: "Tôi muốn cải thiện các môn có điểm 5-7", "Tôi muốn đánh giá lại môn có điểm 7-8"
            if (request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any())
            {
                // Map toàn bộ bảng điểm sang SubjectMarks để AI phân tích
                subjectMarks = studentTranscripts.Select(st => new SubjectMarkContext
                {
                    SubjectCode = st.SubjectCode,
                    SubjectName = st.SubjectName,
                    Mark = st.Grade
                }).ToList();
                
                // Lấy danh sách tất cả các mã môn học hợp lệ từ CourseService
                // Để kiểm tra các môn trong bảng điểm có tồn tại trong hệ thống khóa học không
                var subjectCodeEventResponse = await _subjectCodeSelectEventRequestClient.GetResponse<SubjectCodeSelectEventResponse>(new SubjectCodeSelectEvent(), cancellationToken);
                if (!subjectCodeEventResponse.Message.Success)
                {
                    response.SetMessage(MessageId.I00000, subjectCodeEventResponse.Message.Message);
                    return false;
                }
    
                var subjectCodes = subjectCodeEventResponse.Message.Response;
                
                // HashSet chứa các môn cần đánh giá lại (có điểm trung bình nhưng sinh viên muốn nâng cao)
                HashSet<string> subjectCodesForEvaluation = new();
                
                // Xử lý từng loại OtherQuestionAnswerCode
                foreach (var questionCode in request.OtherQuestionAnswerCodes)
                {
                    switch (questionCode)
                    {
                        case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_COURSE:
                            // Các môn có điểm 5-7 (yếu-trung bình) -> Cần học lại ở mức Beginner
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 5 && t.Grade < 7) // Lọc môn điểm 5-7
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Beginner, // Khóa học cơ bản
                                        SubjectPrerequisiteCode = t.Prerequisite // Môn tiên quyết (nếu có)
                                    })
                                    // Chỉ thêm những môn có trong hệ thống khóa học
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_COURSE:
                            // Các môn có điểm 7-8 (khá) -> Có thể học nâng cao ở mức Intermediate
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 7 && t.Grade < 8)
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Intermidiate, // Khóa học trung cấp
                                        SubjectPrerequisiteCode = t.Prerequisite
                                    })
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_8_TO_9_COURSE:
                            // Các môn có điểm 8-9 (giỏi) -> Muốn học chuyên sâu ở mức Advanced
                            courseImporve.AddRange(
                                studentTranscripts
                                    .Where(t => t.Grade >= 8 && t.Grade < 9)
                                    .Select(t => new CourseImproveContext
                                    {
                                        SubjectCode = t.SubjectCode,
                                        Level = (short)ConstantEnum.CourseLevel.Advanced, // Khóa học nâng cao
                                        SubjectPrerequisiteCode = t.Prerequisite
                                    })
                                    .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code.SubjectCode))
                            );
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_5_TO_7_EVALUATION:
                            // Sinh viên muốn AI đánh giá lại các môn điểm 5-7 (có thể do học lâu, quên kiến thức)
                            var subjects5To7 = studentTranscripts
                                .Where(t => t.Grade >= 5 && t.Grade < 7)
                                .Select(t => t.SubjectCode)
                                .Where(code => subjectCodes.Any(sc => sc.SubjectCode == code));
                            foreach (var subjectCode in subjects5To7)
                            {
                                // Thêm vào danh sách cần đánh giá - AI sẽ tạo quiz/bài tập để kiểm tra lại
                                subjectCodesForEvaluation.Add(subjectCode);
                            }
                            break;

                        case ConstantEnum.OtherQuestionCode.GRADE_7_TO_8_EVALUATION:
                            // Sinh viên muốn AI đánh giá lại các môn điểm 7-8
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
            
            // Tính toán điểm năng lực (AbilityMarks) cho từng môn học dựa trên kết quả quiz
            // AbilityMarks dùng để đánh giá chi tiết năng lực của sinh viên ở từng lĩnh vực cụ thể
            var abilityMarks = new List<AbilityMarkContext>();
            
            // Lấy danh sách các QuizId mà sinh viên đã trả lời
            // Cần để biết sinh viên có làm quiz nào, bỏ quiz nào (vì không bắt buộc làm hết)
            var answeredQuizIds = studentTestCollection.StudentQuizzes
                .Select(sq => sq.QuizId)
                .ToHashSet();
            
            // Duyệt qua TẤT CẢ các quiz trong test (kể cả quiz sinh viên không làm)
            // Vì cần tính điểm cho tất cả các năng lực, quiz không làm sẽ được tính điểm 0 hoặc dựa vào bảng điểm
            foreach (var quiz in testExist.Quizzes)
            {
                // Bỏ qua quiz không có PlacementTestQuizSetting (không phải quiz đánh giá năng lực)
                if (quiz?.PlacementTestQuizSetting == null)
                    continue;
                
                // Lấy tên môn học từ quiz setting (ví dụ: "Toán rời rạc (DSAP201)")
                var subjectCodeName = quiz.PlacementTestQuizSetting.SubjectCodeName;
                
                // Kiểm tra sinh viên có trả lời quiz này không
                var hasAnsweredQuiz = answeredQuizIds.Contains(quiz.QuizId);
                
                // Tìm môn học tương ứng trong bảng điểm
                // Kiểm tra xem SubjectCode có nằm trong SubjectCodeName không (case-insensitive)
                // Ví dụ: "DSAP201" có trong "Toán rời rạc (DSAP201)"
                var matchingTranscript = studentTranscripts?
                    .FirstOrDefault(t => !string.IsNullOrEmpty(t.SubjectCode) && 
                                         !string.IsNullOrEmpty(subjectCodeName) &&
                                         subjectCodeName.Contains(t.SubjectCode, StringComparison.OrdinalIgnoreCase));
                
                // Khởi tạo điểm quiz = 0
                double quizScore = 0;
                
                // Nếu sinh viên có làm quiz này
                if (hasAnsweredQuiz)
                {
                    // Tính điểm quiz dựa trên số câu trả lời đúng
                    
                    // Lấy tất cả QuestionId trong quiz này
                    var quizQuestionIds = quiz.Questions.Select(q => q.QuestionId).ToHashSet();
                    
                    // Lọc ra các câu trả lời của sinh viên cho quiz này
                    var quizAnswers = studentTestCollection.StudentAnswers
                        .Where(sa => quizQuestionIds.Contains(sa.QuestionId))
                        .ToList();
                    
                    var correctCount = 0; // Số câu trả lời đúng
                    var totalQuestions = quiz.Questions.Count; // Tổng số câu hỏi
                    
                    // Duyệt qua từng câu hỏi để chấm điểm
                    foreach (var question in quiz.Questions)
                    {
                        // Lấy danh sách AnswerId mà sinh viên đã chọn cho câu hỏi này
                        var studentSelectedAnswerIds = quizAnswers
                            .Where(sa => sa.QuestionId == question.QuestionId && sa.AnswerId.HasValue)
                            .Select(sa => sa.AnswerId!.Value)
                            .ToHashSet();
                        
                        // Nếu sinh viên không chọn đáp án nào, bỏ qua (tính là sai)
                        if (!studentSelectedAnswerIds.Any()) continue;
                        
                        // Lấy danh sách AnswerId chính xác của câu hỏi
                        var correctAnswerIds = question.Answers
                            .Where(a => a.IsCorrect)
                            .Select(a => a.AnswerId)
                            .ToHashSet();
                        
                        bool isCorrect;
                        
                        // Kiểm tra câu trả lời đúng hay sai dựa vào loại câu hỏi
                        if (question.QuestionType == (short)ConstantEnum.QuestionType.MultipleChoice)
                        {
                            // Câu hỏi nhiều đáp án đúng: phải chọn CHÍNH XÁC tất cả đáp án đúng
                            // Số lượng phải bằng nhau VÀ tất cả đáp án chọn đều phải đúng
                            isCorrect = studentSelectedAnswerIds.Count == correctAnswerIds.Count 
                                        && studentSelectedAnswerIds.All(id => correctAnswerIds.Contains(id));
                        }
                        else
                        {
                            // Câu hỏi một đáp án đúng: chọn đúng 1 trong các đáp án đúng là được
                            isCorrect = studentSelectedAnswerIds.Any(id => correctAnswerIds.Contains(id));
                        }
                        
                        // Nếu trả lời đúng, tăng số câu đúng
                        if (isCorrect)
                        {
                            correctCount++;
                        }
                    }
                    
                    // Tính điểm quiz theo phần trăm (0-100)
                    quizScore = totalQuestions > 0 ? (double)correctCount / totalQuestions * 100 : 0;
                }
                
                // Tính điểm năng lực cuối cùng (finalScore) bằng cách kết hợp quiz score và transcript score
                double finalScore;
                
                if (matchingTranscript != null && matchingTranscript.Grade.HasValue)
                {
                    // Trường hợp 1: Có cả bảng điểm và quiz
                    // Kết hợp: 60% từ bảng điểm (kiến thức lâu dài) + 40% từ quiz (đánh giá hiện tại)
                    var transcriptScore = matchingTranscript.Grade.Value * 10; // Chuyển từ thang 0-10 sang 0-100
                    finalScore = (transcriptScore * 0.6) + (quizScore * 0.4);
                }
                else if (hasAnsweredQuiz)
                {
                    // Trường hợp 2: Không có bảng điểm nhưng có làm quiz
                    // Lấy 100% từ quiz (đây là đánh giá duy nhất)
                    finalScore = quizScore;
                }
                else
                {
                    // Trường hợp 3: Không có bảng điểm và không làm quiz
                    // Không có dữ liệu để đánh giá -> điểm 0
                    finalScore = 0;
                }
                
                // Thêm vào danh sách ability marks
                abilityMarks.Add(new AbilityMarkContext
                {
                    Name = subjectCodeName, // Tên môn học
                    Mark = finalScore // Điểm năng lực (0-100)
                });
            }
            
            // Nếu sinh viên có làm bài practice test (bài tự luận code), thêm điểm practice test vào ability marks
            if (request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any())
            {
                // Tính điểm practice test dựa trên kết quả submit thực tế (số test case pass)
                // List chứa: (Tên bài, Độ khó, Điểm số)
                var practiceTestScoresWithInfo = new List<(string Title, string Difficulty, double Score)>();
                
                // Duyệt qua từng bài practice test mà sinh viên đã submit
                foreach (var practiceAnswer in request.PracticeTestAnswers)
                {
                    // Lấy thông tin bài toán từ database
                    var problem = await _problemRepository.FirstOrDefaultAsync(p => p.ProblemId == practiceAnswer.ProblemId, cancellationToken: cancellationToken);
                    if (problem != null)
                    {
                        double score = 0.0;
                        
                        // Tìm kết quả submit tương ứng theo độ khó (Easy/Medium/Hard)
                        if (practiceTestResults.TryGetValue(problem.Difficulty, out var submitResult))
                        {
                            // Tính điểm: (số test case pass / tổng số test case) * 100
                            // Ví dụ: pass 8/10 test cases -> điểm = 80
                            score = submitResult.Response.TotalTests > 0
                                ? (double)submitResult.Response.PassedTests / submitResult.Response.TotalTests * 100
                                : 0.0;
                        }
                        // Nếu không tìm thấy kết quả submit (không bao giờ xảy ra vì đã submit ở trên), điểm = 0
                        
                        // Lưu thông tin bài toán và điểm
                        practiceTestScoresWithInfo.Add((problem.Title, problem.Difficulty, score));
                    }
                }
                
                // Thêm điểm practice test vào ability marks với tên mô tả chi tiết
                foreach (var (title, difficulty, score) in practiceTestScoresWithInfo)
                {
                    abilityMarks.Add(new AbilityMarkContext
                    {
                        Name = $"Bài test tự luận cấu trúc dữ liệu và giải thuật ({title}) với độ khó: {difficulty}",
                        Mark = score // Điểm từ 0-100 dựa trên % test case pass
                    });
                }
            }
            
            // Lấy tất cả bài khảo sát (survey) của sinh viên từ cache hoặc database
            // Cache 10 phút để tối ưu hiệu suất - survey ít thay đổi
            var allStudentSurveys = await _studentQuizCollectionRepository.GetOrSetListAsync(
                CacheKey.StudentSurvey(currentUser.UserId), // Cache key
                async () => await _studentQuizCollectionRepository.ToListAsync(sq => sq.StudentId == currentUser.UserId && sq.QuizType == (short) ConstantEnum.TestType.Survey), // Hàm lấy data nếu cache miss
                TimeSpan.FromMinutes(10)); // Thời gian cache
            
            // Lấy bài khảo sát HABIT mới nhất
            // HABIT survey: khảo sát về thói quen học tập (có bao nhiêu giờ/tuần để học)
            var latestHabitSurvey = allStudentSurveys
                .Where(x => x.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.HABIT))
                .OrderByDescending(x => x.CreatedAt) // Lấy bài mới nhất
                .FirstOrDefault();
            
            // Lấy bài khảo sát INTEREST mới nhất
            // INTEREST survey: khảo sát về sở thích học tập (thích học về AI, Web, Mobile...)
            var latestInterestSurvey = allStudentSurveys
                .Where(x => x.Quiz?.SurveyQuizSetting?.SurveyCode == nameof(ConstantEnum.SurveyCode.INTEREST))
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefault();
            
            // Xây dựng danh sách các survey mới nhất (HABIT và INTEREST)
            var studentSurveys = new List<StudentQuizCollection>();
            if (latestHabitSurvey != null) studentSurveys.Add(latestHabitSurvey);
            if (latestInterestSurvey != null) studentSurveys.Add(latestInterestSurvey);
            
            // Kiểm tra sinh viên có hoàn thành survey nào không
            // Nếu chưa làm survey -> không thể tạo lộ trình học tập
            if (!studentSurveys.Any())
            {
                response.SetMessage(MessageId.E00000, "Sinh viên chưa hoàn thành bài khảo sát nào");
                return false;
            }
            
            // Ưu tiên lấy HABIT survey, nếu không có thì lấy survey đầu tiên
            // Cần HABIT survey để tính limitTime (số giờ học/tuần)
            var surveyHabit = latestHabitSurvey ?? studentSurveys.First();

            // Lấy tất cả AnswerId từ các câu trả lời trong HABIT survey
            var selectedAnswerIds = surveyHabit.Quiz.Questions
                .SelectMany(q => q.Answers) // Flatten tất cả answers từ tất cả questions
                .Select(a => a.AnswerId)
                .ToList();
       
            // Map sang StudentQuizAnswerCollection để tính limitTime
            var studentQuizAnswers = surveyHabit.Quiz.Questions
                .SelectMany(q => q.Answers)
                .Where(a => selectedAnswerIds.Contains(a.AnswerId)) // Chỉ lấy các answer sinh viên đã chọn
                .Select(a => new StudentQuizAnswerCollection
                {
                    AnswerId = a.AnswerId,
                    Answer = a,
                })
                .ToList();

            // Tính số giờ học mỗi tuần dựa trên câu trả lời trong HABIT survey
            // Ví dụ: "Tôi có 10 giờ/tuần", "Tôi có 20 giờ/tuần"
            int limitTime = GetStudentStudyTime(studentQuizAnswers);
            
            // Tạo ID mới cho lộ trình học tập
            var learningPathId = Guid.NewGuid();

            // Chuyển đổi OtherQuestionAnswerCodes thành chuỗi để lưu vào database
            // Ví dụ: [GRADE_5_TO_7_COURSE, GRADE_7_TO_8_EVALUATION] -> "1,4"
            var evaluationAndImprove = request.OtherQuestionAnswerCodes != null && request.OtherQuestionAnswerCodes.Any()
                ? string.Join(",", request.OtherQuestionAnswerCodes.Select(c => ((int)c).ToString()))
                : null;
            
            // Lấy tên mục tiêu học tập từ request
            string learningGoalName = request.LearningGoal.LearningGoalName;
            
            // Nếu sinh viên chọn "None" (không có mục tiêu cụ thể)
            // Cần dùng AI để phân tích INTEREST survey và đề xuất mục tiêu phù hợp
            if (request.LearningGoal.LearningGoalType == (short) ConstantEnum.LearningGoalType.None)
            {
                // Kiểm tra xem có INTEREST survey không
                if (latestInterestSurvey == null)
                {
                    response.SetMessage(MessageId.E00000, "Không tìm thấy bài khảo sát sở thích học tập");
                    return false;
                }

                // Lấy các AnswerId mà sinh viên đã chọn trong INTEREST survey
                var selectedAnswerIdInterests = latestInterestSurvey!.StudentQuizAnswers
                    .Where(a => a.AnswerId != Guid.Empty) // Lọc các answer hợp lệ
                    .Select(a => a.AnswerId)
                    .ToHashSet();

                // Map sang format phù hợp để gửi cho AI
                // Mỗi question kèm theo các answer sinh viên đã chọn
                var interestQuestions = latestInterestSurvey.Quiz.Questions.Select(question => new StudentInterestQuestion
                {
                    QuestionText = question.QuestionText, // Nội dung câu hỏi
                    StudentAnswers = question.Answers
                        .Where(a => selectedAnswerIdInterests.Contains(a.AnswerId)) // Chỉ lấy answer sinh viên chọn
                        .Select(a => a.AnswerText) // Text của answer
                        .ToList()
                }).Where(q => q.StudentAnswers.Any()).ToList(); // Chỉ lấy question có answer

                // Tạo event gửi đến AI Service để phân tích sở thích
                var studentInterestAnalysisEvent = new StudentInterestSurveyAnalysisEvent
                {
                    StudentId = currentUser.UserId,
                    Questions = interestQuestions
                };

                // Gọi AI Service thông qua message bus
                var aiAnalysisResponse = await _requestStudentInterestAnalysisClient.GetResponse<StudentInterestSurveyAnalysisEventResponse>(
                    studentInterestAnalysisEvent, 
                    cancellationToken);
                
                // Nếu AI phân tích thất bại
                if (!aiAnalysisResponse.Message.Success)
                {
                    response.SetMessage(MessageId.E99999);
                    return false;
                }

                // Lấy mục tiêu học tập do AI đề xuất
                // Ví dụ: "Backend Developer", "AI Engineer", "Mobile Developer"
                learningGoalName = aiAnalysisResponse.Message.Response.LearningGoal;
            }
            
            // Tạo event để insert learning path vào database (write model)
            var learningPathEvent = new InsertLearningPathEvent
            {
                LearningPathId = learningPathId,
                StudentId = currentUser.UserId,
                CurrentUserEmail = currentUser.Email,
                PathName = $"Lộ trình {learningGoalName}", // Tên lộ trình: "Lộ trình Backend Developer"
                Level = (short) studentLevel, // Level của sinh viên (1-3)
                LevelReason = levelReason, // Lý do được level này (chuỗi text chi tiết)
                IsSkipTest = false, // Sinh viên đã làm test (không skip)
                LimitTime = limitTime, // Số giờ học/tuần
                EvaluationAndImprove = evaluationAndImprove, // Mã các môn cần đánh giá/cải thiện
                StudentTestId = studentTest.StudentTestId, // ID bài test vừa submit
                // Danh sách PracticeSubmissionIds nếu có làm practice test
                PracticeSubmissionIds = request.PracticeTestAnswers != null && request.PracticeTestAnswers.Any()
                    ? practiceTestResults.Values.Select(ptr => ptr.Response.SubmissionId).ToList()
                    : null,
                StudentSurveyIds = studentSurveys.Select(ss => ss.StudentQuizId).ToList(), // Danh sách survey IDs
            };
            
            // Gửi event tới CourseService để tạo learning path
            var learningPathResponse = await _requestInsertLearningPathEventClient.GetResponse<InsertLearningPathEventResponse>(learningPathEvent, cancellationToken);
            if (!learningPathResponse.Message.Success)
            {
                response.MessageId = learningPathResponse.Message.MessageId;
                response.Message = learningPathResponse.Message.Message;
                return false;
            }
            
            // Tạo danh sách các năng lực cần cải thiện từ abilityMarks
            // Các năng lực có điểm < 70 được coi là cần cải thiện
            var abilityImprove = abilityMarks
                .Where(am => am.Mark < 70) // Ngưỡng 70 điểm
                .Select(am => new AbilityImprove
                {
                    Name = am.Name, // Tên năng lực
                    Mark = am.Mark // Điểm hiện tại
                })
                .ToList();
            
            // Tạo context chứa tất cả thông tin cần thiết để tạo lộ trình học tập chi tiết
            // Context này sẽ được gửi đến LearningPathService để AI phân tích và tạo các khóa học cụ thể
            var context = new LearningPathCreationContext
            {
                // Danh sách các bài khảo sát mới nhất (HABIT, INTEREST) mà sinh viên đã hoàn thành
                StudentQuizCollections = studentSurveys,
                // Thông tin người dùng hiện tại (sinh viên)
                CurrentUser = currentUser,
                // Thông tin về mục tiêu học tập, công nghệ quan tâm, học kỳ và chuyên ngành của sinh viên
                InformationResponse = new StudentInformationSelectsEventResponseEntity
                {
                    LearningGoalName = request.LearningGoal.LearningGoalName,
                    LearningGoalType = (short) request.LearningGoal.LearningGoalType,
                    Technologies = informationResponse.Message.Response.Technologies,
                    SemesterId = informationResponse.Message.Response.SemesterId,
                    MajorId = informationResponse.Message.Response.MajorId
                },
                // ID của lộ trình học tập mới được tạo
                LearningPathId = learningPathId,
                // Số giờ học mỗi tuần mà sinh viên có thể dành ra (dựa trên bài khảo sát HABIT)
                LimitTime = limitTime,
                // Trình độ của sinh viên (1-3) - kết hợp từ điểm bài test (80%) và bảng điểm (20%)
                StudentLevel = (short)studentLevel,
                // Thông tin chuyên ngành của sinh viên
                StudentMajor = new StudentMajor
                {
                    MajorCode = majorAndSemesterEventResponse.Message.Response.MajorCode,
                    MajorName = majorAndSemesterEventResponse.Message.Response.MajorName
                },
                // Điểm số các môn học từ bảng điểm (chỉ có khi sinh viên chọn OtherQuestionAnswerCodes)
                SubjectMarks = subjectMarks,
                // Điểm năng lực từ các câu hỏi trong bài test: kết hợp điểm test (100%) hoặc bảng điểm (60%) + test (40%)
                AbilityMarks = abilityMarks,
                // Danh sách các môn học cần cải thiện (dựa trên OtherQuestionAnswerCodes: môn có điểm 5-7, 7-8, 8-9)
                CourseImprove = courseImporve,
                // Danh sách các năng lực cần cải thiện (các năng lực có điểm < 70 từ AbilityMarks)
                AbilityImprove = abilityImprove
            };

            // Nếu sinh viên có bảng điểm, thêm vào context
            if (studentTranscripts != null && studentTranscripts.Any())
            {
                // Bảng điểm đầy đủ của sinh viên (tất cả các môn đã học) - nếu có
                context.StudentTranscripts = studentTranscripts.Select(x => new StudentTranscriptContext
                {
                    SubjectCode = x.SubjectCode,
                    Status = x.Status,
                    Mark = x.Grade
                }).ToList();
            }
            
            // Gọi LearningPathService để tạo lộ trình học tập chi tiết
            // Service này sẽ phân tích context và tạo ra các khóa học cụ thể phù hợp với sinh viên
            var result = await _learningPathService.CreateLearningPathAsync(context, cancellationToken);
            if (!result.Success)
            {
                response.MessageId = result.MessageId;
                response.Message = result.Message;
                return false;
            }
            
            // Đến đây là thành công - set Success = true
            response.Success = true;
            response.Response = learningPathId; // Trả về ID của lộ trình vừa tạo
            
            // Map thông tin StudentTestSubmit để trả về cho client
            // Client cần biết ID của test và các practice test submission để tracking
            response.StudentTestSubmit = new StudentTestSubmitResponse
            {
                StudentTestId = studentTest.StudentTestId, // ID bài test vừa submit
                // Danh sách ID các practice test submission (nếu có)
                PracticeTestSubmits = practiceTestResults.Values
                    .Select(ptr => new StudentPracticeTestSubmitResponse
                    {
                        PracticeTestSubmitId = ptr.Response.SubmissionId
                    })
                    .ToList()
            };
            
            // Set message thành công
            response.SetMessage(MessageId.I00001, "Thêm bài kiểm tra của học sinh");
            
            // Return true để commit transaction
            // Nếu return false, toàn bộ transaction sẽ rollback
            return true;
        }, cancellationToken); // Kết thúc BeginTransactionAsync
        
        // Trả về response cho client
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
            
            // Calculate total correct answers with proper logic for MultipleChoice
            var totalCorrectAnswers = 0;
            
            foreach (var question in quiz.Questions)
            {
                // Get student's selected answers for this question
                var studentSelectedAnswerIds = studentTest.StudentAnswers
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
                    totalCorrectAnswers++;
                }
            }
            
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

                bool isCorrect;
                
                // For MultipleChoice: student must select ALL correct answers and NO incorrect answers
                if (question.QuestionType == (short)ConstantEnum.QuestionType.MultipleChoice)
                {
                    isCorrect = studentAnswersForQuestion.Count == correctAnswerIds.Count 
                        && studentAnswersForQuestion.All(id => correctAnswerIds.Contains(id ?? Guid.Empty));
                }
                else
                {
                    // For SingleChoice/TrueFalse: just check if selected answer is correct
                    isCorrect = studentAnswersForQuestion.Any(id => correctAnswerIds.Contains(id ?? Guid.Empty));
                }
                
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
        reason.AppendLine(" **Kết quả bài kiểm tra lý thuyết:**");
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
