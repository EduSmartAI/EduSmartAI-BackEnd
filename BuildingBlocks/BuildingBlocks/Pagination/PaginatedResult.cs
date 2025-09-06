namespace BuildingBlocks.Pagination
{
	public class PaginatedResult<TEntity>(int pageIndex, int pageSize, long totalCount, IEnumerable<TEntity> data) where TEntity : class
	{
		public int PageIndex { get; } = pageIndex;
		public int PageSize { get; } = pageSize;
		public long Count { get; } = totalCount;
		public IEnumerable<TEntity> Data { get; } = data;
		public int TotalPages { get; } = (int)Math.Ceiling(totalCount / (double)pageSize);
	}
}
