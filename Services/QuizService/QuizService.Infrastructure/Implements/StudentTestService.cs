using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.StudentTests.Commands;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class StudentTestService : IStudentTestService
{
    private readonly ICommandRepository<StudentTest> _studentTestRepository;
    private readonly IQueryRepository<StudentTestCollection> _studentTestQueryRepository;
    private readonly IQueryRepository<TestCollection> _testQueryRepository;
    private readonly IQueryRepository<QuestionCollection> _questionQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="studentTestRepository"></param>
    /// <param name="studentTestQueryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="testQueryRepository"></param>
    /// <param name="questionQueryRepository"></param>
    public StudentTestService(ICommandRepository<StudentTest> studentTestRepository, IQueryRepository<StudentTestCollection> studentTestQueryRepository,
        IIdentityService identityService, IUnitOfWork unitOfWork, IQueryRepository<TestCollection> testQueryRepository, 
        IQueryRepository<QuestionCollection> questionQueryRepository)
    {
        _studentTestRepository = studentTestRepository;
        _studentTestQueryRepository = studentTestQueryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _testQueryRepository = testQueryRepository;
        _questionQueryRepository = questionQueryRepository;
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
        
        // Validate questionIds in answers
        var questionIds = request.Answers.Select(a => a.QuestionId).ToList();
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
                    CreatedAt = currentTime,
                    CreatedBy = currentUser.Email,
                    UpdatedAt = currentTime,
                    UpdatedBy = currentUser.Email,
                    IsActive = true
                }).ToList()
            };
            
            // Save to database
            await _studentTestRepository.AddAsync(studentTest, currentUser.Email);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                }).ToList()
            };
            
            _unitOfWork.Store(studentTestCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // True
            response.Success = true;
            response.Response = studentTest.StudentTestId;
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
        
        string cacheKey = "studentTest:id";
        
        // Get majors from cache or database
        var studentTest = await _studentTestQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _studentTestQueryRepository.FirstOrDefaultAsync(x => x.StudentTestId == request.StudentTestId && x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (studentTest == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra của học sinh");
            return response;
        }
        
        var responseEntity = new StudentTestSelectResponseEntity
        {
            StudentTestId = studentTest.StudentTestId,
            TestId = studentTest.TestId,
            StartedAt = studentTest.StartedAt,
            FinishedAt = studentTest.FinishedAt,
            Answers = studentTest.StudentAnswers.Select(st => new StudentAnswerSelectResponseEntity
            {
                QuestionId = st.QuestionId,
                AnswerId = st.AnswerId,
                IsCorrect = st.Answer!.IsCorrect,
                Explanation = st.Question!.Explanation
            }).ToList()
        };
        
        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài kiểm tra của học sinh");
        return response;
    }
}