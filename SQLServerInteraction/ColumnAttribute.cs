namespace SQLServerInteraction
{

    public partial class SQLServerInstance
    {
        /// <summary>
        /// Specifies the name of a database column associated with a class property.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="ColumnAttribute"/> class with the specified column name.
        /// </remarks>
        /// <param name="name">The name of the database column associated with the property.</param>
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        public class ColumnAttribute(string name) : Attribute
        {
            /// <summary>
            /// Gets the name of the database column.
            /// </summary>
            public string Name { get; } = name;
        }
    }
}
