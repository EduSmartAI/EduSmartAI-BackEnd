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
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="commandRepository"></param>
    /// <param name="answerCommandRepository"></param>
    /// <param name="identityService"></param>
    /// <param name="unitOfWork"></param>
    public QuestionService(
        ICommandRepository<Question> commandRepository,
        ICommandRepository<Answer> answerCommandRepository,
        IIdentityService identityService,
        IUnitOfWork unitOfWork)
    {
        _commandRepository = commandRepository;
        _answerCommandRepository = answerCommandRepository;
        _identityService = identityService;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Insert question
    /// </summary>
    /// <param name="quizId"></param>
    /// <param name="text"></param>
    /// <returns></returns>
    public async Task<Guid> InsertQuestionAsync(Guid quizId, string text, string email)
    {
        var question = new Question
        {
            QuestionId = Guid.NewGuid(),
            QuizId = quizId,
            QuestionText = text,
        };

        await _commandRepository.AddAsync(question, email);
        _unitOfWork.Store(QuestionCollection.FromWriteModel(question));
        return question.QuestionId;
    }

    /// <summary>
    /// Insert question với answers vào quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuestionInsertResponse> InsertQuestionWithAnswersAsync(QuestionInsertCommand request,
        CancellationToken cancellationToken)
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

            var questionCollection = new QuestionCollection
            {
                QuestionId = newQuestion.QuestionId,
                QuestionText = newQuestion.QuestionText,
                CreatedAt = newQuestion.CreatedAt,
                CreatedBy = newQuestion.CreatedBy,
                UpdatedAt = newQuestion.UpdatedAt,
                UpdatedBy = newQuestion.UpdatedBy,
                IsActive = newQuestion.IsActive,
                Answers = request.Answers.Select(a => new AnswerCollection
                {
                    AnswerId = Guid.NewGuid(),
                    QuestionId = newQuestion.QuestionId,
                    AnswerText = a.AnswerText,
                    IsCorrect = a.IsCorrect,
                    CreatedAt = newQuestion.CreatedAt,
                    CreatedBy = newQuestion.CreatedBy,
                    UpdatedAt = newQuestion.UpdatedAt,
                    UpdatedBy = newQuestion.UpdatedBy,
                    IsActive = newQuestion.IsActive
                }).ToList()
            };
            _unitOfWork.Store(questionCollection);

            _unitOfWork.Store(QuestionCollection.FromWriteModel(newQuestion));
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
    public async Task<QuestionUpdateResponse> UpdateQuestionAsync(QuestionUpdateCommand request,
        CancellationToken cancellationToken)
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

            // Delete question exist
            _commandRepository.Update(existingQuestion, currentUser.Email, true);
            _answerCommandRepository.UpdateRange(existingQuestion.Answers, currentUser.Email, true);
            foreach (var answer in existingQuestion.Answers)
            {
                _unitOfWork.Delete(AnswerCollection.FromWriteModel(answer));
            }
            _unitOfWork.Delete(QuestionCollection.FromWriteModel(existingQuestion));

            // Add new question
            var newQuestion = new Question
            {
                QuizId = existingQuestion.QuizId,
                QuestionText = request.QuestionText,
            };
            
            // Add new answers for the question
            foreach (var answerRequest in request.Answers)
            {
                var newAnswer = new Answer
                {
                    QuestionId = newQuestion.QuestionId,
                    AnswerText = answerRequest.AnswerText,
                    IsCorrect = answerRequest.IsCorrect ?? false,
                };

                await _answerCommandRepository.AddAsync(newAnswer, currentUser.Email);
                _unitOfWork.Store(AnswerCollection.FromWriteModel(newAnswer));
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

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
                    _answerCommandRepository.Update(answer, currentUser.Email);
                    _unitOfWork.Delete(AnswerCollection.FromWriteModel(answer));
                }
            }

            _commandRepository.Update(existingQuestion, currentUser.Email);
            _unitOfWork.Delete(QuestionCollection.FromWriteModel(existingQuestion));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Xoá câu hỏi và câu trả lời");
            return true;
        }, cancellationToken);
        return response;
    }
}