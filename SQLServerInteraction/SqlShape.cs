namespace SQLServerInteraction
{
    /// <summary>
    /// Reads the top-level keywords of a T-SQL statement, skipping string literals,
    /// quoted identifiers, comments and anything inside parentheses.
    /// </summary>
    internal static class SqlShape
    {
        /// <summary>
        /// Whether the statement is a query: SELECT is its first keyword (after a UNION,
        /// INTERSECT or EXCEPT, with ALL, that <see cref="QueryBuilder"/> may put first),
        /// and FROM appears outside parentheses.
        /// </summary>
        internal static bool IsSelectFrom(string sql)
        {
            var words = TopLevelWords(sql).ToList();
            int first = 0;

            if (first < words.Count && words[first] is "UNION" or "INTERSECT" or "EXCEPT")
            {
                first++;
                if (first < words.Count && words[first] == "ALL") first++;
            }

            return first < words.Count && words[first] == "SELECT" && words.Skip(first + 1).Contains("FROM");
        }

        /// <summary>
        /// The words outside parentheses, quotes and comments, in upper case. A word
        /// is a run of letters, digits, _, @, # and $; anything else separates words.
        /// </summary>
        internal static IEnumerable<string> TopLevelWords(string sql)
        {
            int depth = 0;
            int i = 0;

            while (i < sql.Length)
            {
                char c = sql[i];

                if (c == '\'' || c == '"' || c == '[')
                {
                    char close = c == '[' ? ']' : c;
                    i++;
                    while (i < sql.Length)
                    {
                        if (sql[i] == close)
                        {
                            if (i + 1 < sql.Length && sql[i + 1] == close) { i += 2; continue; }
                            break;
                        }
                        i++;
                    }
                    i++;
                }
                else if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
                {
                    while (i < sql.Length && sql[i] != '\n') i++;
                }
                else if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
                {
                    int end = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                    i = end < 0 ? sql.Length : end + 2;
                }
                else if (c == '(')
                {
                    depth++;
                    i++;
                }
                else if (c == ')')
                {
                    depth = Math.Max(0, depth - 1);
                    i++;
                }
                else if (IsWordChar(c))
                {
                    int start = i;
                    while (i < sql.Length && IsWordChar(sql[i])) i++;
                    if (depth == 0) yield return sql[start..i].ToUpperInvariant();
                }
                else
                {
                    i++;
                }
            }
        }

        private static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c is '_' or '@' or '#' or '$';
    }
}
