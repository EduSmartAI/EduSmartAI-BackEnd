using Course.Application.DTOs.CoursesDTO;
using Course.Application.DTOs.CoursesDTO.CourseStudentDTO;
using Course.Application.DTOs.LessonsDTO;
using Course.Application.DTOs.LessonsDTO.LessonStudentDTO;
using Course.Application.DTOs.ModulesDTO;
using Course.Application.DTOs.ModulesDTO.ModuleDiscussionDTO;
using Course.Application.DTOs.ModulesDTO.ModuleMaterialDTO;
using Course.Application.DTOs.ModulesDTO.ModuleStudentDTO;
using Course.Application.DTOs.QuizDTO;
using Course.Application.Interfaces.Helpers;
using static BaseService.Common.Utils.Const.ConstantEnum;

namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class CourseMapper : ICourseMapper
	{
		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public CourseDetailForGuestDto MapCourseDetailForGuest(CourseEntity e)
		{
			var modules = e.Modules
				.OrderBy(m => m.PositionIndex)
				.Select(m => new ModuleDetailDto<GuestLessonDetailDto>(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(
							o.ObjectiveId,
							o.Content,
							o.PositionIndex,
							o.IsActive
						)).ToList(),
					m.Lessons
						.OrderBy(l => l.PositionIndex)
						.Select(l => new GuestLessonDetailDto(
							l.LessonId,
							l.Title,
							l.PositionIndex,
							l.IsActive))
						.ToList()
				)).ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForGuestDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public CourseDetailForLectureDto MapCourseDetailForLecture(CourseEntity e, IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId, IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId, IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId)
		{
			var modules = e.Modules
			.Where(m => m.IsActive)
			.OrderBy(m => m.PositionIndex)
			.Select(m =>
			{
				// Lấy quiz cho module (nếu có)
				QuizOutDto? moduleQuiz = null;
				if (moduleQuizIdByModuleId is not null &&
					moduleQuizIdByModuleId.TryGetValue(m.ModuleId, out var qid) &&
					quizByQuizId is not null &&
					quizByQuizId.TryGetValue(qid, out var qdto))
				{
					moduleQuiz = qdto;
				}

				var lessons = m.Lessons
					.Where(l => l.IsActive)
					.OrderBy(l => l.PositionIndex)
					.Select(l =>
					{
						QuizOutDto? lessonQuiz = null;
						if (lessonQuizIdByLessonId is not null &&
							lessonQuizIdByLessonId.TryGetValue(l.LessonId, out var lqid) &&
							quizByQuizId is not null &&
							quizByQuizId.TryGetValue(lqid, out var lqdto))
						{
							lessonQuiz = lqdto;
						}

						return new LectureLessonDetailDto(
							l.LessonId,
							l.Title,
							l.VideoUrl,
							l.VideoDurationSec,
							l.PositionIndex,
							l.IsActive,
							lessonQuiz // NEW
						);
					})
					.ToList();

				return new ModuleDetailForLectureDto(
					m.ModuleId,
					m.ModuleName,
					m.Description,
					m.PositionIndex,
					m.IsActive,
					m.IsCore,
					m.DurationMinutes,
					m.DurationHours,
					m.Level,
					m.ModuleObjectives.Where(o => o.IsActive)
						.OrderBy(o => o.PositionIndex)
						.Select(o => new ModuleObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
						.ToList(),
					m.ModuleDiscussions.Where(d => d.IsActive)
						.Select(d => new ModuleDiscussionDetailDto(d.DiscussionId, d.Title, d.Description, d.DiscussionQuestion, d.CreatedAt, d.UpdatedAt))
						.ToList(),
					m.ModuleMaterials.Where(mat => mat.IsActive)
						.Select(mat => new ModuleMaterialDetailDto(mat.MaterialId, mat.Title, mat.Description, mat.FileUrl, mat.CreatedAt, mat.UpdatedAt))
						.ToList(),
					lessons,
					moduleQuiz // NEW
				);
			})
			.ToList();

			// Comments
			var comments = e.CourseComments
							.OrderBy(c => c.CreatedAt)
							.Select(c => new CourseCommentDto(
								c.CommentId,
								c.UserId,
								c.Content,
								c.ParentCommentId,
								c.CreatedAt,
								c.IsActive
							)).ToList();

			// Tags
			var tags = e.CourseTags
				.Select(t => new CourseTagDto(
					t.TagId,
					t.Tag?.TagName ?? string.Empty
				)).ToList();

			// Ratings + thống kê
			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(
					r.RatingId,
					r.UserId,
					r.Rating,
					r.CreatedAt
				)).ToList();

			var ratingsCount = ratings.Count;
			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			return new CourseDetailForLectureDto(
				e.CourseId,
				e.TeacherId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				e.Modules.SelectMany(m => m.Lessons)
					.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoUrl ?? string.Empty,
				e.Modules.SelectMany(m => m.Lessons)
				.OrderBy(l => l.PositionIndex)
					.FirstOrDefault()?.VideoDurationSec ?? 0,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives
					.OrderBy(o => o.PositionIndex)
					.Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
					.ToList(),
				e.CourseRequirements
					.OrderBy(r => r.PositionIndex)
					.Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive))
					.ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDetailForStudentDto
		/// </summary>
		/// <param name="e"></param>
		/// <param name="moduleQuizIdByModuleId"></param>
		/// <param name="lessonQuizIdByLessonId"></param>
		/// <param name="quizByQuizId"></param>
		/// <param name="progressByLessonId"></param>
		/// <param name="moduleProgressById"></param>
		/// <param name="courseProgress"></param>
		/// <param name="preferCoreForCourse"></param>
		/// <returns></returns>
		public CourseDetailForStudentDto MapCourseDetailForStudent(CourseEntity e, IReadOnlyDictionary<Guid, Guid>? moduleQuizIdByModuleId, IReadOnlyDictionary<Guid, Guid>? lessonQuizIdByLessonId, IReadOnlyDictionary<Guid, QuizOutDto?>? quizByQuizId, IReadOnlyDictionary<Guid, LessonProgressSnap> progressByLessonId, IReadOnlyDictionary<Guid, ModuleProgressSnap> moduleProgressById, CourseProgressSnap? courseProgress, bool preferCoreForCourse)
		{
			// MODULES
			var modules = e.Modules
				.Where(m => m.IsActive)
				.OrderBy(m => m.PositionIndex)
				.Select(m =>
				{
					// Quiz cho module
					QuizOutDto? moduleQuiz = null;
					if (moduleQuizIdByModuleId is not null &&
						moduleQuizIdByModuleId.TryGetValue(m.ModuleId, out var qid) &&
						quizByQuizId is not null &&
						quizByQuizId.TryGetValue(qid, out var qdto))
					{
						moduleQuiz = qdto;
					}

					// LESSONS + tick
					var lessons = m.Lessons
						.Where(l => l.IsActive)
						.OrderBy(l => l.PositionIndex)
						.Select(l =>
						{
							QuizOutDto? lessonQuiz = null;
							if (lessonQuizIdByLessonId is not null &&
								lessonQuizIdByLessonId.TryGetValue(l.LessonId, out var lqid) &&
								quizByQuizId is not null &&
								quizByQuizId.TryGetValue(lqid, out var lqdto))
							{
								lessonQuiz = lqdto;
							}

							var has = progressByLessonId.TryGetValue(l.LessonId, out var lp);
							var isCompleted = has && lp!.Status == (short)LessonStatus.Completed;
							var lastPos = has ? lp!.LastPositionSec : 0;

							return new StudentLessonDetailDto(
								l.LessonId,
								l.Title,
								l.VideoUrl,
								l.VideoDurationSec,
								l.PositionIndex,
								l.IsActive,
								isCompleted,
								lastPos,
								lessonQuiz // để FE có thể hiển thị quiz của bài
							);
						})
						.ToList();

					// MODULE PROGRESS (dùng snapshot nếu có; fallback tự tính)
					int lessonsTotal = lessons.Count;
					int lessonsCompleted = lessons.Count(x => x.IsCompleted);
					decimal percent = lessonsTotal == 0 ? 0 : Math.Round((decimal)lessonsCompleted * 100m / lessonsTotal, 2);

					short status = lessonsCompleted == 0 ? (short)LessonStatus.NotStarted : (lessonsCompleted == lessonsTotal ? (short)LessonStatus.Completed : (short)LessonStatus.InProgress);
					DateTime? startedAt = null, completedAt = null;

					if (moduleProgressById.TryGetValue(m.ModuleId, out var mp))
					{
						lessonsTotal = mp.LessonsTotal;
						lessonsCompleted = mp.LessonsCompleted;
						percent = mp.PercentCompleted;
						status = mp.Status;
						startedAt = mp.StartedAt;
						completedAt = mp.CompletedAt;
					}

					return new ModuleDetailForStudentDto(
						m.ModuleId,
						m.ModuleName,
						m.Description,
						m.PositionIndex,
						m.IsActive,
						m.IsCore,
						m.DurationMinutes,
						m.DurationHours,
						m.Level,
						// objectives, discussions, materials giữ nguyên như lecture
						m.ModuleObjectives.Where(o => o.IsActive)
							.OrderBy(o => o.PositionIndex)
							.Select(o => new ModuleObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive))
							.ToList(),
						m.ModuleDiscussions.Where(d => d.IsActive)
							.Select(d => new ModuleDiscussionDetailDto(d.DiscussionId, d.Title, d.Description, d.DiscussionQuestion, d.CreatedAt, d.UpdatedAt))
							.ToList(),
						m.ModuleMaterials.Where(mat => mat.IsActive)
							.Select(mat => new ModuleMaterialDetailDto(mat.MaterialId, mat.Title, mat.Description, mat.FileUrl, mat.CreatedAt, mat.UpdatedAt))
							.ToList(),
						lessons,
						moduleQuiz,
						// progress
						new ModuleProgressDto(lessonsTotal, lessonsCompleted, percent, status, startedAt, completedAt)
					);
				})
				.ToList();

			// COURSE PROGRESS (chỉ CORE nếu preferCoreForCourse=true)
			int courseTotalLessons, courseCompletedLessons;
			decimal coursePercent;
			short courseStatus;
			DateTime? courseStartedAt = null, courseCompletedAt = null;

			if (courseProgress is not null)
			{
				courseTotalLessons = courseProgress.LessonsTotal;
				courseCompletedLessons = courseProgress.LessonsCompleted;
				coursePercent = courseProgress.PercentCompleted;
				courseStatus = courseProgress.Status;
				courseStartedAt = courseProgress.StartedAt;
				courseCompletedAt = courseProgress.CompletedAt;
			}
			else
			{
				// Fallback: tự tính theo modules is_core = true
				var coreModules = preferCoreForCourse ? modules.Where(m => m.IsCore) : modules;
				courseTotalLessons = coreModules.Sum(m => m.Progress.LessonsTotal);
				courseCompletedLessons = coreModules.Sum(m => m.Progress.LessonsCompleted);
				coursePercent = courseTotalLessons == 0 ? 0 : Math.Round((decimal)courseCompletedLessons * 100m / courseTotalLessons, 2);
				courseStatus = courseCompletedLessons == 0 ? (short)0 : (courseCompletedLessons == courseTotalLessons ? (short)CourseStatus.Completed : (short)CourseStatus.InProgress);
			}

			// Comments, Tags, Ratings giống lecture
			var comments = e.CourseComments
				.OrderBy(c => c.CreatedAt)
				.Select(c => new CourseCommentDto(c.CommentId, c.UserId, c.Content, c.ParentCommentId, c.CreatedAt, c.IsActive))
				.ToList();

			var tags = e.CourseTags.Select(t => new CourseTagDto(t.TagId, t.Tag?.TagName ?? string.Empty)).ToList();

			var ratings = e.CourseRatings
				.OrderByDescending(r => r.CreatedAt)
				.Select(r => new CourseRatingDto(r.RatingId, r.UserId, r.Rating, r.CreatedAt))
				.ToList();

			var ratingsCount = ratings.Count;

			var ratingsAverage = ratingsCount > 0
				? Math.Round(e.CourseRatings.Average(r => r.Rating), 2)
				: 0.0;

			var firstLesson = e.Modules.SelectMany(m => m.Lessons).OrderBy(l => l.PositionIndex).FirstOrDefault();

			// Gợi ý “tiếp tục học”
			var continueHint = ComputeContinueLesson(modules);

			return new CourseDetailForStudentDto(
				e.CourseId,
				e.SubjectId,
				e.Subject?.SubjectCode ?? string.Empty,
				e.Title ?? string.Empty,
				e.ShortDescription,
				e.Description,
				e.Slug,
				e.CourseImageUrl,
				e.LearnerCount,
				firstLesson?.VideoUrl ?? string.Empty,
				firstLesson?.VideoDurationSec ?? 0,
				e.DurationMinutes,
				e.DurationHours,
				e.Level,
				e.Price,
				e.DealPrice,
				e.IsActive,
				e.CreatedAt,
				e.UpdatedAt,
				e.CourseObjectives.OrderBy(o => o.PositionIndex).Select(o => new CourseObjectiveDto(o.ObjectiveId, o.Content, o.PositionIndex, o.IsActive)).ToList(),
				e.CourseRequirements.OrderBy(r => r.PositionIndex).Select(r => new CourseRequirementDto(r.RequirementId, r.Content, r.PositionIndex, r.IsActive)).ToList(),
				modules,
				comments,
				tags,
				ratings,
				ratingsCount,
				//ratingsAverage
				5.0,
				// Progress course
				new CourseProgressDto(courseTotalLessons, courseCompletedLessons, coursePercent, courseStatus, courseStartedAt, courseCompletedAt),
				// Continue hint – set ở ngoài
				continueHint
			);
		}

		/// <summary>
		/// Map CourseEntity -> CourseDto
		/// </summary>
		/// <param name="e"></param>
		/// <returns></returns>
		public CourseDto ToDto(CourseEntity e) => new(
			CourseId: e.CourseId,
			TeacherId: e.TeacherId,
			SubjectId: e.SubjectId,
			SubjectCode: e.Subject?.SubjectCode ?? string.Empty,
			Title: e.Title ?? string.Empty,
			ShortDescription: e.ShortDescription,
			Description: e.Description,
			Slug: e.Slug,
			CourseImageUrl: e.CourseImageUrl,
			LearnerCount: e.LearnerCount,
			DurationMinutes: e.DurationMinutes,
			DurationHours: e.DurationHours,
			Level: e.Level,
			Price: e.Price,
			DealPrice: e.DealPrice,
			IsActive: e.IsActive,
			CreatedAt: e.CreatedAt,
			UpdatedAt: e.UpdatedAt
		);

		/// <summary>
		/// Return the next lesson to continue learning
		/// </summary>
		/// <param name="modules"></param>
		/// <returns></returns>
		private static ContinueHintDto? ComputeContinueLesson(List<ModuleDetailForStudentDto> modules)
		{
			// Ưu tiên modules core trước, sau đó theo position_index
			foreach (var m in modules.OrderByDescending(x => x.IsCore).ThenBy(x => x.PositionIndex))
			{
				// Tìm bài chưa hoàn thành có position nhỏ nhất
				var next = m.Lessons.OrderBy(l => l.PositionIndex).FirstOrDefault(l => !l.IsCompleted);
				if (next is not null)
				{
					return new ContinueHintDto(
						ModuleId: m.ModuleId,
						ModuleName: m.ModuleName,
						LessonId: next.LessonId,
						LessonTitle: next.Title,
						ResumeSecond: next.LastPositionSec
					);
				}
			}
			return null;
		}
	}
}
