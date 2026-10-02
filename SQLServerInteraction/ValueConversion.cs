namespace SQLServerInteraction
{
    /// <summary>
    /// Converts a value read from SQL Server to the type a caller asked for.
    /// </summary>
    internal static class ValueConversion
    {
        /// <summary>
        /// The value as <typeparamref name="T"/>: null and DBNull.Value give default(T),
        /// and a nullable type converts to its underlying type, so <c>int?</c> works.
        /// </summary>
        /// <exception cref="InvalidCastException">The value cannot be converted to <typeparamref name="T"/>.</exception>
        internal static T? ChangeType<T>(object? value)
        {
            if (value == null || value == DBNull.Value) return default;

            Type target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            return (T)Convert.ChangeType(value, target);
        }
    }
}
