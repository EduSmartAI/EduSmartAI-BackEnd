using Course.Application.DTOs;
using Mapster;

namespace Course.Application.Mapping
{
	public class MapsterRegister : IRegister
	{
		public void Register(TypeAdapterConfig config)
		{
			// CourseEntity -> CourseDto
			config.NewConfig<CourseEntity, CourseDto>()
				.Map(d => d.SubjectCode, s => s.Subject != null ? s.Subject.SubjectCode : string.Empty)
				// các field trùng tên sẽ tự map: Title, ShortDescription, Slug, CourseImageUrl, ...
				// DurationHours là cột GENERATED (decimal?), Mapster sẽ lấy từ entity
				.IgnoreNullValues(true);
		}
	}
}
