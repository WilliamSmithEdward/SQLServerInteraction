using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    /// <summary>
    /// Represents a SQL Server instance with a connection string.
    /// </summary>
    public partial class SQLServerInstance
    {
        private readonly string _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLServerInstance"/> class with a connection string.
        /// </summary>
        /// <param name="connectionString">The SQL Server connection string.</param>
        public SQLServerInstance(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SQLServerInstance"/> class with a <see cref="SQLServerConnectionString"/> object.
        /// </summary>
        /// <param name="connectionString">The <see cref="SQLServerConnectionString"/> object containing connection details.</param>
        public SQLServerInstance(SQLServerConnectionString connectionString)
        {
            _connectionString = connectionString.GetConnectionString();
        }

        /// <summary>
        /// Opens a connection and begins a transaction on it, with SqlClient's default isolation level (read committed). The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.Commit"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <returns>The transaction, which owns the connection.</returns>
        public SQLServerTransaction BeginTransaction() => BeginTransaction(IsolationLevel.Unspecified);

        /// <summary>
        /// Opens a connection and begins a transaction on it with the given isolation level. The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.Commit"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <param name="isolationLevel">The transaction's isolation level. <see cref="IsolationLevel.Unspecified"/> takes SqlClient's default, read committed.</param>
        /// <returns>The transaction, which owns the connection.</returns>
        public SQLServerTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                connection.Open();
                return new SQLServerTransaction(connection, connection.BeginTransaction(isolationLevel));
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Asynchronously opens a connection and begins a transaction on it, with SqlClient's default isolation level (read committed). The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.CommitAsync"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <returns>A task whose result is the transaction, which owns the connection.</returns>
        public Task<SQLServerTransaction> BeginTransactionAsync() => BeginTransactionAsync(IsolationLevel.Unspecified);

        /// <summary>
        /// Asynchronously opens a connection and begins a transaction on it with the given isolation level. The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.CommitAsync"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <param name="isolationLevel">The transaction's isolation level. <see cref="IsolationLevel.Unspecified"/> takes SqlClient's default, read committed.</param>
        /// <returns>A task whose result is the transaction, which owns the connection.</returns>
        public async Task<SQLServerTransaction> BeginTransactionAsync(IsolationLevel isolationLevel)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return new SQLServerTransaction(connection, (SqlTransaction)await connection.BeginTransactionAsync(isolationLevel));
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// An open connection in autocommit, wrapped so that one call can run on it
        /// the same way it runs in a transaction. Disposing it closes the connection.
        /// </summary>
        private SQLServerTransaction Connect()
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                connection.Open();
                return new SQLServerTransaction(connection, null);
            }
            catch
            {
                connection.Dispose();
                throw;
            }
        }

        /// <inheritdoc cref="Connect"/>
        private async Task<SQLServerTransaction> ConnectAsync()
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync();
                return new SQLServerTransaction(connection, null);
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// A WHERE condition as given, refused when it is empty or whitespace, so that
        /// a missing condition never turns into every row.
        /// </summary>
        internal static string RequireCondition(string condition, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("A condition is required. To affect every row, pass \"1 = 1\".", parameterName);
            return condition;
        }
    }
}
