namespace SQLServerInteraction
{
    /// <summary>
    /// Represents a configuration for constructing a SQL Server connection string.
    /// </summary>
    public class SQLServerConnectionString
    {
        /// <summary>
        /// Gets or sets the SQL Server host or instance name.
        /// </summary>
        public string Server { get; set; }

        /// <summary>
        /// Gets or sets the name of the database to connect to.
        /// </summary>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets the optional user ID for authentication.
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// Gets or sets the optional password for authentication.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to use encryption in the connection.
        /// </summary>
        public bool Encrypt { get; set; } = true;

        /// <summary>
        /// Gets or sets additional parameters to include in the connection string.
        /// </summary>
        public string AdditionalParameters { get; set; }

        /// <summary>
        /// Connection string using user name and password. A null or empty <paramref name="userId"/> gives a trusted connection instead.
        /// </summary>
        /// <param name="serverName">The SQL Server host or instance name.</param>
        /// <param name="databaseName">The name of the database to connect to.</param>
        /// <param name="userId">The SQL Server login name.</param>
        /// <param name="password">The password for <paramref name="userId"/>.</param>
        /// <param name="encrypt">Whether to add <c>Encrypt=True</c> (the default) or <c>Encrypt=False</c>.</param>
        /// <param name="additionalParameters">Text appended to the end of the connection string as written, such as <c>"TrustServerCertificate=True;"</c>.</param>
        public SQLServerConnectionString(string serverName, string databaseName, string? userId, string? password, bool encrypt = true, string additionalParameters = "")
        {
            Server = serverName;
            DatabaseName = databaseName;
            UserId = userId;
            Password = password;
            Encrypt = encrypt;
            AdditionalParameters = additionalParameters;
        }

        /// <summary>
        /// Connection string using trusted connection (Windows authentication).
        /// </summary>
        /// <param name="serverName">The SQL Server host or instance name.</param>
        /// <param name="databaseName">The name of the database to connect to.</param>
        /// <param name="encrypt">Whether to add <c>Encrypt=True</c> (the default) or <c>Encrypt=False</c>.</param>
        /// <param name="additionalParameters">Text appended to the end of the connection string as written, such as <c>"TrustServerCertificate=True;"</c>.</param>
        public SQLServerConnectionString(string serverName, string databaseName, bool encrypt = true, string additionalParameters = "")
        {
            Server = serverName;
            DatabaseName = databaseName;
            Encrypt = encrypt;
            AdditionalParameters = additionalParameters;
        }

        /// <summary>
        /// Constructs and returns a connection string for connecting to a SQL Server database.
        /// </summary>
        /// <returns>The constructed connection string.</returns>
        /// <remarks>
        /// Values are inserted as written, without validation or escaping, so a value containing <c>;</c> breaks the string.
        /// </remarks>
        public string GetConnectionString()
        {
            string encryptPart = Encrypt ? "Encrypt=True;" : "Encrypt=False;";

            if (string.IsNullOrEmpty(UserId))
            {
                return $"Server={Server};Database={DatabaseName};Trusted_Connection=True;{encryptPart};" + AdditionalParameters;
            }
            else
            {
                return $"Server={Server};Database={DatabaseName};User Id={UserId};Password={Password};{encryptPart};" + AdditionalParameters;
            }
        }
    }
}
