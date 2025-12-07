using BaseService.API.BaseControllers;
using BaseService.API.Sse;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningPathCourse.Commands.UpdateLearningPathCourseStatus;
using StudentService.Application.Applications.LearningPaths.Commands;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateLearningPathStatus;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using StudentService.Application.Interfaces;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers;

/// <summary>
/// Constructor
/// </summary>
/// <param name="mediator"></param>
/// <param name="identityService"></param>
/// <param name="httpContextAccessor"></param>
[Route("api/[controller]")]
[ApiController]
public class LearningPathsController(
    IMediator mediator,
    IIdentityService identityService,
    IHttpContextAccessor httpContextAccessor,
    IServerSentEventsService sseService,
    ILearningPathRealtimeNotifier learningPathRealtimeNotifier) : ControllerBase
{
    private readonly IMediator _mediator = mediator;
    private readonly IIdentityService _identityService = identityService;
    private readonly IdentityEntity _identityEntity = default!;
    private readonly Logger _logger = LogManager.GetCurrentClassLogger();
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private readonly IServerSentEventsService _sseService = sseService;
    private readonly ILearningPathRealtimeNotifier _learningPathRealtimeNotifier = learningPathRealtimeNotifier;
    private static readonly TimeSpan SseConnectionLifetime = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Get LearningPath
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet]
    [ProducesResponseType(typeof(LearningPathSelectResponse), StatusCodes.Status200OK)]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy Learning Path (hỗ trợ SSE)",
        Description = "Nếu client gửi Accept: text/event-stream thì server trả SSE realtime, ngược lại trả JSON thông thường."
    )]
    public async Task<ActionResult<LearningPathSelectResponse>> GetLearningPathById([FromQuery] LearningPathSelectsQuery request, CancellationToken cancellationToken)
    {
        if (IsSseRequest())
        {
            if (request.LearningPathId == Guid.Empty)
            {
                return BadRequest("LearningPathId is required for SSE streaming.");
            }

            using var sseTimeoutCts = CreateSseCancellationTokenSource(cancellationToken);

            await _sseService.StreamAsync(Response, async (client, ct) =>
            {
                await client.SendCommentAsync("streaming learning path", ct);

                var payload = await ApiControllerHelper.HandleRequest<LearningPathSelectsQuery, LearningPathSelectResponse, LearningPathSelectDto>(
                    request,
                    _logger,
                    ModelState,
                    async () => await _mediator.Send(request, ct),
                    _identityService,
                    _identityEntity,
                    _httpContextAccessor,
                    new LearningPathSelectResponse());

                await client.SendEventAsync("learning-path", payload, ct);

                if (!payload.Success)
                {
                    await client.SendEventAsync("completed", new { success = false }, ct);
                    return;
                }

                await foreach (var update in _learningPathRealtimeNotifier.SubscribeAsync(request.LearningPathId, ct))
                {
                    await client.SendEventAsync("learning-path", update, ct);
                }

                await client.SendEventAsync("completed", new { success = true }, ct);
            }, sseTimeoutCts.Token);

            return new EmptyResult();
        }

        var response = await ApiControllerHelper.HandleRequest<LearningPathSelectsQuery, LearningPathSelectResponse, LearningPathSelectDto>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request, cancellationToken),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningPathSelectResponse());

        return Ok(response);
    }

    /// <summary>
    /// Rename learning path
    /// </summary>
    [HttpPut("rename")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Đổi tên Learning Path",
        Description = "Chỉ cập nhật PathName của lộ trình."
    )]
    public async Task<LearningPathRenameResponse> RenameLearningPath([FromBody] LearningPathRenameCommand request, CancellationToken cancellationToken)
    {
        return await ApiControllerHelper.HandleRequest<LearningPathRenameCommand, LearningPathRenameResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request, cancellationToken),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningPathRenameResponse());
    }

    /// <summary>
    /// Stream learning path detail by Id using SSE
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("stream-by-id")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Streaming Learning Path theo Id",
        Description = "Sử dụng SSE để lấy Learning PathById (POST body)."
    )]
    public async Task<IActionResult> StreamLearningPathById([FromBody] LearningPathSelectsQuery request, CancellationToken cancellationToken)
    {
        if (request.LearningPathId == Guid.Empty)
        {
            return BadRequest("LearningPathId is required");
        }

        using var sseTimeoutCts = CreateSseCancellationTokenSource(cancellationToken);

        await _sseService.StreamAsync(Response, async (client, ct) =>
        {
            await client.SendCommentAsync("processing learning path", ct);

            var payload = await ApiControllerHelper.HandleRequest<LearningPathSelectsQuery, LearningPathSelectResponse, LearningPathSelectDto>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request, ct),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new LearningPathSelectResponse());

            await client.SendEventAsync("learning-path", payload, ct);

            if (!payload.Success)
            {
                await client.SendEventAsync("completed", new { success = false }, ct);
                return;
            }

            await foreach (var update in _learningPathRealtimeNotifier.SubscribeAsync(request.LearningPathId, ct))
            {
                await client.SendEventAsync("learning-path", update, ct);
            }

            await client.SendEventAsync("completed", new { success = true }, ct);
        }, sseTimeoutCts.Token);

        return new EmptyResult();
    }
    /// <summary>
    /// Update selected courses in learning path
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật các khóa học được chọn trong lộ trình học tập",
        Description = "API này cho phép sinh viên chọn các khóa học mong muốn."
    )]
    public async Task<LearningPathCourseUpdateResponse> UpdateLearningPathCourses([FromBody] LearningPathCourseUpdateCommand request)
    {
        return await ApiControllerHelper.HandleRequest<LearningPathCourseUpdateCommand, LearningPathCourseUpdateResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new LearningPathCourseUpdateResponse());
    }
    /// <summary>
    /// Choosing major
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("choose-major")]
    [SwaggerOperation(
        Summary = "Pick lộ trình chuyên ngành phù hợp",
        Description = ""
    )]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<UpdateStatusLearningPathResponse> UpdateStatusLearningPathById(UpdateStatusLearningPathCommand request)
    {
        return await ApiControllerHelper.HandleRequest<UpdateStatusLearningPathCommand, UpdateStatusLearningPathResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new UpdateStatusLearningPathResponse());
    }
    /// <summary>
    /// Sync data from write-model to read-model
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPost("Sync-data-readmodel")]
    [SwaggerOperation(
        Summary = "Đồng bộ dữ liệu từ write-model sang read-model (BACKEND)",
        Description = "Dùng để đồng bộ dữ liệu từ write-model khi chỉnh data, chỉ dùng cho Backend"
    )]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<UpdateReadModelLearningPathResponse> UpdateStatusLearningPathReadModelById(UpdateReadModelLearningPathCommand request)
    {
        return await ApiControllerHelper.HandleRequest<UpdateReadModelLearningPathCommand, UpdateReadModelLearningPathResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new UpdateReadModelLearningPathResponse());
    }
    /// <summary>
    /// Get all learning path controller
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpGet("get-all")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Lấy tất cả Learning Path",
        Description = "Trả về Learning Path đang có. Cần xác thực Bearer."
    )]
    public async Task<SelectAllLearningPathResponse> GetAllLearningPath([FromQuery] SelectAllLearningPathQuery request)
    {
        return await ApiControllerHelper.HandleRequest<SelectAllLearningPathQuery, SelectAllLearningPathResponse, PaginatedResult<LearningPathSelectAllDto>>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new SelectAllLearningPathResponse());
    }

    /// <summary>
    /// Update course status to Skipped (Student accepts course overload/skip)
    /// </summary>
    /// <param name="request"></param>
    /// <returns></returns>
    [HttpPut("[action]")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
        Summary = "Cập nhật trạng thái khóa học sang Skipped",
        Description = "API cho phép sinh viên chấp nhận học vượt/bỏ qua khóa học trong lộ trình học tập"
    )]
    public async Task<UpdateCourseStatusToSkippedResponse> UpdateCourseStatusToSkipped([FromBody] UpdateCourseStatusToSkippedCommand request)
    {
        return await ApiControllerHelper.HandleRequest<UpdateCourseStatusToSkippedCommand, UpdateCourseStatusToSkippedResponse, string>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new UpdateCourseStatusToSkippedResponse());
    }

    [HttpPost("update-course-status")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    [SwaggerOperation(
    Summary = "Update course status in all learning paths for a user",
    Description = "Internal debug endpoint - userId lấy từ body, không dùng token")]
    public async Task<UpdateLearningPathCourseStatusResponse> UpdateCourseStatus(
    [FromBody] UpdateLearningPathCourseStatusCommand command)
    {
        return await ApiControllerHelper.HandleRequest<
            UpdateLearningPathCourseStatusCommand,
            UpdateLearningPathCourseStatusResponse,
            string>(
            command,
            _logger,
            ModelState,
            async () => await _mediator.Send(command),
            new UpdateLearningPathCourseStatusResponse());
    }

    [HttpPost("[action]")]
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<UpdateLearningPathStatusResponse> UpdateLearningPathStatus([FromBody] UpdateLearningPathStatusCommand request)
    {
        return await ApiControllerHelper.HandleRequest<UpdateLearningPathStatusCommand, UpdateLearningPathStatusResponse, bool>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new UpdateLearningPathStatusResponse());
    }

    private bool IsSseRequest()
    {
        var context = _httpContextAccessor?.HttpContext ?? HttpContext;
        var request = context?.Request;
        if (request == null)
        {
            return false;
        }

        var accept = request.GetTypedHeaders()?.Accept;
        if (accept == null || accept.Count == 0)
        {
            return false;
        }

        return accept.Any(mediaType =>
            mediaType.MediaType.HasValue &&
            mediaType.MediaType.Value.Equals("text/event-stream", StringComparison.OrdinalIgnoreCase));
    }
    
    /// <summary>
    /// Re-generate learning path
    /// </summary>
    /// <returns></returns>
    [HttpPost("[action]")]
    [SwaggerOperation(Summary = "Tạo lại Learning Path, Khi student chưa ưng ý lộ trình học tập vừa tạo", Description = "API này cho phép sinh viên tạo lại lộ trình học tập của mình.")]
    [Authorize(Roles = ConstRole.Student, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<RegenerateLearningPathCommandResponse> ReGenerateLearningPath()
    {
        var request = new RegenerateLearningPathCommand();
        return await ApiControllerHelper.HandleRequest<RegenerateLearningPathCommand, RegenerateLearningPathCommandResponse, Guid?>(
            request,
            _logger,
            ModelState,
            async () => await _mediator.Send(request),
            _identityService,
            _identityEntity,
            _httpContextAccessor,
            new RegenerateLearningPathCommandResponse());
    }

    private CancellationTokenSource CreateSseCancellationTokenSource(CancellationToken cancellationToken)
    {
        var httpAbortToken = HttpContext?.RequestAborted ?? CancellationToken.None;
        var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, httpAbortToken);
        linkedSource.CancelAfter(SseConnectionLifetime);
        return linkedSource;
    }
}

