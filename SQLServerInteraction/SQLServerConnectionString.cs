using Microsoft.Data.SqlClient;

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
        /// Gets or sets additional connection string keywords, such as <c>"TrustServerCertificate=True"</c>, merged into the connection string.
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
        /// <param name="additionalParameters">Other connection string keywords, such as <c>"TrustServerCertificate=True"</c>. They are parsed and merged, not appended as text, and replace any keyword the other arguments set; see <see cref="GetConnectionString"/>.</param>
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
        /// <param name="additionalParameters">Other connection string keywords, such as <c>"TrustServerCertificate=True"</c>. They are parsed and merged, not appended as text, and replace any keyword the other arguments set; see <see cref="GetConnectionString"/>.</param>
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
        /// <returns>The connection string, built with <see cref="SqlConnectionStringBuilder"/>, so a value containing <c>;</c> or a quote is escaped.</returns>
        /// <remarks>
        /// <see cref="AdditionalParameters"/> is parsed with <see cref="SqlConnectionStringBuilder"/> and merged with the other properties,
        /// each of its keywords replacing any the properties set, as it did when 1.x appended it to the end of the string.
        /// </remarks>
        /// <exception cref="ArgumentException"><see cref="AdditionalParameters"/> is not a valid connection string.</exception>
        public string GetConnectionString()
        {
            var additional = new SqlConnectionStringBuilder();

            if (!string.IsNullOrWhiteSpace(AdditionalParameters))
            {
                try
                {
                    additional.ConnectionString = AdditionalParameters;
                }
                catch (Exception e) when (e is ArgumentException or FormatException or InvalidOperationException or KeyNotFoundException)
                {
                    throw new ArgumentException($"The additional parameters are not a valid connection string: {e.Message}", nameof(AdditionalParameters), e);
                }
            }

            var builder = new SqlConnectionStringBuilder();
            builder.DataSource = Server;
            builder.InitialCatalog = DatabaseName;

            if (string.IsNullOrEmpty(UserId))
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = UserId;
                builder.Password = Password ?? "";
            }

            builder.Encrypt = Encrypt ? SqlConnectionEncryptOption.Mandatory : SqlConnectionEncryptOption.Optional;

            foreach (string keyword in additional.Keys)
            {
                if (additional.ShouldSerialize(keyword)) builder[keyword] = additional[keyword];
            }

            return builder.ConnectionString;
        }
    }
}
