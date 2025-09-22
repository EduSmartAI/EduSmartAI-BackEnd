using Course.Domain.Models;

namespace Course.Domain.ReadModels
{
	public sealed class CourseStudentEnrollmentCollection
	{
		public Guid EnrollmentId { get; init; }
		public Guid UserId { get; init; }
		public Guid CourseId { get; init; }
		public bool IsActive { get; init; }
		public DateTime StartedAt { get; init; }
		public DateTime? ExpiresAt { get; init; }
		public DateTime CreatedAt { get; init; }
		public DateTime UpdatedAt { get; init; }
		public string CreatedBy { get; init; }
		public string UpdatedBy { get; init; }

		public static CourseStudentEnrollmentCollection FromWriteModel(CourseStudentEnrollment model)
		{
			var courseStudentEnrollment = new CourseStudentEnrollmentCollection
			{
				EnrollmentId = model.EnrollmentId,
				UserId = model.UserId,
				CourseId = model.CourseId,
				IsActive = model.IsActive,
				StartedAt = model.StartedAt,
				ExpiresAt = model.ExpiresAt,
				CreatedAt = model.CreatedAt,
				UpdatedAt = model.UpdatedAt,
				CreatedBy = model.CreatedBy,
				UpdatedBy = model.UpdatedBy
			};

			return courseStudentEnrollment;
		}
	}
}
