namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a list of SQL commands as a transaction synchronously. To mix the library's other methods into one transaction, use <see cref="BeginTransaction()"/>.
        /// </summary>
        /// <param name="sqlCommands">A list of SQL commands to be executed within the transaction.</param>
        public void ExecuteTransaction(List<string> sqlCommands)
        {
            using var transaction = BeginTransaction();

            foreach (var sql in sqlCommands)
            {
                transaction.ExecuteSQL(sql);
            }

            transaction.Commit();
        }
    }
}
