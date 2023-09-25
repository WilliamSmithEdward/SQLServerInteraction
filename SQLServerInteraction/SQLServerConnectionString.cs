using System;

namespace SQLServerInteraction
{
    public class SQLServerConnectionString
    {
        public string Server { get; set; }
        public string DatabaseName { get; set; }
        public string? UserId { get; set; }
        public string? Password { get; set; }
        public bool TrustedConnection { get; set; } = false;
        public bool Encrypt { get; set; } = true;

        public SQLServerConnectionString(string serverName, string databaseName, string? userId = null, string? password = null, bool trustedConnection = false, bool encrypt = true)
        {
            Server = serverName;
            DatabaseName = databaseName;
            UserId = userId;
            Password = password;
            TrustedConnection = trustedConnection;
            Encrypt = encrypt;

            if (!TrustedConnection && (string.IsNullOrEmpty(UserId) || string.IsNullOrEmpty(Password)))
            {
                throw new ArgumentException("When TrustedConnection is false, both UserId and Password must be provided.");
            }
        }

        public string GetConnectionString()
        {
            string encryptPart = Encrypt ? "Encrypt=True;" : "Encrypt=False;";

            if (TrustedConnection)
            {
                return $"Server={Server};Database={DatabaseName};Trusted_Connection=True;{encryptPart}";
            }
            else
            {
                return $"Server={Server};Database={DatabaseName};User Id={UserId};Password={Password};{encryptPart}";
            }
        }
    }
}
