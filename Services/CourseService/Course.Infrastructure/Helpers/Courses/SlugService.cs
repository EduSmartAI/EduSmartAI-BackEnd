namespace Course.Infrastructure.Helpers.Courses
{
	public sealed class SlugService(ICommandRepository<CourseEntity> _courseRepository) : ISlugService
	{
		/// <summary>
		/// Đảm bảo slug unique khi UPDATE (bỏ qua chính course hiện tại).
		/// Nếu allowRandomSuffix=true, khi trùng sẽ gắn thêm -xxxxxx (6 hex) để tránh loop nhiều lần.
		/// </summary>
		/// <param name="courseId"></param>
		/// <param name="candidate"></param>
		/// <param name="ct"></param>
		/// <param name="allowRandomSuffix"></param>
		/// <returns></returns>
		public async Task<string> EnsureUniqueSlugForUpdateAsync(Guid courseId, string candidate, CancellationToken ct, bool allowRandomSuffix = false)
		{
			var slug = ToSlug(candidate);

			// Nếu slug đã thuộc về chính course này -> ok
			var ownedByCurrent = await _courseRepository
				.Find(c => c.CourseId == courseId && c.Slug == slug)
				.AnyAsync(ct);
			if (ownedByCurrent) return slug;

			var exists = await _courseRepository
				.Find(c => c.Slug == slug && c.CourseId != courseId)
				.AnyAsync(ct);

			if (!exists) return slug;

			if (allowRandomSuffix)
			{
				var suffix = Guid.NewGuid().ToString("N")[..6];
				return await EnsureUniqueSlugForUpdateAsync(courseId, $"{slug}-{suffix}", ct, allowRandomSuffix: false);
			}
			else
			{
				// fallback: tăng dần -1, -2 ... (hiếm khi cần)
				var baseSlug = slug;
				var i = 1;
				while (await _courseRepository.Find(c => c.Slug == slug && c.CourseId != courseId).AnyAsync(ct))
				{
					slug = $"{baseSlug}-{i}";
					i++;
				}
				return slug;
			}
		}

		/// <summary>
		/// Generate a unique slug by appending a random suffix if necessary
		/// </summary>
		/// <param name="title"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public async Task<string> GenerateUniqueSlugAsync(string title, CancellationToken ct)
		{
			var baseSlug = ToSlug(title);
			var slug = baseSlug;

			while (await _courseRepository.Find(c => c.Slug == slug).AnyAsync(ct))
			{
				// lấy 6 ký tự ngẫu nhiên từ Guid
				var suffix = Guid.NewGuid().ToString("N")[..6];
				slug = $"{baseSlug}-{suffix}";
			}

			return slug;
		}

		/// <summary>
		/// Generate slug from title
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public string ToSlug(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return Guid.NewGuid().ToString("n")[..8];
			var s = input.ToLowerInvariant().Trim();
			s = System.Text.RegularExpressions.Regex.Replace(s, @"\s+", "-");
			s = System.Text.RegularExpressions.Regex.Replace(s, @"[^a-z0-9\-]", "");
			s = System.Text.RegularExpressions.Regex.Replace(s, "-{2,}", "-").Trim('-');
			return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("n")[..8] : s;
		}
	}
}
