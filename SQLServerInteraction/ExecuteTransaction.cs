using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        /// <summary>
        /// Executes a list of SQL commands as a transaction synchronously.
        /// </summary>
        /// <param name="sqlCommands">A list of SQL commands to be executed within the transaction.</param>
        public void ExecuteTransaction(List<string> sqlCommands)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var sql in sqlCommands)
                {
                    using var command = new SqlCommand(sql, connection, transaction);
                    command.ExecuteNonQuery();
                }

                transaction.Commit();
            }

            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }
    }
}
