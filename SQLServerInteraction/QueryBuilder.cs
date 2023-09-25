using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace SQLServerInteraction
{
    /// <summary>
    /// Represents a query builder for constructing SQL queries.
    /// </summary>
    public class QueryBuilder
    {
        private readonly StringBuilder _query;
        private readonly List<SQLParameter> _parameters;
        private readonly List<QueryBuilder> _subqueries;
        private readonly Stack<string> _nestedConditions;
        private bool _isUnion;
        private bool _isIntersect;
        private bool _isExcept;
        private bool _usePagination;
        private int _pageNumber;
        private int _pageSize;
        private bool _useCaseStatement;
        private string _caseStatement;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
        /// </summary>
        public QueryBuilder()
        {
            _query = new StringBuilder();
            _parameters = new List<SQLParameter>();
            _subqueries = new List<QueryBuilder>();
            _nestedConditions = new Stack<string>();
            _isUnion = false;
            _isIntersect = false;
            _isExcept = false;
            _usePagination = false;
            _pageNumber = 1;
            _pageSize = 10;
            _useCaseStatement = false;
            _caseStatement = string.Empty;
        }

        /// <summary>
        /// Constructs a SELECT SQL statement with the specified columns.
        /// </summary>
        /// <param name="columns">A comma-separated list of column names. Leave empty for selecting all columns.</param>
        public void Select(string columns = "")
        {
            if (columns.IsNullOrEmpty()) _query.Append($"SELECT * ");
            else _query.Append($"SELECT {columns} ");
        }

        /// <summary>
        /// Specifies the table from which to select data in the SQL statement.
        /// </summary>
        /// <param name="tableName">The name of the table.</param>
        public void From(string tableName)
        {
            _query.Append($"FROM {tableName} ");
        }

        /// <summary>
        /// Adds a WHERE clause to the SQL statement based on the specified condition.
        /// </summary>
        /// <param name="condition">The condition for the WHERE clause.</param>
        public void Where(string condition)
        {
            _query.Append($"WHERE {condition} ");
        }

        /// <summary>
        /// Adds an AND condition to the WHERE clause in the SQL statement.
        /// </summary>
        /// <param name="condition">The additional condition to be combined with the existing WHERE clause.</param>
        public void And(string condition)
        {
            _query.Append($"AND {condition} ");
        }

        /// <summary>
        /// Adds an OR condition to the WHERE clause in the SQL statement.
        /// </summary>
        /// <param name="condition">The additional condition to be combined with the existing WHERE clause using OR.</param>
        /// <returns>The updated SQL statement with the specified OR condition in the WHERE clause.</returns>
        public void Or(string condition)
        {
            _query.Append($"OR {condition} ");
        }

        /// <summary>
        /// Adds a parameter and its value for use in the SQL query.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        public void AddParameter(string parameterName, object value)
        {
            var parameter = new SQLParameter { Name = parameterName, Value = value };
            _parameters.Add(parameter);
        }

        /// <summary>
        /// Adds a join clause to the SQL statement based on the specified table, condition, and join type.
        /// </summary>
        /// <param name="tableName">The name of the table to join.</param>
        /// <param name="onCondition">The condition for the join.</param>
        /// <param name="joinType">The type of join (default is INNER JOIN).</param>
        public void Join(string tableName, string onCondition, JoinType joinType = JoinType.Inner)
        {
            _query.Append($"{joinType.ToString().ToUpper()} JOIN {tableName} ON {onCondition} ");
        }

        /// <summary>
        /// Adds an ORDER BY clause to the SQL statement based on the specified columns and sort order.
        /// </summary>
        /// <param name="columns">A comma-separated list of columns to order by.</param>
        /// <param name="sortOrder">The sort order for the columns (default is ascending).</param>
        public void OrderBy(string columns, SortOrder sortOrder = SortOrder.Ascending)
        {
            _query.Append($"ORDER BY {columns} {sortOrder.ToString().ToUpper()} ");
        }

        /// <summary>
        /// Adds a GROUP BY clause to the SQL statement based on the specified columns.
        /// </summary>
        /// <param name="columns">A comma-separated list of columns to group by.</param>
        public void GroupBy(string columns)
        {
            _query.Append($"GROUP BY {columns} ");
        }

        /// <summary>
        /// Adds a COUNT aggregate function to the SQL statement for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to count.</param>
        /// <param name="alias">The alias for the COUNT result.</param>
        public void Count(string columnName, string alias)
        {
            _query.Append($"COUNT({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds a SUM aggregate function to the SQL statement for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to sum.</param>
        /// <param name="alias">The alias for the SUM result.</param>
        public void Sum(string columnName, string alias)
        {
            _query.Append($"SUM({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds an AVG aggregate function to the SQL statement for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the average.</param>
        /// <param name="alias">The alias for the AVG result.</param>
        public void Avg(string columnName, string alias)
        {
            _query.Append($"AVG({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds an AVG aggregate function to the SQL statement for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the average.</param>
        /// <param name="alias">The alias for the AVG result.</param>
        public void Min(string columnName, string alias)
        {
            _query.Append($"MIN({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds a MAX aggregate function to the SQL statement for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the maximum value.</param>
        /// <param name="alias">The alias for the MAX result.</param>
        public void Max(string columnName, string alias)
        {
            _query.Append($"MAX({columnName}) AS {alias}" );
        }

        /// <summary>
        /// Creates a subquery within the current SQL statement using a new instance of the QueryBuilder.
        /// </summary>
        /// <returns>A new QueryBuilder instance representing the subquery.</returns>
        public QueryBuilder CreateSubquery()
        {
            var subquery = new QueryBuilder();
            _subqueries.Add(subquery);
            return subquery;
        }

        /// <summary>
        /// Starts a nested condition within the SQL WHERE clause.
        /// </summary>
        public void StartNestedCondition()
        {
            _nestedConditions.Push("(");
        }

        /// <summary>
        /// Ends a nested condition within the SQL WHERE clause.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to end a nested condition without starting one.</exception>
        public void EndNestedCondition()
        {
            if (_nestedConditions.Count == 0)
                throw new InvalidOperationException("No nested condition to end.");

            var nestedCondition = _nestedConditions.Pop();
            _query.Append(nestedCondition);
            _query.Append(") ");
        }

        /// <summary>
        /// Enables pagination for the SQL statement, specifying the page number and page size.
        /// </summary>
        public void Paginate(int page, int pageSize)
        {
            _usePagination = true;
            _pageNumber = page;
            _pageSize = pageSize;
        }

        /// <summary>
        /// Starts a CASE statement for conditional logic within the SQL statement.
        /// </summary>
        /// <param name="columnName">The name of the column to apply the CASE statement.</param>
        public void StartCaseStatement(string columnName)
        {
            _useCaseStatement = true;
            _caseStatement = $"CASE {columnName} ";
        }

        /// <summary>
        /// Adds a WHEN-THEN clause to the current CASE statement within the SQL statement.
        /// </summary>
        /// <param name="condition">The condition for the WHEN clause.</param>
        /// <param name="result">The result for the THEN clause.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to add a WHEN-THEN clause without starting a CASE statement.</exception>
        public void AddCaseWhen(string condition, string result)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"WHEN {condition} THEN {result} ";
        }

        /// <summary>
        /// Adds an ELSE clause to the current CASE statement within the SQL statement.
        /// </summary>
        /// <param name="result">The result for the ELSE clause.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to add an ELSE clause without starting a CASE statement.</exception>
        public void AddCaseElse(string result)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"ELSE {result} ";
        }

        /// <summary>
        /// Ends the current CASE statement within the SQL statement and provides an alias for the result.
        /// </summary>
        /// <param name="alias">The alias for the CASE statement result.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to end a CASE statement without starting one.</exception>
        public void EndCaseStatement(string alias)
        {
            if (!_useCaseStatement)
                throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

            _caseStatement += $"END AS {alias} ";
            _useCaseStatement = false;
            _query.Append(_caseStatement);
            _caseStatement = string.Empty;
        }

        /// <summary>
        /// Marks the SQL statement as a UNION query.
        /// </summary>
        public void Union()
        {
            _isUnion = true;
        }

        /// <summary>
        /// Marks the SQL statement as an INTERSECT query.
        /// </summary>
        public void Intersect()
        {
            _isIntersect = true;
        }

        /// <summary>
        /// Marks the SQL statement as an EXCEPT query.
        /// </summary>
        public void Except()
        {
            _isExcept = true;
        }

        /// <summary>
        /// Builds the final SQL statement based on the constructed query and parameters.
        /// </summary>
        /// <returns>A QueryBuildResult containing the generated SQL statement and formatted parameters.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the SQL statement is not valid (missing SELECT or FROM clause).</exception>
        public QueryBuildResult Build()
        {
            if (!_query.ToString().ToUpper().Contains("SELECT") || !_query.ToString().ToUpper().Contains("FROM"))
                throw new InvalidOperationException("A valid SELECT statement with FROM clause is required.");

            var formattedParameters = new StringBuilder();
            foreach (var parameter in _parameters)
            {
                formattedParameters.Append($"@{parameter.Name}, ");
            }

            var parametersString = formattedParameters.ToString().TrimEnd(' ', ',');

            foreach (var subquery in _subqueries)
            {
                _query.Append($"({subquery.Build()}) ");
            }

            if (_usePagination)
            {
                _query.Append($"OFFSET {(_pageNumber - 1) * _pageSize} ROWS FETCH NEXT {_pageSize} ROWS ONLY ");
            }

            while (_nestedConditions.Count > 0)
            {
                var nestedCondition = _nestedConditions.Pop();
                _query.Append(nestedCondition);
                _query.Append(") ");
            }

            if (_isUnion)
                _query.Insert(0, "UNION ");
            else if (_isIntersect)
                _query.Insert(0, "INTERSECT ");
            else if (_isExcept)
                _query.Insert(0, "EXCEPT ");

            var queryString = _query.ToString();

            return new QueryBuildResult
            {
                SQL = queryString,
                Parameters = parametersString
            };
        }

        private class SQLParameter
        {
            public string? Name { get; set; }
            public object? Value { get; set; }
        }
    }

    /// <summary>
    /// Represents the result of building a SQL query.
    /// </summary>
    public class QueryBuildResult
    {
        /// <summary>
        /// Gets or sets the generated SQL statement.
        /// </summary>
        public string? SQL { get; set; }

        /// <summary>
        /// Gets or sets the formatted parameters for the SQL statement.
        /// </summary>
        public string? Parameters { get; set; }
    }

    /// <summary>
    /// Specifies the type of SQL join for use in the query.
    /// </summary>
    public enum JoinType
    {
        /// <summary>
        /// Represents an INNER JOIN in the SQL query.
        /// </summary>
        Inner,

        /// <summary>
        /// Represents a LEFT JOIN in the SQL query.
        /// </summary>
        Left,

        /// <summary>
        /// Represents a RIGHT JOIN in the SQL query.
        /// </summary>
        Right,

        /// <summary>
        /// Represents a FULL JOIN in the SQL query.
        /// </summary>
        Full
    }

    /// <summary>
    /// Specifies the sort order for ordering query results.
    /// </summary>
    public enum SortOrder
    {
        /// <summary>
        /// Represents an ascending sort order in the query.
        /// </summary>
        Ascending,

        /// <summary>
        /// Represents a descending sort order in the query.
        /// </summary>
        Descending
    }
}
