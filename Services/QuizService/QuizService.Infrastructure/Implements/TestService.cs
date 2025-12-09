using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class TestService : ITestService
{
    private readonly ICommandRepository<Test> _commandRepository;
    private readonly ICommandRepository<Quiz> _commandQuizRepository;
    private readonly ICommandRepository<Question> _commandQuestionRepository;
    private readonly IQueryRepository<TestCollection> _queryRepository;
    private readonly IQueryRepository<QuestionCollection> _questionQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRequestClient<SubjectSelectsEvent> _requestSubjectSelectClient;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="queryRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="requestSubjectSelectClient"></param>
    /// <param name="commandQuizRepository"></param>
    /// <param name="commandQuestionRepository"></param>
    /// <param name="questionQueryRepository"></param>
    public TestService(ICommandRepository<Test> commandRepository,
        IQueryRepository<TestCollection> queryRepository,
        IIdentityService identityService, IUnitOfWork unitOfWork,
        IRequestClient<SubjectSelectsEvent> requestSubjectSelectClient,
        ICommandRepository<Quiz> commandQuizRepository,
        ICommandRepository<Question> commandQuestionRepository, 
        IQueryRepository<QuestionCollection> questionQueryRepository)
    {
        _commandRepository = commandRepository;
        _queryRepository = queryRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _requestSubjectSelectClient = requestSubjectSelectClient;
        _commandQuizRepository = commandQuizRepository;
        _commandQuestionRepository = commandQuestionRepository;
        _questionQueryRepository = questionQueryRepository;
    }

    /// <summary>
    /// Insert test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestInsertResponse> InsertTestAsync(TestInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new TestInsertResponse { Success = false };

        var currentEmail = _identityService.GetCurrentUser()!.Email;

        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new test
            var newTest = new Test
            {
                TestId = Guid.NewGuid(),
                TestName = request.TestName,
                Description = request.Description,
            };

            await _commandRepository.AddAsync(newTest, currentEmail);
            
            var subjectIds = request.Quizzes.Select(q => q.SubjectCode).Distinct().ToList();
            
            var subjectSelectEvent = new SubjectSelectsEvent
            {
                SubjectIds = subjectIds,
            };

            var messageResponse = await _requestSubjectSelectClient.GetResponse<SubjectSelectsEventResponse>(subjectSelectEvent, cancellationToken);
            if (!messageResponse.Message.Success)
            {
                response.MessageId = messageResponse.Message.MessageId;
                response.Message = messageResponse.Message.Message;
                return false;
            }

            // Create subject mapping dictionary
            var subjectMapping = messageResponse.Message.Response
                .ToDictionary(s => s.SubjectId, s => s.SubjectNameCode);

            foreach (var quiz in request.Quizzes)
            {
                var newQuiz = new Quiz
                {
                    QuizId = Guid.NewGuid(),
                    TestId = newTest.TestId,
                    QuizType = (short) ConstantEnum.TestType.Quiz,
                    PlacementTestQuizSetting = new PlacementTestQuizSetting
                    {
                        SubjectCode = quiz.SubjectCode,
                        Title = quiz.Title,
                        Description = quiz.Description,
                    },
                    Questions = quiz.Questions.Select(q => new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = q.QuestionText,
                        QuestionType = q.QuestionType,
                        DifficultyLevel = q.DifficultyLevel,
                        Answers = q.Answers.Select(a => new Answer
                        {
                            AnswerId = Guid.NewGuid(),
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                        }).ToList()
                    }).ToList()
                };
                newTest.Quizzes.Add(newQuiz);
            }

            await _commandRepository.AddAsync(newTest);
            await _unitOfWork.SaveChangesAsync(currentEmail, cancellationToken);
            
            // Store to read database
            _unitOfWork.Store(TestCollection.FromWriteModel(newTest, subjectMapping));
            
            foreach (var quiz in newTest.Quizzes)
            {
                // Get subject name from mapping, fallback to empty string if not found
                var subjectName = subjectMapping.TryGetValue(quiz.PlacementTestQuizSetting!.SubjectCode, 
                    out var name) ? name : string.Empty;

                _unitOfWork.Store(QuizCollection.FromWriteModel(quiz, subjectName));
                foreach (var question in quiz.Questions)
                {
                    _unitOfWork.Store(QuestionCollection.FromWriteModel(question));
                    foreach (var answer in question.Answers)
                    {
                        _unitOfWork.Store(AnswerCollection.FromWriteModel(answer));
                    }
                }
            }
            
            await _unitOfWork.SessionSaveChangesAsync();
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm bài kiểm tra");
            return true;
        }, cancellationToken);
        return response;
    }

    /// <summary>
    /// Insert quizzes into existing test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestQuizInsertResponse> InsertTestQuizAsync(TestQuizInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new TestQuizInsertResponse { Success = false };
        var currentEmail = _identityService.GetCurrentUser()!.Email;

        // Get existing test
        var existingTest = await _commandRepository.FirstOrDefaultAsync(
            t => t.TestId == request.TestId && t.IsActive,
            cancellationToken: cancellationToken);
        if (existingTest == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
            return response;
        }

        // Get subject names from CourseService
        var subjectIds = request.Quizzes.Select(q => q.SubjectCode).Distinct().ToList();
        var subjectSelectEvent = new SubjectSelectsEvent
        {
            SubjectIds = subjectIds,
        };

        var messageResponse = await _requestSubjectSelectClient.GetResponse<SubjectSelectsEventResponse>(
            subjectSelectEvent, cancellationToken);
        if (!messageResponse.Message.Success)
        {
            response.MessageId = messageResponse.Message.MessageId;
            response.Message = messageResponse.Message.Message;
            return response;
        }

        // Create subject mapping dictionary
        var subjectMapping = messageResponse.Message.Response
            .ToDictionary(s => s.SubjectId, s => s.SubjectNameCode);
        await _unitOfWork.BeginTransactionAsync(async () =>
        { 
            // Add new quizzes to existing test
            var newQuizzes = new List<Quiz>();
            foreach (var quizDto in request.Quizzes)
            {
                var newQuiz = new Quiz
                {
                    TestId = existingTest.TestId,
                    QuizType = (short) ConstantEnum.TestType.Quiz,
                    PlacementTestQuizSetting = new PlacementTestQuizSetting
                    {
                        SubjectCode = quizDto.SubjectCode,
                        Title = quizDto.Title,
                        Description = quizDto.Description,
                    },
                    Questions = quizDto.Questions.Select(q => new Question
                    {
                        QuestionText = q.QuestionText,
                        QuestionType = (short) q.QuestionType,
                        DifficultyLevel = q.DifficultyLevel,
                        Answers = q.Answers.Select(a => new Answer
                        {
                            AnswerText = a.AnswerText,
                            IsCorrect = a.IsCorrect,
                        }).ToList()
                    }).ToList()
                };
                
                existingTest.Quizzes.Add(newQuiz);
                newQuizzes.Add(newQuiz);
            }

            // Save to write database
            _commandRepository.Update(existingTest);
            await _unitOfWork.SaveChangesAsync(currentEmail, cancellationToken);

            // Update TestCollection to include new quizzes
            var testCollection = await _queryRepository.FirstOrDefaultAsync(
                t => t.TestId == request.TestId && t.IsActive);
            
            if (testCollection != null)
            {
                foreach (var quiz in newQuizzes)
                {
                    var quizCollection = QuizCollection.FromWriteModel(quiz,
                        subjectMapping.ContainsKey(quiz.PlacementTestQuizSetting!.SubjectCode)
                            ? subjectMapping[quiz.PlacementTestQuizSetting!.SubjectCode]
                            : string.Empty);
                    testCollection.Quizzes.Add(quizCollection);

                    // Store questions and answers
                    foreach (var question in quiz.Questions)
                    {
                        _unitOfWork.Store(QuestionCollection.FromWriteModel(question));
                        foreach (var answer in question.Answers)
                        {
                            _unitOfWork.Store(AnswerCollection.FromWriteModel(answer));
                        }
                    }
                }
                _unitOfWork.Store(testCollection);
            }

            await _unitOfWork.SessionSaveChangesAsync();
            
            // Clear cache
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm quiz vào bài test");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Delete quiz from test
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestQuizDeleteResponse> DeleteTestQuizAsync(TestQuizDeleteCommand request, CancellationToken cancellationToken)
    {
        var response = new TestQuizDeleteResponse { Success = false };
        var currentEmail = _identityService.GetCurrentUser()!.Email;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Get existing test with quizzes
            var existingTest = await _commandRepository.FirstOrDefaultAsync(
                t => t.TestId == request.TestId && t.IsActive,
                cancellationToken: cancellationToken,
                includes: t => t.Quizzes);

            if (existingTest == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
                return false;
            }

            // Find quiz to delete
            var quizToDelete = existingTest.Quizzes.FirstOrDefault(q => q.QuizId == request.QuizId && q.IsActive);
            if (quizToDelete == null)
            {
                response.SetMessage(MessageId.E00000, "Quiz không tồn tại trong bài test này");
                return false;
            }
            
            // Save to write database
            _commandQuizRepository.Update(quizToDelete);
            await _unitOfWork.SaveChangesAsync(currentEmail, cancellationToken, needLogicalDelete: true);
            
            var testCollection = await _queryRepository.FirstOrDefaultAsync(
                t => t.TestId == request.TestId && t.IsActive);
            
            var quizCollection = testCollection?.Quizzes
                .FirstOrDefault(q => q.QuizId == request.QuizId);
            
            testCollection!.Quizzes.Remove(quizCollection!);

            // Store new TestCollection in RavenDB (will only include active quizzes)
            _unitOfWork.Store(testCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Clear cache
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xóa bài quiz");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Insert questions into quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestQuizQuestionsInsertResponse> InsertTestQuizQuestionsAsync(TestQuizQuestionsInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new TestQuizQuestionsInsertResponse { Success = false };
        var currentEmail = _identityService.GetCurrentUser()!.Email;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Get existing test with quizzes and questions (with proper tracking)
            var existingTest = await _commandRepository.Find(
                t => t.TestId == request.TestId && t.IsActive,
                isTracking: true,
                cancellationToken: cancellationToken)
                .Include(t => t.Quizzes)
                .ThenInclude(q => q.Questions)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (existingTest == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
                return false;
            }

            // Find quiz to add questions
            var targetQuiz = existingTest.Quizzes.FirstOrDefault(q => q.QuizId == request.QuizId && q.IsActive);
            if (targetQuiz == null)
            {
                response.SetMessage(MessageId.E00000, "Quiz không tồn tại trong bài test này");
                return false;
            }

            // Add new questions
            var newQuestions = new List<Question>();
            foreach (var questionDto in request.Questions)
            {
                var newQuestion = new Question
                {
                    QuestionId = Guid.NewGuid(),
                    QuizId = targetQuiz.QuizId, // Set QuizId explicitly
                    QuestionText = questionDto.QuestionText,
                    QuestionType = (short)questionDto.QuestionType,
                    DifficultyLevel = questionDto.DifficultyLevel,
                    Answers = questionDto.Answers.Select(a => new Answer
                    {
                        AnswerId = Guid.NewGuid(),
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect,
                    }).ToList()
                };
                await _commandQuestionRepository.AddAsync(newQuestion);
                newQuestions.Add(newQuestion);
            }
            await _unitOfWork.SaveChangesAsync(currentEmail, cancellationToken);
            
            // Reload and update QuizCollection to include new questions
            var testCollection = await _queryRepository.FirstOrDefaultAsync(t => t.TestId == request.TestId && t.IsActive);
            var quizCollection = testCollection?.Quizzes
                .FirstOrDefault(q => q.QuizId == request.QuizId);
            foreach (var question in newQuestions)
            {
                var questionExists = quizCollection?.Questions.FirstOrDefault(q => q.QuestionId == question.QuestionId);
                if (questionExists == null)
                {
                    quizCollection.Questions.Add(QuestionCollection.FromWriteModel(question));
                    _unitOfWork.Store(QuestionCollection.FromWriteModel(question));
                }
            }
            _unitOfWork.Store(testCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Clear cache
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm câu hỏi");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Delete multiple questions from quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<TestQuizQuestionsDeleteResponse> DeleteTestQuizQuestionsAsync(TestQuizQuestionsDeleteCommand request, CancellationToken cancellationToken)
    {
        var response = new TestQuizQuestionsDeleteResponse { Success = false };
        var currentEmail = _identityService.GetCurrentUser()!.Email;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Get existing test with quizzes
            var existingTest = await _commandRepository.Find(
                t => t.TestId == request.TestId && t.IsActive,
                cancellationToken: cancellationToken)
                .Include(t => t.Quizzes)
                .ThenInclude(t => t.Questions)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (existingTest == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
                return false;
            }

            // Find quiz
            var targetQuiz = existingTest.Quizzes.FirstOrDefault(q => q.QuizId == request.QuizId && q.IsActive);
            if (targetQuiz == null)
            {
                response.SetMessage(MessageId.E00000, "Quiz không tồn tại trong bài test này");
                return false;
            }

            // Find questions to delete
            var questionsToDelete = await _commandQuestionRepository
                .Find(q => request.QuestionIds.Contains(q.QuestionId) && q.IsActive)
                .ToListAsync(cancellationToken: cancellationToken);

            if (!questionsToDelete.Any())
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy câu hỏi nào để xóa");
                return false;
            }

            // Soft delete questions
            foreach (var question in questionsToDelete)
            {
                _commandQuestionRepository.Update(question!);
            }
            await _unitOfWork.SaveChangesAsync(currentEmail, cancellationToken, needLogicalDelete: true);
            
            var testCollection = await _queryRepository.FirstOrDefaultAsync(
                t => t.TestId == request.TestId && t.IsActive);
            var quizCollection = testCollection?.Quizzes
                .FirstOrDefault(q => q.QuizId == request.QuizId);
            var questionsCollectionToDelete = quizCollection?.Questions
                .Where(q => request.QuestionIds.Contains(q.QuestionId))
                .ToList();
            
            foreach (var questionCollection in questionsCollectionToDelete!)
            {
                quizCollection!.Questions.Remove(questionCollection);
            }
            _unitOfWork.Store(testCollection);
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Clear cache
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.CacheRemoveAsync("quiz:list");

            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xóa câu hỏi");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Select test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<TestSelectResponse> SelectTestAsync(TestSelectQuery request)
    {
        var response = new TestSelectResponse { Success = false };

        string cacheKey = "test:id";

        // Get test from cache or database
        var test = await _queryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                // If not in cache, get from database
                return await _queryRepository.FirstOrDefaultAsync(x => x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (test == null)
        {
            response.SetMessage(MessageId.E00000, CommonMessages.TestNotFound);
            return response;
        }

        // Build quizzes with full details
        var quizzes = test.Quizzes
            .Where(x => !request.QuizId.Any() || request.QuizId.Contains(x.QuizId))
            .Select(q => new QuizzDetailResponse
            {
                QuizId = q.QuizId,
                Title = q.PlacementTestQuizSetting!.Title,
                Description = q.PlacementTestQuizSetting.Description,
                SubjectCode = q.PlacementTestQuizSetting!.SubjectCode,
                SubjectCodeName = q.PlacementTestQuizSetting.SubjectCodeName,
                TotalQuestions = q.Questions.Count,
                Questions = q.Questions.Select(ques => new QuestionDetailResponse
                {
                    QuestionId = ques.QuestionId,
                    QuestionText = ques.QuestionText,
                    QuestionType = ques.QuestionType,
                    DifficultyLevel = ques.DifficultyLevel,
                    Answers = ques.Answers.Select(a => new AnswerDetailResponse
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText
                    }).ToList()
                }).ToList()
            }).ToList();

        var responseEntity = new TestSelectResponseEntity
        {
            TestId = test.TestId,
            TestName = test.TestName,
            Description = test.Description,
            Quizzes = quizzes
        };

        // True
        response.Success = true;
        response.Response = responseEntity;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài test");
        return response;
    }
}