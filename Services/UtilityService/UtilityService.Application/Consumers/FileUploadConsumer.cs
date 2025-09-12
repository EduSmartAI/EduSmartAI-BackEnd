// using BaseService.Common.Utils;
// using BuildingBlocks.Messaging.Events.UploadFileEvents;
// using MassTransit;
// using Shared.Common.Utils;
// using UtilityService.Application.Interfaces;
//
// namespace UtilityService.Application.Consumers;
//
// public class FileUploadConsumer : IConsumer<FileUploadMessage>
// {
//     private readonly ICloudinaryService _cloudinaryService;
//     private readonly IFileUploadStatusService _fileUploadStatusService;
//     private readonly LoggingUtil _loggingUtil;
//     
//     public Task Consume(ConsumeContext<FileUploadMessage> context)
//     {
//         throw new NotImplementedException();
//     }
// }