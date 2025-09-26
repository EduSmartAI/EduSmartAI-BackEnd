using Course.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Course.Domain.ReadModels
{
	public sealed class UserLessonProgressCollection
	{
		public Guid UserLessonProgressId { get; set; }

		public Guid UserId { get; set; }

		public Guid LessonId { get; set; }

		public short Status { get; set; } // 0 - Not Started, 1 - In Progress, 2 - Completed

		public DateTime? CompletedAt { get; set; }

		public int DurationWatchedSec { get; set; }

		public DateTime CreatedAt { get; set; }

		public DateTime UpdatedAt { get; set; }

		public int? LastPositionSec { get; set; }

		public static UserLessonProgressCollection FromWriteModel(UserLessonProgress model)
		{
			var userLessonProgress = new UserLessonProgressCollection
			{
				UserLessonProgressId = model.UserLessonProgressId,
				UserId = model.UserId,
				LessonId = model.LessonId,
				Status = model.Status,
				CompletedAt = model.CompletedAt,
				DurationWatchedSec = model.DurationWatchedSec,
				CreatedAt = model.CreatedAt,
				UpdatedAt = model.UpdatedAt,
				LastPositionSec = model.LastPositionSec
			};
			return userLessonProgress;
		}
	}
}
