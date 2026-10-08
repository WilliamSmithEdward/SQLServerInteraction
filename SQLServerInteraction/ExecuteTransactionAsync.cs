namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a list of SQL commands as one transaction, in order; any exception rolls back and is rethrown. To mix the library's other methods into one transaction, use <see cref="BeginTransactionAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="sqlCommands">A list of SQL commands to be executed within the transaction.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task ExecuteTransactionAsync(List<string> sqlCommands, CancellationToken cancellationToken = default) =>
            RunAsync(useTransaction: true, async transaction =>
            {
                foreach (var sql in sqlCommands)
                {
                    await transaction.ExecuteSQLAsync(sql, cancellationToken);
                }
            }, cancellationToken);
    }
}
