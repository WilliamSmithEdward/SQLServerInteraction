using System.Text;

namespace SQLServerInteraction
{
    /// <summary>
    /// Represents a query builder for constructing SQL queries.
    /// </summary>
    /// <remarks>
    /// Every string the builder takes is SQL, placed in the query as written: build them in your own code,
    /// and pass values with <see cref="AddParameter"/>. The SELECT list comes first; the other clauses follow
    /// in the order their methods are called.
    /// </remarks>
    public class QueryBuilder
    {
        /// <summary>Marks where <see cref="EndNestedCondition"/> closes a parenthesis.</summary>
        private static readonly object CloseParenthesis = new();

        private readonly List<string> _selectItems;
        private readonly List<object> _clauses;
        private readonly List<SQLParameter> _parameters;
        private bool _selectCalled;
        private int _pendingOpenParentheses;
        private int _openParentheses;
        private bool _isUnion;
        private bool _isIntersect;
        private bool _isExcept;
        private bool _usePagination;
        private int _pageNumber;
        private int _pageSize;
        private StringBuilder? _caseStatement;

        /// <summary>
        /// Initializes a new instance of the <see cref="QueryBuilder"/> class.
        /// </summary>
        public QueryBuilder()
        {
            _selectItems = [];
            _clauses = [];
            _parameters = [];
            _pageNumber = 1;
            _pageSize = 10;
        }

        /// <summary>
        /// Constructs a SELECT SQL statement with the specified columns.
        /// </summary>
        /// <param name="columns">A comma-separated list of column names. Leave empty for selecting all columns, unless an aggregate or CASE adds to the list.</param>
        public void Select(string columns = "")
        {
            _selectCalled = true;
            if (!string.IsNullOrEmpty(columns)) _selectItems.Add(columns);
        }

        /// <summary>
        /// Specifies the table from which to select data in the SQL statement.
        /// </summary>
        /// <param name="tableName">The table, as SQL: a name, or a name with an alias.</param>
        public void From(string tableName)
        {
            _clauses.Add($"FROM {tableName} ");
        }

        /// <summary>
        /// Adds a WHERE clause to the SQL statement based on the specified condition.
        /// </summary>
        /// <param name="condition">The condition for the WHERE clause.</param>
        public void Where(string condition)
        {
            AddCondition("WHERE", condition);
        }

        /// <summary>
        /// Adds an AND condition to the WHERE clause in the SQL statement.
        /// </summary>
        /// <param name="condition">The additional condition to be combined with the existing WHERE clause.</param>
        public void And(string condition)
        {
            AddCondition("AND", condition);
        }

        /// <summary>
        /// Adds an OR condition to the WHERE clause in the SQL statement.
        /// </summary>
        /// <param name="condition">The additional condition to be combined with the existing WHERE clause using OR.</param>
        public void Or(string condition)
        {
            AddCondition("OR", condition);
        }

        private void AddCondition(string keyword, string condition)
        {
            _clauses.Add($"{keyword} {new string('(', _pendingOpenParentheses)}{condition} ");
            _openParentheses += _pendingOpenParentheses;
            _pendingOpenParentheses = 0;
        }

        /// <summary>
        /// Records a parameter, which <see cref="Build"/> returns with the SQL so the query can run with it.
        /// </summary>
        /// <param name="parameterName">The name of the parameter, with or without the @.</param>
        /// <param name="value">The value of the parameter. Null is sent as NULL.</param>
        public void AddParameter(string parameterName, object value)
        {
            var parameter = new SQLParameter { Name = CommandParameters.Name(parameterName), Value = value };
            _parameters.Add(parameter);
        }

        /// <summary>
        /// Adds a join clause to the SQL statement based on the specified table, condition, and join type.
        /// </summary>
        /// <param name="tableName">The table to join, as SQL: a name, a name with an alias, or a derived table.</param>
        /// <param name="onCondition">The condition for the join.</param>
        /// <param name="joinType">The type of join (default is INNER JOIN).</param>
        public void Join(string tableName, string onCondition, JoinType joinType = JoinType.Inner)
        {
            _clauses.Add($"{joinType.ToString().ToUpper()} JOIN {tableName} ON {onCondition} ");
        }

        /// <summary>
        /// Adds an ORDER BY clause to the SQL statement based on the specified columns and sort order.
        /// </summary>
        /// <param name="columns">A comma-separated list of columns to order by.</param>
        /// <param name="sortOrder">The sort order, written as <c>ASC</c> or <c>DESC</c> after the last column (default is ascending).</param>
        public void OrderBy(string columns, QuerySortOrder sortOrder = QuerySortOrder.Ascending)
        {
            _clauses.Add($"ORDER BY {columns} {(sortOrder == QuerySortOrder.Descending ? "DESC" : "ASC")} ");
        }

        /// <summary>
        /// Adds a GROUP BY clause to the SQL statement based on the specified columns.
        /// </summary>
        /// <param name="columns">A comma-separated list of columns to group by.</param>
        public void GroupBy(string columns)
        {
            _clauses.Add($"GROUP BY {columns} ");
        }

        /// <summary>
        /// Adds a COUNT aggregate function to the SELECT list for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to count, or <c>*</c>.</param>
        /// <param name="alias">The alias for the COUNT result.</param>
        public void Count(string columnName, string alias)
        {
            _selectItems.Add($"COUNT({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds a SUM aggregate function to the SELECT list for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to sum.</param>
        /// <param name="alias">The alias for the SUM result.</param>
        public void Sum(string columnName, string alias)
        {
            _selectItems.Add($"SUM({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds an AVG aggregate function to the SELECT list for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the average.</param>
        /// <param name="alias">The alias for the AVG result.</param>
        public void Avg(string columnName, string alias)
        {
            _selectItems.Add($"AVG({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds a MIN aggregate function to the SELECT list for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the minimum value.</param>
        /// <param name="alias">The alias for the MIN result.</param>
        public void Min(string columnName, string alias)
        {
            _selectItems.Add($"MIN({columnName}) AS {alias}");
        }

        /// <summary>
        /// Adds a MAX aggregate function to the SELECT list for the specified column with an alias.
        /// </summary>
        /// <param name="columnName">The name of the column to calculate the maximum value.</param>
        /// <param name="alias">The alias for the MAX result.</param>
        public void Max(string columnName, string alias)
        {
            _selectItems.Add($"MAX({columnName}) AS {alias}");
        }

        /// <summary>
        /// Creates a subquery at this point of the query, using a new instance of the QueryBuilder.
        /// </summary>
        /// <returns>A new QueryBuilder for the subquery. <see cref="Build"/> places its SQL here, in parentheses, and returns its parameters with the outer query's.</returns>
        /// <remarks>
        /// Call it where the subquery belongs, for example after <c>Where("CustomerId IN")</c>.
        /// </remarks>
        public QueryBuilder CreateSubquery()
        {
            var subquery = new QueryBuilder();
            _clauses.Add(subquery);
            return subquery;
        }

        /// <summary>
        /// Starts a nested condition within the SQL WHERE clause: the next WHERE, AND or OR condition opens a parenthesis after its keyword.
        /// </summary>
        public void StartNestedCondition()
        {
            _pendingOpenParentheses++;
        }

        /// <summary>
        /// Ends a nested condition within the SQL WHERE clause, closing the parenthesis after the last condition.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to end a nested condition without starting one, or one with no condition in it.</exception>
        public void EndNestedCondition()
        {
            if (_openParentheses == 0)
            {
                if (_pendingOpenParentheses > 0)
                    throw new InvalidOperationException("The nested condition has no condition in it.");
                throw new InvalidOperationException("No nested condition to end.");
            }

            _openParentheses--;
            _clauses.Add(CloseParenthesis);
        }

        /// <summary>
        /// Enables pagination for the SQL statement, specifying the page number and page size.
        /// </summary>
        /// <param name="page">The page number, starting at 1.</param>
        /// <param name="pageSize">The number of rows per page.</param>
        /// <remarks>
        /// <see cref="Build"/> appends <c>OFFSET ... ROWS FETCH NEXT ... ROWS ONLY</c>, which needs an ORDER BY clause.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="page"/> or <paramref name="pageSize"/> is less than 1.</exception>
        public void Paginate(int page, int pageSize)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
            ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
            _usePagination = true;
            _pageNumber = page;
            _pageSize = pageSize;
        }

        /// <summary>
        /// Starts a CASE expression for the SELECT list. <see cref="EndCaseStatement"/> adds it to the list.
        /// </summary>
        /// <param name="columnName">
        /// An empty string for a searched CASE, <c>CASE WHEN condition THEN result ... END</c>, whose WHEN clauses are conditions.
        /// Otherwise the expression placed after <c>CASE</c>, which makes it a simple CASE that compares this expression with each WHEN value.
        /// </param>
        public void StartCaseStatement(string columnName)
        {
            _caseStatement = new StringBuilder("CASE ");
            if (!string.IsNullOrEmpty(columnName)) _caseStatement.Append(columnName).Append(' ');
        }

        /// <summary>
        /// Adds a WHEN-THEN clause to the current CASE statement within the SQL statement.
        /// </summary>
        /// <param name="condition">The condition for the WHEN clause, or the value to compare with in a simple CASE.</param>
        /// <param name="result">The result for the THEN clause.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to add a WHEN-THEN clause without starting a CASE statement.</exception>
        public void AddCaseWhen(string condition, string result)
        {
            CurrentCase().Append($"WHEN {condition} THEN {result} ");
        }

        /// <summary>
        /// Adds an ELSE clause to the current CASE statement within the SQL statement.
        /// </summary>
        /// <param name="result">The result for the ELSE clause.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to add an ELSE clause without starting a CASE statement.</exception>
        public void AddCaseElse(string result)
        {
            CurrentCase().Append($"ELSE {result} ");
        }

        /// <summary>
        /// Ends the current CASE statement and adds it to the SELECT list, with an alias for the result.
        /// </summary>
        /// <param name="alias">The alias for the CASE statement result.</param>
        /// <exception cref="InvalidOperationException">Thrown when attempting to end a CASE statement without starting one.</exception>
        public void EndCaseStatement(string alias)
        {
            _selectItems.Add($"{CurrentCase()}END AS {alias}");
            _caseStatement = null;
        }

        private StringBuilder CurrentCase() =>
            _caseStatement ?? throw new InvalidOperationException("CASE statement not started. Call StartCaseStatement first.");

        /// <summary>
        /// Marks the SQL statement as a UNION query: <see cref="Build"/> puts <c>UNION</c> before it.
        /// </summary>
        [Obsolete("Union only puts UNION before this query's SQL; it does not combine two builders. Build each query and join their SQL with \" UNION \". See the README.")]
        public void Union()
        {
            _isUnion = true;
        }

        /// <summary>
        /// Marks the SQL statement as an INTERSECT query: <see cref="Build"/> puts <c>INTERSECT</c> before it.
        /// </summary>
        [Obsolete("Intersect only puts INTERSECT before this query's SQL; it does not combine two builders. Build each query and join their SQL with \" INTERSECT \". See the README.")]
        public void Intersect()
        {
            _isIntersect = true;
        }

        /// <summary>
        /// Marks the SQL statement as an EXCEPT query: <see cref="Build"/> puts <c>EXCEPT</c> before it.
        /// </summary>
        [Obsolete("Except only puts EXCEPT before this query's SQL; it does not combine two builders. Build each query and join their SQL with \" EXCEPT \". See the README.")]
        public void Except()
        {
            _isExcept = true;
        }

        /// <summary>
        /// Builds the final SQL statement based on the constructed query and parameters.
        /// The builder is not changed, so a second call returns the same result.
        /// </summary>
        /// <returns>A QueryBuildResult containing the generated SQL statement, the parameter names and the parameter values.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the SQL has no SELECT at its start or no FROM outside parentheses, quotes and comments, or when it is paginated without an ORDER BY.</exception>
        public QueryBuildResult Build()
        {
            var parameters = new List<SQLParameter>();
            string sql = BuildSql(parameters);

            if (!SqlShape.IsSelectFrom(sql))
                throw new InvalidOperationException("A valid SELECT statement with FROM clause is required.");

            return new QueryBuildResult
            {
                SQL = sql,
                Parameters = string.Join(", ", parameters.Select(p => p.Name)),
                ParameterValues = parameters.GroupBy(p => p.Name).ToDictionary(g => g.Key, g => CommandParameters.Value(g.Last().Value)),
            };
        }

        /// <summary>
        /// The SQL of this builder and its subqueries, collecting their parameters, without changing any builder.
        /// </summary>
        private string BuildSql(List<SQLParameter> parameters)
        {
            var query = new StringBuilder();

            if (_selectCalled || _selectItems.Count > 0)
            {
                query.Append("SELECT ").Append(SelectList()).Append(' ');
            }

            bool hasOrderBy = false;
            foreach (var clause in _clauses)
            {
                if (clause == CloseParenthesis)
                {
                    TrimEnd(query);
                    query.Append(") ");
                }
                else if (clause is QueryBuilder subquery)
                {
                    query.Append('(').Append(subquery.BuildSql(parameters).TrimEnd()).Append(") ");
                }
                else
                {
                    string text = (string)clause;
                    hasOrderBy |= text.StartsWith("ORDER BY ", StringComparison.Ordinal);
                    query.Append(text);
                }
            }

            if (_openParentheses > 0)
            {
                TrimEnd(query);
                query.Append(new string(')', _openParentheses)).Append(' ');
            }

            if (_usePagination)
            {
                if (!hasOrderBy)
                    throw new InvalidOperationException("Paginate needs an ORDER BY clause: call OrderBy first.");
                query.Append($"OFFSET {(_pageNumber - 1) * _pageSize} ROWS FETCH NEXT {_pageSize} ROWS ONLY ");
            }

            if (_isUnion)
                query.Insert(0, "UNION ");
            else if (_isIntersect)
                query.Insert(0, "INTERSECT ");
            else if (_isExcept)
                query.Insert(0, "EXCEPT ");

            parameters.AddRange(_parameters);
            return query.ToString();
        }

        /// <summary>
        /// The SELECT list: the columns given to <see cref="Select"/>, then aggregates and CASE expressions in call order,
        /// separated by commas; <c>*</c> when there are none.
        /// </summary>
        private string SelectList()
        {
            if (_selectItems.Count == 0) return "*";

            var list = new StringBuilder(_selectItems[0]);
            foreach (string item in _selectItems.Skip(1))
            {
                if (list.ToString().TrimEnd().EndsWith(','))
                {
                    TrimEnd(list);
                    list.Append(' ');
                }
                else
                {
                    list.Append(", ");
                }
                list.Append(item);
            }
            return list.ToString();
        }

        private static void TrimEnd(StringBuilder text)
        {
            while (text.Length > 0 && char.IsWhiteSpace(text[^1])) text.Length--;
        }

        private class SQLParameter
        {
            public string Name { get; set; } = "";
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
        /// Gets or sets the parameter names recorded with <see cref="QueryBuilder.AddParameter"/>, as a comma-separated list such as <c>@A, @B</c>.
        /// </summary>
        public string? Parameters { get; set; }

        /// <summary>
        /// Gets or sets the parameters recorded with <see cref="QueryBuilder.AddParameter"/>, by name with the @, null values as DBNull.Value.
        /// Pass it with <see cref="SQL"/> to a method that takes a parameter dictionary, such as <see cref="SQLServerInstance.ExecuteQueryToObjectListAsync{T}(string, Dictionary{string, object}?, CancellationToken)"/>.
        /// </summary>
        public Dictionary<string, object> ParameterValues { get; set; } = [];
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
    /// Specifies the sort order for <see cref="QueryBuilder.OrderBy"/>. Named so that it does not clash with
    /// Microsoft.Data.SqlClient.SortOrder; before 2.0.0 it was SQLServerInteraction.SortOrder.
    /// </summary>
    public enum QuerySortOrder
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
