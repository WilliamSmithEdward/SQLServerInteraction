namespace SQLServerInteraction
{
    public partial class SQLServerInteraction
    {
        private string _connectionString;

        public SQLServerInteraction(string connectionString)
        {
            _connectionString = connectionString;
        }
    }
}