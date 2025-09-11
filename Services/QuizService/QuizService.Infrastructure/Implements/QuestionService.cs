using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using QuizService.Application.Applications.Questions.Commands;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;

namespace QuizService.Infrastructure.Implements;

public class QuestionService : IQuestionService
{
    private readonly ICommandRepository<Question> _commandRepository;
    private readonly ICommandRepository<Answer> _answerCommandRepository;
    private readonly IQueryRepository<TestCollection> _testQueryRepository;
    private readonly IQueryRepository<QuestionCollection> _questionQueryRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="answerCommandRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    /// <param name="testQueryRepository"></param>
    /// <param name="questionQueryRepository"></param>
    public QuestionService(
        ICommandRepository<Question> commandRepository,
        ICommandRepository<Answer> answerCommandRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork, IQueryRepository<TestCollection> testQueryRepository, IQueryRepository<QuestionCollection> questionQueryRepository)
    {
        _commandRepository = commandRepository;
        _answerCommandRepository = answerCommandRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
        _testQueryRepository = testQueryRepository;
        _questionQueryRepository = questionQueryRepository;
    }

    /// <summary>
    /// Insert question
    /// </summary>
    /// <param name="quizId"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<Guid> InsertQuestionAsync(Guid quizId, string text, string explanation, string email)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid(),
            QuizId = quizId,
            QuestionText = text,
            Explanation = explanation
        };

        await _commandRepository.AddAsync(question, email);
        return question.QuestionId;
    }

    /// <summary>
    /// Insert question với answers vào quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionInsertResponse> InsertQuestionWithAnswersAsync(QuestionInsertCommand request, CancellationToken cancellationToken)
    {
        var response = new QuestionInsertResponse { Success = false };
        var currentUser = _identityService.GetCurrentUser()!;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Insert new question
            var newQuestion = new Question
            {
                QuestionId = Guid.NewGuid(),
                QuizId = request.QuizId,
                QuestionText = request.QuestionText,
                Explanation = request.Explanation
            };

            await _commandRepository.AddAsync(newQuestion, currentUser.Email);
            
            // Insert answers for the question
            foreach (var answerRequest in request.Answers)
            {
                var newAnswer = new Answer
                {
                    QuestionId = newQuestion.QuestionId,
                    AnswerText = answerRequest.AnswerText,
                    IsCorrect = answerRequest.IsCorrect,
                };

                await _answerCommandRepository.AddAsync(newAnswer, currentUser.Email);
                _unitOfWork.Store(AnswerCollection.FromWriteModel(newAnswer));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var questionCollection = QuestionCollection.FromWriteModel(newQuestion);
            _unitOfWork.Store(questionCollection);
            
            var testCollection = await _testQueryRepository.FirstOrDefaultAsync(x => x.Quizzes.Any(q => q.QuizId == request.QuizId));
            if (testCollection != null)
            {
                var quiz = testCollection.Quizzes.FirstOrDefault(q => q.QuizId == request.QuizId);
                if (quiz != null)
                {
                    quiz.Questions.Add(questionCollection);
                }
                _unitOfWork.Store(testCollection);
            }
            
            // Delete cache test if any
            await _unitOfWork.CacheRemoveAsync("test:id");
            await _unitOfWork.SessionSaveChangesAsync();
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Thêm câu hỏi và câu trả lời");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Update question and its answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionUpdateResponse> UpdateQuestionAsync(QuestionUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new QuestionUpdateResponse { Success = false };
        var currentUser = _identityService.GetCurrentUser()!;
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check question exist
            var existingQuestion = await _commandRepository
                .FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId
                                          && q.IsActive, cancellationToken,
                    question => question.Answers);
            if (existingQuestion == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.QuestionNotFound);
                return false;
            }
            
            // Update question text
            existingQuestion.QuestionText = request.QuestionText;
            existingQuestion.Explanation = request.Explanation;
            
            foreach (var ans in request.Answers)
            {
                if (ans.AnswerId.HasValue)
                {
                    // Update existing answer
                    var existingAnswer = existingQuestion.Answers
                        .FirstOrDefault(a => a.AnswerId == ans.AnswerId.Value);

                    if (existingAnswer != null)
                    {
                        existingAnswer.AnswerText = ans.AnswerText;
                        existingAnswer.IsCorrect = ans.IsCorrect;
                    }
                }
                else
                {
                    // Insert new answer
                    var newAnswer = new Answer
                    {
                        AnswerText = ans.AnswerText,
                        IsCorrect = ans.IsCorrect,
                        QuestionId = existingQuestion.QuestionId
                    };
                    existingQuestion.Answers.Add(newAnswer);
                }
            }
            
            var requestAnswerIds = request.Answers.Where(a => a.AnswerId.HasValue).Select(a => a.AnswerId!.Value).ToList();
            var answersToRemove = existingQuestion.Answers
                .Where(a => !requestAnswerIds.Contains(a.AnswerId))
                .ToList();

            foreach (var answer in answersToRemove)
            {
                _answerCommandRepository.Update(answer, currentUser.Email, true);
                _unitOfWork.Delete(AnswerCollection.FromWriteModel(answer));
            }
            
            _commandRepository.Update(existingQuestion, currentUser.Email);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Update marten
            var testCollection = await _testQueryRepository.FirstOrDefaultAsync(x => x.Quizzes.Any(q => q.QuizId == existingQuestion.QuizId));
            if (testCollection != null)
            {
                var quiz = testCollection.Quizzes.FirstOrDefault(q => q.QuizId == existingQuestion.QuizId);
                if (quiz != null)
                {
                    var questionRead = quiz.Questions.FirstOrDefault(q => q.QuestionId == existingQuestion.QuestionId);
                    if (questionRead != null)
                    {
                        // update existing question in read model
                        questionRead.QuestionText = existingQuestion.QuestionText;
                        questionRead.Explanation = existingQuestion.Explanation;
                        questionRead.Answers = existingQuestion.Answers
                            .Where(a => a.IsActive)
                            .Select(a => new AnswerCollection
                            {
                                AnswerId = a.AnswerId,
                                AnswerText = a.AnswerText,
                                IsCorrect = a.IsCorrect
                            }).ToList();
                    }
                    else
                    {
                        // If not exist, add new question to read model
                        quiz.Questions.Add(new QuestionCollection
                        {
                            QuestionId = existingQuestion.QuestionId,
                            QuestionText = existingQuestion.QuestionText,
                            Explanation = existingQuestion.Explanation,
                            Answers = existingQuestion.Answers
                                .Where(a => a.IsActive)
                                .Select(a => new AnswerCollection
                                {
                                    AnswerId = a.AnswerId,
                                    AnswerText = a.AnswerText,
                                    IsCorrect = a.IsCorrect
                                }).ToList()
                        });
                    }
                    _unitOfWork.Store(testCollection);
                }
                await _unitOfWork.SessionSaveChangesAsync();
            }
            
            // Delete cache test if any
            await _unitOfWork.CacheRemoveAsync("test:id");


            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật câu hỏi và câu trả lời");
            return true;
        }, cancellationToken);

        return response;
    }

    /// <summary>
    /// Delete question và tất cả answers của nó
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionDeleteResponse> DeleteQuestionAsync(QuestionDeleteCommand request, CancellationToken cancellationToken)
    {
        var response = new QuestionDeleteResponse { Success = false };
        var currentUser = _identityService.GetCurrentUser()!;

        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check question exist
            var existingQuestion = await _commandRepository.FirstOrDefaultAsync(q => q.QuestionId == request.QuestionId, cancellationToken);
            if (existingQuestion == null)
            {
                response.SetMessage(MessageId.E00000, CommonMessages.QuestionNotFound);
                return false;
            }

            // Delete answers of the question
            var answers = _answerCommandRepository.Find(a => a.QuestionId == request.QuestionId && a.IsActive).ToList();
            foreach (var answer in answers)
            {
                if (answer != null)
                {
                    _answerCommandRepository.Update(answer, currentUser.Email, true);
                    _unitOfWork.Delete(AnswerCollection.FromWriteModel(answer));
                }
            }

            _commandRepository.Update(existingQuestion, currentUser.Email, true);
            _unitOfWork.Delete(QuestionCollection.FromWriteModel(existingQuestion));

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            // Update marten
            var testCollection = await _testQueryRepository
                .FirstOrDefaultAsync(x => x.Quizzes.Any(q => q.QuizId == existingQuestion.QuizId));
            if (testCollection != null)
            {
                var quiz = testCollection.Quizzes.FirstOrDefault(q => q.QuizId == existingQuestion.QuizId);
                if (quiz != null)
                {
                    quiz.Questions = quiz.Questions
                        .Where(q => q.QuestionId != existingQuestion.QuestionId)
                        .ToList();
                    _unitOfWork.Store(testCollection);
                }
            }
            
            await _unitOfWork.SessionSaveChangesAsync();
            
            // Delete cache test if any
            await _unitOfWork.CacheRemoveAsync("test:id");

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xoá câu hỏi và câu trả lời");
            return true;
        }, cancellationToken);
        return response;
    }
}