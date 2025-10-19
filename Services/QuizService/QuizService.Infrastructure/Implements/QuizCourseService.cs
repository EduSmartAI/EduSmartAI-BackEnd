using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Application.Interfaces.Repositories;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Messaging.Events.QuizService;
using FluentValidation;
using MassTransit;
using MassTransit.Initializers;
using Microsoft.EntityFrameworkCore;
using QuizService.Application.Applications.QuizCourses.Commands;
using QuizService.Application.Applications.QuizCourses.Consumers;
using QuizService.Application.Applications.QuizCourses.Queries;
using QuizService.Application.Interfaces;
using QuizService.Domain.ReadModels;
using QuizService.Domain.WriteModels;
using System.Text.Json;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace QuizService.Infrastructure.Implements;

public class QuizCourseService : IQuizCourseService
{
    private readonly ICommandRepository<Quiz> _quizCommandRepository;
    private readonly IQueryRepository<QuizCollection> _quizQueryRepository;
    private readonly ICommandRepository<OutboxMessage> _outboxCommandRepository;
    private readonly IIdentityService _identityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICommandRepository<StudentQuiz> _studentQuizCommandRepository;
    private readonly ICommandRepository<Answer> _answerCommandRepository;
    private readonly ICommandRepository<Question> _questionCommandRepository;
	private readonly IQueryRepository<StudentQuizCollection> _studentQuizQueryRepository;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="quizCommandRepository"></param>
	/// <param name="quizQueryRepository"></param>
	/// <param name="unitOfWork"></param>
	/// <param name="outboxCommandRepository"></param>
	/// <param name="answerCommandRepository"></param>
	/// <param name="questionCommandRepository"></param>
	public QuizCourseService(ICommandRepository<Quiz> quizCommandRepository,
		IQueryRepository<QuizCollection> quizQueryRepository, 
		IUnitOfWork unitOfWork, 
		ICommandRepository<OutboxMessage> outboxCommandRepository,
		IIdentityService identityService,
		ICommandRepository<StudentQuiz> studentQuizCommandRepository, 
		IQueryRepository<StudentQuizCollection> studentQuizQueryRepository, 
		ICommandRepository<Answer> answerCommandRepository,
		ICommandRepository<Question> questionCommandRepository)
    {
        _quizCommandRepository = quizCommandRepository;
        _quizQueryRepository = quizQueryRepository;
        _unitOfWork = unitOfWork;
        _outboxCommandRepository = outboxCommandRepository;
        _identityService = identityService;
        _studentQuizCommandRepository = studentQuizCommandRepository;
        _studentQuizQueryRepository = studentQuizQueryRepository;
        _answerCommandRepository = answerCommandRepository;
        _questionCommandRepository = questionCommandRepository;
    }

    /// <summary>
    /// Insert new quiz for course
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<QuizCourseInsertResponse> InsertQuizCourseAsync(QuizCourseInsertCommand request)
    {
        var response = new QuizCourseInsertResponse { Success = false };
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            var newQuiz = new Quiz
            { 
				QuizType = (short)TestType.Exam,
                CourseQuizSetting = new CourseQuizSetting
                {
                    DurationMinutes = request.DurationMinutes,
                    PassingScorePercentage = request.PassingScorePercentage,
                    ShuffleQuestions = request.ShuffleQuestions,
                    ShowResultsImmediately = request.ShowResultsImmediately,
                    AllowRetake = request.AllowRetake
                },
                Questions = request.Questions.Select(x => new Question
                {
                    QuestionText = x.QuestionText,
                    Explanation = x.Explanation,
                    QuestionType = x.QuestionType,
                    Answers = x.Answers.Select(x => new Answer
                    {
                        AnswerText = x.AnswerText,
                        IsCorrect = x.IsCorrect
                    }).ToList()
                }).ToList(),
            };
            
            // Save to database
            await _quizCommandRepository.AddAsync(newQuiz);
            await _unitOfWork.SaveChangesAsync(request.UserEmail, CancellationToken.None);
            
            // Publish event to read model
            var @quizCourseCollectionInsertEvent = new QuizCourseCollectionUpsertEvent
            {
                Quiz = QuizCollection.FromWriteModel(newQuiz)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(QuizCourseCollectionUpsertEvent),
                Content = JsonSerializer.Serialize(@quizCourseCollectionInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(request.UserEmail, CancellationToken.None);
            
            // True
            response.Success = true;
            response.Response = new QuizCourseInsertResponseEntity { QuizId = newQuiz.QuizId };
            response.SetMessage(MessageId.I00001, "Thêm câu hỏi cho khoá học");
            return true;
        });
        return response;
    }

    /// <summary>
    /// Update quiz for course - Only updates existing quiz settings and questions/answers
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuizCourseUpdateResponse> UpdateQuizCourseAsync(QuizCourseUpdateCommand request, CancellationToken cancellationToken)
    {
        var response = new QuizCourseUpdateResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser();
        
        // Begin transaction
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Check if quiz exists
            var existingQuiz = await _quizCommandRepository
                .Find(q => q.QuizId == request.QuizId && 
                           q.QuizType == (short) TestType.Exam &&
                           q.IsActive, 
                    isTracking: true,
                    cancellationToken: cancellationToken,
                    cq => cq.CourseQuizSetting!)
                .Include(q => q!.Questions.Where(x => x.IsActive))
	            .ThenInclude(q => q.Answers.Where(a => a.IsActive))
                .AsSplitQuery()
                .FirstOrDefaultAsync(cancellationToken);
            
            if (existingQuiz == null)
            {
                response.SetMessage(MessageId.E00000, "Không tìm thấy bài kiểm tra");
                return false;
            }
            
            // Update quiz settings only if provided
            if (existingQuiz.CourseQuizSetting != null)
            {
                if (request.DurationMinutes.HasValue)
                {
                    existingQuiz.CourseQuizSetting.DurationMinutes = request.DurationMinutes.Value;
                }
                if (request.PassingScorePercentage.HasValue)
                {
                    existingQuiz.CourseQuizSetting.PassingScorePercentage = request.PassingScorePercentage.Value;
                }
                if (request.ShuffleQuestions.HasValue)
                {
                    existingQuiz.CourseQuizSetting.ShuffleQuestions = request.ShuffleQuestions.Value;
                }
                if (request.ShowResultsImmediately.HasValue)
                {
                    existingQuiz.CourseQuizSetting.ShowResultsImmediately = request.ShowResultsImmediately.Value;
                }
                if (request.AllowRetake.HasValue)
                {
                    existingQuiz.CourseQuizSetting.AllowRetake = request.AllowRetake.Value;
                }
            }
            
            // Update questions if provided
            if (request.Questions != null && request.Questions.Any())
            {
                foreach (var questionRequest in request.Questions)
                {
                    // Find existing question
                    var existingQuestion = existingQuiz.Questions.FirstOrDefault(q => q.QuestionId == questionRequest.QuestionId && q.IsActive);
                    if (existingQuestion != null)
                    {
                        // Update question properties only if provided
                        if (!string.IsNullOrWhiteSpace(questionRequest.QuestionText))
                        {
                            existingQuestion.QuestionText = questionRequest.QuestionText;
                        }
                        if (questionRequest.QuestionType.HasValue)
                        {
                            existingQuestion.QuestionType = questionRequest.QuestionType.Value;
                        }
                        if (questionRequest.Explanation != null)
                        {
                            existingQuestion.Explanation = questionRequest.Explanation;
                        }
                        
                        // Update answers if provided
                        if (questionRequest.Answers.Any())
                        {
                            foreach (var answerRequest in questionRequest.Answers)
                            {
                                // Find existing answer
                                var existingAnswer = existingQuestion.Answers.FirstOrDefault(a => a.AnswerId == answerRequest.AnswerId && a.IsActive);
                                if (existingAnswer != null)
                                {
                                    existingAnswer.AnswerText = answerRequest.AnswerText;
                                    existingAnswer.IsCorrect = answerRequest.IsCorrect;
                                }
                            }
                        }
                    }
                }
            }
            
            // Save to database
            _quizCommandRepository.Update(existingQuiz);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            // Publish event to update read model
            var quizCourseCollectionUpdateEvent = new QuizCourseCollectionUpsertEvent
            {
                Quiz = QuizCollection.FromWriteModel(existingQuiz)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(QuizCourseCollectionUpsertEvent),
                Content = JsonSerializer.Serialize(quizCourseCollectionUpdateEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            
            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, "Cập nhật bài kiểm tra cho khoá học");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Add new questions to existing quiz
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuizCourseAddQuestionsResponse> InsertQuestionsToQuizAsync(QuizCourseAddQuestionsCommand request, CancellationToken cancellationToken)
    {
        var response = new QuizCourseAddQuestionsResponse { Success = false };
        
	    var currentUser = _identityService.GetCurrentUser();
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Add new questions
            foreach (var questionRequest in request.Questions)
            {
                var newQuestion = new Question
                {
                    QuestionText = questionRequest.QuestionText,
                    QuestionType = questionRequest.QuestionType,
                    Explanation = questionRequest.Explanation,
                    Answers = questionRequest.Answers.Select(a => new Answer
                    {
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
                    }).ToList(),
                    QuizId = request.QuizId
                };
                await _questionCommandRepository.AddAsync(newQuestion);
            }
            
            // Save to database
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            // Check if quiz exists
            var existingQuiz = await _quizCommandRepository
	            .Find(q => q.QuizId == request.QuizId && q.QuizType == (short) TestType.Exam && q.IsActive, cancellationToken: cancellationToken)
	            .Include(q => q.CourseQuizSetting!)
	            .Include(q => q.Questions)
	            .ThenInclude(q => q.Answers)
	            .FirstOrDefaultAsync(cancellationToken);
            
            // Publish event to update read model
            var quizCourseCollectionInsertEvent = new QuizCourseCollectionUpsertEvent
            {
                Quiz = QuizCollection.FromWriteModel(existingQuiz!)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(QuizCourseCollectionUpsertEvent),
                Content = JsonSerializer.Serialize(quizCourseCollectionInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, $"Đã thêm câu hỏi vào bài kiểm tra");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Delete questions from quiz (soft delete - set IsActive = false)
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<QuizCourseDeleteQuestionsResponse> DeleteQuestionsFromQuizAsync(QuizCourseDeleteQuestionsCommand request, CancellationToken cancellationToken)
    {
        var response = new QuizCourseDeleteQuestionsResponse { Success = false };
        
        var currentUser = _identityService.GetCurrentUser();
        
        await _unitOfWork.BeginTransactionAsync(async () =>
        {
            // Mark questions and their answers as inactive
            foreach (var questionId in request.QuestionIds)
            {
                var question = await _questionCommandRepository
	                .Find(predicate: q => q.QuizId == request.QuizId && q.QuestionId == questionId && q.IsActive,
		                isTracking: true,
		                cancellationToken: cancellationToken,
		                x => x.Answers)
	                .FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (question == null)
                {
	                response.Success = false;
	                response.SetMessage(MessageId.E00000, $"Không tìm thấy câu hỏi với ID: {questionId} trong bài kiểm tra");
	                return false;
                }
                _questionCommandRepository.Update(question);
                foreach (var answer in question.Answers)
                {
                    _answerCommandRepository.Update(answer);
                }
            }
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken, true);
            
            // Check if quiz exists
            var existingQuiz = await _quizCommandRepository
	            .Find(q => q.QuizId == request.QuizId && q.QuizType == (short) TestType.Exam && q.IsActive, cancellationToken: cancellationToken)
	            .Include(q => q.CourseQuizSetting!)
	            .Include(q => q.Questions)
	            .ThenInclude(q => q.Answers)
	            .FirstOrDefaultAsync(cancellationToken);
            
            // Publish event to update read model
            var quizCourseCollectionInsertEvent = new QuizCourseCollectionUpsertEvent
            {
                Quiz = QuizCollection.FromWriteModel(existingQuiz!)
            };

            var outboxMessage = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                Type = nameof(QuizCourseCollectionUpsertEvent),
                Content = JsonSerializer.Serialize(quizCourseCollectionInsertEvent),
                OccurredOnUtc = DateTime.UtcNow,
            };
            await _outboxCommandRepository.AddAsync(outboxMessage);
            await _unitOfWork.SaveChangesAsync(currentUser!.Email, cancellationToken);
            
            // True
            response.Success = true;
            response.SetMessage(MessageId.I00001, $"Đã xóa câu hỏi khỏi bài kiểm tra");
            return true;
        }, cancellationToken);
        
        return response;
    }

    /// <summary>
    /// Select quiz for course
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    public async Task<QuizCourseSelectQueryResponse> SelectCourseQuiz(QuizCourseSelectQuery request)
    {
        var response = new QuizCourseSelectQueryResponse { Success = false };
        
        var cacheKey = CacheKey.QuizCourses(request.QuizId);

        var quizSelect = await _quizQueryRepository.GetOrSetAsync(
            cacheKey,
			async () => await _quizQueryRepository.FirstOrDefaultAsync(q => q.QuizId == request.QuizId && q.QuizType == (short) TestType.Exam && q.IsActive),
            TimeSpan.FromMinutes(10))
            .Select(x => new QuizCourseSelectQueryResponseEntity
            {
                QuizId = x.QuizId,
                DurationMinutes = x.CourseQuizSetting!.DurationMinutes,
                PassingScorePercentage = x.CourseQuizSetting.PassingScorePercentage,
                ShuffleQuestions = x.CourseQuizSetting.ShuffleQuestions ?? false,
                ShowResultsImmediately = x.CourseQuizSetting.ShowResultsImmediately ?? false,
                AllowRetake = x.CourseQuizSetting.AllowRetake ?? false,
                TotalQuestions = x.Questions.Count(q => q.IsActive),
                Questions = x.Questions.Where(q => q.IsActive).Select(q => new QuizCourseSelectQuestionDetailResponse
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    QuestionType = q.QuestionType,
                    Explanation = q.Explanation ?? string.Empty,
                    Answers = q.Answers.Where(a => a.IsActive).Select(a => new QuizCourseSelectAnswerDetailResponse
                    {
                        AnswerId = a.AnswerId,
                        AnswerText = a.AnswerText,
                        IsCorrect = a.IsCorrect
					}).ToList()
                }).ToList()
            });
        if (quizSelect == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy bài quiz");
            return response;
        }
        
        // True
        response.Success = true;
        response.Response = quizSelect;
        response.SetMessage(MessageId.I00001, "Lấy thông tin bài quiz");
        return response;
    }

	/// <summary>
	/// Insert student quiz course
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	public async Task<StudentQuizCourseInsertResponse> InsertStudentQuizCourseAsync(StudentQuizCourseInsertCommand request, CancellationToken cancellationToken)
	{
		var response = new StudentQuizCourseInsertResponse { Success = false };

		var currentUser = _identityService.GetCurrentUser();

		// Begin transaction
		await _unitOfWork.BeginTransactionAsync(async () =>
		{
			// Check quiz exist
			var quizCollectionExist = await _quizQueryRepository.FirstOrDefaultAsync(q =>
				q.QuizId == request.QuizId && q.QuizType == (short)TestType.Exam && q.IsActive);
			if (quizCollectionExist == null)
			{
				response.SetMessage(MessageId.E00000, "Bài kiểm tra không tồn tại");
				return false;
			}

			// Insert new StudentQuiz
			var newStudentQuiz = new StudentQuiz
			{
				QuizId = request.QuizId,
				StudentId = currentUser!.UserId,
				QuizType = (short) TestType.Exam,
				StudentQuizAnswers = request.StudentQuizAnswers.Select(ans => new StudentQuizAnswer
				{
					QuestionId = ans.QuestionId,
					AnswerId = ans.AnswerId,
				}).ToList()
			};

			await _studentQuizCommandRepository.AddAsync(newStudentQuiz);
			await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

			// Publish event to read model
			var studentQuizCourseInsertEvent = new StudentQuizCourseInsertEvent
			{
				StudentQuiz = new StudentQuizCollection
				{
					StudentQuizId = newStudentQuiz.StudentQuizId,
					StudentId = newStudentQuiz.StudentId,
					QuizType = newStudentQuiz.QuizType,
					QuizId = newStudentQuiz.QuizId,
					IsActive = newStudentQuiz.IsActive,
					CreatedAt = newStudentQuiz.CreatedAt,
					UpdatedAt = newStudentQuiz.UpdatedAt,
					CreatedBy = newStudentQuiz.CreatedBy,
					UpdatedBy = newStudentQuiz.UpdatedBy,
					Quiz = quizCollectionExist,
					StudentQuizAnswers = newStudentQuiz.StudentQuizAnswers.Select(x => new StudentQuizAnswerCollection
					{
						StudentQuizAnswerId = x.StudentQuizAnswerId,
						StudentQuizId = x.StudentQuizId,
						QuestionId = x.QuestionId,
						AnswerId = x.AnswerId,
						IsActive = x.IsActive,
						CreatedAt = x.CreatedAt,
						UpdatedAt = x.UpdatedAt,
						CreatedBy = x.CreatedBy,
						UpdatedBy = x.UpdatedBy,
						Question = quizCollectionExist.Questions.FirstOrDefault(q => q.QuestionId == x.QuestionId),
						Answer = quizCollectionExist.Questions
							.SelectMany(q => q.Answers)
							.FirstOrDefault(a => a.AnswerId == x.AnswerId)
					}).ToList()
				}
			};

			var outboxMessage = new OutboxMessage
			{
				Id = Guid.NewGuid(),
				Type = nameof(StudentQuizCourseInsertEvent),
				Content = JsonSerializer.Serialize(studentQuizCourseInsertEvent),
				OccurredOnUtc = DateTime.UtcNow,
			};

			// Load quiz with questions and answers
			var quiz = await GetQuizWithDetailsAsync(request.QuizId, cancellationToken);

			if (quiz is null)
			{
				response.SetMessage(MessageId.E00000, "Không tải được chi tiết bài kiểm tra");
				return false;
			}
			// Prepare QuizEvaluableCreatedEvent
			var (scope, scopeId) = ValidateAndResolveScope(request);
			var (total, correct, details) = ComputeAttemptResult(quiz, newStudentQuiz.StudentQuizAnswers);
			var courseId = request.CourseId;

			// Prepare event
			var evt = new QuizEvaluableCreatedEvent(
				EventId: Guid.NewGuid(),
				AttemptId: newStudentQuiz.StudentQuizId,
				QuizId: request.QuizId,
				Scope: scope,
				ScopeId: scopeId,
				CourseId: courseId,
				UserId: currentUser!.UserId,
				TotalQuestions: total,
				TotalCorrectAnswers: correct,
				Questions: details.Select(d => new QuestionResult(
					d.QuestionId, d.QuestionText, d.QuestionType, d.Explanation,
					d.Answers.Select(a => new AnswerResult(a.AnswerId, a.AnswerText, a.IsCorrectAnswer, a.SelectedByStudent)).ToList()
				)).ToList(),
				OccurredAtUtc: DateTime.UtcNow
			);

			var quizEvaluableCreatedEventOutboxMessage = new OutboxMessage
			{
				Id = Guid.NewGuid(),
				Type = nameof(QuizEvaluableCreatedEvent),
				Content = JsonSerializer.Serialize(evt),
				OccurredOnUtc = DateTime.UtcNow,
			};
			
			await _outboxCommandRepository.AddAsync(outboxMessage);
			await _outboxCommandRepository.AddAsync(quizEvaluableCreatedEventOutboxMessage);
			await _unitOfWork.SaveChangesAsync(currentUser.Email, cancellationToken);

			// True
			response.Success = true;
			response.Response = newStudentQuiz.StudentQuizId;
			response.SetMessage(MessageId.I00001, "Lưu kết quả làm bài course");
			return true;
		}, cancellationToken);
		return response;
	}

	/// <summary>
	/// Validate and resolve scope from request
	/// </summary>
	/// <param name="req"></param>
	/// <returns></returns>
	/// <exception cref="ValidationException"></exception>
	private static (QuizScope scope, Guid scopeId) ValidateAndResolveScope(StudentQuizCourseInsertCommand req)
	{
		var hasModule = req.ModuleId.HasValue;
		var hasLesson = req.LessonId.HasValue;
		if (hasModule == hasLesson) // cả 2 hoặc cả 0
			throw new ValidationException("Phải truyền đúng 1 trong ModuleId hoặc LessonId.");

		return hasModule ? (QuizScope.Module, req.ModuleId!.Value) : (QuizScope.Lesson, req.LessonId!.Value);
	}

	/// <summary>
	/// Check if the question is answered correctly
	/// </summary>
	/// <param name="q"></param>
	/// <param name="chosen"></param>
	/// <returns></returns>
	private static bool IsQuestionCorrect(Question q, IEnumerable<StudentQuizAnswer> chosen)
	{
		var correct = q.Answers.Where(a => a.IsCorrect)
							   .Select(a => a.AnswerId)
							   .OrderBy(x => x)
							   .ToArray();

		var picked = chosen.Where(a => a.QuestionId == q.QuestionId)
						   .Select(a => a.AnswerId)
						   .Distinct()
						   .OrderBy(x => x)
						   .ToArray();

		return correct.SequenceEqual(picked);
	}

	/// <summary>
	/// Get quiz with questions and answers
	/// </summary>
	/// <param name="quizId"></param>
	/// <param name="ct"></param>
	/// <returns></returns>
	private async Task<Quiz?> GetQuizWithDetailsAsync(Guid quizId, CancellationToken ct)
	{
		var query = _quizCommandRepository.Find(
			predicate: q => q.QuizId == quizId && q.IsActive,
			isTracking: false,
			cancellationToken: ct
		);

		// EF Core: Include + ThenInclude + filtered include (Where)
		var quiz = await query
			.Include(q => q.Questions.Where(x => x.IsActive))
				.ThenInclude(q => q.Answers)
			.AsSplitQuery() // khuyến nghị: tránh Cartesian explosion
			.FirstOrDefaultAsync(ct);

		return quiz;
	}

	/// <summary>
	/// Summarize the attempt result
	/// </summary>
	/// <param name="attempt"></param>
	/// <returns></returns>
	private static (int total, int correct, List<QuestionsCourseResultSelectResponseEntity> details) ComputeAttemptResult(Quiz quiz, IEnumerable<StudentQuizAnswer> chosen)
	{
		var details = new List<QuestionsCourseResultSelectResponseEntity>();
		int total = quiz.Questions.Count(q => q.IsActive);
		int correct = 0;

		foreach (var q in quiz.Questions.Where(q => q.IsActive))
		{
			var ans = q.Answers.Select(a => new StudentQuizCourseAnswerDetailResponse
			{
				AnswerId = a.AnswerId,
				AnswerText = a.AnswerText,
				IsCorrectAnswer = a.IsCorrect,
				SelectedByStudent = chosen.Any(sa => sa.QuestionId == q.QuestionId && sa.AnswerId == a.AnswerId)
			}).ToList();

			if (IsQuestionCorrect(q, chosen)) correct++;

			details.Add(new QuestionsCourseResultSelectResponseEntity
			{
				QuestionId = q.QuestionId,
				QuestionText = q.QuestionText,
				QuestionType = q.QuestionType,
				Explanation = q.Explanation,
				Answers = ans
			});
		}
		return (total, correct, details);
	}

	public async Task<StudentCourseQuizSelectResponse> SelectStudentCourseQuizAsync(StudentCourseQuizSelectQuery request)
    {
        var response = new StudentCourseQuizSelectResponse { Success = false };

        var currentUser = _identityService.GetCurrentUser();
        
        var cacheKey = CacheKey.StudentQuizCourse(currentUser!.UserId, request.StudentQuizCourseId);

        // Get student quiz from cache or database
        var studentCourseQuiz = await _studentQuizQueryRepository.GetOrSetAsync(
            cacheKey,
            async () =>
            {
                return await _studentQuizQueryRepository.FirstOrDefaultAsync(x => x.StudentQuizId == request.StudentQuizCourseId && x.IsActive);
            },
            TimeSpan.FromMinutes(10)
        );
        if (studentCourseQuiz == null)
        {
            response.SetMessage(MessageId.E00000, "Không tìm thấy kết quả làm bài kiểm tra");
            return response;
        }

        if (studentCourseQuiz.StudentId != currentUser.UserId)
        {
	        response.SetMessage(MessageId.I00000, "Bạn không có quyền xem kết quả làm bài kiểm tra này");
	        return response;
        }
        
        var questionResults = new List<QuestionsCourseResultSelectResponseEntity>();

        // Get question results for this quiz - including answers and whether student selected them
        foreach (var question in studentCourseQuiz.Quiz.Questions)
        {
            var answerResults = new List<StudentQuizCourseAnswerDetailResponse>();
            foreach (var answer in question.Answers)
            {
                var selectedByStudent = studentCourseQuiz.StudentQuizAnswers.Any(sa => sa.QuestionId == question.QuestionId && sa.AnswerId == answer.AnswerId);
                answerResults.Add(new StudentQuizCourseAnswerDetailResponse
                {
                    AnswerId = answer.AnswerId,
                    IsCorrectAnswer = answer.IsCorrect,
                    SelectedByStudent = selectedByStudent,
                    AnswerText = answer.AnswerText
                });
            }
            questionResults.Add(new QuestionsCourseResultSelectResponseEntity
            {
                QuestionId = question.QuestionId,
                QuestionText = question.QuestionText,
                QuestionType = question.QuestionType,
                Answers = answerResults
            });
        }
            
        // Caculate total correct answers
        var answeredQuestionIds = studentCourseQuiz.StudentQuizAnswers
            .Where(sa => studentCourseQuiz.Quiz.Questions.Any(q => q.QuestionId == sa.QuestionId))
            .Select(sa => sa.QuestionId)
            .Distinct()
            .ToHashSet();
                
        // Count correct answers
        var totalCorrectAnswers = studentCourseQuiz.Quiz.Questions
            .Where(q => answeredQuestionIds.Contains(q.QuestionId))
            .Count(q => studentCourseQuiz.StudentQuizAnswers.Any(sa =>
                sa.QuestionId == q.QuestionId &&
                q.Answers.Any(a => a.AnswerId == sa.AnswerId && a.IsCorrect)
            ));
        
        response.Response = new StudentCourseQuizSelectResponseEntity
        {
            QuizId = studentCourseQuiz.QuizId,
            TotalQuestions = studentCourseQuiz.Quiz.Questions.Count(q => q.IsActive),
            QuestionResults = questionResults,
            TotalCorrectAnswers = totalCorrectAnswers,
        };
        response.SetMessage(MessageId.I00001, "Lấy kết quả làm bài kiểm tra trong khoá học của sinh viên");
        return response;
    }

	/// <summary>
	/// Check student quiz attempt
	/// </summary>
	/// <param name="request"></param>
	/// <param name="cancellationToken"></param>
	/// <returns></returns>
	/// <exception cref="NotImplementedException"></exception>
	public async Task<QuizCourseCheckAttemptResponse> CheckStudentQuizAttemptAsync(QuizCourseCheckAttemptCommand request, CancellationToken cancellationToken)
	{
		var response = new QuizCourseCheckAttemptResponse { Success = false };

		if (request.QuizId == Guid.Empty)
		{
			response.SetMessage(MessageId.E00000, "QuizId không được để trống");
			return response;
		}

		var currentUser = _identityService.GetCurrentUser();

		// Check if student has already attempted the quiz
		var existingAttempt = await _studentQuizCommandRepository
			.Find(sq => sq.QuizId == request.QuizId &&
						sq.StudentId == currentUser!.UserId &&
						sq.IsActive,
				isTracking: false,
				cancellationToken: cancellationToken)
			.FirstOrDefaultAsync(cancellationToken);

		// If no existing attempt, student can take the quiz
		if (existingAttempt == null)
		{
			response.Success = true;
			response.Response = new BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents.QuizCourseCheckAttemptEntity
            {
                CanAttempt = true,
                StudentQuizId = null
			};
			response.SetMessage(MessageId.I00000, "Bạn có thể làm bài kiểm tra này");
			return response;
		}

		// If existing attempt found, student has already taken the quiz
		response.Success = true;
		response.Response = new BuildingBlocks.Messaging.Events.CourseService.QuizCourseCheckAttemptEvents.QuizCourseCheckAttemptEntity
		{
			CanAttempt = false,
			StudentQuizId = existingAttempt.StudentQuizId
		};
		response.SetMessage(MessageId.I00001, "Bạn đã làm bài kiểm tra này");

		return response;
	}
}

