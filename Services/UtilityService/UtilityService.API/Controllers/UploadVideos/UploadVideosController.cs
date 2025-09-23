using BaseService.API.BaseControllers;
using BaseService.Application.Interfaces.IdentityHepers;
using BaseService.Common.Utils.Const;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using OpenIddict.Validation.AspNetCore;
using Swashbuckle.AspNetCore.Annotations;
using UtilityService.Application.Feature.UploadVideo;

namespace UtilityService.API.Controllers.UploadVideos
{
    [ApiController]
    [Route("api/v1/[controller]")]
    public class UploadVideosController : ControllerBase
    {
        private readonly IMediator _mediator;
        private readonly IIdentityService _identityService;
        private readonly IdentityEntity _identityEntity;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly IHttpContextAccessor _httpContextAccessor;
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="mediator"></param>
        /// <param name="identityService"></param>
        /// <param name="httpContextAccessor"></param>
        public UploadVideosController(IMediator mediator, IIdentityService identityService, IHttpContextAccessor httpContextAccessor)
        {
            _mediator = mediator;
            _identityService = identityService;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Upload Video and publish to RabbitMQ for asyncronus
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = ConstRole.Lecturer, AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
        [Consumes("multipart/form-data")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = 2L * 1024 * 1024 * 1024)] // 2GB
        [SwaggerOperation(Summary = "Upload video")]
        public async Task<VideoUploadResponse> UploadVideo(VideoUploadRequest request)
        {
            return await ApiControllerHelper.HandleRequest<VideoUploadRequest, VideoUploadResponse, string>(
                request,
                _logger,
                ModelState,
                async () => await _mediator.Send(request),
                _identityService,
                _identityEntity,
                _httpContextAccessor,
                new VideoUploadResponse());
        }
    }
}
