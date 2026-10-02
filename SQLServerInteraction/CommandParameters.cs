using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    /// <summary>
    /// How the library turns a caller's parameter dictionary into SqlParameters,
    /// the same way for every method that takes one.
    /// </summary>
    internal static class CommandParameters
    {
        /// <summary>
        /// The prefix of the parameters the library names itself for the values
        /// of an insert or update (<c>@__value_0</c>, <c>@__value_1</c>, ...).
        /// </summary>
        internal const string ValuePrefix = "@__value_";

        /// <summary>A parameter name with the <c>@</c> added only when it is missing.</summary>
        internal static string Name(string key) => key.StartsWith('@') ? key : "@" + key;

        /// <summary>A value as SqlClient should send it: null becomes DBNull.Value, which is SQL NULL.</summary>
        internal static object Value(object? value) => value ?? DBNull.Value;

        /// <summary>
        /// Adds a table name's parts as <c>@database</c>, <c>@schema</c> and <c>@table</c>,
        /// for a query that compares them with a catalog view. A part the name leaves
        /// out is NULL, which such a query reads as "any".
        /// </summary>
        /// <exception cref="ArgumentException">The name is not a valid one-, two- or three-part name.</exception>
        internal static void AddTableName(SqlCommand command, string tableName)
        {
            var (database, schema, table) = SqlIdentifier.ParseTable(tableName);
            command.Parameters.Add("@database", SqlDbType.NVarChar, SqlIdentifier.MaxPartLength).Value = Value(database);
            command.Parameters.Add("@schema", SqlDbType.NVarChar, SqlIdentifier.MaxPartLength).Value = Value(schema);
            command.Parameters.Add("@table", SqlDbType.NVarChar, SqlIdentifier.MaxPartLength).Value = table;
        }

        /// <summary>
        /// Adds the caller's parameters to a command: names through <see cref="Name"/>,
        /// values through <see cref="Value"/>. Null adds nothing.
        /// </summary>
        internal static void Add(SqlCommand command, Dictionary<string, object>? parameters)
        {
            if (parameters == null) return;

            foreach (var kvp in parameters)
            {
                command.Parameters.AddWithValue(Name(kvp.Key), Value(kvp.Value));
            }
        }
    }
}
