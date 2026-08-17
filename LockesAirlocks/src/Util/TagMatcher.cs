namespace IngameScript
{
    /// <summary>
    /// Matches whole-word tags (e.g. <c>#AL</c>) inside a block's custom name.
    /// Matching is case-insensitive and whole-word, so <c>#ALX</c> does not match <c>#AL</c>.
    /// Uses only SE-sandbox-safe APIs (no System.Text.RegularExpressions).
    /// </summary>
    public static class TagMatcher
    {
        /// <summary>True if <paramref name="customName"/> contains <paramref name="tag"/> as a whole word.</summary>
        public static bool HasTag(string tag, string customName)
        {
            if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(customName)) return false;
            int tagLen = tag.Length;
            int nameLen = customName.Length;
            for (int i = 0; i <= nameLen - tagLen; i++)
            {
                bool match = true;
                for (int j = 0; j < tagLen; j++)
                {
                    if (char.ToUpperInvariant(customName[i + j]) != char.ToUpperInvariant(tag[j]))
                    {
                        match = false;
                        break;
                    }
                }
                if (!match) continue;
                bool beforeOk = i == 0 || char.IsWhiteSpace(customName[i - 1]);
                bool afterOk = i + tagLen == nameLen || char.IsWhiteSpace(customName[i + tagLen]);
                if (beforeOk && afterOk) return true;
            }
            return false;
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
