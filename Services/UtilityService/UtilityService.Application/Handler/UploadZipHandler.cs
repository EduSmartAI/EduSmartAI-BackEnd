using BaseService.Application.Interfaces.Repositories;
using MediatR;
using UtilityService.Application.Feature.UploadZip;
using UtilityService.Application.Interfaces;
using UtilityService.Domain.Models;

namespace UtilityService.Application.Handler
{
    public class UploadZipHandler : IRequestHandler<UploadZipRequest, UploadZipResponse>
    {
        private readonly ICommandRepository<CloudinaryConfig> _cloudinaryConfigRepository;
        private readonly ICloudinaryService _cloudinaryService;

        public UploadZipHandler(
            ICommandRepository<CloudinaryConfig> cloudinaryConfigRepository,
            ICloudinaryService cloudinaryService
            )
        {
            _cloudinaryConfigRepository = cloudinaryConfigRepository;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<UploadZipResponse> Handle(UploadZipRequest request, CancellationToken ct)
        {
            if (request.formFile == null || request.formFile.Length == 0)
                throw new ArgumentException("Thiếu file upload.");

            var fileName = request.formFile.FileName ?? string.Empty;
            if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("File phải có đuôi .zip.");

            var secureUrl = await _cloudinaryService.UploadZipAsync(
                request.formFile,
                ct
            );
            return new UploadZipResponse
            {
                Success = true,
                Message = "Uploaded successfully",
                Response = secureUrl
            };
        }
    }
}
