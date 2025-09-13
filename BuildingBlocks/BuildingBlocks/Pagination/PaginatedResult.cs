namespace BuildingBlocks.Pagination
{
	public class PaginatedResult<TEntity> where TEntity : class
	{
		public int? PageIndex { get; }
		public int? PageSize { get; }
		public long TotalCount { get; }
		public IEnumerable<TEntity> Data { get; }
		public int TotalPages { get; }
		public bool HasPreviousPage => PageIndex > 0;
		public bool HasNextPage => PageIndex + 1 < TotalPages;

		public PaginatedResult(int? pageIndex, int? pageSize, long totalCount, IEnumerable<TEntity> data)
		{
			PageIndex = pageIndex;
			PageSize = pageSize;
			TotalCount = totalCount;
			Data = data;
			TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
		}
	}
}
