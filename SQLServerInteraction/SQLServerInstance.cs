using Microsoft.Data.SqlClient;
using System.Data;

namespace SQLServerInteraction
{
    /// <summary>
    /// Represents a SQL Server instance with a connection string. Each method opens a
    /// connection, does one piece of work and closes it; <see cref="BeginTransactionAsync(CancellationToken)"/>
    /// gives an object that runs several of them in one transaction.
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
        /// Opens a connection and begins a transaction on it, with SqlClient's default isolation level (read committed). The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.CommitAsync"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel opening the connection and beginning the transaction.</param>
        /// <returns>A task whose result is the transaction, which owns the connection.</returns>
        public Task<SQLServerTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) =>
            BeginTransactionAsync(IsolationLevel.Unspecified, cancellationToken);

        /// <summary>
        /// Opens a connection and begins a transaction on it with the given isolation level. The data methods of the returned object run in that transaction until <see cref="SQLServerTransaction.CommitAsync"/>; disposing it without a commit rolls back.
        /// </summary>
        /// <param name="isolationLevel">The transaction's isolation level. <see cref="IsolationLevel.Unspecified"/> takes SqlClient's default, read committed.</param>
        /// <param name="cancellationToken">A token to cancel opening the connection and beginning the transaction.</param>
        /// <returns>A task whose result is the transaction, which owns the connection.</returns>
        public async Task<SQLServerTransaction> BeginTransactionAsync(IsolationLevel isolationLevel, CancellationToken cancellationToken = default)
        {
            var connection = await OpenAsync(cancellationToken);
            try
            {
                return new SQLServerTransaction(connection, (SqlTransaction)await connection.BeginTransactionAsync(isolationLevel, cancellationToken));
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>A new connection, open, or disposed again if opening it failed.</summary>
        private async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            try
            {
                await connection.OpenAsync(cancellationToken);
                return connection;
            }
            catch
            {
                await connection.DisposeAsync();
                throw;
            }
        }

        /// <summary>
        /// Runs one piece of work on a connection of its own, in autocommit, through the
        /// same implementation that serves a transaction. The connection closes afterwards.
        /// </summary>
        private async Task<T> RunAsync<T>(Func<SQLServerTransaction, Task<T>> work, CancellationToken cancellationToken)
        {
            await using var autocommit = new SQLServerTransaction(await OpenAsync(cancellationToken), null);
            return await work(autocommit);
        }

        /// <inheritdoc cref="RunAsync{T}(Func{SQLServerTransaction, Task{T}}, CancellationToken)"/>
        private Task RunAsync(Func<SQLServerTransaction, Task> work, CancellationToken cancellationToken) =>
            RunAsync(async autocommit => { await work(autocommit); return true; }, cancellationToken);

        /// <summary>
        /// Runs one piece of work in a transaction of its own, committed when the work
        /// returns and rolled back when it throws, or in autocommit when
        /// <paramref name="useTransaction"/> is false.
        /// </summary>
        private async Task<T> RunAsync<T>(bool useTransaction, Func<SQLServerTransaction, Task<T>> work, CancellationToken cancellationToken)
        {
            if (!useTransaction) return await RunAsync(work, cancellationToken);

            await using var transaction = await BeginTransactionAsync(cancellationToken);
            T result = await work(transaction);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }

        /// <inheritdoc cref="RunAsync{T}(bool, Func{SQLServerTransaction, Task{T}}, CancellationToken)"/>
        private Task RunAsync(bool useTransaction, Func<SQLServerTransaction, Task> work, CancellationToken cancellationToken) =>
            RunAsync(useTransaction, async transaction => { await work(transaction); return true; }, cancellationToken);

        /// <summary>
        /// Runs one piece of work that needs the bare connection, such as a schema
        /// lookup, and closes the connection afterwards.
        /// </summary>
        private async Task<T> WithConnectionAsync<T>(Func<SqlConnection, Task<T>> work, CancellationToken cancellationToken)
        {
            await using var connection = await OpenAsync(cancellationToken);
            return await work(connection);
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
