namespace AiService.Infrastructure.Helpers.TranscriptHelpers
{
	public static class CloudinaryAudio
	{
		public static bool TryParseVideoUrl(string url, out string cloud, out string versionedIdNoExt)
		{
			cloud = ""; versionedIdNoExt = "";
			if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return false;
			var segs = u.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
			if (segs.Length < 5 || segs[1] != "video" || segs[2] != "upload") return false;
			cloud = segs[0];
			var version = segs[3];
			var file = string.Join('/', segs.Skip(4));
			var dot = file.LastIndexOf('.');
			var fileNoExt = dot > 0 ? file[..dot] : file;
			versionedIdNoExt = $"{version}/{fileNoExt}";
			return true;
		}

		public static string BuildAudioUrl(string cloud, string versionedIdNoExt)
			=> $"https://res.cloudinary.com/{cloud}/video/upload/af_16000,ac_mp3,br_24k/{versionedIdNoExt}.mp3";

		public static string BuildAudioChunkUrl(string cloud, string versionedIdNoExt, int startSec, int durSec)
			=> $"https://res.cloudinary.com/{cloud}/video/upload/af_16000,ac_mp3,br_24k,so_{startSec},du_{durSec}/{versionedIdNoExt}.mp3";
	}
}
