using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        public Dictionary<string, string> GetTableColumns(string tableName)
        {
            var columns = new Dictionary<string, string>();

            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            string sql = $"SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{tableName}'";

            using var command = new SqlCommand(sql, connection);
            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string columnName = reader["COLUMN_NAME"].ToString() ?? "";
                string dataType = reader["DATA_TYPE"].ToString() ?? "";
                if(!string.IsNullOrEmpty(columnName)) columns[columnName] = dataType;
            }

            return columns;
        }
    }
}
