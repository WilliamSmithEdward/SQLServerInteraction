using System.Text;

namespace SQLServerInteraction
{
    /// <summary>
    /// Turns a caller's table, column, index or database name into a quoted
    /// T-SQL identifier, so that a name is only ever read as a name.
    /// </summary>
    internal static class SqlIdentifier
    {
        /// <summary>The longest name SQL Server allows for one part (sysname).</summary>
        internal const int MaxPartLength = 128;

        /// <summary>
        /// Splits a one-, two- or three-part name into its parts, without brackets.
        /// </summary>
        /// <param name="name">
        /// A name such as <c>Sales</c>, <c>dbo.Sales</c>, <c>[dbo].[Sales]</c> or
        /// <c>[my db].dbo.[a.b]</c>. Parts are separated by dots outside brackets.
        /// A part that starts with <c>[</c> runs to the matching <c>]</c>, with
        /// <c>]]</c> standing for one <c>]</c>; any other part is taken as written.
        /// </param>
        /// <param name="maxParts">How many parts the name may have: 3 for a table, 1 for a column or index.</param>
        /// <exception cref="ArgumentException">
        /// The name is null or empty, has an empty part, a part longer than 128
        /// characters, an unclosed bracket, text after a closing bracket, or more
        /// than <paramref name="maxParts"/> parts.
        /// </exception>
        internal static IReadOnlyList<string> Parse(string name, int maxParts = 3)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("A name is required.", nameof(name));

            var parts = new List<string>();
            var part = new StringBuilder();
            int i = 0;

            while (true)
            {
                part.Clear();

                if (i < name.Length && name[i] == '[')
                {
                    i++;
                    while (true)
                    {
                        if (i >= name.Length)
                            throw new ArgumentException($"The name {name} has a [ with no closing ].", nameof(name));
                        if (name[i] == ']')
                        {
                            if (i + 1 < name.Length && name[i + 1] == ']')
                            {
                                part.Append(']');
                                i += 2;
                                continue;
                            }
                            i++;
                            break;
                        }
                        part.Append(name[i++]);
                    }

                    if (i < name.Length && name[i] != '.')
                        throw new ArgumentException($"The name {name} has text after a closing ].", nameof(name));
                }
                else
                {
                    while (i < name.Length && name[i] != '.')
                        part.Append(name[i++]);
                }

                if (part.Length == 0)
                    throw new ArgumentException($"The name {name} has an empty part.", nameof(name));
                if (part.Length > MaxPartLength)
                    throw new ArgumentException($"A part of the name {name} is longer than {MaxPartLength} characters.", nameof(name));

                parts.Add(part.ToString());
                if (parts.Count > maxParts)
                    throw new ArgumentException(
                        maxParts == 1 ? $"The name {name} must be a single name; bracket it if it contains a dot."
                                      : $"The name {name} has more than {maxParts} parts.",
                        nameof(name));

                if (i >= name.Length)
                    return parts;

                i++; // the dot
            }
        }

        /// <summary>
        /// The name as a quoted T-SQL identifier: each part in brackets, with any
        /// <c>]</c> in it doubled, joined with dots.
        /// </summary>
        /// <inheritdoc cref="Parse" path="/param"/>
        /// <inheritdoc cref="Parse" path="/exception"/>
        internal static string Quote(string name, int maxParts = 3) =>
            string.Join(".", Parse(name, maxParts).Select(QuotePart));

        /// <summary>
        /// A table name's parts, for comparing with catalog views as values:
        /// the database and schema are null when the name leaves them out.
        /// </summary>
        /// <inheritdoc cref="Parse" path="/exception"/>
        internal static (string? Database, string? Schema, string Table) ParseTable(string name)
        {
            var parts = Parse(name, 3);
            return parts.Count switch
            {
                1 => (null, null, parts[0]),
                2 => (null, parts[0], parts[1]),
                _ => (parts[0], parts[1], parts[2]),
            };
        }

        /// <summary>One part, already unquoted, in brackets with any <c>]</c> doubled.</summary>
        internal static string QuotePart(string part) => "[" + part.Replace("]", "]]") + "]";
    }
}
