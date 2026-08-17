using System.Text.RegularExpressions;

namespace IngameScript
{
    /// <summary>
    /// Matches whole-word tags (e.g. <c>#AL</c>) inside a block's custom name.
    /// Matching is case-insensitive and whole-word, so <c>#ALX</c> does not match <c>#AL</c>.
    /// </summary>
    public static class TagMatcher
    {
        /// <summary>True if <paramref name="customName"/> contains <paramref name="tag"/> as a whole word.</summary>
        public static bool HasTag(string tag, string customName)
        {
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(customName)) return false;
            // (^|whitespace) tag (whitespace|$), case-insensitive, tag treated literally.
            return Regex.IsMatch(customName, @"(^|\s)" + Regex.Escape(tag) + @"(\s|$)", RegexOptions.IgnoreCase);
        }

        /// <summary>True if <paramref name="customName"/> contains any of the supplied tags as a whole word.</summary>
        public static bool HasAnyTag(string customName, params string[] tags)
        {
            foreach (var tag in tags)
                if (HasTag(tag, customName)) return true;
            return false;
        }
    }
}
