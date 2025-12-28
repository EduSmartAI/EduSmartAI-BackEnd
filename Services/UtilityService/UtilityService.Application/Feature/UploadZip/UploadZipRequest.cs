using MediatR;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace UtilityService.Application.Feature.UploadZip
{
    public record UploadZipRequest : IRequest<UploadZipResponse>
    {
        [Required]
        public IFormFile formFile { get; set; } = default!;

    }
}
