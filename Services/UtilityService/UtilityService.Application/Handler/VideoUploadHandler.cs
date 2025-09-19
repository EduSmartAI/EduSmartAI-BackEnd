using BaseService.Application.Interfaces.Repositories;
using MassTransit;
using MediatR;
using UtilityService.Application.Contracts;
using UtilityService.Application.Feature.UploadVideo;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Handler
{
    public class VideoUploadHandler : IRequestHandler<VideoUploadRequest, VideoUploadResponse>
    {
        private readonly IPublishEndpoint _requestPublishEndpoint;
        private readonly ICommandRepository<CloudinaryConfig> _cloudinaryConfigRepository;

        public VideoUploadHandler(IPublishEndpoint requestPublishEndpoint, ICommandRepository<CloudinaryConfig> cloudinaryConfigRepository)
        {
            _requestPublishEndpoint = requestPublishEndpoint;
            _cloudinaryConfigRepository = cloudinaryConfigRepository;
        }

        public async Task<VideoUploadResponse> Handle(VideoUploadRequest request, CancellationToken ct)
        {
            var cloudinaryKey = await _cloudinaryConfigRepository.FirstOrDefaultAsync(x => x.IsActive);
            if (cloudinaryKey == null)
            {
                throw new Exception("Cloudinary configuration not found");
            }
            var fileName = request.formFile.FileName;
            var publicId = $"{Guid.NewGuid():N}";
            var cloudName = cloudinaryKey.CloudApiName;
            var hlsUrl = $"https://res.cloudinary.com/{cloudName}/video/upload/sp_auto:maxres_2160p/{publicId}.m3u8";

            // 2) Save temp file and publish to RabbitMQ
            var tmp = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}{Path.GetExtension(fileName)}");
            await using (var fs = File.Create(tmp))
                await request.formFile.CopyToAsync(fs);

            await _requestPublishEndpoint.Publish(new UploadVideoRequested(
                TempPath: tmp,
                PublicId: publicId,
                FileName: fileName));

            return new VideoUploadResponse
            {
                Success = true,
                Message = "Uploaded successfully",
                Response = hlsUrl
            };
        }
    }
}
