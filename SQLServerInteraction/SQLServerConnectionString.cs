using System;

namespace SQLServerInteraction
{
    public class SQLServerConnectionString
    {
        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public bool Encrypt { get; set; } = true;
        public string AdditionalParameters { get; set; }

        /// <summary>
        /// Connection string using user name and password.
        /// </summary>
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
        /// Connection string using trusted connection.
        /// </summary>
        public SQLServerConnectionString(string serverName, string databaseName, bool encrypt = true, string additionalParameters = "")
        {
            Server = serverName;
            DatabaseName = databaseName;
            Encrypt = encrypt;
            AdditionalParameters = additionalParameters;
        }

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
