namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ColumnAttribute : Attribute
        {
            public string Name { get; }

            public ColumnAttribute(string name)
            {
                Name = name;
            }
        }
    }
}
