using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.ApiEntities;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using QuizService.Application.Applications.Admin.Queries.PracticeTests;
using QuizService.Application.Applications.Admin.Queries.StudentSurveys;
using QuizService.Application.Applications.Admin.Queries.StudentTests;
using QuizService.Application.Applications.Admin.Queries.Surveys;
using QuizService.Application.Applications.Admin.Queries.Tests;
using QuizService.Application.Applications.PracticeTest;
using QuizService.Application.Applications.StudentTests.Queries;
using QuizService.Application.Applications.Surveys.Commands;
using QuizService.Application.Applications.Tests.Commands;
using QuizService.Application.Applications.Tests.Queries;
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

    // /// <summary>
    // /// Create new placement test
    // /// </summary>
    // /// <param name="request"></param>
    // /// <returns></returns>
    // [HttpPost("[action]")]
    // [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    // [SwaggerOperation(Summary = "Tạo bài kiểm tra đầu vào mới", Description = "Cần cấp quyền Admin cho API")]
    // public async Task<TestInsertResponse> InsertTest(TestInsertCommand request)
    // {
    //     return await ApiControllerHelper.HandleRequest<TestInsertCommand, TestInsertResponse, string>(
    //         request,
    //         _logger,
    //         ModelState,
    //         async () => await mediator.Send(request),
    //         identityService,
    //         _identityEntity,
    //         httpContextAccessor,
    //         new TestInsertResponse());
    // }
    
    /// <summary>
    /// Add quizzes to existing test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Thêm quiz vào bài kiểm tra đầu vào", Description = "Thêm một hoặc nhiều quiz vào bài test đã tồn tại. Cần cấp quyền Admin cho API")]
    public async Task<TestQuizInsertResponse> InsertTestQuiz([FromBody] TestQuizInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestQuizInsertCommand, TestQuizInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new TestQuizInsertResponse());
    }
    
    /// <summary>
    /// Delete quiz from test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Xóa quiz khỏi bài test", Description = "Xóa (soft delete) một quiz khỏi bài test. Cần cấp quyền Admin cho API")]
    public async Task<TestQuizDeleteResponse> DeleteTestQuiz([FromBody] TestQuizDeleteCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestQuizDeleteCommand, TestQuizDeleteResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new TestQuizDeleteResponse());
    }
    
    /// <summary>
    /// Add questions to quiz in test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Thêm câu hỏi vào quiz", Description = "Thêm một hoặc nhiều câu hỏi vào quiz trong bài test. Cần cấp quyền Admin cho API")]
    public async Task<TestQuizQuestionsInsertResponse> InsertTestQuizQuestions([FromBody] TestQuizQuestionsInsertCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestQuizQuestionsInsertCommand, TestQuizQuestionsInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new TestQuizQuestionsInsertResponse());
    }
    
    /// <summary>
    /// Delete multiple questions from quiz
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Xóa nhiều câu hỏi khỏi quiz", Description = "Xóa (soft delete) một hoặc nhiều câu hỏi khỏi quiz. Cần cấp quyền Admin cho API")]
    public async Task<TestQuizQuestionsDeleteResponse> DeleteTestQuizQuestions([FromBody] TestQuizQuestionsDeleteCommand request)
    {
        return await ApiControllerHelper.HandleRequest<TestQuizQuestionsDeleteCommand, TestQuizQuestionsDeleteResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new TestQuizQuestionsDeleteResponse());
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
    /// Get all surveys with pagination and filters
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách tất cả surveys", Description = "Hỗ trợ phân trang và lọc theo SurveyTypeId, SurveyCode, tìm kiếm theo Title. Trả về thông tin chi tiết bao gồm Questions và Answers. Cần cấp quyền Admin cho API")]
    public async Task<AdminSurveysSelectResponse> SelectSurveys([FromQuery] AdminSurveysSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminSurveysSelectQuery, AdminSurveysSelectResponse, AdminSurveysSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminSurveysSelectResponse());
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
    /// Get detail of a specific student survey
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Chi tiết một khảo sát của sinh viên", Description = "Lấy toàn bộ thông tin chi tiết của một khảo sát bao gồm câu hỏi và câu trả lời của sinh viên")]
    public async Task<AdminStudentSurveySelectDetailResponse> SelectStudentSurveyDetail([FromQuery] AdminStudentSurveySelectDetailQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminStudentSurveySelectDetailQuery, AdminStudentSurveySelectDetailResponse, AdminStudentSurveySelectDetailResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminStudentSurveySelectDetailResponse());
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
    // [HttpGet("[action]")]
    // [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    // [SwaggerOperation(Summary = "Lấy danh sách tất cả quiz/survey", Description = "Hỗ trợ phân trang và lọc theo QuizType, SubjectCode, SurveyCode")]
    // public async Task<AdminQuizzesSelectResponse> SelectQuizzes([FromQuery] AdminQuizzesSelectQuery request)
    // {
    //     return await ApiControllerHelper.HandleRequest<AdminQuizzesSelectQuery, AdminQuizzesSelectResponse, AdminQuizzesSelectResponseEntity>(
    //         request,
    //         _logger,
    //         ModelState,
    //         async () => await mediator.Send(request),
    //         identityService,
    //         _identityEntity,
    //         httpContextAccessor,
    //         new AdminQuizzesSelectResponse());
    // }
    
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
    
    /// <summary>
    /// Update existing practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Cập nhật bài kiểm tra thực hành", Description = "Cập nhật thông tin bài kiểm tra thực hành bao gồm: tiêu đề, mô tả, độ khó, test cases, templates và examples. Chỉ cập nhật/xóa items có ID trong request. Items không có trong request sẽ bị xoá. Phải giữ ít nhất 1 test case và 1 example. Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminUpdateResponse> UpdatePracticeTest([FromBody] PracticeTestAdminUpdateRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminUpdateRequest, PracticeTestAdminUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminUpdateResponse());
    }
    
    /// <summary>
    /// Delete practice test (soft delete)
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpDelete("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Xóa bài tập thực hành", Description = "Xóa bài tập thực hành. Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminDeleteResponse> DeletePracticeTest([FromBody] PracticeTestAdminDeleteRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminDeleteRequest, PracticeTestAdminDeleteResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminDeleteResponse());
    }
    
    /// <summary>
    /// Add testcases to practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Thêm test cases vào bài tập thực hành", Description = "Thêm public và private test cases vào bài tập thực hành đã tồn tại. Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminTestcasesInsertResponse> InsertPracticeTestTestcases([FromBody] PracticeTestAdminTestcasesInsertRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminTestcasesInsertRequest, PracticeTestAdminTestcasesInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminTestcasesInsertResponse());
    }
    
    /// <summary>
    /// Add templates to practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Thêm code templates vào bài tập thực hành", Description = "Thêm code templates cho các ngôn ngữ lập trình vào bài tập thực hành đã tồn tại. Mỗi ngôn ngữ chỉ có thể có 1 template. Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminTemplatesInsertResponse> InsertPracticeTestTemplates([FromBody] PracticeTestAdminTemplatesInsertRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminTemplatesInsertRequest, PracticeTestAdminTemplatesInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminTemplatesInsertResponse());
    }
    
    /// <summary>
    /// Add examples to practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Thêm ví dụ vào bài tập thực hành", Description = "Thêm các ví dụ minh họa input/output vào bài tập thực hành đã tồn tại. Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminExamplesInsertResponse> InsertPracticeTestExamples([FromBody] PracticeTestAdminExamplesInsertRequest request)
    {
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminExamplesInsertRequest, PracticeTestAdminExamplesInsertResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminExamplesInsertResponse());
    }
    
    /// <summary>
    /// Import programming languages from Judge0
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Đồng bộ ngôn ngữ lập trình từ Judge0 (Không spam API này)", Description = "Lấy danh sách ngôn ngữ lập trình từ Judge0 API và thêm các ngôn ngữ mới vào hệ thống. Các ngôn ngữ đã tồn tại sẽ được bỏ qua. (Không spam API này). Cần cấp quyền Admin cho API")]
    public async Task<PracticeTestAdminLanguageInsertResponse> InsertPracticeLanguage()
    {
        var request = new PracticeTestAdminLanguageInsertRequest();
        return await ApiControllerHelper.HandleRequest<PracticeTestAdminLanguageInsertRequest, PracticeTestAdminLanguageInsertResponse, PracticeTestAdminLanguageInsertResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new PracticeTestAdminLanguageInsertResponse());
    }
    
    /// <summary>
    /// Get all practice tests for management
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Lấy danh sách tất cả bài kiểm tra thực hành", Description = "Hỗ trợ phân trang và lọc theo độ khó, tìm kiếm theo tiêu đề. Cần cấp quyền Admin cho API")]
    public async Task<AdminPracticeTestsSelectResponse> SelectPracticeTests([FromQuery] AdminPracticeTestsSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminPracticeTestsSelectQuery, AdminPracticeTestsSelectResponse, AdminPracticeTestsSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminPracticeTestsSelectResponse());
    }
    
    /// <summary>
    /// Get detail of a specific practice test
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Chi tiết một bài kiểm tra thực hành", Description = "Lấy toàn bộ thông tin chi tiết bao gồm test cases, templates, examples. Cần cấp quyền Admin cho API")]
    public async Task<AdminPracticeTestSelectResponse> SelectPracticeTest([FromQuery] AdminPracticeTestSelectQuery request)
    {
        return await ApiControllerHelper.HandleRequest<AdminPracticeTestSelectQuery, AdminPracticeTestSelectResponse, AdminPracticeTestSelectResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminPracticeTestSelectResponse());
    }
    
    /// <summary>
    /// Get detail of a specific placement test
    /// </summary>
    /// <returns></returns>
    [HttpGet("[action]")]
    [Authorize(Roles = ConstRole.Admin, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(Summary = "Chi tiết một bài kiểm tra đầu vào", Description = "Lấy toàn bộ thông tin chi tiết của một bài kiểm tra đầu vào cho sinh viên")]
    public async Task<AdminSelectPlacementTestQueryResponse> SelectPlacementTestDetail()
    {
        var request = new AdminSelectPlacementTestQuery();
        return await ApiControllerHelper.HandleRequest<AdminSelectPlacementTestQuery, AdminSelectPlacementTestQueryResponse, AdminSelectPlacementTestQueryResponseEntity>(
            request,
            _logger,
            ModelState,
            async () => await mediator.Send(request),
            identityService,
            _identityEntity,
            httpContextAccessor,
            new AdminSelectPlacementTestQueryResponse());
    }
}