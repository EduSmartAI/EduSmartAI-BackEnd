using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using StudentService.Application.Applications.LearningPaths.Commands.InsertInternal;
using StudentService.Application.Applications.LearningPaths.Queries;
using StudentService.Application.Applications.LearningPaths.Queries.SelectLearningPaths;

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
        [HttpPost]
        [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        public async Task<InsertInternalLearningPathResponse> InsertInternalLearningPath(InsertInternalLearningPathCommand request)
        {
            return await ApiControllerHelper.HandleRequest<InsertInternalLearningPathCommand, InsertInternalLearningPathResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new InsertInternalLearningPathResponse());
        }
    }
}
