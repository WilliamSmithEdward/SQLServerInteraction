using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLServerInteraction
{
    public partial class SQLServerInstance
    {
        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        internal class ColumnAttribute : Attribute
        {
            internal string Name { get; }

            internal ColumnAttribute(string name)
            {
                Name = name;
            }
        }
    }
}
