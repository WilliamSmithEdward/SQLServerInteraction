using Microsoft.Data.SqlClient;

namespace SQLServerInteraction
{
    /// <summary>
    /// One connection with one open transaction, on which the data methods of
    /// <see cref="SQLServerInstance"/> run together: queries, commands, inserts,
    /// updates, deletes, merges and bulk copies. Get one from
    /// <see cref="SQLServerInstance.BeginTransaction()"/>, call the methods, then
    /// <see cref="Commit"/>. Disposing it without a commit rolls everything back
    /// and closes the connection, so a <c>using</c> block is enough on failure.
    /// </summary>
    /// <remarks>
    /// Every method throws <see cref="InvalidOperationException"/> after the
    /// transaction is committed or rolled back, and
    /// <see cref="ObjectDisposedException"/> after it is disposed. The object is
    /// not thread-safe: use it from one thread, or one async flow, at a time.
    /// </remarks>
    public sealed partial class SQLServerTransaction : IDisposable, IAsyncDisposable
    {
        private readonly SqlConnection _connection;
        private readonly SqlTransaction? _transaction;
        private bool _completed;
        private bool _disposed;

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
                if (_completed)
                    throw new InvalidOperationException("The transaction has already been committed or rolled back.");
                return _transaction;
            }
        }

        /// <summary>The open transaction, for a commit or rollback.</summary>
        private SqlTransaction OpenTransaction =>
            Transaction ?? throw new InvalidOperationException("There is no transaction to complete.");

        /// <summary>
        /// Commits the transaction. The connection stays open until the object is disposed, but no method can run after this.
        /// </summary>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public void Commit()
        {
            OpenTransaction.Commit();
            _completed = true;
        }

        /// <summary>
        /// Asynchronously commits the transaction. The connection stays open until the object is disposed, but no method can run after this.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public async Task CommitAsync()
        {
            await OpenTransaction.CommitAsync();
            _completed = true;
        }

        /// <summary>
        /// Rolls the transaction back. Disposing without a commit does the same, so this is only needed to roll back early.
        /// </summary>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public void Rollback()
        {
            OpenTransaction.Rollback();
            _completed = true;
        }

        /// <summary>
        /// Asynchronously rolls the transaction back. Disposing without a commit does the same, so this is only needed to roll back early.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        /// <exception cref="InvalidOperationException">The transaction was already committed or rolled back.</exception>
        /// <exception cref="ObjectDisposedException">The object was disposed.</exception>
        public async Task RollbackAsync()
        {
            await OpenTransaction.RollbackAsync();
            _completed = true;
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
