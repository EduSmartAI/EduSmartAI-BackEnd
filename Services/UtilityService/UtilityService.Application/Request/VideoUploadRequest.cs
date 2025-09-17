using MediatR;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using UtilityService.Application.Response;

namespace UtilityService.Application.Request
{
    public record VideoUploadRequest : IRequest<VideoUploadResponse>
    {
        [Required]
        public IFormFile formFile { get; set; } = default!;
    }
}
