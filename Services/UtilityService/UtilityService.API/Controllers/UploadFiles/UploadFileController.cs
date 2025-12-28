// using BaseService.Common.Utils;
// using BaseService.Common.Utils.Const;
// using MassTransit;
// using Microsoft.AspNetCore.Authorization;
// using Microsoft.AspNetCore.Mvc;
// using NLog;
// using OpenIddict.Validation.AspNetCore;
// using Shared.Application.Utils.Const;
// using Shared.Common.Utils;
// using UtilityService.Application.Interfaces;
//
// namespace UtilityService.API.Controllers.UploadFiles;
//
// /// <summary>
// /// UploadFileController - Upload file to the server
// /// </summary>
// [ApiController]
// [Route("api/v1/[controller]")]
// public class UploadFileController : ControllerBase
// {
//     private readonly ICloudinaryService _cloudinaryService;
//     private readonly Logger _logger = LogManager.GetCurrentClassLogger();
//
//     /// <summary>
//     /// Constructor
//     /// </summary>
//     /// <param name="cloudinaryService"></param>
//     public UploadFileController(ICloudinaryService cloudinaryService)
//     {
//         _cloudinaryService = cloudinaryService;
//     }
//     
//     /// <summary>
//     /// UploadFile - Upload a file to the server
//     /// </summary>
//     /// <param name="request"></param>
//     /// <returns></returns>
//     [HttpPost("upload")]
//     [ProducesResponseType(typeof(UploadFileResponse), 200)]
//     // [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
//     public async Task<ActionResult<UploadFileResponse>>  UploadFile([FromForm] UploadFileRequest request)
//     {
//         var loggingUtil = new LoggingUtil(_logger, "Admin");
//         loggingUtil.StartLog(request);
//         var response = new UploadFileResponse
//         {
//             Success = false,
//             MessageId = MessageId.E00000,
//             Message = "File upload failed",
//         };
//         try
//         {
//             // Upload the file using the cloudinary service
//             var result = await _cloudinaryService.UploadImageAsync(request.File);
//             if (string.IsNullOrEmpty(result))
//             {
//                 loggingUtil.ErrorLog("File upload failed");
//                 return response;
//             }
//             
//             // Set the response with the file URL
//             response.Response = new UploadFileEntity(
//                 FileUrl: result
//             );
//         }
//         catch (Exception e)
//         {
//             // Log the error and return a 500 status code
//             loggingUtil.ErrorLog(e.Message);
//             return StatusCode(500, response);
//         }
//         finally
//         {
//             // End the logging
//             loggingUtil.EndLog(response);
//         }
//         
//         // True
//         response.Success = true;
//         response.SetMessage(MessageId.I00001, "Upload");
//         return response;
//     }
// }