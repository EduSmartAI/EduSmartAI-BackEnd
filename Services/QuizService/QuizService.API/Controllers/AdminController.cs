using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Admin.Queries.Quizzes;
using QuizService.Application.Applications.Admin.Queries.StudentSurveys;
using QuizService.Application.Applications.Admin.Queries.StudentTests;
using QuizService.Application.Applications.PracticeTest;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Tests.Commands;
using Swashbuckle.AspNetCore.Annotations;

namespace QuizService.API.Controllers;

/// <summary>
/// Admin controller for managing quizzes, surveys, and student submissions
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AdminController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor) : ControllerBase
{
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IdentityEntity _identityEntity;

    /// <summary>
    /// Create new placement test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Tạo bài kiểm tra đầu vào mới", Description = "Cần cấp quyền Admin cho API")]
    public async Task<TestInsertResponse> InsertTest(TestInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestInsertCommand, TestInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new TestInsertResponse());
    }

    /// <summary>
    /// Create new survey
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Tạo khảo sát mới", Description = "Tạo khảo sát mới với các câu hỏi và câu trả lời tương ứng")]
    public async Task<SurveyInsertResponse> InsertSurvey(SurveyInsertCommand request)
    {
        var response = new SurveyInsertResponse { Success = false };
        var detailErrors = new List<DetailError>();

        // Validate that each question of type 2 (multiple choice) has at least one answer
        foreach (var question in request.Questions)
        {
            if (!question.Answers.Any())
            {
                var detailEror = new DetailError();
                detailEror.SetMessage(MessageId.E10000);
                detailEror.ErrorMessage = "Câu hỏi loại trắc nghiệm phải có ít nhất một câu trả lời";
                detailErrors.Add(detailEror);

                response.SetMessage(MessageId.E10000);
                response.DetailErrors = detailErrors;
                return response;
            }
        }

        // Call the helper to handle the request
        response = await ApiControllerHelper.HandleRequest<SurveyInsertCommand, SurveyInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new SurveyInsertResponse());
        return response;
    }

    /// <summary>
    /// Get all student surveys with pagination and filters
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách tất cả khảo sát của học sinh", Description = "Hỗ trợ phân trang và lọc theo StudentId hoặc SurveyId")]
    public async Task<AdminStudentSurveysSelectResponse> SelectStudentSurveys([FromQuery] AdminStudentSurveysSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminStudentSurveysSelectQuery, AdminStudentSurveysSelectResponse, AdminStudentSurveysSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminStudentSurveysSelectResponse());
    }

    /// <summary>
    /// Get all student tests with pagination and filters
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách tất cả bài kiểm tra của học sinh", Description = "Hỗ trợ phân trang và lọc theo StudentId hoặc TestId")]
    public async Task<AdminStudentTestsSelectResponse> SelectStudentTests([FromQuery] AdminStudentTestsSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminStudentTestsSelectQuery, AdminStudentTestsSelectResponse, AdminStudentTestsSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminStudentTestsSelectResponse());
    }
    
    /// <summary>
    /// Get detail of a specific student test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Chi tiết một bài kiểm tra đầu vào của sinh viên")]
    public async Task<AdminStudentTestSelectDetailResponse> SelectStudentTestDetail([FromQuery] AdminStudentTestSelectDetailQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminStudentTestSelectDetailQuery, AdminStudentTestSelectDetailResponse, StudentTestSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminStudentTestSelectDetailResponse());
    }

    /// <summary>
    /// Get all quizzes/surveys for management
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin,
        AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy danh sách tất cả quiz/survey",
        Description = "Hỗ trợ phân trang và lọc theo QuizType, SubjectCode, SurveyCode"
    )]
    public async Task<AdminQuizzesSelectResponse> SelectQuizzes([FromQuery] AdminQuizzesSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminQuizzesSelectQuery, AdminQuizzesSelectResponse, AdminQuizzesSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminQuizzesSelectResponse());
    }
    
    /// <summary>
    /// Create new practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Tạo bài tập thực hành mới", Description = "Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminInsertResponse> InsertPracticeTest([FromBody] PracticeTestAdminInsertRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminInsertRequest, PracticeTestAdminInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminInsertResponse());
    }
}