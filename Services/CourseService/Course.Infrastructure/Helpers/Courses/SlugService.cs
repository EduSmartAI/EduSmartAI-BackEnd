using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

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
			if (string.IsNullOrWhiteSpace(input))
				return Guid.NewGuid().ToString("n")[..8];

			// 1. Chuyển đổi tiếng Việt có dấu thành không dấu
			var s = RemoveDiacritics(input).ToLowerInvariant().Trim();

			// 2. Thay thế khoảng trắng (hoặc các chuỗi ký tự trắng) bằng dấu gạch ngang
			s = Regex.Replace(s, @"\s+", "-");

			// 3. Loại bỏ tất cả các ký tự không phải chữ cái Latin thường (a-z), số (0-9), hoặc dấu gạch ngang (-)
			s = Regex.Replace(s, @"[^a-z0-9\-]", "");

			// 4. Thay thế nhiều dấu gạch ngang liên tiếp bằng một dấu gạch ngang duy nhất và loại bỏ dấu gạch ngang ở đầu/cuối
			s = Regex.Replace(s, "-{2,}", "-").Trim('-');

			// 5. Trả về slug hoặc một GUID ngắn nếu slug bị rỗng sau khi xử lý
			return string.IsNullOrWhiteSpace(s) ? Guid.NewGuid().ToString("n")[..8] : s;
		}

		/// <summary>
		/// Hàm phụ trợ để chuyển đổi chuỗi tiếng Việt có dấu thành không dấu.
		/// </summary>
		private static string RemoveDiacritics(string text)
		{
			string formD = text.Normalize(NormalizationForm.FormD);
			var sb = new StringBuilder();

			for (int i = 0; i < formD.Length; i++)
			{
				UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(formD[i]);
				if (uc != UnicodeCategory.NonSpacingMark)
				{
					sb.Append(formD[i]);
				}
			}

			return sb.ToString().Normalize(NormalizationForm.FormC);
		}
	}
}
