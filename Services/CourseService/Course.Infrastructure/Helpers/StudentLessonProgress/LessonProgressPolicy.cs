namespace Course.Infrastructure.Helpers.StudentLessonProgress
{
	public static class LessonProgressPolicy
	{
		public const double CompletePosPct = 0.90; // 90% vị trí
		public const double CompleteWatchPct = 0.85; // 85% thời lượng xem

		public static bool ShouldCompleteHardened(int videoDurationSec, int? lastPositionSec, int watchedSec,
												  double posPct = CompletePosPct, double watchPct = CompleteWatchPct)
		{
			if (videoDurationSec <= 0) return false;

			var pos = Math.Max(0, lastPositionSec ?? 0);
			var posOk = pos >= Math.Floor(videoDurationSec * posPct);
			var watch = Math.Max(0, watchedSec);
			var watchOk = watch >= Math.Floor(videoDurationSec * watchPct);

			// Mềm: posOk || watchOk; chặt: posOk && watchOk
			return posOk || watchOk;
		}

		public static bool ShouldCompleteSimple(int videoDurationSec, int? lastPositionSec, double posPct = CompletePosPct)
		{
			if (videoDurationSec <= 0) return false;
			var pos = Math.Max(0, lastPositionSec ?? 0);
			return pos >= Math.Floor(videoDurationSec * posPct);
		}
	}
}
