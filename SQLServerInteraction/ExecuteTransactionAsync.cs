using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        public async Task ExecuteTransactionAsync(List<string> sqlCommands)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            using var transaction = connection.BeginTransaction();

            try
            {
                foreach (var sql in sqlCommands)
                {
                    using var command = new SqlCommand(sql, connection, transaction);
                    await command.ExecuteNonQueryAsync();
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
