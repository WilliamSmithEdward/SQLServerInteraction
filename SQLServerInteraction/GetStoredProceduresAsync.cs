namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// The names of the database's stored procedures and functions, from SqlClient's Procedures schema collection, without schema names.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task whose result is the list of names.</returns>
        public Task<List<string>> GetStoredProceduresAsync(CancellationToken cancellationToken = default) =>
            SchemaColumnAsync("Procedures", null, "ROUTINE_NAME", cancellationToken);
    }
}
