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
        /// A WHERE condition as given, refused when it is empty or whitespace, so that
        /// a missing condition never turns into every row.
        /// </summary>
        private static string RequireCondition(string condition, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(condition))
                throw new ArgumentException("A condition is required. To affect every row, pass \"1 = 1\".", parameterName);
            return condition;
        }
    }
}