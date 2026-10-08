namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a list of SQL commands as a transaction asynchronously. To mix the library's other methods into one transaction, use <see cref="BeginTransactionAsync(CancellationToken)"/>.
        /// </summary>
        /// <param name="sqlCommands">A list of SQL commands to be executed within the transaction.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task ExecuteTransactionAsync(List<string> sqlCommands)
        {
            await using var transaction = await BeginTransactionAsync();

            foreach (var sql in sqlCommands)
            {
                await transaction.ExecuteSQLAsync(sql);
            }

            await transaction.CommitAsync();
        }
    }
}
