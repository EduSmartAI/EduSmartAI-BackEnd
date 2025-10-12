using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningPaths.Commands.UpdateCourses;
using StudentService.Application.Applications.LearningPaths.Queries;
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
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
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
    }
}
