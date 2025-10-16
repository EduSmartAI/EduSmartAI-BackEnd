using AiService.Application.DTOs;
using System.Text;
using System.Text.RegularExpressions;

namespace AiService.Infrastructure.Helpers.TranscriptHelpers
{
	public static class VttConverter
	{
		public static string FromResult(TranscriptResult r)
		{
			var sb = new StringBuilder();
			sb.AppendLine("WEBVTT");
			sb.AppendLine();

			int index = 1;

			if (r.Segments is { Count: > 0 })
			{
				foreach (var s in r.Segments.OrderBy(x => x.Start))
				{
					var text = NormalizeSpaces(s.Text);
					if (string.IsNullOrWhiteSpace(text)) continue;

					sb.AppendLine(index.ToString());
					sb.AppendLine($"{Fmt(s.Start)} --> {Fmt(s.End)}");
					sb.AppendLine(text);
					sb.AppendLine();
					index++;
				}
				return sb.ToString();
			}

			if (r.Words is { Count: > 0 })
			{
				foreach (var w in r.Words.OrderBy(x => x.Start))
				{
					var text = NormalizeSpaces(w.Text);
					if (string.IsNullOrWhiteSpace(text)) continue;

					sb.AppendLine(index.ToString());
					sb.AppendLine($"{Fmt(w.Start)} --> {Fmt(w.End)}");
					sb.AppendLine(text);
					sb.AppendLine();
					index++;
				}
				return sb.ToString();
			}

			return string.Empty;
		}

		static string Fmt(double seconds) => TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss\.fff");
		static string NormalizeSpaces(string s) => string.IsNullOrWhiteSpace(s) ? string.Empty : Regex.Replace(s, @"\s+", " ").Trim();
	}
}
