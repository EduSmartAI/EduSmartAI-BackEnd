using AiService.Application.Interfaces;
using BaseService.Common.Utils.Const;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using System.Text;

namespace AiService.Infrastructure.Helpers.TranscriptHelpers
{
	public class CloudinarySubtitlePublisher : ISubtitlePublisher
	{
		public async Task<(string? url, string? publicId)> UploadVttAsync(string lessonId, string vtt, CancellationToken ct)
		{
			//var cloudName = config["Cloudinary:CloudName"]
			var cloudName = Environment.GetEnvironmentVariable(ConstEnv.CloudinaryCloudName);
			//var apiKey = config["Cloudinary:ApiKey"]
			var apiKey = Environment.GetEnvironmentVariable(ConstEnv.CloudApiKey);
			//var apiSecret = config["Cloudinary:ApiSecret"]
			var apiSecret = Environment.GetEnvironmentVariable(ConstEnv.CloudApiSecret);

			if (string.IsNullOrWhiteSpace(cloudName) ||
				string.IsNullOrWhiteSpace(apiKey) ||
				string.IsNullOrWhiteSpace(apiSecret))
				throw new InvalidOperationException("Cloudinary config không đầy đủ. Kiểm tra Cloudinary:CloudName, Cloudinary:ApiKey, Cloudinary:ApiSecret");

			var account = new Account(
				cloudName,
				apiKey,
				apiSecret);
			var cld = new Cloudinary(account);

			var bytes = Encoding.UTF8.GetBytes(vtt);
			using var ms = new MemoryStream(bytes);

			var uploadParams = new RawUploadParams
			{
				File = new FileDescription($"{lessonId}.vtt", ms),
				Folder = "edusmart/subtitles",
				PublicId = lessonId,
				Overwrite = true,
				UseFilename = false,
				UniqueFilename = false
			};

			// Dùng UploadAsync để tải lên tệp nhỏ (.vtt thường là file nhỏ)
			var uploadResult = await cld.UploadAsync(uploadParams);
			//var uploadResult = await cld.UploadLargeAsync(uploadParams)

			return (uploadResult?.SecureUrl?.ToString(), uploadResult?.PublicId);
		}
	}
}
