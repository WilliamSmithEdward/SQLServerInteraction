using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
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
