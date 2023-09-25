﻿namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        private string _connectionString;

        public SQLServerInstance(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SQLServerInstance(SQLServerConnectionString connectionString)
        {
            _connectionString = connectionString.GetConnectionString();
        }
    }
}