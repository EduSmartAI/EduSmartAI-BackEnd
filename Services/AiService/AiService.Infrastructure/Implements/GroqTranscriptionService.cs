using AiService.Application.Contracts;
using AiService.Application.DTOs;
using AiService.Application.Handler.Transcripts.Commands.CreateTranscript;
using AiService.Application.Interfaces;
using AiService.Infrastructure.Helpers.TranscriptHelpers;
using BaseService.Common.Utils.Const;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace AiService.Infrastructure.Implements
{
	public class GroqTranscriptionService(
		ISubtitlePublisher subtitlePublisher) : ITranscriptionService
	{
		const string GroqUrl = "https://api.groq.com/openai/v1/audio/transcriptions";
		const string Model = "whisper-large-v3-turbo";

		/// <summary>
		/// Processes a transcription job asynchronously, converting audio from the specified video URL into text.
		/// </summary>
		/// <remarks>This method attempts to transcribe audio from the provided video URL. It first tries to process
		/// the entire audio file. If that fails, it falls back to processing the audio in smaller chunks. The resulting
		/// transcription, including text, segments, and word-level details, is saved and published if the operation
		/// succeeds.</remarks>
		/// <param name="job">The transcription job containing details such as the video URL, language, and lesson ID.</param>
		/// <param name="ct">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
		/// <returns>A <see cref="CreateTranscriptResponse"/> indicating whether the transcription was successful, along with any
		/// relevant messages.</returns>
		/// <exception cref="InvalidOperationException">Thrown if the API key is missing or invalid, or if the video URL is not a valid Cloudinary URL.</exception>
		public async Task<CreateTranscriptResponse> ProcessAsync(TranscribeJob job, CancellationToken ct)
		{
			var response = new CreateTranscriptResponse { Success = false };
			using var http = new HttpClient();

			// Lấy API key từ .env
			var apiKey = Environment.GetEnvironmentVariable(ConstEnv.GroqAIVoiceToText)?.Trim();
			if (string.IsNullOrWhiteSpace(apiKey))
			{
				response.SetMessage(MessageId.E11006, "Groq API key không có. Kiểm tra GroqAI:ApiKey hoặc env.");
				return response;
			}

			http.DefaultRequestHeaders.Authorization =
				new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

			// Validate url
			if (!CloudinaryAudio.TryParseVideoUrl(job.VideoUrl!, out var cloud, out var versionedIdNoExt))
			{
				response.SetMessage(MessageId.E11006, "URL Cloudinary không hợp lệ: " + job.VideoUrl);
				return response;
			}

			// 1) Thử nén 1 phát
			//var audioUrl = CloudinaryAudio.BuildAudioUrl(cloud, versionedIdNoExt)
			var videoUrlMp4 = CloudinaryAudio.BuildMp3Url(cloud, versionedIdNoExt);
			var attempt = await TryTranscribeUrlOnce(http, job, videoUrlMp4, 0, ct);
			if (attempt.succeeded && attempt.result is not null)
			{
				await SaveAndPublish(job, attempt.result, ct);

				response.Success = true;
				response.SetMessage(MessageId.I00001, "Đã chuyển văn bản thành công.");
				response.Response = attempt.result;

				return response;
			}

			#region Comment
			// 2) Fallback: chunk 60s
			//var duration = job.DurationSec ?? 3600;
			//var totalText = new StringBuilder();
			//var allSegs = new List<Segment>();
			//var allWords = new List<Word>();

			//int start = 0, safety = 0;
			//while (start < duration + 1 && safety < 10000)
			//{
			//	safety++;
			//	var chunkUrl = CloudinaryAudio.BuildAudioChunkUrl(cloud, versionedIdNoExt, start, ChunkSeconds);
			//	var res = await CallGroqVerboseJson(http, job.Language, chunkUrl, ct);

			//	if (!res.success)
			//	{
			//		log.LogWarning("Chunk at {start}s failed: {err}", start, res.error);
			//		if (res.retriableErrors >= 3) break;
			//		start += ChunkSeconds;
			//		continue;
			//	}

			//	var offset = (double)start;
			//	var text = res.parsed?.Text ?? "";
			//	totalText.Append(' ').Append(text);

			//	if (res.parsed?.Segments is not null)
			//		allSegs.AddRange(res.parsed.Segments.Select(s => new Segment { Start = s.Start + offset, End = s.End + offset, Text = s.Text }));
			//	if (res.parsed?.Words is not null)
			//		allWords.AddRange(res.parsed.Words.Select(w => new Word { Start = w.Start + offset, End = w.End + offset, Text = w.Word }));

			//	start += ChunkSeconds;
			//}

			//var merged = new TranscriptResult
			//{
			//	LessonId = job.LessonId,
			//	Language = job.Language,
			//	Text = totalText.ToString().Trim(),
			//	Segments = allSegs,
			//	Words = allWords
			//};

			//if (await SaveAndPublish(job, merged, ct))
			//{
			//	response.Success = true;
			//	response.SetMessage(MessageId.I00001, "Đã chuyển văn bản thành công.");
			//	response.Response = merged;
			//}
			//else
			//{
			//	response.SetMessage(MessageId.E10000, "Lưu kết quả thất bại.");
			//}
			#endregion

			response.SetMessage(MessageId.E00000, "Chuyển văn bản thất bại sau nhiều lần thử.");
			return response;
		}

		/// <summary>
		/// help transcribe a single URL once
		/// giúp chuyển văn bản một URL một lần
		/// </summary>
		/// <param name="http"></param>
		/// <param name="job"></param>
		/// <param name="url"></param>
		/// <param name="offset"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		async Task<(bool succeeded, bool isSizeTooLarge, TranscriptResult? result)> TryTranscribeUrlOnce(
		HttpClient http, TranscribeJob job, string url, double offset, CancellationToken ct)
		{
			var res = await http.PostAsync(GroqUrl, BuildForm(job.Language, url), ct);
			var body = await res.Content.ReadAsStringAsync(ct);

			if (!res.IsSuccessStatusCode)
			{
				var tooLarge = body.Contains("media file too large", StringComparison.OrdinalIgnoreCase);
				if (!tooLarge)
				{
					//await store.Upsert(new TranscriptResult
					//{
					//	LessonId = job.LessonId,
					//	Status = "failed",
					//	Error = $"{(int)res.StatusCode} {res.ReasonPhrase}: {body}"
					//});
				}
				return (false, tooLarge, null);
			}

			var parsed = JsonSerializer.Deserialize<GroqVerboseJson>(body);

			List<Segment>? segs = null;
			List<Word>? words = null;

			if (parsed?.Segments is { Count: > 0 })
			{
				segs = parsed.Segments.Select(s => new Segment
				{
					Start = s.Start + offset,
					End = s.End + offset,
					Text = s.Text
				}).ToList();
			}
			if (parsed?.Words is { Count: > 0 })
			{
				words = parsed.Words.Select(w => new Word
				{
					Start = w.Start + offset,
					End = w.End + offset,
					Text = w.Word
				}).ToList();
			}

			var hasTs = segs is { Count: > 0 } || words is { Count: > 0 };
			if (!hasTs && string.IsNullOrWhiteSpace(parsed?.Text))
				return (false, false, null);

			var result = new TranscriptResult
			{
				LessonId = job.LessonId,
				Language = job.Language,
				Text = parsed?.Text ?? "",
				Segments = segs,
				Words = words
			};

			return (true, false, result);
		}

		static MultipartFormDataContent BuildForm(string language, string url)
		{
			var form = new MultipartFormDataContent();
			form.Add(new StringContent(Model), "model");
			form.Add(new StringContent(language), "language");
			form.Add(new StringContent("verbose_json"), "response_format");
			form.Add(new StringContent("segment"), "timestamp_granularities[]");
			form.Add(new StringContent("word"), "timestamp_granularities[]");
			form.Add(new StringContent(url), "url");
			return form;
		}

		//async Task<(bool success, int retriableErrors, GroqVerboseJson? parsed, string? error)> CallGroqVerboseJson(
		//HttpClient http, string language, string url, CancellationToken ct)
		//{
		//	var res = await http.PostAsync(GroqUrl, BuildForm(language, url), ct);
		//	var body = await res.Content.ReadAsStringAsync(ct);
		//	if (!res.IsSuccessStatusCode)
		//	{
		//		var retriable = body.Contains("media file too large", StringComparison.OrdinalIgnoreCase)
		//						|| (int)res.StatusCode == 429 || (int)res.StatusCode >= 500;
		//		return (false, retriable ? 1 : 3, null, body);
		//	}
		//	var parsed = JsonSerializer.Deserialize<GroqVerboseJson>(body);
		//	return (true, 0, parsed, null);
		//}

		async Task<bool> SaveAndPublish(TranscribeJob job, TranscriptResult result, CancellationToken ct)
		{
			// Publish VTT nếu có timestamps
			string? vttUrl = null, vttPid = null;
			var vtt = VttConverter.FromResult(result);
			if (!string.IsNullOrWhiteSpace(vtt))
			{
				var up = await subtitlePublisher.UploadVttAsync(job.LessonId, vtt, ct);
				vttUrl = up.url; vttPid = up.publicId;
			}

			result.Status = "succeeded";
			result.VttUrl = vttUrl;
			result.VttPublicId = vttPid;

			//await store.Upsert(result)
			return true;

		}
	}
}
