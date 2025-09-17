using System.Text.RegularExpressions;

namespace UtilityService.Application.Logics
{
    public class CloudinaryLogic
    {
        public static string SlugifyPublicId(string raw)
        {
            raw = raw.Replace('\\', '/');                    // tránh %5C
            raw = Regex.Replace(raw, @"\s+", "-");          // space -> dash
            raw = Regex.Replace(raw, @"[^a-zA-Z0-9/_-]", ""); // bỏ ký tự lạ
            raw = raw.Trim('/');
            return string.IsNullOrWhiteSpace(raw) ? Guid.NewGuid().ToString("N") : raw;
        }
    }
}
