using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EduAssess.Extensions
{
    public static class StringExtensions
    {
        public static string GenerateSlug(this string phrase)
        {
            if (string.IsNullOrWhiteSpace(phrase))
                return string.Empty;

            var normalizedString = phrase.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            var slug = stringBuilder.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();

            slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
            slug = Regex.Replace(slug, @"\s+", " ").Trim();
            slug = slug.Replace(" ", "-");
            slug = Regex.Replace(slug, @"-+", "-");

            return slug;
        }
    }
}