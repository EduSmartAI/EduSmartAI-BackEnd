using MediatR;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UtilityService.Application.Feature.UploadVideo
{
    public record VideoUploadRequest : IRequest<VideoUploadResponse>
    {
        [Required]
        public IFormFile formFile { get; set; } = default!;
    }
}
