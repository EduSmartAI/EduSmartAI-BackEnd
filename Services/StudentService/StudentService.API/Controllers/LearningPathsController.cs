using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using BuildingBlocks.Pagination;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningPathCourse.Commands.UpdateLearningPathCourseStatus;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourseStatusToSkipped;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateReadModel;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateStatusLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectAllLearningPath;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;
using Swashbuckle.AspNetCore.Annotations;

namespace StudentService.API.Controllers
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="mediator"></param>
    /// <param name="identityService"></param>
    /// <param name="httpContextAccessor"></param>
    [Route("api/[controller]")]
    [ApiController]
    public class LearningPathsController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor) : ControllerBase
    {
        private readonly IMediator _mediator = mediator;
        private readonly IIdentityService _identityService = identityService;
        private readonly IdentityEntity _identityEntity;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

        /// <summary>
        /// Get LearningPath
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [SwaggerOperation(
            Summary = "Lấy Learning Path",
            Description = "Trả về Learning Path theo tham số query. Cần xác thực Bearer."
        )]
        public async Task<LearningPathSelectResponse> GetLearningPathById([FromQuery] LearningPathSelectsQuery request)
        {
            return await ApiControllerHelper.HandleRequest<LearningPathSelectsQuery, LearningPathSelectResponse, LearningPathSelectDto>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new LearningPathSelectResponse());
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
	}
}
