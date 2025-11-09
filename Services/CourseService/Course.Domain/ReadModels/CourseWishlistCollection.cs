namespace Course.Domain.ReadModels
{
	public class CourseWishlistCollection
	{
		public Guid WishlistId { get; set; }

		public Guid UserId { get; set; }

		public Guid CourseId { get; set; }

		public bool IsActive { get; set; }

		public DateTime CreatedAt { get; set; }

		public string CreatedBy { get; set; }

		public DateTime UpdatedAt { get; set; }

		public string UpdatedBy { get; set; }

		public static CourseWishlistCollection FromWriteModel(Models.CourseWishlist model)
		{
			var courseWishlist = new CourseWishlistCollection
			{
				WishlistId = model.WishlistId,
				UserId = model.UserId,
				CourseId = model.CourseId,
				IsActive = model.IsActive,
				CreatedAt = model.CreatedAt,
				CreatedBy = model.CreatedBy,
				UpdatedAt = model.UpdatedAt,
				UpdatedBy = model.UpdatedBy
			};
			return courseWishlist;
		}
	}
}
