using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    public class SQLServerInteraction
    {
        private string _connectionString;

        public SQLServerInteraction(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void BulkCopy(DataTable dt, string sqlServerTableName, bool flushTableBeforeCopy)
        {
            using var connection = new SqlConnection(_connectionString);
            using var bulkCopy = new SqlBulkCopy(connection);

            connection.Open();

            if (flushTableBeforeCopy) using (var command = new SqlCommand("DELETE FROM " + sqlServerTableName, connection)) command.ExecuteNonQuery();

            bulkCopy.DestinationTableName = sqlServerTableName;
            bulkCopy.WriteToServer(dt);
        }

        public DataTable QueryIntoDataTable(string sql)
        {
            var dt = new DataTable();

            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand(sql, connection);
            using var adapter = new SqlDataAdapter(command);

            adapter.Fill(dt);

            return dt;
        }

        public string? QueryScalarAsString(string sql)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand(sql, connection);

            return command.ExecuteScalar().ToString();
        }

        public void ExecuteSQL(string sql)
        {
            using var connection = new SqlConnection(_connectionString);

            connection.Open();

            using var command = new SqlCommand(sql, connection);

            command.ExecuteNonQuery();
        }
    }
}