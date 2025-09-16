using MassTransit;
using UtilityService.Application.Contracts;
using UtilityService.Application.Interfaces;

namespace UtilityService.Application.Consumers.UploadVideo
{
    public class UploadVideoRequestedConsumer : IConsumer<UploadVideoRequested>
    {
        private readonly ICloudinaryService _cloudinary;
        public UploadVideoRequestedConsumer(ICloudinaryService cloudinary) => _cloudinary = cloudinary;

        public async Task Consume(ConsumeContext<UploadVideoRequested> ctx)
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromHours(2));
            await _cloudinary.UploadVideoAsync(ctx.Message.TempPath, ctx.Message.PublicId, cts.Token);
            try
            {
                File.Delete(ctx.Message.TempPath);
            }
            catch
            {

            }
        }
    }
}
