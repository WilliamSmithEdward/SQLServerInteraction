using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    /// <summary>
    /// One connection with one open transaction, on which the data methods of
    /// <see cref="SQLServerInstance"/> run together: queries, commands, inserts,
    /// updates, deletes, merges, bulk copies and index changes. Get one from
    /// <see cref="SQLServerInstance.BeginTransactionAsync(CancellationToken)"/>, call the
    /// methods, then <see cref="CommitAsync"/>. Disposing it without a commit rolls
    /// everything back and closes the connection, so an <c>await using</c> block is
    /// enough on failure.
    /// </summary>
    /// <remarks>
    /// <para>
    /// After an exception from any method, roll back, or let the <c>await using</c>
    /// block do it. Do not catch the exception and carry on to <see cref="CommitAsync"/>:
    /// SQL Server fails only the statement for some errors, such as a constraint
    /// violation, and leaves the transaction open, so the commit would keep the
    /// work done before the failure. To undo part of a transaction on purpose,
    /// use <see cref="SaveAsync"/> and <see cref="RollbackToAsync"/>.
    /// </para>
    /// <para>
    /// Every method throws <see cref="InvalidOperationException"/> after the
    /// transaction is committed or rolled back, or after a commit that failed, and
    /// <see cref="ObjectDisposedException"/> after it is disposed. The object is
    /// not thread-safe: use it from one async flow at a time.
    /// </para>
    /// </remarks>
    public sealed partial class SQLServerTransaction : IDisposable, IAsyncDisposable
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction? _transaction;
        private string? _closedBecause;
        private bool _disposed;

        private const string Committed = "The transaction has already been committed.";
        private const string RolledBack = "The transaction has already been rolled back.";
        private const string CommitFailed = "The commit failed, so the transaction cannot be used again. Dispose it; whatever the server did not commit is rolled back.";

        /// <summary>
        /// Over an open connection. With a null transaction the methods run in
        /// autocommit, which is how <see cref="SQLServerInstance"/> runs one call;
        /// such an object is never handed out.
        /// </summary>
        internal SQLServerTransaction(SqlConnection connection, SqlTransaction? transaction)
        {
            _connection = connection;
            _transaction = transaction;
        }

        /// <summary>
        /// The transaction every command and bulk copy runs in, checked for use
        /// after a commit, rollback or dispose. Null when running in autocommit.
        /// </summary>
        private SqlTransaction? Transaction
        {
            get
            {
                ObjectDisposedException.ThrowIf(_disposed, this);
                if (_closedBecause != null)
                    throw new InvalidOperationException(_closedBecause);
                return _transaction;
            }
        }

        /// <summary>The open transaction, for a commit, rollback or savepoint.</summary>
        private SqlTransaction OpenTransaction =>
            Transaction ?? throw new InvalidOperationException("There is no transaction to complete.");

        /// <summary>
        /// Commits the transaction. The connection stays open until the object is disposed, but no method can run after this.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the commit.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            var transaction = OpenTransaction;
            try
            {
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                _closedBecause = CommitFailed;
                throw;
            }
            _closedBecause = Committed;
        }

        /// <summary>
        /// Rolls the transaction back. Disposing without a commit does the same, so this is only needed to roll back early.
        /// </summary>
        /// <param name="cancellationToken">A token to cancel the rollback.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            await OpenTransaction.RollbackAsync(cancellationToken);
            _closedBecause = RolledBack;
        }

        /// <summary>
        /// Marks a savepoint, so that the work after it can be undone with <see cref="RollbackToAsync"/> while the transaction stays open.
        /// </summary>
        /// <param name="savepointName">A name of up to 32 characters, plain or bracketed, quoted as an identifier. Marking it again moves it.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The name is not a valid single name, or is longer than 32 characters.</exception>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public Task SaveAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            string sql = SavepointSql("SAVE", savepointName);
            _ = OpenTransaction;
            return ExecuteSQLAsync(sql, cancellationToken);
        }

        /// <summary>
        /// Undoes the work done since <see cref="SaveAsync"/> marked the savepoint. The transaction stays open, and the savepoint can be rolled back to again.
        /// </summary>
        /// <remarks>
        /// This works after an error that failed one statement, such as a constraint violation. After an error that
        /// made the transaction uncommittable, such as a deadlock or a conversion failure, SQL Server refuses to roll
        /// back to a savepoint, and the exception says so; only a full <see cref="RollbackAsync"/>, or disposing, is left.
        /// </remarks>
        /// <param name="savepointName">The name given to <see cref="SaveAsync"/>.</param>
        /// <param name="cancellationToken">A token to cancel the operation.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentException">The name is not a valid single name, or is longer than 32 characters.</exception>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public Task RollbackToAsync(string savepointName, CancellationToken cancellationToken = default)
        {
            string sql = SavepointSql("ROLLBACK", savepointName);
            _ = OpenTransaction;
            return ExecuteSQLAsync(sql, cancellationToken);
        }

        /// <summary>The longest savepoint name SQL Server keeps; a longer one is silently cut.</summary>
        internal const int MaxSavepointNameLength = 32;

        /// <summary>
        /// <c>SAVE TRANSACTION [name]</c> or <c>ROLLBACK TRANSACTION [name]</c>, with the name quoted.
        /// </summary>
        /// <exception cref="ArgumentException">The name is not a valid single name, or is longer than 32 characters.</exception>
        internal static string SavepointSql(string keyword, string savepointName)
        {
            string name = SqlIdentifier.Parse(savepointName, maxParts: 1)[0];
            if (name.Length > MaxSavepointNameLength)
                throw new ArgumentException($"A savepoint name can have at most {MaxSavepointNameLength} characters.", nameof(savepointName));
            return $"{keyword} TRANSACTION {SqlIdentifier.QuotePart(name)}";
        }

        /// <summary>
        /// Rolls the transaction back if it was neither committed nor rolled back, and closes the connection.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _transaction?.Dispose();
            _connection.Dispose();
        }

        /// <summary>
        /// Asynchronously rolls the transaction back if it was neither committed nor rolled back, and closes the connection.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (_disposed) return;
            _disposed = true;
            if (_transaction != null) await _transaction.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
