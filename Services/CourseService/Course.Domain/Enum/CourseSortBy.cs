namespace Course.Domain.Enum
{
	public enum CourseSortBy
	{
		Latest = 1,        // UpdatedAt desc (default)
		Popular = 2,       // LearnerCount desc
		TitleAsc = 3,
		TitleDesc = 4
	}
}
