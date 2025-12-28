using System.ComponentModel.DataAnnotations;

namespace UtilityService.API.Controllers.UploadFiles;

public record UploadFileRequest
{
    [Required(ErrorMessage = "File is required")]
    public IFormFile File { get; set; } = null!;
}